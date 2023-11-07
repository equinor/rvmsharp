#include "mesh.h"
#include <fbxsdk.h>
#include <vector>
#include <map>
#include <tuple>
#include <set>

using namespace fbxsdk;
using namespace std;
typedef std::tuple<float, float, float, float, float, float> vertex_tuple;

// this function allocates memory
// there should be a corresponding mesh_clean call for each call of this function
// it should never return nullptr, if the mesh is invalid for some reason, set the valid field to false
ExportableMesh* mesh_get_geometry_data(CFbxMesh geometry)
{
    ExportableMesh* mesh_out_tmp = DBG_NEW ExportableMesh();
    mesh_out_tmp->vertex_count = 0;
    mesh_out_tmp->vertex_position_data = nullptr;
    mesh_out_tmp->vertex_normal_data = nullptr;
    mesh_out_tmp->index_count = 0;
    mesh_out_tmp->index_data = nullptr;

    auto mesh = (FbxMesh*)geometry;

    auto fbxVertexPositionsCount = mesh->GetPolygonVertexCount();
    auto fbxVertexPositionIndexArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    // TODO find out difference between fbxVertexPositionsCount and controlPointCount
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
        mesh_out_tmp->valid = false;
        mesh_clean_memory(mesh_out_tmp);
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

    mesh_out_tmp->valid = true;
    mesh_out_tmp->index_count = lMeshOutVertexIndices.size();
    mesh_out_tmp->vertex_count = lMeshOutVertexPositions.size() / 3;

    mesh_out_tmp->index_data = DBG_NEW int[mesh_out_tmp->index_count];
    mesh_out_tmp->vertex_position_data = DBG_NEW float[lMeshOutVertexPositions.size()];
    mesh_out_tmp->vertex_normal_data = DBG_NEW float[lMeshOutVertexPositions.size()];

    std::copy(lMeshOutVertexIndices.begin(), lMeshOutVertexIndices.end(), mesh_out_tmp->index_data);
    std::copy(lMeshOutVertexPositions.begin(), lMeshOutVertexPositions.end(), mesh_out_tmp->vertex_position_data);
    std::copy(lMeshOutVertexNormals.begin(), lMeshOutVertexNormals.end(), mesh_out_tmp->vertex_normal_data);

    return mesh_out_tmp;
}

void mesh_clean_memory(ExportableMesh* mesh_data)
{
    if (mesh_data)
    {
        delete mesh_data;
        mesh_data = nullptr;
    }
}
