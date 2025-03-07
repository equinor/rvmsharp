#ifndef __CFBX_SDK_H__
#define __CFBX_SDK_H__

#include "common.h"
#include <iostream>

extern "C" {
    CFBX_API CFbxManager* manager_create();
    CFBX_API void manager_destroy(CFbxManager* manager);

    CFBX_API bool assert_fbxsdk_version_newer_or_equal_than(const char* minFbxVersion);
}

#endif // __CFBX_SDK_H__
