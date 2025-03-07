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

string arrayToString(int* arr, int count) {
    // Variable to store the answer
    string answer = "[";
    // Iterate over all the elements in arr[]
    for (int i = 0; i < count; i++) {
        answer += to_string(arr[i]);
        if (i < count - 1)
            answer += ", ";
    }
    return answer + "]";
}

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

    auto fbxVertexPositionsCount = mesh->GetPolygonVertexCount();
    auto fbxVertexPositionIndexArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    /*std::cout << "Polygon vertices (" << fbxVertexPositionsCount << "), " << arrayToString(fbxVertexPositionIndexArray, fbxVertexPositionsCount) << std::endl;
    std::cout << "Control points (" << controlPointCount << "), " << std::endl;

    std::vector<FbxVector4> uniqueVertices;
    std::vector<int> indices;

    if (mesh) {
        std::cout << "Is mesh" << std::endl;

        int polygonCount = mesh->GetPolygonCount();
        std::cout << "Count: " << polygonCount << std::endl;
        for (int p = 0; p < polygonCount; p++) {
            if (mesh->GetPolygonSize(p) == 3) { // Check if it's a triangle
                for (int j = 0; j < 3; j++) {
                    int vertexIndex = mesh->GetPolygonVertex(p, j);
                    FbxVector4 vertex = mesh->GetControlPointAt(vertexIndex);

                    // Check if the vertex is already in the uniqueVertices array
                    auto it = std::find(uniqueVertices.begin(), uniqueVertices.end(), vertex);
                    if (it != uniqueVertices.end()) {
                        // Vertex already exists, use its index
                        indices.push_back(std::distance(uniqueVertices.begin(), it));
                    } else {
                        // New vertex, add it to the uniqueVertices array
                        uniqueVertices.push_back(vertex);
                        indices.push_back(uniqueVertices.size() - 1); // Use the new index
                    }
                }
            }
        }

        std::cout << "indices (" << indices.size() << "), " << arrayToString(indices.data(), indices.size()) << std::endl;
    }*/

    // GetPolygonVertexCount() can be smaller than the value returned by GetControlPointsCount() (meaning that not all
    // of the control points stored in the object are used to define the mesh). However, typically it will be much
    // bigger since any given control point can be used to define a vertex on multiple polygons.

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
