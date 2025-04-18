#ifdef _MSC_VER
#define _CRTDBG_MAP_ALLOC  
#include <crtdbg.h>
#define VS_MEM_CHECK _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#else
#define VS_MEM_CHECK
#endif

#include "fbx_info.h"
#include <iostream>
#include <node.h>
#include <mesh.h>
#include <material.h>
#include <manager.h>
#include <importer.h>

using namespace std;

FbxInfo::FbxInfo(const std::string& fileName)
{
    load(fileName);
}

size_t FbxInfo::get_node_count() const
{
    return m_node_info.size();
}

const FbxInfo::InfoItem& FbxInfo::get_node_info(const size_t& index) const
{
    return m_node_info[index];
}

std::string FbxInfo::print_info() const
{
    std::string output;

    output+= string("FBX info\r\n");
    output+= string("-----------------------------------------------------\r\n");

    for (const InfoItem& item : m_node_info)
    {
        if (item.m_vertex_count == 0)
        {
            output+= string("Could not retrieve geometry") + "\r\n";
        }
        else
        {
            output+= string("Vertex count: ") + to_string(item.m_vertex_count) + "\r\n";
            output+= string("Triangle count: ") + to_string(item.m_triangle_count) + "\r\n";
            output+= string("Color: (") + to_string(item.m_color.r) + ", " + to_string(item.m_color.g) + ", " + to_string(item.m_color.b) + ", " + to_string(item.m_color.a) + ")\r\n";
        }
    }

    output+= "\r\n";

    return output;
}

std::string FbxInfo::print_comparison(const FbxInfo& a, const FbxInfo& b)
{
    string output;

    output+= string("Comparison:") + "\r\n";
    output+= string("-----------------------------------------------------") + "\r\n";
    output+= string("tri.cnt1; tri.cnt2; vert.cnt1; vert.cnt2") + "\r\n";
    const size_t count = (a.get_node_count() < b.get_node_count()) ? a.get_node_count() : b.get_node_count();

    std::pair<double, double> sum_triangle_count = { 0.0, 0.0 };
    std::pair<double, double> sum_vertex_count = { 0.0, 0.0 };
    for (size_t i=0; i<count; i++)
    {
        const InfoItem& fbxInfoA = a.get_node_info(i);
        const InfoItem& fbxInfoB = b.get_node_info(i);

        sum_triangle_count.first+= fbxInfoA.m_triangle_count;
        sum_triangle_count.second+= fbxInfoB.m_triangle_count;
        sum_vertex_count.first+= fbxInfoA.m_vertex_count;
        sum_vertex_count.second+= fbxInfoB.m_vertex_count;

        output+= to_string(fbxInfoA.m_triangle_count) + "; ";
        output+= to_string(fbxInfoB.m_triangle_count) + "; ";
        output+= to_string(fbxInfoA.m_vertex_count) + "; ";
        output+= to_string(fbxInfoB.m_vertex_count) + "\r\n";
    }
    output+= "\r\n\r\n";

    output+= string("Total triangle count 1: ") + to_string(sum_triangle_count.first) + "\r\n";
    output+= string("Total triangle count 2: ") + to_string(sum_triangle_count.second) + "\r\n";
    output+= string("Total vertex count 1: ") + to_string(sum_vertex_count.first) + "\r\n";
    output+= string("Total vertex count 2: ") + to_string(sum_vertex_count.second) + "\r\n";

    output+= "\r\n";

    output+= string("Reduction in triangle count: ") + to_string((sum_triangle_count.first - sum_triangle_count.second) * 100.0 / sum_triangle_count.first) + string("%") + "\r\n";
    output+= string("Reduction in vertex count: ") + to_string((sum_vertex_count.first - sum_vertex_count.second) * 100.0 / sum_vertex_count.first) + string("%") + "\r\n";

    return output;
}

void FbxInfo::iterate(CFbxNode* parent, int ident)
{
    char* name = new char[512];
    node_get_name(parent, name, 512);
    for (int i = 0; i < ident; i++) cout << "\t";

    cout << ident << ": " << name << endl;
    delete[] name;

    for (int i = 0; i < node_get_child_count(parent); i++)
    {
        iterate(node_get_child(parent, i), ident + 1);
    }
    auto geometry = node_get_mesh(parent);
    if (geometry != nullptr)
    {
        auto material = node_get_material(parent);
        auto color = material != nullptr ? material_get_color(material) : new Color();

        auto data = mesh_get_geometry_data(geometry);
        m_node_info.emplace_back(InfoItem(data->vertex_count, data->index_count, color));

        delete data;
    }
}

void FbxInfo::load(const std::string& fileName)
{
    auto sdk = manager_create();
    auto root = load_file(fileName.c_str(), sdk);

    iterate(root);

    node_destroy(root);
    manager_destroy(sdk);
}
