#include "mesh.h"
#include <fbxsdk.h>
#include <vector>
#include <map>

using namespace fbxsdk;
using namespace std;

ExportableMesh mesh_get_geometry_data(CFbxMesh geometry)
{
    ExportableMesh mesh_out_tmp;
    mesh_out_tmp.triangle_count = 0;
    mesh_out_tmp.vertex_count = 0;
    mesh_out_tmp.vertex_data = nullptr;
    mesh_out_tmp.triangle_data = nullptr;
    mesh_out_tmp.normal_data = nullptr;

    auto mesh = (FbxMesh*)geometry;
    // TODO UVs

    auto polyCount = mesh->GetPolygonVertexCount();
    auto triangleArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    vector<FbxVector4> lFbxVertices;
    vector<int> lFbxTriangles;

    map<int, vector<int>> lFbxToOutIndexLookup;

    vector<FbxVector4> lOutVertices;
    vector<FbxVector4> lOutNormals;
    vector<int> lOutTriangles;


    for (auto i = 0; i < controlPointCount; i++)
    {
        auto cp = mesh->GetControlPointAt(i);
        lFbxVertices.push_back(cp);
    }

    FbxArray<FbxVector4> lFbxNormals;
    mesh->GetPolygonVertexNormals(lFbxNormals); // TODO if fail

    for (auto i = 0; i < polyCount; i++)
    {
        const auto fbxVertexIndex = triangleArray[i];
        auto lVertex = lFbxVertices[fbxVertexIndex];
        auto lNormal = lFbxNormals[i];

        auto lookUpOutIndex = lFbxToOutIndexLookup.find(fbxVertexIndex);
        if (lookUpOutIndex != lFbxToOutIndexLookup.end())
        {
            bool found = false;
            auto outPossibleIndicies = (*lookUpOutIndex).second;
            for (const auto outIndex : outPossibleIndicies)
            {
                auto toCompareNormal = lOutNormals[outIndex];
                if (lNormal == toCompareNormal)
                {
                    lOutTriangles.push_back(outIndex);
                    found = true;
                    break;
                }
            }

            if (!found) {
                auto outIndex = (int)lOutVertices.size();
                lOutVertices.push_back(lVertex);
                lOutNormals.push_back(lNormal);
                lOutTriangles.push_back(outIndex);
                lFbxToOutIndexLookup[fbxVertexIndex].push_back(outIndex);
            }
            // compare normal
                // if equals, add an index to triangle array
                // if not equals, add new vertex with new normal and andd to triangle array
        }
        else
        {
            // add new normal and vertex and triangle
            auto newIndex = lOutVertices.size();
            vector<int> newVector;
            newVector.push_back(newIndex);
            lFbxToOutIndexLookup[fbxVertexIndex] = newVector;
            lOutVertices.push_back(lVertex);
            lOutNormals.push_back(lNormal);
            lOutTriangles.push_back(newIndex);
        }
    }

    if (lOutVertices.size() != lOutNormals.size())
    {
        // TODO: something wrong
        return mesh_out_tmp;
    }

    mesh_out_tmp.triangle_count = lOutTriangles.size();
    mesh_out_tmp.vertex_count = lOutVertices.size();
    mesh_out_tmp.vertex_data = new float[lOutVertices.size() * 3];
    mesh_out_tmp.triangle_data = new int[lOutTriangles.size()];
    mesh_out_tmp.normal_data = new float[lOutVertices.size() * 3];

    for (int i = 0; i < lOutVertices.size(); i++)
    {
        mesh_out_tmp.vertex_data[i * 3] = (float)lOutVertices[i][0];
        mesh_out_tmp.vertex_data[i * 3 + 1] = (float)lOutVertices[i][1];
        mesh_out_tmp.vertex_data[i * 3 + 2] = (float)lOutVertices[i][2];
        mesh_out_tmp.normal_data[i * 3] = (float)lOutNormals[i][0];
        mesh_out_tmp.normal_data[i * 3 + 1] = (float)lOutNormals[i][1];
        mesh_out_tmp.normal_data[i * 3 + 2] = (float)lOutNormals[i][2];
    }
    for (int i = 0; i < lOutTriangles.size(); i++)
    {
        mesh_out_tmp.triangle_data[i] = lOutTriangles[i];
    }
    return mesh_out_tmp;

}

void mesh_clean(ExportableMesh mesh_data)
{
    delete[] mesh_data.vertex_data;
    delete[] mesh_data.triangle_data;
    delete[] mesh_data.normal_data;
}