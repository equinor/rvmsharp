#include <catch2/catch_test_macros.hpp>

#ifdef _MSC_VER
#define _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#define VS_MEM_CHECK _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
#else
#define VS_MEM_CHECK
#endif

#include "tests.h"
#include "fbx_info.h"

#include <node.h>
#include <mesh.h>
#include <importer.h>
#include <manager.h>

#include <iostream>

using namespace std;

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

TEST_CASE("Load and iterate", "[FBX sdk]")
{
    VS_MEM_CHECK

    std::cout << std::string("Using file path: ") << get_test_model_file_path() << std::endl;

#ifdef _WIN32
    _CrtMemState s1, s2, s3;
    _CrtMemCheckpoint(&s1);
#endif

    FbxInfo fbx_info(get_test_model_file_path());
    std::cout << fbx_info.print_info();

#ifdef _WIN32
    _CrtMemCheckpoint(&s2);
    _CrtMemDifference(&s3, &s1, &s2);
    _CrtMemDumpStatistics(&s3);
#endif
}

TEST_CASE("Assert that the fbxsdk is newer or equal to a specified version", "[FBX version]")
{
    //We don't require a specific fbx-sdk version, but it needs to be version 2020.3.2 or newer
    REQUIRE(assert_fbxsdk_version_newer_or_equal_than("2020.3.2") == true);

    //Test that assert_fbxsdk_version_newer_or_equal_than with (nonexisting) future version fails.
    REQUIRE(assert_fbxsdk_version_newer_or_equal_than("3020.3.2") == false);
}
/*
TEST_CASE("Check ignore normals", "[FBX sdk]")
{
    auto testModelPath = get_test_model_file_path();

    std::cout << std::string("Using file path: ") << testModelPath << std::endl;

    FbxInfo fbx_info1(testModelPath);
    FbxInfo fbx_info2(testModelPath);

    size_t node_count1 = fbx_info1.get_node_count();
    size_t node_count2 = fbx_info2.get_node_count();
    size_t count = (node_count1 < node_count2) ? node_count1 : node_count2;
    REQUIRE(node_count1 > 0);
    REQUIRE(node_count2 > 0);
    REQUIRE(count > 0);

    REQUIRE(node_count2 == node_count1);

    for (size_t i=0; i<count; i++)
    {
        const FbxInfo::InfoItem& item1 = fbx_info1.get_node_info(i);
        const FbxInfo::InfoItem& item2 = fbx_info2.get_node_info(i);

        REQUIRE(item1.m_vertex_count >= item2.m_vertex_count);
        REQUIRE(item1.m_triangle_count == item2.m_triangle_count);
    }

    std::string output = FbxInfo::print_comparison(fbx_info1, fbx_info2);
    REQUIRE(output.size() > 0);
    std::cout << output;
}*/
