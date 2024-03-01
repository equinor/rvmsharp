#include <catch2/catch_test_macros.hpp>

#ifdef _MSC_VER
#define _CRTDBG_MAP_ALLOC  
#include <crtdbg.h>
#define VS_MEM_CHECK _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#else
#define VS_MEM_CHECK
#endif

#include "tests.h"
#include "common.h"
#include "node.h"
#include "mesh.h"
#include "importer.h"
#include "manager.h"

#include <iostream>
#include <fbxsdk.h>
#include "fbx_info.h"

using namespace std;
using namespace fbxsdk;

std::shared_ptr<std::string> test_model_file_path_;

void set_test_model_file_path(const std::string& file_path)
{
    test_model_file_path_ = std::make_shared<std::string>(file_path);
}

const std::string get_test_model_file_path()
{
    REQUIRE(test_model_file_path_.get() != nullptr);
    return test_model_file_path_.get() ? *test_model_file_path_ : "";
}

/*
unsigned int Factorial( unsigned int number )
{
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
        auto data = mesh_get_geometry_data(geometry, false);
        if (data->vertex_count == 0)
        {
            cerr << "Could not retreive geometry" << endl;
        }
        else
        {
            cout << "Vertex count: " << data->vertex_count << endl;
            cout << "Triangle count: " << data->index_count << endl;
        }
        delete data;
    }
}

void load_and_iterate()
{
    auto sdk = manager_create();
    const char* lInputFbxFilename = "D:/models/FBX/AQ110South-3DView.fbx";
    auto root = (FbxNode*)load_file(lInputFbxFilename, sdk);

    iterate_children(root);
    
    root->Destroy();
    manager_destroy(sdk);
}

TEST_CASE( "Factorials are computed", "[factorial]" )
{
    REQUIRE( Factorial(1) == 1 );
    REQUIRE( Factorial(2) == 2 );
    REQUIRE( Factorial(3) == 6 );
    REQUIRE( Factorial(10) == 3628800 );
}
*/

TEST_CASE( "Load and iterate", "[FBX sdk]")
{
//    load_and_iterate();
    VS_MEM_CHECK

    std::cout << std::string("Using file path: ") << get_test_model_file_path() << std::endl;

    _CrtMemState s1, s2, s3;
    _CrtMemCheckpoint(&s1);
    FbxInfo fbx_info1(get_test_model_file_path(), false);
    FbxInfo fbx_info2(get_test_model_file_path(), true);
    fbx_info1.print_info();
    fbx_info2.print_info();
    FbxInfo::print_comparison(fbx_info1, fbx_info2);
    _CrtMemCheckpoint(&s2);
    _CrtMemDifference(&s3, &s1, &s2);
    _CrtMemDumpStatistics(&s3);
}
