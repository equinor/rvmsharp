#ifndef __CFBX_IMPORTER_H__
#define __CFBX_IMPORTER_H__

#include "common.h"

extern "C" {
    // this function is a mess
    CFBX_API void* load_file(const char* filename, void* sdk);
}


#endif // __CFBX_IMPORTER_H__
