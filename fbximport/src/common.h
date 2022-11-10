#ifndef __CFBX_COMMON_H__
#define __CFBX_COMMON_H__

#if _WIN32
    #ifdef CFBX_BUILD_AS_DLL
    #define CFBX_API __declspec(dllexport)
    #else
    #define CFBX_API __declspec(dllimport)
    #endif
#else
#define CFBX_API
#endif

typedef void* CFbxManager;
typedef void* CFbxNode;
typedef void* CFbxMesh;

extern "C" {
    CFBX_API struct Transform {
        float posX;
        float posY;
        float posZ;
        float rotX;
        float rotY;
        float rotZ;
        float rotW;
        float scaleX;
        float scaleY;
        float scaleZ;
    };

    CFBX_API struct ExportableMesh {
        int vertex_count;
        int triangle_count;
        float* vertex_data;
        float* normal_data;
        int* triangle_data;
    };
}

#endif // __CFBX_COMMON_H__