#include "material.h"
#include <fbxsdk.h>

Color* material_get_color(CFbxMaterial material)
{
    const auto fbxMaterial = (FbxSurfaceLambert*)material;
    if(fbxMaterial == nullptr)
      return nullptr;

    const auto diffuse = fbxMaterial->Diffuse.Get();

    Color* color = new Color();
    color->r = diffuse[0];
    color->g = diffuse[1];
    color->b = diffuse[2];
    color->a = 1.0;

    return color;
}

void material_clean_memory(Color* color)
{
    if (color == nullptr)
        return;

    delete color;
    color = nullptr;
}
