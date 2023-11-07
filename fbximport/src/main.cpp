
#define _CRTDBG_MAP_ALLOC
#include<iostream>
#include <crtdbg.h>



#include "common.h"
#include "node.h"
#include "mesh.h"
#include <fbxsdk.h>
#include <iostream>
#include "manager.h"
#include "importer.h"

using namespace fbxsdk;
using namespace std;

void iterate(FbxNode* parent, int ident = 0)
{
    char* name = DBG_NEW char[512];
    node_get_name(parent, name, 512);
    for (int i = 0; i < ident; i++) cout << "\t";

    cout << ident << ": " << name << endl;
    delete[] name;

    for (int i = 0; i < node_get_child_count(parent); i++)
    {
        iterate((FbxNode*)node_get_child(parent, i), ident + 1);
    }
    auto geometry = (FbxMesh*)node_get_mesh(parent);
    if (geometry != nullptr)
    {
        auto data = mesh_get_geometry_data(geometry);
        if (data->vertex_count == 0)
        {
            cerr << "Could not retreive geometry" << endl;
        }
        else {
            cout << "Vertex count: " << data->vertex_count << endl;
            cout << "Triangle count: " << data->index_count << endl;
        }

        delete data;
    }

}

void load() {
    auto sdk = manager_create();

    const char* lInputFbxFilename = "D:/models/FBX/AQ110South-3DView.fbx";
    auto root = (FbxNode*)load_file(lInputFbxFilename, sdk);

    iterate(root);

    root->Destroy();
    manager_destroy(sdk);
}

int main()
{
    //VS_MEM_CHECK

    //_CrtMemState s1, s2, s3;
    //_CrtMemCheckpoint(&s1);
    load();
    //_CrtMemCheckpoint(&s2);
    //_CrtMemDifference(&s3, &s1, &s2);
    //_CrtMemDumpStatistics(&s3);
    _CrtSetReportMode(_CRT_WARN, _CRTDBG_MODE_DEBUG);
    _CrtDumpMemoryLeaks();
    
    return 0;
}


