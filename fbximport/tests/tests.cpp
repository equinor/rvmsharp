#include <catch2/catch_test_macros.hpp>

#include "common.h"
#include "node.h"
#include "mesh.h"
#include "importer.h"
#include "manager.h"
#include "tests.h"

#include <iostream>
#include <fbxsdk.h>

using namespace std;
using namespace fbxsdk;


unsigned int Factorial( unsigned int number ) {
    return number <= 1 ? number : Factorial(number-1)*number;
}

void iterate_children(FbxNode* parent, int ident = 0)
{
    char* name = new char[512];
    node_get_name(parent, name, 512);
    for (int i = 0; i < ident; i++) cout << "\t";

    cout << ident << ": " << name << endl;
    delete[] name;

    for (int i = 0; i < node_get_child_count(parent); i++)
    {
        iterate_children((FbxNode*)node_get_child(parent, i), ident + 1);
    }
    auto geometry = (FbxMesh*)node_get_mesh(parent);
    if (geometry != nullptr)
    {
        auto data = mesh_get_geometry_data(geometry);
        if (data.vertex_count == 0)
        {
            cerr << "Could not retreive geometry" << endl;
        }
        else {
            cout << "Vertex count: " << data.vertex_count << endl;
            cout << "Triangle count: " << data.triangle_count << endl;
        }

    }
}

void load_and_iterate() {
    auto sdk = manager_create();
    const char* lInputFbxFilename = "D:/models/FBX/AQ110South-3DView.fbx";
    auto root = (FbxNode*)load_file(lInputFbxFilename, sdk);

    iterate_children(root);

    manager_destroy(sdk);
}

TEST_CASE( "Factorials are computed", "[factorial]" ) {
    REQUIRE( Factorial(1) == 1 );
    REQUIRE( Factorial(2) == 2 );
    REQUIRE( Factorial(3) == 6 );
    REQUIRE( Factorial(10) == 3628800 );
}

TEST_CASE( "Load and iterate", "[FBX sdk]") {
    load_and_iterate();
}