#ifndef __CFBX_MATERIAL_H__
#define __CFBX_MATERIAL_H__

#include "common.h"

extern "C" {
    CFBX_API void material_clean_memory(Color* color);
    CFBX_API Color* material_get_color(CFbxMaterial* material);
}

#endif // __CFBX_MATERIAL_H__
