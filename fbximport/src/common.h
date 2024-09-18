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
typedef void* CFbxMaterial;

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
        bool valid;
        int vertex_count;
        int index_count;
        float* vertex_position_data;
        float* vertex_normal_data;
        int* index_data;

        ~ExportableMesh()
        {
            if (vertex_position_data)
            {
                delete vertex_position_data;
                vertex_position_data = nullptr;
            }

            if (index_data)
            {
                delete index_data;
                index_data = nullptr;
            }

            if (vertex_normal_data)
            {
                delete vertex_normal_data;
                vertex_normal_data = nullptr;
            }
        }
    };

    CFBX_API struct Color {
        float r;
        float g;
        float b;
        float a;
    };
}

#endif // __CFBX_COMMON_H__
