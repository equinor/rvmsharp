#pragma once
#include <string>
#include <vector>
#include <fbxsdk.h>

class FbxInfo
{
public:
    class InfoItem
    {
    public:
        InfoItem(const int& vertex_count, const int& triangle_count) { vertex_count_ = vertex_count; triangle_count_ = triangle_count; }

    public:
        int vertex_count_ = 0;
        int triangle_count_ = 0;
    };

public:
    FbxInfo(const std::string& fileName, const bool& ignore_normals);

public:
    size_t get_node_count() const;
    const FbxInfo::InfoItem& get_node(const size_t& index) const;
    std::string print_info() const;
    static std::string print_comparison(const FbxInfo& a, const FbxInfo& b);

private:
    void iterate(FbxNode* parent, const bool& ignore_normals, int ident = 0);
    void load(const std::string& fileName, const bool& ignore_normals);

private:
    std::vector<InfoItem> node_info_;
    bool ignore_normals_ = false;
};
