using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace rvmsharp.Tessellator
{
    using RvmSharp.Primitives;

    public class TessellatorBridge
    {
        public static Mesh Tessellate(RvmPrimitive geometry, float scale, float tolerance)
        {
            return geometry switch
            {
                RvmBox box => Tessellate(box, scale),
                RvmFacetGroup facetGroup => Tessellate(facetGroup, scale),
                RvmPyramid pyramid => Tessellate(pyramid, scale),
                RvmRectangularTorus rectangularTorus => Tessellate(rectangularTorus, scale, tolerance),
                RvmCylinder cylinder => TessellateCylinder(cylinder, scale, tolerance),
                RvmCircularTorus circularTorus => Tessellate(circularTorus, scale, tolerance),
                /*RvmLine line => throw new NotImplementedException(),
                RvmSnout snout => throw new NotImplementedException(),
                RvmSphere sphere => throw new NotImplementedException(),
                RvmSphericalDish sphericalDish => throw new NotImplementedException(),
                RvmEllipticalDish ellipticalDish => throw new NotImplementedException(),*/
                _ => null
                //_ => throw new NotImplementedException($"Unsupported type for tesselation: {geometry?.Kind}"),
            };
        }

        private static int triIndices(int[] indices, int l, int o, int v0, int v1, int v2)
        {
            indices[l++] = o + v0;
            indices[l++] = o + v1;
            indices[l++] = o + v2;
            return l;
        }


        private static int quadIndices(int[] indices, int l, int o, int v0, int v1, int v2, int v3)
        {
            indices[l++] = o + v0;
            indices[l++] = o + v1;
            indices[l++] = o + v2;

            indices[l++] = o + v2;
            indices[l++] = o + v3;
            indices[l++] = o + v0;
            return l;
        }

        private static int vertex(Vector3[] normals, Vector3[] vertices, int l, Vector3 normal, Vector3 point)
        {
            normals[l] = new Vector3(normal.X, normal.Y, normal.Z);
            vertices[l] = new Vector3(point.X, point.Y, point.Z);
            return ++l;
        }


        private static int vertex(Vector3[] normals, Vector3[] vertices, int l, float nx, float ny, float nz, float px,
            float py, float pz)
        {
            normals[l] = new Vector3(nx, ny, nz);
            vertices[l] = new Vector3(px, py, pz);
            return ++l;
        }

        private static int vertex(float[] normals, float[] vertices, int l, Vector3 normal, Vector3 point)
        {
            normals[l] = normal.X;
            vertices[l++] = point.X;
            normals[l] = normal.Y;
            vertices[l++] = point.Y;
            normals[l] = normal.Z;
            vertices[l++] = point.Z;
            return l;
        }

        private static int vertex(float[] normals, float[] vertices, int l, float nx, float ny, float nz, float px,
            float py, float pz)
        {
            normals[l] = nx;
            vertices[l++] = px;
            normals[l] = ny;
            vertices[l++] = py;
            normals[l] = nz;
            vertices[l++] = pz;
            return l;
        }

        private const int minSamples = 3;
        private const int maxSamples = 100;
        private const float MinimumThreshold = 1e-7f;


        private static int sagittaBasedSegmentCount(double arc, float radius, float scale, float tolerance)
        {
            var samples = arc / Math.Acos(Math.Max(-1.0f, 1.0f - tolerance / (scale * radius)));
            return Math.Min(maxSamples, (int)(Math.Max(minSamples, Math.Ceiling(samples))));
        }


        private static float sagittaBasedError(double arc, float radius, float scale, int segments)
        {
            var s = scale * radius * (1.0f - Math.Cos(arc / segments)); // Length of sagitta
            //assert(s <= tolerance);
            return (float)s;
        }


        private class Interface
        {
            public enum Kind
            {
                Undefined,
                Square,
                Circular
            }

            public Kind kind = Kind.Undefined;
            public Vector3[] p = new Vector3[4];
            public float radius;
        }

        private static Interface GetInterface(RvmPrimitive geo, int o)
        {
            var iface = new Interface();
            var connection = geo.Connections[o];
            var ix = connection.p1 == geo ? 1 : 0;
            if (!Matrix4x4.Decompose(geo.Matrix, out var vscale, out var _, out var _))
                throw new Exception();
            var scale = Math.Max(vscale.X, Math.Max(vscale.Y, vscale.Z));
            switch (geo)
            {
                case RvmPyramid pyramid:
                {
                    var bx = 0.5f * pyramid.BottomX;
                    var by = 0.5f * pyramid.BottomY;
                    var tx = 0.5f * pyramid.TopX;
                    var ty = 0.5f * pyramid.TopY;
                    var ox = 0.5f * pyramid.OffsetX;
                    var oy = 0.5f * pyramid.OffsetY;
                    var h2 = 0.5f * pyramid.Height;
                    Vector3[,] quad = new Vector3[,]
                    {
                        {
                            new Vector3(-bx - ox, -by - oy, -h2), new Vector3(bx - ox, -by - oy, -h2),
                            new Vector3(bx - ox, by - oy, -h2), new Vector3(-bx - ox, by - oy, -h2)
                        },
                        {
                            new Vector3(-tx + ox, -ty + oy, h2), new Vector3(tx + ox, -ty + oy, h2),
                            new Vector3(tx + ox, ty + oy, h2), new Vector3(-tx + ox, ty + oy, h2)
                        },
                    };

                    iface.kind = Interface.Kind.Square;
                    if (o < 4)
                    {
                        var oo = (o + 1) & 3;
                        iface.p[0] = Vector3.Transform(quad[0, o], geo.Matrix);
                        iface.p[1] = Vector3.Transform(quad[0, oo], geo.Matrix);
                        iface.p[2] = Vector3.Transform(quad[1, oo], geo.Matrix);
                        iface.p[3] = Vector3.Transform(quad[1, o], geo.Matrix);
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++) iface.p[k] = Vector3.Transform(quad[o - 4, k], geo.Matrix);
                    }

                    break;
                }
                case RvmBox box:
                {
                    var xp = 0.5f * box.LengthX;
                    var xm = -xp;
                    var yp = 0.5f * box.LengthY;
                    var ym = -yp;
                    var zp = 0.5f * box.LengthZ;
                    var zm = -zp;
                    Vector3[,] V =
                    {
                        {
                            new Vector3(xm, ym, zp), new Vector3(xm, yp, zp), new Vector3(xm, yp, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xp, ym, zm), new Vector3(xp, yp, zm), new Vector3(xp, yp, zp),
                            new Vector3(xp, ym, zp)
                        },
                        {
                            new Vector3(xp, ym, zm), new Vector3(xp, ym, zp), new Vector3(xm, ym, zp),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, yp, zm), new Vector3(xm, yp, zp), new Vector3(xp, yp, zp),
                            new Vector3(xp, yp, zm)
                        },
                        {
                            new Vector3(xm, yp, zm), new Vector3(xp, yp, zm), new Vector3(xp, ym, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, ym, zp), new Vector3(xp, ym, zp), new Vector3(xp, yp, zp),
                            new Vector3(xm, yp, zp)
                        }
                    };
                    for (var k = 0; k < 4; k++) iface.p[k] = Vector3.Transform(V[o, k], geo.Matrix);
                    break;
                }
                case RvmRectangularTorus tor:
                {
                    var h2 = 0.5f * tor.Height;
                    float[,] square =
                    {
                        {tor.RadiusOuter, -h2}, {tor.RadiusInner, -h2}, {tor.RadiusInner, h2},
                        {tor.RadiusOuter, h2},
                    };
                    if (o == 0)
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            iface.p[k] = Vector3.Transform(new Vector3(square[k, 0], 0.0f, square[k, 1]), geo.Matrix);
                        }
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            iface.p[k] = Vector3.Transform(new Vector3((float)(square[k, 0] * Math.Cos(tor.Angle)),
                                (float)(square[k, 0] * Math.Sin(tor.Angle)),
                                square[k, 1]), geo.Matrix);
                        }
                    }

                    break;
                }
                case RvmCircularTorus circularTorus:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * circularTorus.Radius;
                    break;

                case RvmEllipticalDish ellipticalDish:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * ellipticalDish.BaseRadius;
                    break;

                case RvmSphericalDish sphericalDish:
                {
                    float r_circ = sphericalDish.BaseRadius;
                    var h = sphericalDish.Height;
                    float r_sphere = (r_circ * r_circ + h * h) / (2.0f * h);
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * r_sphere;
                    break;
                }
                case RvmSnout snout:
                    iface.kind = Interface.Kind.Circular;
                    var offset = ix == 0 ? connection.OffsetX : connection.OffsetY;
                    iface.radius = scale * (offset == 0 ? snout.RadiusBottom : snout.RadiusTop);
                    break;
                case RvmCylinder cylinder:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * cylinder.Radius;
                    break;
                case RvmSphere:
                case RvmLine:
                case RvmFacetGroup:
                    iface.kind = Interface.Kind.Undefined;
                    break;
                default:
                    throw new NotSupportedException("Unhandled primitive type");
            }

            return iface;
        }

        private static bool DoInterfacesMatch(RvmPrimitive geo, RvmConnection con)
        {
            bool isFirst = geo == con.p1;

            var thisGeo = isFirst ? con.p1 : con.p2;
            var thisOffset = isFirst ? con.OffsetX : con.OffsetY;
            var thisIFace = GetInterface(thisGeo, (int)thisOffset);

            var thatGeo = isFirst ? con.p2 : con.p1;
            var thatOffset = isFirst ? con.OffsetY : con.OffsetX;
            var thatIFace = GetInterface(thatGeo, (int)thatOffset);


            if (thisIFace.kind != thatIFace.kind) return false;

            if (thisIFace.kind == Interface.Kind.Circular)
            {
                return thisIFace.radius <= 1.05f * thatIFace.radius;
            }
            else
            {
                for (var j = 0; j < 4; j++)
                {
                    bool found = false;
                    for (var i = 0; i < 4; i++)
                    {
                        if (Vector3.DistanceSquared(thisIFace.p[j], thatIFace.p[i]) < 0.001f * 0.001f)
                        {
                            found = true;
                        }
                    }

                    if (!found) return false;
                }

                return true;
            }
        }

        private static Mesh Tessellate(RvmPyramid pyramid, float scale)
        {
            var bx = 0.5f * pyramid.BottomX;
            var by = 0.5f * pyramid.BottomY;
            var tx = 0.5f * pyramid.TopX;
            var ty = 0.5f * pyramid.TopY;
            var ox = 0.5f * pyramid.OffsetX;
            var oy = 0.5f * pyramid.OffsetY;
            var h2 = 0.5f * pyramid.Height;


            Vector3[,] quad =
            {
                {
                    new Vector3(-bx - ox, -by - oy, -h2), new Vector3(bx - ox, -by - oy, -h2),
                    new Vector3(bx - ox, by - oy, -h2), new Vector3(-bx - ox, by - oy, -h2)
                },
                {
                    new Vector3(-tx + ox, -ty + oy, h2), new Vector3(tx + ox, -ty + oy, h2),
                    new Vector3(tx + ox, ty + oy, h2), new Vector3(-tx + ox, ty + oy, h2)
                },
            };

            Vector3[] n =
            {
                new Vector3(0.0f, -h2, (quad[1, 0].Y - quad[0, 0].Y)),
                new Vector3(h2, 0.0f, -(quad[1, 1].X - quad[0, 1].X)),
                new Vector3(0.0f, h2, -(quad[1, 2].Y - quad[0, 2].Y)),
                new Vector3(-h2, 0.0f, (quad[1, 3].X - quad[0, 3].X)), new Vector3(0, 0, -1), new Vector3(0, 0, 1),
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

                if (DoInterfacesMatch(pyramid, con))
                {
                    cap[i] = false;
                }
            }

            var caps = 0;
            for (var i = 0; i < 6; i++)
                if (cap[i])
                    caps++;

            var error = 0.0f;

            var vertices = new float[3 * 4 * caps];
            var normals = new float[3 * 4 * caps];

            var l = 0;
            for (var i = 0; i < 4; i++)
            {
                if (cap[i] == false) continue;
                var ii = (i + 1) & 3;
                l = vertex(normals, vertices, l, n[i], quad[0, i]);
                l = vertex(normals, vertices, l, n[i], quad[0, ii]);
                l = vertex(normals, vertices, l, n[i], quad[1, ii]);
                l = vertex(normals, vertices, l, n[i], quad[1, i]);
            }

            if (cap[4])
            {
                for (var i = 0; i < 4; i++)
                {
                    l = vertex(normals, vertices, l, n[4], quad[0, i]);
                }
            }

            if (cap[5])
            {
                for (var i = 0; i < 4; i++)
                {
                    l = vertex(normals, vertices, l, n[5], quad[1, i]);
                }
            }

            if (l != vertices.Length)
                throw new Exception("Missing vertices");

            l = 0;
            var o = 0;
            var indices = new int[3 * 2 * caps];
            for (var i = 0; i < 4; i++)
            {
                if (cap[i] == false) continue;
                l = quadIndices(indices, l, o /*4 * i*/, 0, 1, 2, 3);
                o += 4;
            }

            if (cap[4])
            {
                l = quadIndices(indices, l, o, 3, 2, 1, 0);
                o += 4;
            }

            if (cap[5])
            {
                l = quadIndices(indices, l, o, 0, 1, 2, 3);
                o += 4;
            }

            if (l != 3 * 2 * caps || o != vertices.Length / 3)
                throw new Exception();

            return new Mesh(vertices, normals, indices, error);
        }


        private static Mesh Tessellate(RvmRectangularTorus rectangularTorus, float scale, float tolerance)
        {
            var segments =
                sagittaBasedSegmentCount(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, tolerance);
            var samples = segments + 1; // Assumed to be open, add extra sample.

            var error = sagittaBasedError(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, segments);

            bool shell = true;
            bool[] cap = {true, true};

            for (var i = 0; i < 2; i++)
            {
                var con = rectangularTorus.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasRectangularSide)
                {
                    if (DoInterfacesMatch(rectangularTorus, con))
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
            var segments_l = sagittaBasedSegmentCount(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius,
                scale, tolerance); // large radius, toroidal direction
            var segments_s =
                sagittaBasedSegmentCount(Math.PI * 2, circularTorus.Radius, scale,
                    tolerance); // small radius, poloidal direction

            var error = Math.Max(
                sagittaBasedError(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius, scale, segments_l),
                sagittaBasedError(Math.PI * 2, circularTorus.Radius, scale, segments_s));

            var samples_l = segments_l + 1; // Assumed to be open, add extra sample
            var samples_s = segments_s; // Assumed to be closed

            bool shell = true;
            bool[] cap = {true, true};
            for (var i = 0; i < 2; i++)
            {
                var con = circularTorus.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (DoInterfacesMatch(circularTorus, con))
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

            AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", 3 * vertices_n);

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

            AssertEquals(nameof(l), l, nameof(triangles_n) + " * 3", 3 * triangles_n);
            AssertEquals(nameof(o), o, nameof(vertices_n), vertices_n);

            return new Mesh(vertices, normals, indices, error);
        }


        private static Mesh Tessellate(RvmBox box, float scale)
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

                if (DoInterfacesMatch(box, con))
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
                        i_v = vertex(normals, vertices, i_v, N[f], V[f, i]);
                    }

                    i_p = quadIndices(indices, i_p, o, 0, 1, 2, 3);

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

        private static Mesh Tessellate(RvmFacetGroup facetGroup, float scale)
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
                int counter_count = poly.Contours.Length;

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

            int segments = sagittaBasedSegmentCount(Math.PI * 2, cylinder.Radius, scale, tolerance);
            int samples = segments; // Assumed to be closed

            var error = sagittaBasedError(Math.PI * 2, cylinder.Radius, scale, segments);

            bool shell = true;
            bool[] shouldCap = {true, true};


            for (int i = 0; i < 2; i++)
            {
                var con = cylinder.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (DoInterfacesMatch(cylinder, con))
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
                t0[2 * i + 0] = (float)Math.Cos(((Math.Tau) / samples) * i + cylinder.SampleStartAngle);
                t0[2 * i + 1] = (float)Math.Sin((Math.Tau / samples) * i + cylinder.SampleStartAngle);
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
                    l = vertex(normals, vertices, l, t0[2 * i + 0], t0[2 * i + 1], 0, t1[2 * i + 0], t1[2 * i + 1],
                        -h2);
                    l = vertex(normals, vertices, l, t0[2 * i + 0], t0[2 * i + 1], 0, t1[2 * i + 0], t1[2 * i + 1], h2);
                }
            }

            if (shouldCap[0])
            {
                for (int i = 0; i < samples; i++)
                {
                    l = vertex(normals, vertices, l, new Vector3(0, 0, -1),
                        new Vector3(t1[2 * i + 0], t1[2 * i + 1], -h2));
                }
            }

            if (shouldCap[1])
            {
                for (int i = 0; i < samples; i++)
                {
                    l = vertex(normals, vertices, l, new Vector3(0, 0, 1),
                        new Vector3(t1[2 * i + 0], t1[2 * i + 1], h2));
                }
            }

            AssertEquals(nameof(l), l, nameof(vertCount), vertCount);

            l = 0;
            int o = 0;
            if (shell)
            {
                for (int i = 0; i < samples; i++)
                {
                    int ii = (i + 1) % samples;
                    l = quadIndices(indices, l, 0, 2 * i, 2 * ii, 2 * ii + 1, 2 * i + 1);
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

            AssertEquals(nameof(l), l, nameof(triangles_n), triangles_n * 3);
            AssertEquals(nameof(o), o, nameof(vertCount), vertCount);

            return new Mesh(vertices, normals, indices, error);
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

        private static void AssertEquals<T>(string name1, T value1, string name2, T value2) where T : IEquatable<T>
        {
            if ((value1?.Equals(value2) == true))
                return;

            throw new Exception($"Expected {name1} {value1} to equal {name2} {value2}.");
        }
    }
}