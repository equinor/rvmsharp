using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace RvmSharp.Tessellation
{
    using Primitives;

    public class TessellatorBridge
    {
        private const float MinimumThreshold = 1e-7f;

        public static Mesh[] Tessellate(RvmNode group, float tolerance)
        {   
            var meshes = group.Children.OfType<RvmPrimitive>().Select(p =>
            {
                if (!Matrix4x4.Decompose(p.Matrix, out var scale, out _, out _))
                {
                    throw new InvalidOperationException($"Could not decompose matrix for {@group.Name}");
                }

                var scaleScalar = Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
                var mesh = Tessellate(p, scaleScalar, tolerance);
                mesh?.Apply(p.Matrix);
                return mesh;
            }).Where(m => m!= null);
            return meshes.ToArray();
        }
        
        public static Mesh Tessellate(RvmPrimitive geometry, float scale, float tolerance)
        {
            switch (geometry)
            {
                case RvmBox box:
                    return Tessellate(box);
                case RvmFacetGroup facetGroup:
                    return Tessellate(facetGroup);
                case RvmPyramid pyramid:
                    return Tessellate(pyramid);
                case RvmRectangularTorus rectangularTorus:
                    return Tessellate(rectangularTorus, scale, tolerance);
                case RvmCylinder cylinder:
                    return TessellateCylinder(cylinder, scale, tolerance);
                case RvmCircularTorus circularTorus:
                    return Tessellate(circularTorus, scale, tolerance);
                case RvmSnout snout:
                    return Tessellate(snout, scale, tolerance);
                case RvmLine:
                    // we cannot tessellate a line, we should handle it elsewhere
                    return null;
                case RvmSphere sphere:
                    return Tessellate(sphere, 0.5f * sphere.Diameter, (float)Math.PI, 0.0f, 1.0f, scale, tolerance);
                case RvmEllipticalDish ellipticalDish:
                    return Tessellate(ellipticalDish, ellipticalDish.BaseRadius, (float)Math.PI / 2, 0.0f,
                        ellipticalDish.Height / ellipticalDish.BaseRadius, scale, tolerance);
                case RvmSphericalDish sphericalDish:
                {
                    var baseRadius = sphericalDish.BaseRadius;
                    var height = sphericalDish.Height;
                    var sphereRadius = (baseRadius * baseRadius + height * height) / (2.0f * height);
                    var sinval = Math.Min(1.0f, Math.Max(-1.0, (baseRadius / sphereRadius)));
                    var arc = (float)Math.Asin(sinval);
                    if (baseRadius < height) { arc = (float)Math.PI - arc; }

                    return Tessellate(sphericalDish, sphereRadius, arc, height - sphereRadius, 1.0f, scale, tolerance);
                }
                default:
                    throw new NotImplementedException($"Unsupported type for tesselation: {geometry?.Kind}");
            }
        }

        private static Mesh Tessellate(RvmPyramid pyramid)
        {
            var bx = 0.5f * pyramid.BottomX;
            var by = 0.5f * pyramid.BottomY;
            var tx = 0.5f * pyramid.TopX;
            var ty = 0.5f * pyramid.TopY;
            var ox = 0.5f * pyramid.OffsetX;
            var oy = 0.5f * pyramid.OffsetY;
            var halfHeight = 0.5f * pyramid.Height;


            Vector3[,] quad =
            {
                {
                    new Vector3(-bx - ox, -by - oy, -halfHeight), new Vector3(bx - ox, -by - oy, -halfHeight),
                    new Vector3(bx - ox, by - oy, -halfHeight), new Vector3(-bx - ox, by - oy, -halfHeight)
                },
                {
                    new Vector3(-tx + ox, -ty + oy, halfHeight), new Vector3(tx + ox, -ty + oy, halfHeight),
                    new Vector3(tx + ox, ty + oy, halfHeight), new Vector3(-tx + ox, ty + oy, halfHeight)
                },
            };

            Vector3[] n =
            {
                new Vector3(0.0f, -halfHeight, (quad[1, 0].Y - quad[0, 0].Y)),
                new Vector3(halfHeight, 0.0f, -(quad[1, 1].X - quad[0, 1].X)),
                new Vector3(0.0f, halfHeight, -(quad[1, 2].Y - quad[0, 2].Y)),
                new Vector3(-halfHeight, 0.0f, (quad[1, 3].X - quad[0, 3].X)), 
                new Vector3(0, 0, -1), new Vector3(0, 0, 1),
            };

            bool[] cap =
            {
                true, true, true, true,
                MinimumThreshold <= Math.Min(Math.Abs(pyramid.BottomX), Math.Abs(pyramid.BottomY)),
                MinimumThreshold <= Math.Min(Math.Abs(pyramid.TopX), Math.Abs(pyramid.TopY))
            };

            for (var i = 0; i < 6; i++)
            {
                var con = pyramid.Connections[i];
                if (cap[i] == false || con == null || con.flags != RvmConnection.Flags.HasRectangularSide) continue;

                if (ConnectionInterface.DoInterfacesMatch(pyramid, con))
                {
                    cap[i] = false;
                }
            }

            var capCount = cap.Count(c => c);

            var error = 0.0f;

            var vertices = new float[3 * 4 * capCount];
            var normals = new float[3 * 4 * capCount];

            var arrayPosition = 0;
            for (var i = 0; i < 4; i++)
            {
                if (cap[i] == false) continue;
                var ii = (i + 1) & 3;
                arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[i], quad[0, i]);
                arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[i], quad[0, ii]);
                arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[i], quad[1, ii]);
                arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[i], quad[1, i]);
            }

            if (cap[4])
            {
                for (var i = 0; i < 4; i++)
                {
                    arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[4], quad[0, i]);
                }
            }

            if (cap[5])
            {
                for (var i = 0; i < 4; i++)
                {
                    arrayPosition = TessellationHelpers.Vertex(normals, vertices, arrayPosition, n[5], quad[1, i]);
                }
            }

            if (arrayPosition != vertices.Length)
                throw new Exception("Missing vertices");

            arrayPosition = 0;
            var o = 0;
            var indices = new int[3 * 2 * capCount];
            for (var i = 0; i < 4; i++)
            {
                if (cap[i] == false) continue;
                arrayPosition = TessellationHelpers.QuadIndices(indices, arrayPosition, o /*4 * i*/, 0, 1, 2, 3);
                o += 4;
            }

            if (cap[4])
            {
                arrayPosition = TessellationHelpers.QuadIndices(indices, arrayPosition, o, 3, 2, 1, 0);
                o += 4;
            }

            if (cap[5])
            {
                arrayPosition = TessellationHelpers.QuadIndices(indices, arrayPosition, o, 0, 1, 2, 3);
                o += 4;
            }

            if (arrayPosition != 3 * 2 * capCount || o != vertices.Length / 3)
                throw new Exception();

            return new Mesh(vertices, normals, indices, error);
        }


        private static Mesh Tessellate(RvmRectangularTorus rectangularTorus, float scale, float tolerance)
        {
            var segments = TessellationHelpers.SagittaBasedSegmentCount(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, tolerance);
            var samples = segments + 1; // Assumed to be open, add extra sample.

            var error = TessellationHelpers.SagittaBasedError(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, segments);

            bool shell = true;
            bool[] cap = {true, true};

            for (var i = 0; i < 2; i++)
            {
                var con = rectangularTorus.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasRectangularSide)
                {
                    if (ConnectionInterface.DoInterfacesMatch(rectangularTorus, con))
                    {
                        cap[i] = false;
                    }
                }
            }

            var h2 = 0.5f * rectangularTorus.Height;
            float[,] square =
            {
                {rectangularTorus.RadiusOuter, -h2}, {rectangularTorus.RadiusInner, -h2},
                {rectangularTorus.RadiusInner, h2}, {rectangularTorus.RadiusOuter, h2},
            };

            // Not closed
            var t0 = new float[2 * samples + 1];
            for (var i = 0; i < samples; i++)
            {
                t0[2 * i + 0] = (float)Math.Cos((rectangularTorus.Angle / segments) * i);
                t0[2 * i + 1] = (float)Math.Sin((rectangularTorus.Angle / segments) * i);
            }

            var l = 0;

            var vertices_n = (shell ? 4 * 2 * samples : 0) + (cap[0] ? 4 : 0) + (cap[1] ? 4 : 0);

            var vertices = new float[3 * vertices_n];
            var normals = new float[3 * vertices_n];

            if (shell)
            {
                for (var i = 0; i < samples; i++)
                {
                    float[,] n =
                    {
                        {0.0f, 0.0f, -1.0f}, {-t0[2 * i + 0], -t0[2 * i + 1], 0.0f}, {0.0f, 0.0f, 1.0f},
                        {t0[2 * i + 0], t0[2 * i + 1], 0.0f},
                    };

                    for (var k = 0; k < 4; k++)
                    {
                        var kk = (k + 1) & 3;

                        normals[l] = n[k, 0];
                        vertices[l++] = square[k, 0] * t0[2 * i + 0];
                        normals[l] = n[k, 1];
                        vertices[l++] = square[k, 0] * t0[2 * i + 1];
                        normals[l] = n[k, 2];
                        vertices[l++] = square[k, 1];

                        normals[l] = n[k, 0];
                        vertices[l++] = square[kk, 0] * t0[2 * i + 0];
                        normals[l] = n[k, 1];
                        vertices[l++] = square[kk, 0] * t0[2 * i + 1];
                        normals[l] = n[k, 2];
                        vertices[l++] = square[kk, 1];
                    }
                }
            }

            if (cap[0])
            {
                for (var k = 0; k < 4; k++)
                {
                    normals[l] = 0.0f;
                    vertices[l++] = square[k, 0] * t0[0];
                    normals[l] = -1.0f;
                    vertices[l++] = square[k, 0] * t0[1];
                    normals[l] = 0.0f;
                    vertices[l++] = square[k, 1];
                }
            }

            if (cap[1])
            {
                for (var k = 0; k < 4; k++)
                {
                    normals[l] = -t0[2 * (samples - 1) + 1];
                    vertices[l++] = square[k, 0] * t0[2 * (samples - 1) + 0];
                    normals[l] = t0[2 * (samples - 1) + 0];
                    vertices[l++] = square[k, 0] * t0[2 * (samples - 1) + 1];
                    normals[l] = 0.0f;
                    vertices[l++] = square[k, 1];
                }
            }

            if (l != 3 * vertices_n)
                throw new Exception();

            l = 0;
            var o = 0;

            var triangles_n = (shell ? 4 * 2 * (samples - 1) : 0) + (cap[0] ? 2 : 0) + (cap[1] ? 2 : 0);
            var indices = new int[3 * triangles_n];

            if (shell)
            {
                for (var i = 0; i + 1 < samples; i++)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        indices[l++] = 4 * 2 * (i + 0) + 0 + 2 * k;
                        indices[l++] = 4 * 2 * (i + 0) + 1 + 2 * k;
                        indices[l++] = 4 * 2 * (i + 1) + 0 + 2 * k;

                        indices[l++] = 4 * 2 * (i + 1) + 0 + 2 * k;
                        indices[l++] = 4 * 2 * (i + 0) + 1 + 2 * k;
                        indices[l++] = 4 * 2 * (i + 1) + 1 + 2 * k;
                    }
                }

                o += 4 * 2 * samples;
            }

            if (cap[0])
            {
                indices[l++] = o + 0;
                indices[l++] = o + 2;
                indices[l++] = o + 1;
                indices[l++] = o + 2;
                indices[l++] = o + 0;
                indices[l++] = o + 3;
                o += 4;
            }

            if (cap[1])
            {
                indices[l++] = o + 0;
                indices[l++] = o + 1;
                indices[l++] = o + 2;
                indices[l++] = o + 2;
                indices[l++] = o + 3;
                indices[l++] = o + 0;
                o += 4;
            }

            if (o != vertices_n || l != 3 * triangles_n)
                throw new Exception();

            return new Mesh(vertices, normals, indices, error);
        }


        private static Mesh Tessellate(RvmCircularTorus circularTorus, float scale, float tolerance)
        {
            var segments_l = TessellationHelpers.SagittaBasedSegmentCount(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius,
                scale, tolerance); // large radius, toroidal direction
            var segments_s = TessellationHelpers.SagittaBasedSegmentCount(Math.PI * 2, circularTorus.Radius, scale,
                    tolerance); // small radius, poloidal direction

            var error = Math.Max(TessellationHelpers.SagittaBasedError(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius, scale, segments_l), TessellationHelpers.SagittaBasedError(Math.PI * 2, circularTorus.Radius, scale, segments_s));

            var samples_l = segments_l + 1; // Assumed to be open, add extra sample
            var samples_s = segments_s; // Assumed to be closed

            bool shell = true;
            bool[] cap = {true, true};
            for (var i = 0; i < 2; i++)
            {
                var con = circularTorus.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (ConnectionInterface.DoInterfacesMatch(circularTorus, con))
                    {
                        cap[i] = false;
                    }
                    else
                    {
                        //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0x00ffff);
                    }
                }
            }

            var t0 = new float[2 * samples_l];
            for (var i = 0; i < samples_l; i++)
            {
                t0[2 * i + 0] = (float)Math.Cos((circularTorus.Angle / (samples_l - 1.0f)) * i);
                t0[2 * i + 1] = (float)Math.Sin((circularTorus.Angle / (samples_l - 1.0f)) * i);
            }

            var t1 = new float[2 * samples_s];
            for (var i = 0; i < samples_s; i++)
            {
                t1[2 * i + 0] = (float)Math.Cos((Math.PI * 2 / samples_s) * i + circularTorus.SampleStartAngle);
                t1[2 * i + 1] = (float)Math.Sin((Math.PI * 2 / samples_s) * i + circularTorus.SampleStartAngle);
            }


            var vertices_n = ((shell ? samples_l : 0) + (cap[0] ? 1 : 0) + (cap[1] ? 1 : 0)) * samples_s;
            var vertices = new float[3 * vertices_n];
            var normals = new float[3 * vertices_n];

            var triangles_n = (shell ? 2 * (samples_l - 1) * samples_s : 0) +
                              (samples_s - 2) * ((cap[0] ? 1 : 0) + (cap[1] ? 1 : 0));
            var indices = new int[3 * triangles_n];

            // generate vertices
            var l = 0;

            if (shell)
            {
                //Vec3f n(cos(Math.PI * 2 *v) * cos(circularTorus.Angle * u),
                //        cos(Math.PI * 2 *v) * sin(circularTorus.Angle * u),
                //        (float)Math.Sin(Math.PI * 2 *v));
                //Vec3f p((circularTorus.Radius * cos(Math.PI * 2 *v) + circularTorus.Offset) * cos(circularTorus.Angle * u),
                //        (circularTorus.Radius * cos(Math.PI * 2 *v) + circularTorus.Offset) * sin(circularTorus.Angle * u),
                //        circularTorus.Radius * sin(Math.PI * 2 *v));
                for (var u = 0; u < samples_l; u++)
                {
                    for (var v = 0; v < samples_s; v++)
                    {
                        normals[l] = t1[2 * v + 0] * t0[2 * u + 0];
                        vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[2 * u + 0]);
                        normals[l] = t1[2 * v + 0] * t0[2 * u + 1];
                        vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[2 * u + 1]);
                        normals[l] = t1[2 * v + 1];
                        vertices[l++] = circularTorus.Radius * t1[2 * v + 1];
                    }
                }
            }

            if (cap[0])
            {
                for (var v = 0; v < samples_s; v++)
                {
                    normals[l] = 0.0f;
                    vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[0]);
                    normals[l] = -1.0f;
                    vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[1]);
                    normals[l] = 0.0f;
                    vertices[l++] = circularTorus.Radius * t1[2 * v + 1];
                }
            }

            if (cap[1])
            {
                var m = 2 * (samples_l - 1);
                for (var v = 0; v < samples_s; v++)
                {
                    normals[l] = -t0[m + 1];
                    vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[m + 0]);
                    normals[l] = t0[m + 0];
                    vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[m + 1]);
                    normals[l] = 0.0f;
                    vertices[l++] = circularTorus.Radius * t1[2 * v + 1];
                }
            }

            Asserts.AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", 3 * vertices_n);

            // generate indices
            l = 0;
            var o = 0;
            if (shell)
            {
                for (var u = 0; u + 1 < samples_l; u++)
                {
                    for (var v = 0; v + 1 < samples_s; v++)
                    {
                        indices[l++] = samples_s * (u + 0) + (v + 0);
                        indices[l++] = samples_s * (u + 1) + (v + 0);
                        indices[l++] = samples_s * (u + 1) + (v + 1);

                        indices[l++] = samples_s * (u + 1) + (v + 1);
                        indices[l++] = samples_s * (u + 0) + (v + 1);
                        indices[l++] = samples_s * (u + 0) + (v + 0);
                    }

                    indices[l++] = samples_s * (u + 0) + (samples_s - 1);
                    indices[l++] = samples_s * (u + 1) + (samples_s - 1);
                    indices[l++] = samples_s * (u + 1) + 0;
                    indices[l++] = samples_s * (u + 1) + 0;
                    indices[l++] = samples_s * (u + 0) + 0;
                    indices[l++] = samples_s * (u + 0) + (samples_s - 1);
                }

                o += samples_l * samples_s;
            }

            var u1 = new int[samples_s];
            var u2 = new int[samples_s];
            if (cap[0])
            {
                for (var i = 0; i < samples_s; i++)
                {
                    u1[i] = o + i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples_s);
                o += samples_s;
            }

            if (cap[1])
            {
                for (var i = 0; i < samples_s; i++)
                {
                    u1[i] = o + (samples_s - 1) - i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples_s);
                o += samples_s;
            }

            Asserts.AssertEquals(nameof(l), l, nameof(triangles_n) + " * 3", 3 * triangles_n);
            Asserts.AssertEquals(nameof(o), o, nameof(vertices_n), vertices_n);

            return new Mesh(vertices, normals, indices, error);
        }


        private static Mesh Tessellate(RvmBox box)
        {
            var xp = 0.5f * box.LengthX;
            var xm = -xp;
            var yp = 0.5f * box.LengthY;
            var ym = -yp;
            var zp = 0.5f * box.LengthZ;
            var zm = -zp;

            Vector3[,] V = new Vector3[,]
            {
                {
                    new Vector3(xm, ym, zp), new Vector3(xm, yp, zp), new Vector3(xm, yp, zm), new Vector3(xm, ym, zm)
                },
                {new Vector3(xp, ym, zm), new Vector3(xp, yp, zm), new Vector3(xp, yp, zp), new Vector3(xp, ym, zp)},
                {new Vector3(xp, ym, zm), new Vector3(xp, ym, zp), new Vector3(xm, ym, zp), new Vector3(xm, ym, zm)},
                {new Vector3(xm, yp, zm), new Vector3(xm, yp, zp), new Vector3(xp, yp, zp), new Vector3(xp, yp, zm)},
                {new Vector3(xm, yp, zm), new Vector3(xp, yp, zm), new Vector3(xp, ym, zm), new Vector3(xm, ym, zm)},
                {new Vector3(xm, ym, zp), new Vector3(xp, ym, zp), new Vector3(xp, yp, zp), new Vector3(xm, yp, zp)}
            };

            Vector3[] N =
            {
                new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 1, 0),
                new Vector3(0, 0, -1), new Vector3(0, 0, 1)
            };

            bool[] faces =
            {
                1e-5 <= box.LengthX, 1e-5 <= box.LengthX, 1e-5 <= box.LengthY, 1e-5 <= box.LengthY,
                1e-5 <= box.LengthZ, 1e-5 <= box.LengthZ,
            };

            for (var i = 0; i < 6; i++)
            {
                var con = box.Connections[i];
                if (faces[i] == false || con == null || con.flags != RvmConnection.Flags.HasRectangularSide) continue;

                if (ConnectionInterface.DoInterfacesMatch(box, con))
                {
                    faces[i] = false;
                    //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0xff0000);
                }
            }

            var faces_n = 0;
            for (var i = 0; i < 6; i++)
            {
                if (faces[i]) faces_n++;
            }


            if (faces_n > 0)
            {
                var vertices_n = 4 * faces_n;
                var vertices = new float[3 * vertices_n];
                var normals = new float[3 * vertices_n];

                var triangles_n = 2 * faces_n;
                var indices = new int[3 * triangles_n];

                var o = 0;
                var i_v = 0;
                var i_p = 0;
                for (var f = 0; f < 6; f++)
                {
                    if (!faces[f]) continue;

                    for (var i = 0; i < 4; i++)
                    {
                        i_v = TessellationHelpers.Vertex(normals, vertices, i_v, N[f], V[f, i]);
                    }

                    i_p = TessellationHelpers.QuadIndices(indices, i_p, o, 0, 1, 2, 3);

                    o += 4;
                }

                var tri = new Mesh(vertices, normals, indices, 0.0f);

                if (!(i_v == 3 * vertices_n) ||
                    !(i_p == 3 * triangles_n) ||
                    !(o == vertices_n))
                {
                    throw new Exception();
                }

                return tri;
            }

            return new Mesh(new float[0], new float[0], new int[0], 0);
        }

        private static Mesh Tessellate(RvmFacetGroup facetGroup)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();

            for (var p = 0; p < facetGroup.Polygons.Length; p++)
            {
                var poly = facetGroup.Polygons[p];

                var (bMin, bMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
                foreach (var cont in poly.Contours)
                {
                    foreach (var vn in cont.Vertices)
                    {
                        (bMin.X, bMin.Y, bMin.Z) = (Math.Min(bMin.X, vn.v.X), Math.Min(bMin.Y, vn.v.Y),
                            Math.Min(bMin.Z, vn.v.Z));
                        (bMax.X, bMax.Y, bMax.Z) = (Math.Max(bMax.X, vn.v.X), Math.Max(bMax.Y, vn.v.Y),
                            Math.Max(bMax.Z, vn.v.Z));
                    }
                }

                var m = 0.5f * (bMin + bMax);

                var vo = vertices.Count;

                var adjustedContours = poly.Contours.Select(v => new RvmContour(
                    v.Vertices.Select(x => (x.v - m, x.n)).ToArray()
                )).ToArray();

                var outJob = TessNet.Tessellate(adjustedContours);

                vertices.AddRange(outJob.VertexData.Select(v => v + m));
                normals.AddRange(outJob.NormalData);
                indices.AddRange(outJob.Indices.Select(i => i + vo));

                if (vertices.Count != normals.Count)
                    throw new Exception();
            }

            return new Mesh(vertices.ToArray(), normals.ToArray(), indices.ToArray(), 0);
        }


        private static Mesh TessellateCylinder(RvmCylinder cylinder, float scale, float tolerance)
        {
            //if (cullTiny && cy.radius*scale < tolerance) {
            //  tri.error = cy.radius * scale;
            //  return;
            //}

            int segments = TessellationHelpers.SagittaBasedSegmentCount(Math.PI * 2, cylinder.Radius, scale, tolerance);
            int samples = segments; // Assumed to be closed

            var error = TessellationHelpers.SagittaBasedError(Math.PI * 2, cylinder.Radius, scale, segments);

            bool shell = true;
            bool[] shouldCap = {true, true};


            for (int i = 0; i < 2; i++)
            {
                var con = cylinder.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (ConnectionInterface.DoInterfacesMatch(cylinder, con))
                    {
                        shouldCap[i] = false;
                        //discardedCaps++;
                    }
                    else
                    {
                        //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0x00ffff);
                    }
                }
            }

            int vertCount = (shell ? 2 * samples : 0) + (shouldCap[0] ? samples : 0) + (shouldCap[1] ? samples : 0);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];

            int triangles_n = (shell ? 2 * samples : 0) + (shouldCap[0] ? samples - 2 : 0) +
                              (shouldCap[1] ? samples - 2 : 0);
            var indices = new int[triangles_n * 3];

            float[] t0 = new float[2 * samples];
            for (int i = 0; i < samples; i++)
            {
                t0[2 * i + 0] = (float)Math.Cos((Math.PI * 2 / samples) * i + cylinder.SampleStartAngle);
                t0[2 * i + 1] = (float)Math.Sin((Math.PI * 2 / samples) * i + cylinder.SampleStartAngle);
            }

            float[] t1 = new float[2 * samples];
            for (int i = 0; i < 2 * samples; i++)
            {
                t1[i] = cylinder.Radius * t0[i];
            }

            float h2 = 0.5f * cylinder.Height;
            int l = 0;

            if (shell)
            {
                for (int i = 0; i < samples; i++)
                {
                    l = TessellationHelpers.Vertex(normals, vertices, l, t0[2 * i + 0], t0[2 * i + 1], 0, t1[2 * i + 0], t1[2 * i + 1],
                        -h2);
                    l = TessellationHelpers.Vertex(normals, vertices, l, t0[2 * i + 0], t0[2 * i + 1], 0, t1[2 * i + 0], t1[2 * i + 1], h2);
                }
            }

            if (shouldCap[0])
            {
                for (int i = 0; i < samples; i++)
                {
                    l = TessellationHelpers.Vertex(normals, vertices, l, new Vector3(0, 0, -1),
                        new Vector3(t1[2 * i + 0], t1[2 * i + 1], -h2));
                }
            }

            if (shouldCap[1])
            {
                for (int i = 0; i < samples; i++)
                {
                    l = TessellationHelpers.Vertex(normals, vertices, l, new Vector3(0, 0, 1),
                        new Vector3(t1[2 * i + 0], t1[2 * i + 1], h2));
                }
            }

            Asserts.AssertEquals(nameof(l), l, nameof(vertCount), vertCount);

            l = 0;
            int o = 0;
            if (shell)
            {
                for (int i = 0; i < samples; i++)
                {
                    int ii = (i + 1) % samples;
                    l = TessellationHelpers.QuadIndices(indices, l, 0, 2 * i, 2 * ii, 2 * ii + 1, 2 * i + 1);
                }

                o += 2 * samples;
            }

            var u1 = new int[samples];
            var u2 = new int[samples];
            if (shouldCap[0])
            {
                for (int i = 0; i < samples; i++)
                {
                    u1[i] = o + (samples - 1) - i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples);
                o += samples;
            }

            if (shouldCap[1])
            {
                for (int i = 0; i < samples; i++)
                {
                    u1[i] = o + i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples);
                o += samples;
            }

            Asserts.AssertEquals(nameof(l), l, nameof(triangles_n), triangles_n * 3);
            Asserts.AssertEquals(nameof(o), o, nameof(vertCount), vertCount);

            return new Mesh(vertices, normals, indices, error);
        }

        private static Mesh Tessellate(RvmSnout snout, float scale, float tolerance)
        {
            var radius_max = Math.Max(snout.RadiusBottom, snout.RadiusTop);
            var segments = TessellationHelpers.SagittaBasedSegmentCount(Math.PI * 2, radius_max, scale, tolerance);
            var samples = segments; // assumed to be closed

            var error = TessellationHelpers.SagittaBasedError(Math.PI * 2, radius_max, scale, segments);

            bool shell = true;
            bool[] cap = {true, true};
            for (var i = 0; i < 2; i++)
            {
                var con = snout.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (ConnectionInterface.DoInterfacesMatch(snout, con))
                    {
                        cap[i] = false;
                    }
                    else
                    {
                        //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0x00ffff);
                    }
                }
            }

            var t0 = new float[2 * samples];
            for (var i = 0; i < samples; i++)
            {
                t0[2 * i + 0] = (float)Math.Cos((Math.PI * 2 / samples) * i + snout.SampleStartAngle);
                t0[2 * i + 1] = (float)Math.Sin((Math.PI * 2 / samples) * i + snout.SampleStartAngle);
            }

            var t1 = new float[2 * samples];
            for (var i = 0; i < 2 * samples; i++)
            {
                t1[i] = snout.RadiusBottom * t0[i];
            }

            var t2 = new float[2 * samples];
            for (var i = 0; i < 2 * samples; i++)
            {
                t2[i] = snout.RadiusTop * t0[i];
            }

            float h2 = 0.5f * snout.Height;
            var l = 0;
            var ox = 0.5f * snout.OffsetX;
            var oy = 0.5f * snout.OffsetY;
            float[] mb = {(float)Math.Tan(snout.BottomShearX), (float)Math.Tan(snout.BottomShearX)};
            float[] mt = {(float)Math.Tan(snout.TopShearX), (float)Math.Tan(snout.TopShearY)};

            var vertices_n = (shell ? 2 * samples : 0) + (cap[0] ? samples : 0) + (cap[1] ? samples : 0);
            var vertices = new float[3 * vertices_n];
            var normals = new float[3 * vertices_n];

            var triangles_n = (shell ? 2 * samples : 0) + (cap[0] ? samples - 2 : 0) + (cap[1] ? samples - 2 : 0);
            var indices = new int[3 * triangles_n];

            if (shell)
            {
                for (var i = 0; i < samples; i++)
                {
                    float xb = t1[2 * i + 0] - ox;
                    float yb = t1[2 * i + 1] - oy;
                    float zb = -h2 + mb[0] * t1[2 * i + 0] + mb[1] * t1[2 * i + 1];

                    float xt = t2[2 * i + 0] + ox;
                    float yt = t2[2 * i + 1] + oy;
                    float zt = h2 + mt[0] * t2[2 * i + 0] + mt[1] * t2[2 * i + 1];

                    float s = (snout.OffsetX * t0[2 * i + 0] + snout.OffsetY * t0[2 * i + 1]);
                    float nx = t0[2 * i + 0];
                    float ny = t0[2 * i + 1];
                    float nz = Math.Abs(snout.Height) < 0.00001f
                        ? 0
                        : -(snout.RadiusTop - snout.RadiusBottom + s) / snout.Height;

                    l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz, xb, yb, zb);
                    l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz, xt, yt, zt);
                }
            }

            if (cap[0])
            {
                var nx = (float)(Math.Sin(snout.BottomShearX) * Math.Cos(snout.BottomShearY));
                var ny = (float)Math.Sin(snout.BottomShearY);
                var nz = (float)(-Math.Cos(snout.BottomShearX) * Math.Cos(snout.BottomShearY));
                for (var i = 0; cap[0] && i < samples; i++)
                {
                    l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz,
                        t1[2 * i + 0] - ox,
                        t1[2 * i + 1] - oy,
                        -h2 + mb[0] * t1[2 * i + 0] + mb[1] * t1[2 * i + 1]);
                }
            }

            if (cap[1])
            {
                var nx = (float)(-Math.Sin(snout.TopShearX) * Math.Cos(snout.TopShearY));
                var ny = (float)(-Math.Sin(snout.TopShearY));
                var nz = (float)(Math.Cos(snout.TopShearX) * Math.Cos(snout.TopShearY));
                for (var i = 0; i < samples; i++)
                {
                    l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz,
                        t2[2 * i + 0] + ox,
                        t2[2 * i + 1] + oy,
                        h2 + mt[0] * t2[2 * i + 0] + mt[1] * t2[2 * i + 1]);
                }
            }

            Asserts.AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", 3 * vertices_n);

            l = 0;
            var o = 0;
            if (shell)
            {
                for (var i = 0; i < samples; i++)
                {
                    var ii = (i + 1) % samples;
                    l = TessellationHelpers.QuadIndices(indices, l, 0, 2 * i, 2 * ii, 2 * ii + 1, 2 * i + 1);
                }

                o += 2 * samples;
            }


            var u1 = new int[samples];
            var u2 = new int[samples];
            if (cap[0])
            {
                for (var i = 0; i < samples; i++)
                {
                    u1[i] = o + (samples - 1) - i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples);
                o += samples;
            }

            if (cap[1])
            {
                for (var i = 0; i < samples; i++)
                {
                    u1[i] = o + i;
                }

                l = TessellateCircle(indices, l, u2, u1, samples);
                o += samples;
            }

            Asserts.AssertEquals(nameof(l), l, nameof(triangles_n) + " * 3", 3 * triangles_n);
            Asserts.AssertEquals(nameof(o), o, nameof(vertices_n), vertices_n);

            return new Mesh(vertices, normals, indices, error);
        }

        private static Mesh Tessellate(RvmPrimitive sphereBasedPrimitive, float radius, float arc, float shift_z,
            float scale_z, float scale, float tolerance)
        {
            var segments = TessellationHelpers.SagittaBasedSegmentCount(Math.PI * 2, radius, scale, tolerance);
            var samples = segments; // Assumed to be closed

            var error = TessellationHelpers.SagittaBasedError(Math.PI * 2, radius, scale, samples);

            bool is_sphere = false;
            if (Math.PI - 1e-3 <= arc)
            {
                arc = (float)Math.PI;
                is_sphere = true;
            }

            var min_rings = 3; // arc <= half_pi ? 2 : 3;
            var rings = (int)(Math.Max(min_rings, scale_z * samples * arc * (1.0f / Math.PI * 2)));


            var u0 = new int[rings];
            var t0 = new float[2 * rings];
            var theta_scale = arc / (rings - 1);
            for (var r = 0; r < rings; r++)
            {
                float theta = theta_scale * r;
                t0[2 * r + 0] = (float)Math.Cos(theta);
                t0[2 * r + 1] = (float)Math.Sin(theta);
                u0[r] = (int)(Math.Max(3.0f, t0[2 * r + 1] * samples)); // samples in this ring
            }

            u0[0] = 1;
            if (is_sphere)
            {
                u0[rings - 1] = 1;
            }

            var s = 0;
            for (var r = 0; r < rings; r++)
            {
                s += u0[r];
            }


            var vertices_n = s;
            var vertices = new float[3 * vertices_n];
            var normals = new float[3 * vertices_n];

            var l = 0;
            for (var r = 0; r < rings; r++)
            {
                var nz = t0[2 * r + 0];
                var z = radius * scale_z * nz + shift_z;
                var w = t0[2 * r + 1];
                var n = u0[r];

                var phi_scale = Math.PI * 2 / n;
                for (var i = 0; i < n; i++)
                {
                    var phi = (float)(phi_scale * i + sphereBasedPrimitive.SampleStartAngle);
                    var nx = (float)(w * Math.Cos(phi));
                    var ny = (float)(w * Math.Sin(phi));
                    l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz / scale_z, radius * nx, radius * ny, z);
                }
            }

            Asserts.AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", vertices_n * 3);

            var o_c = 0;
            var indices = new List<int>();
            for (var r = 0; r + 1 < rings; r++)
            {
                var n_c = u0[r];
                var n_n = u0[r + 1];
                var o_n = o_c + n_c;

                if (n_c < n_n)
                {
                    for (var i_n = 0; i_n < n_n; i_n++)
                    {
                        var ii_n = (i_n + 1);
                        var i_c = (n_c * (i_n + 1)) / n_n;
                        var ii_c = (n_c * (ii_n + 1)) / n_n;

                        i_c %= n_c;
                        ii_c %= n_c;
                        ii_n %= n_n;

                        if (i_c != ii_c)
                        {
                            indices.Add(o_c + i_c);
                            indices.Add(o_n + ii_n);
                            indices.Add(o_c + ii_c);
                        }

                        Asserts.AssertNotEquals(nameof(i_n), i_n, nameof(ii_n), ii_n);
                        indices.Add(o_c + i_c);
                        indices.Add(o_n + i_n);
                        indices.Add(o_n + ii_n);
                    }
                }
                else
                {
                    for (var i_c = 0; i_c < n_c; i_c++)
                    {
                        var ii_c = (i_c + 1);
                        var i_n = (n_n * (i_c + 0)) / n_c;
                        var ii_n = (n_n * (ii_c + 0)) / n_c;

                        i_n %= n_n;
                        ii_n %= n_n;
                        ii_c %= n_c;

                        Asserts.AssertNotEquals(nameof(i_c), i_c, nameof(ii_c), ii_c);
                        indices.Add(o_c + i_c);
                        indices.Add(o_n + ii_n);
                        indices.Add(o_c + ii_c);

                        if (i_n != ii_n)
                        {
                            indices.Add(o_c + i_c);
                            indices.Add(o_n + i_n);
                            indices.Add(o_n + ii_n);
                        }
                    }
                }

                o_c = o_n;
            }

            return new Mesh(vertices, normals, indices.ToArray(), error);
        }

        static int TessellateCircle(int[] indices, int l, int[] t, int[] src, int N)
        {
            while (3 <= N)
            {
                int m = 0;
                int i;
                for (i = 0; i + 2 < N; i += 2)
                {
                    indices[l++] = src[i];
                    indices[l++] = src[i + 1];
                    indices[l++] = src[i + 2];
                    t[m++] = src[i];
                }

                for (; i < N; i++)
                {
                    t[m++] = src[i];
                }

                N = m;

                // TODO: What does the swap do here.
                // Was: std::swap(t, src);
                Swap(ref t, ref src);
            }

            return l;
        }

        /// <summary>
        /// Copy of std::swap(t, src);
        /// Not sure if this is needed in dotnet.
        /// </summary>
        static void Swap<T>(ref T lhs, ref T rhs)
        {
            // ReSharper disable once JoinDeclarationAndInitializer
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}