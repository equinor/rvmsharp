#ifndef __CFBX_SDK_H__
#define __CFBX_SDK_H__

#include "common.h"
#include <iostream>

extern "C" {
    CFBX_API CFbxManager manager_create();
    CFBX_API void manager_destroy(CFbxManager manager);

    CFBX_API std::string* get_fbxsdk_version();
    CFBX_API void delete_fbxsdk_version(std::string* fbxVers);
}

#endif // __CFBX_SDK_H__
