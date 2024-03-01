/*
#ifdef _MSC_VER
#define _CRTDBG_MAP_ALLOC  
#include <crtdbg.h>
#define VS_MEM_CHECK _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#else
#define VS_MEM_CHECK
#endif
*/

#include "tests.h"
//#include "common.h"
//#include "node.h"
//#include "mesh.h"
//#include <fbxsdk.h>
#include <iostream>
//#include "manager.h"
//#include "importer.h"
#include <catch2/catch_all.hpp>
#include "fbx_info.h"

//using namespace fbxsdk;
using namespace std;

/*
void iterate(FbxNode* parent, const bool& ignore_normals, int ident = 0)
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
        if (data->vertex_count == 0)
        {
            cerr << "Could not retrieve geometry" << endl;
        }
        else
        {
            cout << "Vertex count: " << data->vertex_count << endl;
            cout << "Triangle count: " << data->index_count << endl;
        }

        delete data;
    }
}

void load(const std::string& fileName, const bool& ignore_normals)
{
    auto sdk = manager_create();
    auto root = (FbxNode*)load_file(fileName.c_str(), sdk);

    iterate(root, ignore_normals);

    root->Destroy();
    manager_destroy(sdk);
}

const std::string get_file_path_from_args(const int argc, char* argv[])
{
    std::string filePath = "AQ110South-3DView.fbx";
    if (argc >= 2)
    {
        filePath = argv[1];
        filePath.erase(std::remove(filePath.begin(), filePath.end(), '\"'), filePath.end());
    }

    return filePath;
}
*/

int invoke_catch2_tests(int argc, char* argv[])
{
    Catch::Session session;

    std::string filePath = "AQ110South-3DView.fbx";

    // Build a new parser on top of Catch2's
    using namespace Catch::Clara;
    auto cli = session.cli()                // Get Catch2's command line parser
    | Opt(filePath, "modelfile")            // bind variable to a new option, with a hint string
        ["-f"]["--modelfile"]               // the option names it will respond to
        ("Model file path (fbx-file)");     // description string for the help output

    // Now pass the new composite back to Catch2 so it uses that
    session.cli(cli);

    // Let Catch2 (using Clara) parse the command line
    int returnCode = session.applyCommandLine(argc, argv);
    if( returnCode != 0 )
        return returnCode;

    // If set on the command line then the model file path is now set at this point
    set_test_model_file_path(filePath);

    return session.run();
}

int main(int argc, char* argv[])
{
    invoke_catch2_tests(argc, argv);
    /*
    VS_MEM_CHECK

    const std::string filePath = get_file_path_from_args(argc, argv);
    std::cout << std::string("Using file path: ") << filePath << std::endl;

    _CrtMemState s1, s2, s3;
    _CrtMemCheckpoint(&s1);
    FbxInfo fbx_info1(filePath, false);
    FbxInfo fbx_info2(filePath, true);
    fbx_info1.print_info();
    fbx_info2.print_info();
    FbxInfo::print_comparison(fbx_info1, fbx_info2);
    _CrtMemCheckpoint(&s2);
    _CrtMemDifference(&s3, &s1, &s2);
    _CrtMemDumpStatistics(&s3);
    */

    return 0;
}
