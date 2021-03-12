using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace RvmSharp.Tessellation
{
    using Containers;
    using Primitives;

    public class TessellatorBridge
    {
        private const int MinSamples = 3;
        private const int MaxSamples = 100;
        private const float MinimumThreshold = 1e-7f;

        public static Mesh[] Tessellate(RvmNode group, float tolerance)
        {   
            var meshes = group.Children.OfType<RvmPrimitive>().Select(p =>
            {
                if (!Matrix4x4.Decompose(p.Matrix, out var scale, out var rotation, out var translation))
                {
                    throw new InvalidOperationException($"Could not decompose matrix for {@group.Name}");
                }

                var scaleScalar = MathF.Max(scale.X, MathF.Max(scale.Y, scale.Z));
                var mesh = TessellatorBridge.Tessellate(p, scaleScalar, tolerance);
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
                    return Tessellate(box, scale);
                case RvmFacetGroup facetGroup:
                    return Tessellate(facetGroup, scale);
                case RvmPyramid pyramid:
                    return Tessellate(pyramid, scale);
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
                    return Tessellate(sphere, 0.5f * sphere.Diameter, MathF.PI, 0.0f, 1.0f, scale, tolerance);
                case RvmEllipticalDish ellipticalDish:
                    return Tessellate(ellipticalDish, ellipticalDish.BaseRadius, MathF.PI / 2, 0.0f,
                        ellipticalDish.Height / ellipticalDish.BaseRadius, scale, tolerance);
                case RvmSphericalDish sphericalDish:
                {
                    float r_circ = sphericalDish.BaseRadius;
                    var h = sphericalDish.Height;
                    float r_sphere = (r_circ * r_circ + h * h) / (2.0f * h);
                    float sinval = MathF.Min(1.0f, MathF.Max(-1.0f, r_circ / r_sphere));
                    float arc = MathF.Asin(sinval);
                    if (r_circ < h) { arc = MathF.PI - arc; }

                    return Tessellate(sphericalDish, r_sphere, arc, h - r_sphere, 1.0f, scale, tolerance);
                }
                default:
                    throw new NotImplementedException($"Unsupported type for tesselation: {geometry?.Kind}");
            }

            ;
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

        private static int SagittaBasedSegmentCount(double arc, float radius, float scale, float tolerance)
        {
            var samples = arc / Math.Acos(Math.Max(-1.0f, 1.0f - tolerance / (scale * radius)));
            return Math.Min(MaxSamples, (int)(Math.Max(MinSamples, Math.Ceiling(samples))));
        }

        private static float SagittaBasedError(double arc, float radius, float scale, int segments)
        {
            var s = scale * radius * (1.0f - Math.Cos(arc / segments)); // Length of sagitta
            //assert(s <= tolerance);
            return (float)s;
        }


        private class ConnectionInterface
        {
            public enum Type
            {
                Undefined,
                Square,
                Circular
            }

            public Type InterfaceType = Type.Undefined;
            public readonly Vector3[] SquareConnectionPoints = new Vector3[4];
            public float CircularRadius;
        }

        private static ConnectionInterface GetInterface(RvmPrimitive geo, int o)
        {
            var iface = new ConnectionInterface();
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
                    Vector3[,] quad = {
                        {
                            new Vector3(-bx - ox, -by - oy, -h2), new Vector3(bx - ox, -by - oy, -h2),
                            new Vector3(bx - ox, by - oy, -h2), new Vector3(-bx - ox, by - oy, -h2)
                        },
                        {
                            new Vector3(-tx + ox, -ty + oy, h2), new Vector3(tx + ox, -ty + oy, h2),
                            new Vector3(tx + ox, ty + oy, h2), new Vector3(-tx + ox, ty + oy, h2)
                        },
                    };

                    iface.InterfaceType = ConnectionInterface.Type.Square;
                    if (o < 4)
                    {
                        var oo = (o + 1) & 3;
                        iface.SquareConnectionPoints[0] = Vector3.Transform(quad[0, o], geo.Matrix);
                        iface.SquareConnectionPoints[1] = Vector3.Transform(quad[0, oo], geo.Matrix);
                        iface.SquareConnectionPoints[2] = Vector3.Transform(quad[1, oo], geo.Matrix);
                        iface.SquareConnectionPoints[3] = Vector3.Transform(quad[1, o], geo.Matrix);
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++) iface.SquareConnectionPoints[k] = Vector3.Transform(quad[o - 4, k], geo.Matrix);
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
                    for (var k = 0; k < 4; k++) iface.SquareConnectionPoints[k] = Vector3.Transform(V[o, k], geo.Matrix);
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
                            iface.SquareConnectionPoints[k] = Vector3.Transform(new Vector3(square[k, 0], 0.0f, square[k, 1]), geo.Matrix);
                        }
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            iface.SquareConnectionPoints[k] = Vector3.Transform(new Vector3((float)(square[k, 0] * Math.Cos(tor.Angle)),
                                (float)(square[k, 0] * Math.Sin(tor.Angle)),
                                square[k, 1]), geo.Matrix);
                        }
                    }

                    break;
                }
                case RvmCircularTorus circularTorus:
                    iface.InterfaceType = ConnectionInterface.Type.Circular;
                    iface.CircularRadius = scale * circularTorus.Radius;
                    break;

                case RvmEllipticalDish ellipticalDish:
                    iface.InterfaceType = ConnectionInterface.Type.Circular;
                    iface.CircularRadius = scale * ellipticalDish.BaseRadius;
                    break;

                case RvmSphericalDish sphericalDish:
                {
                    float r_circ = sphericalDish.BaseRadius;
                    var h = sphericalDish.Height;
                    float r_sphere = (r_circ * r_circ + h * h) / (2.0f * h);
                    iface.InterfaceType = ConnectionInterface.Type.Circular;
                    iface.CircularRadius = scale * r_sphere;
                    break;
                }
                case RvmSnout snout:
                    iface.InterfaceType = ConnectionInterface.Type.Circular;
                    var offset = ix == 0 ? connection.OffsetX : connection.OffsetY;
                    iface.CircularRadius = scale * (offset == 0 ? snout.RadiusBottom : snout.RadiusTop);
                    break;
                case RvmCylinder cylinder:
                    iface.InterfaceType = ConnectionInterface.Type.Circular;
                    iface.CircularRadius = scale * cylinder.Radius;
                    break;
                case RvmSphere:
                case RvmLine:
                case RvmFacetGroup:
                    iface.InterfaceType = ConnectionInterface.Type.Undefined;
                    break;
                default:
                    throw new NotSupportedException("Unhandled primitive type");
            }

            return iface;
        }

        private static bool DoInterfacesMatch(RvmPrimitive primitive, RvmConnection connection)
        {
            bool isFirst = primitive == connection.p1;

            var thisGeo = isFirst ? connection.p1 : connection.p2;
            var thisOffset = isFirst ? connection.OffsetX : connection.OffsetY;
            var thisIFace = GetInterface(thisGeo, (int)thisOffset);

            var thatGeo = isFirst ? connection.p2 : connection.p1;
            var thatOffset = isFirst ? connection.OffsetY : connection.OffsetX;
            var thatIFace = GetInterface(thatGeo, (int)thatOffset);


            if (thisIFace.InterfaceType != thatIFace.InterfaceType) 
                return false;

            if (thisIFace.InterfaceType == ConnectionInterface.Type.Circular)
                return thisIFace.CircularRadius <= 1.05f * thatIFace.CircularRadius;
            
            for (var j = 0; j < 4; j++)
            {
                bool found = false;
                for (var i = 0; i < 4; i++)
                {
                    if (Vector3.DistanceSquared(thisIFace.SquareConnectionPoints[j], thatIFace.SquareConnectionPoints[i]) < 0.001f * 0.001f)
                    {
                        found = true;
                    }
                }

                if (!found) 
                    return false;
            }

            return true;
        }

        private static Mesh Tessellate(RvmPyramid pyramid, float scale)
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
                SagittaBasedSegmentCount(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, tolerance);
            var samples = segments + 1; // Assumed to be open, add extra sample.

            var error = SagittaBasedError(rectangularTorus.Angle, rectangularTorus.RadiusOuter, scale, segments);

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
            var segments_l = SagittaBasedSegmentCount(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius,
                scale, tolerance); // large radius, toroidal direction
            var segments_s =
                SagittaBasedSegmentCount(Math.PI * 2, circularTorus.Radius, scale,
                    tolerance); // small radius, poloidal direction

            var error = Math.Max(
                SagittaBasedError(circularTorus.Angle, circularTorus.Offset + circularTorus.Radius, scale, segments_l),
                SagittaBasedError(Math.PI * 2, circularTorus.Radius, scale, segments_s));

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

            int segments = SagittaBasedSegmentCount(Math.PI * 2, cylinder.Radius, scale, tolerance);
            int samples = segments; // Assumed to be closed

            var error = SagittaBasedError(Math.PI * 2, cylinder.Radius, scale, segments);

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

        private static Mesh Tessellate(RvmSnout snout, float scale, float tolerance)
        {
            var radius_max = Math.Max(snout.RadiusBottom, snout.RadiusTop);
            var segments = SagittaBasedSegmentCount(Math.PI * 2, radius_max, scale, tolerance);
            var samples = segments; // assumed to be closed

            var error = SagittaBasedError(Math.PI * 2, radius_max, scale, segments);

            bool shell = true;
            bool[] cap = {true, true};
            float[] radii = {snout.RadiusBottom, snout.RadiusTop};
            for (var i = 0; i < 2; i++)
            {
                var con = snout.Connections[i];
                if (con != null && con.flags == RvmConnection.Flags.HasCircularSide)
                {
                    if (DoInterfacesMatch(snout, con))
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

                    l = vertex(normals, vertices, l, nx, ny, nz, xb, yb, zb);
                    l = vertex(normals, vertices, l, nx, ny, nz, xt, yt, zt);
                }
            }

            if (cap[0])
            {
                var nx = (float)(Math.Sin(snout.BottomShearX) * Math.Cos(snout.BottomShearY));
                var ny = (float)Math.Sin(snout.BottomShearY);
                var nz = (float)(-Math.Cos(snout.BottomShearX) * Math.Cos(snout.BottomShearY));
                for (var i = 0; cap[0] && i < samples; i++)
                {
                    l = vertex(normals, vertices, l, nx, ny, nz,
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
                    l = vertex(normals, vertices, l, nx, ny, nz,
                        t2[2 * i + 0] + ox,
                        t2[2 * i + 1] + oy,
                        h2 + mt[0] * t2[2 * i + 0] + mt[1] * t2[2 * i + 1]);
                }
            }

            AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", 3 * vertices_n);

            l = 0;
            var o = 0;
            if (shell)
            {
                for (var i = 0; i < samples; i++)
                {
                    var ii = (i + 1) % samples;
                    l = quadIndices(indices, l, 0, 2 * i, 2 * ii, 2 * ii + 1, 2 * i + 1);
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

            AssertEquals(nameof(l), l, nameof(triangles_n) + " * 3", 3 * triangles_n);
            AssertEquals(nameof(o), o, nameof(vertices_n), vertices_n);

            return new Mesh(vertices, normals, indices, error);
        }

        private static Mesh Tessellate(RvmPrimitive sphereBasedPrimitive, float radius, float arc, float shift_z,
            float scale_z, float scale, float tolerance)
        {
            var segments = SagittaBasedSegmentCount(Math.PI * 2, radius, scale, tolerance);
            var samples = segments; // Assumed to be closed

            var error = SagittaBasedError(Math.PI * 2, radius, scale, samples);

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
                    l = vertex(normals, vertices, l, nx, ny, nz / scale_z, radius * nx, radius * ny, z);
                }
            }

            AssertEquals(nameof(l), l, nameof(vertices_n) + " * 3", vertices_n * 3);

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

                        AssertNotEquals(nameof(i_n), i_n, nameof(ii_n), ii_n);
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

                        AssertNotEquals(nameof(i_c), i_c, nameof(ii_c), ii_c);
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

        private static void AssertEquals<T>(string name1, T value1, string name2, T value2) where T : IEquatable<T>
        {
            if ((value1?.Equals(value2) == true))
                return;

            throw new Exception($"Expected {name1} {value1} to equal {name2} {value2}.");
        }

        private static void AssertNotEquals<T>(string name1, T value1, string name2, T value2) where T : IEquatable<T>
        {
            if ((value1?.Equals(value2) != true))
                return;

            throw new Exception($"Expected {name1} {value1} to equal {name2} {value2}.");
        }
    }
}