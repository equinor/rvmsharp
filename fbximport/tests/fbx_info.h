#pragma once
#include <string>
#include <vector>
#include <common.h>

class FbxInfo
{
public:
    class InfoItem
    {
    public:
        InfoItem(
            const int& vertex_count,
            const int& triangle_count,
            const Color* color)
        {
            m_vertex_count = vertex_count;
            m_triangle_count = triangle_count;
            m_color.r = color->r;
            m_color.g = color->g;
            m_color.b = color->b;
            m_color.a = color->a;
        }

    public:
        int m_vertex_count = 0;
        int m_triangle_count = 0;
        Color m_color;
    };

public:
    FbxInfo(const std::string& fileName, const bool& ignore_normals);

public:
    size_t get_node_count() const;
    const FbxInfo::InfoItem& get_node_info(const size_t& index) const;
    std::string print_info() const;
    static std::string print_comparison(const FbxInfo& a, const FbxInfo& b);

private:
    void iterate(CFbxNode parent, const bool& ignore_normals, int ident = 0);
    void load(const std::string& fileName, const bool& ignore_normals);

private:
    std::vector<InfoItem> m_node_info;
    bool m_ignore_normals = false;
};
