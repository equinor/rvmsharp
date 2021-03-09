#ifndef __TESSBRIDGE_H__
#define __TESSBRIDGE_H__

#include <stdint.h>

#if _WIN32
#ifdef TESSBRIDGE_BUILD_AS_DLL
#define TESSBRIDGE_API __declspec(dllexport)
#else
#define TESSBRIDGE_API __declspec(dllimport)
#endif
#else
#define TESSBRIDGE_API
#endif

extern "C"
{
    TESSBRIDGE_API int32_t tessellate(float* InVertexData, float* InNormalData, int32_t* InContourVertexCounts, int32_t ContourCount,
                                      int32_t& OutVertexCount, int32_t& OutNormalCount, int32_t& OutIndexCount);

    TESSBRIDGE_API void collect_result(int32_t JobId, float* OutVertexBuffer, float* OutNormalBuffer, int32_t* OutIndexBuffer);
}

#endif // __TESSBRIDGE_H__