#ifndef __CFBX_SDK_H__
#define __CFBX_SDK_H__

#include "common.h"

extern "C" {
    CFBX_API CFbxManager manager_create();
    CFBX_API void manager_destroy(CFbxManager manager);
}

#endif // __CFBX_SDK_H__
