#ifndef __CFBX_NODE_H__
#define __CFBX_NODE_H__

#include "common.h"

extern "C" {
    CFBX_API void node_get_name(CFbxNode node, char* output, int output_size);
    CFBX_API CFbxNode node_get_parent(CFbxNode node);
    CFBX_API int node_get_child_count(CFbxNode node);
    CFBX_API CFbxNode node_get_child(CFbxNode node, int index);
    CFBX_API CFbxMesh node_get_mesh(CFbxNode node);

    // this function should be replaced by corresponding FBX calls, transform should be initialized
    CFBX_API void node_get_transform(CFbxNode node, Transform* transform_out);
}


#endif // __CFBX_NODE_H__