using rvmsharp.Rvm;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace rvmsharp.Tessellator
{
    public static class TessellatorBridge
    {
        public static Mesh Tessellate(RvmPrimitive geometry)
        {
            return geometry switch
            {
                RvmBox box => new Mesh(),
                _ => throw new NotImplementedException($"Unsupported type for tesselation: {geometry.Kind}"),
            };
        }

        private static Mesh Tessellate(RvmBox box)
        {
            var mesh = new Mesh();

            var xExtents = 0.5f * box.lengthX;
            var yExtents = 0.5f * box.lengthY;
            var zExtents = 0.5f * box.lengthZ;


            mesh.Vertices = new List<Vector3>()
            {
                /*new Vector3(-xExtents, ym, zp), new Vector3(-xExtents, yExtents, zp), new Vector3(-xExtents, yExtents, zm), new Vector3(-xExtents, ym, zm),
                new Vector3(xExtents, ym, zm), new Vector3(xExtents, yExtents, zm), new Vector3(xExtents, yExtents, zp), new Vector3(xExtents, ym, zp),
                new Vector3(xExtents, ym, zm), new Vector3(xExtents, ym, zp), new Vector3(-xExtents, ym, zp), new Vector3(-xExtents, ym, zm),
                new Vector3(-xExtents, yExtents, zm), new Vector3(-xExtents, yExtents, zp), new Vector3(xExtents, yExtents, zp), new Vector3(xExtents, yExtents, zm),
                new Vector3(-xExtents, yExtents, zm), new Vector3(xExtents, yExtents, zm), new Vector3(xExtents, ym, zm), new Vector3(-xExtents, ym, zm),
                new Vector3(-xExtents, ym, zp), new Vector3(xExtents, ym, zp), new Vector3(xExtents, yExtents, zp), new Vector3(-xExtents, yExtents, zp)*/
            };
            return mesh;

            /*auto & box = geo->box;

            auto xExtents = 0.5f * box.lengths[0]; auto -xExtents = -xExtents;
            auto yExtents = 0.5f * box.lengths[1]; auto ym = -yp;
            auto zp = 0.5f * box.lengths[2]; auto zm = -zp;

            Vec3f V[6][4] = {
                
            };

            Vec3f N[6] = {
    Vec3f(-1,  0,  0),
    Vec3f(1,  0,  0 ),
    Vec3f(0, -1,  0 ),
    Vec3f(0,  1,  0 ),
    Vec3f(0,  0, -1 ),
    Vec3f(0,  0,  1 )
  };

            bool faces[6] = {
    1e-5 <= box.lengths[0],
    1e-5 <= box.lengths[0],
    1e-5 <= box.lengths[1],
    1e-5 <= box.lengths[1],
    1e-5 <= box.lengths[2],
    1e-5 <= box.lengths[2],
  };
            for (unsigned i = 0; i < 6; i++)
            {
                auto* con = geo->connections[i];
                if (faces[i] == false || con == nullptr || con->flags != Connection::Flags::HasRectangularSide) continue;

                if (doInterfacesMatch(geo, con))
                {
                    faces[i] = false;
                    discardedCaps++;
                    //store->addDebugLine(con->p.data, (con->p.data + 0.05f*con->d).data, 0xff0000);
                }

            }

            unsigned faces_n = 0;
            for (unsigned i = 0; i < 6; i++)
            {
                if (faces[i]) faces_n++;
            }

            Triangulation* tri = arena->alloc<Triangulation>();
            tri->error = 0.f;

            if (faces_n)
            {
                tri->vertices_n = 4 * faces_n;
                tri->vertices = (float*)arena->alloc(3 * sizeof(float) * tri->vertices_n);
                tri->normals = (float*)arena->alloc(3 * sizeof(float) * tri->vertices_n);

                tri->triangles_n = 2 * faces_n;
                tri->indices = (uint32_t*)arena->alloc(3 * sizeof(uint32_t) * tri->triangles_n);

                unsigned o = 0;
                unsigned i_v = 0;
                unsigned i_p = 0;
                for (unsigned f = 0; f < 6; f++)
                {
                    if (!faces[f]) continue;

                    for (unsigned i = 0; i < 4; i++)
                    {
                        i_v = vertex(tri->normals, tri->vertices, i_v, N[f].data, V[f][i].data);
                    }
                    i_p = quadIndices(tri->indices, i_p, o, 0, 1, 2, 3);

                    o += 4;
                }
                assert(i_v == 3 * tri->vertices_n);
                assert(i_p == 3 * tri->triangles_n);
                assert(o == tri->vertices_n);
            }

            return tri;*/
        }
    }
}
