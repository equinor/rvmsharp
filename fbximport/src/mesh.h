#ifndef __CFBX_MESH_H__
#define __CFBX_MESH_H__

#include "common.h"

extern "C" {
	// TODO: these are custom logic methods that should probably be transfered to the provider instead
	CFBX_API void mesh_clean_memory(ExportableMesh* mesh_data);
	CFBX_API ExportableMesh* mesh_get_geometry_data(CFbxMesh geometry, bool ignore_normals);
}


#endif // __CFBX_MESH_H__
