#include "mesh.h"
#include <fbxsdk.h>
#include <vector>
#include <map>
#include <tuple>
#include <set>
#include <iostream>

using namespace fbxsdk;
using namespace std;
typedef std::tuple<float, float, float> vertex_tuple;

// this function allocates memory
// there should be a corresponding mesh_clean call for each call of this function
// it should never return nullptr, if the mesh is invalid for some reason, set the valid field to false
ExportableMesh* mesh_get_geometry_data(CFbxMesh* geometry)
{
    ExportableMesh* mesh_out_tmp = new ExportableMesh();
    mesh_out_tmp->vertex_count = 0;
    mesh_out_tmp->vertex_position_data = nullptr;
    mesh_out_tmp->index_count = 0;
    mesh_out_tmp->index_data = nullptr;

    auto mesh = (FbxMesh*)geometry;

    // GetPolygonVertexCount() can be smaller than the value returned by GetControlPointsCount() (meaning that not all
    // of the control points stored in the object are used to define the mesh). However, typically it will be much
    // bigger since any given control point can be used to define a vertex on multiple polygons.

    auto fbxVertexPositionsCount = mesh->GetPolygonVertexCount();
    auto fbxVertexPositionIndexArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    vector<float> lMeshOutVertexPositions;
    vector<int> lMeshOutVertexIndices;
    std::map<vertex_tuple, int> vertex_data;
    
    for (auto i = 0; i < fbxVertexPositionsCount; i++)
    {
        // Retrieve vertex index and position. If we have choosen to ignore the vertex surface normal,
        // then we set it to (0, 0, 0). Subsequently, the position and normal are assembled into a tuple. Hence,
        // even if two equally positioned vertices have different surface normals, the normals will become zero, making
        // those two tuples equal during the process of removing duplicate tuples. The result is a possible reduction
        // in vertices that reduce the amount of vertices stored by Reveal, which do not need the surface normals. This 
        // has the potential of speeding up the performance in Reveal. 
        const auto fbxVertexPositionIndex = fbxVertexPositionIndexArray[i];
        auto lVertex = mesh->GetControlPointAt(fbxVertexPositionIndex);

        float vx = (float)lVertex[0]; float vy = (float)lVertex[1]; float vz = (float)lVertex[2];
        const vertex_tuple vertex = std::make_tuple(vx, vy, vz);
        
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
            lMeshOutVertexIndices.push_back(newIndexCandidate);
        }
    }

    mesh_out_tmp->valid = true;
    mesh_out_tmp->index_count = lMeshOutVertexIndices.size();
    mesh_out_tmp->vertex_count = lMeshOutVertexPositions.size() / 3;

    mesh_out_tmp->index_data = new int[mesh_out_tmp->index_count];
    mesh_out_tmp->vertex_position_data = new float[lMeshOutVertexPositions.size()];

    std::copy(lMeshOutVertexIndices.begin(), lMeshOutVertexIndices.end(), mesh_out_tmp->index_data);
    std::copy(lMeshOutVertexPositions.begin(), lMeshOutVertexPositions.end(), mesh_out_tmp->vertex_position_data);

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
