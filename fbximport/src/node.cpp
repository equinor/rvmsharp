#include "node.h"
#include <fbxsdk.h>

void node_get_name(CFbxNode node, char* output, int output_size)
{
    if (node == nullptr)
        return;

    const auto fbxNode = static_cast<FbxNode*>(node);
    auto name = fbxNode->GetName();
    strncpy(output, name, output_size - 1);
    output[output_size - 1] = '\0';
}

CFbxNode node_get_parent(CFbxNode node)
{
    if (node == nullptr)
        return nullptr;

    const auto fbxNode = static_cast<FbxNode*>(node);
    return static_cast<CFbxNode>(fbxNode->GetParent());
}

int node_get_child_count(CFbxNode node)
{
    if (node == nullptr)
        return 0;

    const auto fbxNode = static_cast<FbxNode*>(node);
    return fbxNode->GetChildCount();
}

CFbxNode node_get_child(CFbxNode node, int index)
{
    if (node == nullptr)
        return nullptr;

    const auto fbxNode = static_cast<FbxNode*>(node);
    return static_cast<CFbxNode>(fbxNode->GetChild(index));
}

CFbxMesh node_get_mesh(CFbxNode node)
{
    if (node == nullptr)
        return nullptr;

    const auto fbxNode = static_cast<FbxNode*>(node);
    const auto attr = fbxNode->GetNodeAttribute();
    if (attr != nullptr && attr->GetAttributeType() == FbxNodeAttribute::eMesh)
    {
        return static_cast<FbxMesh*>(attr);
    }
    return nullptr;
}

void node_destroy(CFbxNode node)
{
    if (node == nullptr)
        return;

    auto fbxNode = static_cast<FbxNode*>(node);
    fbxNode->Destroy();
    fbxNode = nullptr;
}

Transform node_get_transform(CFbxNode node)
{
    Transform transform_out{0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    if (node == nullptr)
        return transform_out;

    const auto fbxNode = static_cast<FbxNode*>(node);
    auto t = fbxNode->LclTranslation.Get();
    FbxQuaternion r;
    r.ComposeSphericalXYZ(fbxNode->LclRotation.Get());
    auto s = fbxNode->LclScaling.Get();

    transform_out.posX = t[0];
    transform_out.posY = t[1];
    transform_out.posZ = t[2];
    transform_out.rotX = r[0];
    transform_out.rotY = r[1];
    transform_out.rotZ = r[2];
    transform_out.rotW = r[3];
    transform_out.scaleX = s[0];
    transform_out.scaleY = s[1];
    transform_out.scaleZ = s[2];
    return transform_out;
}
