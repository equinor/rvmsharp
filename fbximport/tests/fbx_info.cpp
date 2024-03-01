#ifdef _MSC_VER
#define _CRTDBG_MAP_ALLOC  
#include <crtdbg.h>
#define VS_MEM_CHECK _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#else
#define VS_MEM_CHECK
#endif

#include "fbx_info.h"
#include <iostream>
#include "common.h"
#include "node.h"
#include "mesh.h"
#include "manager.h"
#include "importer.h"

using namespace fbxsdk;
using namespace std;

FbxInfo::FbxInfo(const std::string& fileName, const bool& ignore_normals)
{
    load(fileName, ignore_normals);
}

void FbxInfo::print_info() const
{
    cout << string("FBX info. Surface normals are ") << string(ignore_normals_ ? "ignored." : "used.") << endl;
    cout << "-----------------------------------------------------";

    for (const InfoItem& item : node_info_)
    {
        if (item.vertex_count_ == 0)
        {
            cerr << "Could not retrieve geometry" << endl;
        }
        else
        {
            cout << "Vertex count: " << item.vertex_count_ << endl;
            cout << "Triangle count: " << item.triangle_count_ << endl;
        }
    }

    cout << endl;
}

void FbxInfo::print_comparison(const FbxInfo& a, const FbxInfo& b)
{
    cout << string("Comparison:") << endl;
    cout << string("-----------------------------------------------------") << endl;
    cout << string("tri.cnt1; tri.cnt2; vert.cnt1; vert.cnt2") << endl;
    const size_t count = (a.node_info_.size() < b.node_info_.size()) ? a.node_info_.size() : b.node_info_.size();

    std::pair<double, double> sum_triangle_count = { 0.0, 0.0 };
    std::pair<double, double> sum_vertex_count = { 0.0, 0.0 };
    for (size_t i=0; i<count; i++)
    {
        sum_triangle_count.first+= a.node_info_[i].triangle_count_;
        sum_triangle_count.second+= b.node_info_[i].triangle_count_;
        sum_vertex_count.first+= a.node_info_[i].vertex_count_;
        sum_vertex_count.second+= b.node_info_[i].vertex_count_;

        cout << to_string(a.node_info_[i].triangle_count_) << "; ";
        cout << to_string(b.node_info_[i].triangle_count_) << "; ";
        cout << to_string(a.node_info_[i].vertex_count_) << "; ";
        cout << to_string(b.node_info_[i].vertex_count_) << endl;
    }
    cout << endl << endl;

    cout << string("Total triangle count 1: ") << to_string(sum_triangle_count.first) << endl;
    cout << string("Total triangle count 2: ") << to_string(sum_triangle_count.second) << endl;
    cout << string("Total vertex count 1: ") << to_string(sum_vertex_count.first) << endl;
    cout << string("Total vertex count 2: ") << to_string(sum_vertex_count.second) << endl;

    cout << endl;

    cout << string("Reduction in triangle count: ") << to_string((sum_triangle_count.first - sum_triangle_count.second) * 100.0 / sum_triangle_count.first) << string("%") << endl;
    cout << string("Reduction in vertex count: ") << to_string((sum_vertex_count.first - sum_vertex_count.second) * 100.0 / sum_vertex_count.first) << string("%") << endl;
}

void FbxInfo::iterate(FbxNode* parent, const bool& ignore_normals, int ident)
{
    char* name = new char[512];
    node_get_name(parent, name, 512);
    for (int i = 0; i < ident; i++) cout << "\t";

    cout << ident << ": " << name << endl;
    delete[] name;

    for (int i = 0; i < node_get_child_count(parent); i++)
    {
        iterate((FbxNode*)node_get_child(parent, i), ignore_normals, ident + 1);
    }
    auto geometry = (FbxMesh*)node_get_mesh(parent);
    if (geometry != nullptr)
    {
        auto data = mesh_get_geometry_data(geometry, ignore_normals);
        node_info_.emplace_back(InfoItem(data->vertex_count, data->index_count));

        delete data;
    }
}

void FbxInfo::load(const std::string& fileName, const bool& ignore_normals)
{
    ignore_normals_ = ignore_normals;

    auto sdk = manager_create();
    auto root = (FbxNode*)load_file(fileName.c_str(), sdk);

    iterate(root, ignore_normals);

    root->Destroy();
    manager_destroy(sdk);
}
