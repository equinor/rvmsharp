#include "mesh.h"
#include <fbxsdk.h>
#include <vector>
#include <map>
#include <tuple>
#include <set>

using namespace fbxsdk;
using namespace std;
typedef std::tuple<float, float, float, float, float, float> vertex_tuple;

ExportableMesh mesh_get_geometry_data(CFbxMesh geometry)
{
    ExportableMesh mesh_out_tmp;
    mesh_out_tmp.triangle_count = 0;
    mesh_out_tmp.vertex_count = 0;
    mesh_out_tmp.vertex_data = nullptr;
    mesh_out_tmp.triangle_data = nullptr;
    mesh_out_tmp.normal_data = nullptr;

    auto mesh = (FbxMesh*)geometry;

    auto fbxVertexPositionsCount = mesh->GetPolygonVertexCount();
    auto fbxVertexPositionIndexArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    vector<FbxVector4> lFbxPositions;
    for (auto i = 0; i < controlPointCount; i++)
    {
        auto cp = mesh->GetControlPointAt(i);
        lFbxPositions.push_back(cp);
    }

    FbxArray<FbxVector4> lFbxNormals;
    bool lSuccess = mesh->GetPolygonVertexNormals(lFbxNormals);
    if (!lSuccess)
    {
        mesh_out_tmp.valid = false;
        mesh_clean(mesh_out_tmp);
        return mesh_out_tmp;
    }

    vector<float> lMeshOutVertexPositions;
    vector<float> lMeshOutVertexNormals;
    vector<int> lMeshOutVertexIndices;
    std::map<vertex_tuple, int> vertex_data;
    
    for (auto i = 0; i < fbxVertexPositionsCount; i++)
    {
        const auto fbxVertexPositionIndex = fbxVertexPositionIndexArray[i];
        auto lVertex = lFbxPositions[fbxVertexPositionIndex];
        auto lNormal = lFbxNormals[i];

        float vx = (float)lVertex[0]; float vy = (float)lVertex[1]; float vz = (float)lVertex[2];
        float nx = (float)lNormal[0]; float ny = (float)lNormal[1]; float nz = (float)lNormal[2];
        const vertex_tuple vertex = std::make_tuple(vx, vy, vz, nx, ny, nz);
        
        const int newIndexCandidate = vertex_data.size();
        auto result = vertex_data.insert(std::pair(vertex, newIndexCandidate));

        if (!result.second)
        {
            // insertion did not take place, since the key (position, normal) already exists
            // result.first is thus the iterator pointing to the element that prevented the insertion
            auto vertex = (*(result.first));
            auto outIndex = vertex.second;
            lMeshOutVertexIndices.push_back(outIndex);
        }
        else 
        {
            lMeshOutVertexPositions.insert(lMeshOutVertexPositions.end(), { vx, vy, vz });
            lMeshOutVertexNormals.insert(lMeshOutVertexNormals.end(), { nx, ny, nz });
            lMeshOutVertexIndices.push_back(newIndexCandidate);
        }
    }

    mesh_out_tmp.triangle_count = lMeshOutVertexIndices.size();
    mesh_out_tmp.vertex_count = lMeshOutVertexPositions.size() / 3;

    mesh_out_tmp.triangle_data = new int[mesh_out_tmp.triangle_count];
    mesh_out_tmp.vertex_data = new float[lMeshOutVertexPositions.size()];
    mesh_out_tmp.normal_data = new float[lMeshOutVertexPositions.size()];

    std::copy(lMeshOutVertexIndices.begin(), lMeshOutVertexIndices.end(), mesh_out_tmp.triangle_data);
    std::copy(lMeshOutVertexPositions.begin(), lMeshOutVertexPositions.end(), mesh_out_tmp.vertex_data);
    std::copy(lMeshOutVertexNormals.begin(), lMeshOutVertexNormals.end(), mesh_out_tmp.normal_data);

    return mesh_out_tmp;
}

void mesh_clean(ExportableMesh mesh_data)
{
    if (mesh_data.vertex_data)
    {
        delete mesh_data.vertex_data;
        mesh_data.vertex_data = nullptr;
    }

    if (mesh_data.triangle_data)
    {
        delete mesh_data.triangle_data;
        mesh_data.triangle_data = nullptr;
    }
    
    if (mesh_data.normal_data)
    {
        delete mesh_data.normal_data;
        mesh_data.normal_data = nullptr;
    }
}