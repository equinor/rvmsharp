using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace rvmsharp.Tessellator
{
    using RvmSharp.Containers;
    using RvmSharp.Primitives;

    public class RvmConnect
    {
        class Anchor
        {
            public RvmPrimitive geo;
            public Vector3 p;
            public Vector3 d;
            public uint o;
            public RvmConnection.Flags flags;
            public bool matched;
        };

        class Context
        {
            public RvmStore store;
            public readonly List<Anchor> anchors = new List<Anchor>();
            public const float epsilon = 0.001f;
            public int AnchorsN => anchors.Count;

            public uint anchors_total;
            public uint anchors_matched;
        };

        private static void Connect(Context context, int off)
        {
            var aN = context.AnchorsN;
            var e = Context.epsilon;
            var ee = e * e;
            if (off > aN)
                throw new ArgumentException();

            var a = context.anchors.Skip(off).OrderBy(e => e.p.X).ToArray();


            for (int j = 0; j < a.Length; j++)
            {
                if (a[j].matched) continue;

                for (int i = j + 1; i < a.Length && a[i].p.X <= a[j].p.X + e; i++)
                {
                    bool canMatch = a[i].matched == false;
                    bool close = Vector3.DistanceSquared(a[j].p, a[i].p) <= ee;
                    bool aligned = Vector3.Dot(a[j].d, a[i].d) < -0.98f;

                    if (j + off == 120 && i + off == 125)
                    {
                        var h = 5;
                    }

                    if (canMatch && close && aligned)
                    {
                        RvmConnection connection = new RvmConnection();
                        context.store.Connections.Add(connection);
                        connection.p1 = a[j].geo;
                        connection.p2 = a[i].geo;
                        connection.OffsetX = a[j].o;
                        connection.OffsetY = a[i].o;
                        connection.p = a[j].p;
                        connection.d = a[j].d;
                        connection.flags = RvmConnection.Flags.None;
                        connection.AddFlag(a[i].flags);
                        connection.AddFlag(a[j].flags);

                        a[j].geo.Connections[a[j].o] = connection;
                        a[i].geo.Connections[a[i].o] = connection;

                        a[j].matched = true;
                        a[i].matched = true;
                        context.anchors_matched += 2;


                        //context->store->addDebugLine((a[j].p + 0.03f*a[j].d).data,
                        //                             (a[i].p + 0.03f*a[i].d).data,
                        //                             0x0000ff);
                    }
                }
            }

            // Remove matched anchors.
            for (var j = off; j < context.anchors.Count;)
            {
                if (context.anchors[j].matched)
                {
                    context.anchors.RemoveAt(j);
                }
                else
                {
                    j++;
                }
            }

            if (off > context.anchors.Count)
                throw new Exception("Something went wrong");
        }

        private static void AddAnchor(Context context, RvmPrimitive geo, Vector3 p, Vector3 d, uint o,
            RvmConnection.Flags flags)
        {
            Anchor a = new Anchor();
            a.geo = geo;
            a.p = Vector3.Transform(p, geo.Matrix);
            a.d = Vector3.Normalize(Vector3.TransformNormal(d, geo.Matrix));
            a.o = o;
            a.flags = flags;
            //Console.WriteLine($"add {a.p.X:0.00000} {a.p.Y:0.00000} {a.p.Z:0.00000}");

            context.anchors.Add(a);
            context.anchors_total++;
        }

        private static void Recurse(Context context, RvmGroup group)
        {
            var offset = context.AnchorsN;
            foreach (var child in group.Children)
                Recurse(context, child);

            foreach (var prim in group.Primitives)
            {
                switch (prim)
                {
                    case RvmPyramid pyramid:
                    {
                        var b = 0.5f * new Vector2(pyramid.BottomX, pyramid.BottomY);
                        var t = 0.5f * new Vector2(pyramid.TopX, pyramid.TopY);
                        var m = 0.5f * (b + t);
                        var o = 0.5f * new Vector2(pyramid.OffsetX, pyramid.OffsetY);

                        var h = 0.5f * pyramid.Height;

                        var M = prim.Matrix;

                        Vector3[] n =
                        {
                            new Vector3(0.0f, -h, (-t.Y + o.Y) - (-b.Y - o.Y)),
                            new Vector3(h, 0.0f, -((t.X + o.X) - (b.X - o.X))),
                            new Vector3(0.0f, h, -((t.Y + o.Y) - (b.Y - o.Y))),
                            new Vector3(-h, 0.0f, (-t.X + o.X) - (-b.X - o.X)), new Vector3(0.0f, 0.0f, -1.0f),
                            new Vector3(0.0f, 0.0f, 1.0f)
                        };
                        Vector3[] p =
                        {
                            new Vector3(0.0f, -m.Y, 0.0f), new Vector3(m.X, 0.0f, 0.0f),
                            new Vector3(0.0f, m.Y, 0.0f), new Vector3(-m.X, 0.0f, 0.0f),
                            new Vector3(-o.X, -o.Y, -h), new Vector3(o.X, o.Y, h)
                        };
                        for (uint i = 0; i < 6; i++)
                        {
                            AddAnchor(context, pyramid, p[i], n[i], i, RvmConnection.Flags.HasRectangularSide);
                        }

                        break;
                    }
                    case RvmBox box:
                    {
                        Vector3[] n =
                        {
                            new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, -1, 0),
                            new Vector3(0, 1, 0), new Vector3(0, 0, -1), new Vector3(0, 0, 1)
                        };
                        var xp = 0.5f * box.LengthX;
                        var xm = -xp;
                        var yp = 0.5f * box.LengthY;
                        var ym = -yp;
                        var zp = 0.5f * box.LengthZ;
                        var zm = -zp;
                        Vector3[] p =
                        {
                            new Vector3(xm, 0.0f, 0.0f), new Vector3(xp, 0.0f, 0.0f), new Vector3(0.0f, ym, 0.0f),
                            new Vector3(0.0f, yp, 0.0f), new Vector3(0.0f, 0.0f, zm), new Vector3(0.0f, 0.0f, zp)
                        };
                        for (uint i = 0; i < 6; i++)
                            AddAnchor(context, box, p[i], n[i], i, RvmConnection.Flags.HasRectangularSide);
                        break;
                    }
                    case RvmRectangularTorus rt:
                    {
                        var c = (float)Math.Cos(rt.Angle);
                        var s = (float)Math.Sin(rt.Angle);
                        var m = 0.5f * (rt.RadiusInner + rt.RadiusOuter);
                        Vector3[] n = {new Vector3(0, -1, 0.0f), new Vector3(-s, c, 0.0f)};
                        Vector3[] p = {new Vector3(rt.RadiusInner, 0, 0.0f), new Vector3(m * c, m * s, 0.0f)};
                        for (uint i = 0; i < 2; i++)
                            AddAnchor(context, rt, p[i], n[i], i, RvmConnection.Flags.HasRectangularSide);
                        break;
                    }

                    case RvmCircularTorus ct:
                    {
                        var c = (float)Math.Cos(ct.Angle);
                        var s = (float)Math.Sin(ct.Angle);
                        Vector3[] n = {new Vector3(0, -1, 0.0f), new Vector3(-s, c, 0.0f)};
                        Vector3[] p =
                        {
                            new Vector3(ct.Offset, 0, 0.0f), new Vector3(ct.Offset * c, ct.Offset * s, 0.0f)
                        };
                        for (uint i = 0; i < 2; i++)
                            AddAnchor(context, ct, p[i], n[i], i, RvmConnection.Flags.HasCircularSide);
                        break;
                    }

                    case RvmEllipticalDish:
                    case RvmSphericalDish:
                    {
                        AddAnchor(context, prim, new Vector3(0, 0, 0), new Vector3(0, 0, -1), 0,
                            RvmConnection.Flags.HasCircularSide);
                        break;
                    }

                    case RvmSnout sn:
                    {
                        Vector3[] n =
                        {
                            new Vector3((float)Math.Sin(sn.BottomShearX) * (float)Math.Cos(sn.BottomShearY),
                                (float)Math.Sin(sn.BottomShearY),
                                -(float)Math.Cos(sn.BottomShearX) * (float)Math.Cos(sn.BottomShearY)),
                            new Vector3(-(float)Math.Sin(sn.TopShearX) * (float)Math.Cos(sn.TopShearY),
                                -(float)Math.Sin(sn.TopShearY),
                                (float)Math.Cos(sn.TopShearX) * (float)Math.Cos(sn.TopShearY))
                        };
                        Vector3[] p =
                        {
                            new Vector3(-0.5f * sn.OffsetX, -0.5f * sn.OffsetY, -0.5f * sn.Height),
                            new Vector3(0.5f * sn.OffsetX, 0.5f * sn.OffsetY, 0.5f * sn.Height)
                        };
                        for (uint i = 0; i < 2; i++)
                            AddAnchor(context, sn, p[i], n[i], i, RvmConnection.Flags.HasCircularSide);
                        break;
                    }

                    case RvmCylinder cylinder:
                    {
                        Vector3[] d = {new Vector3(0, 0, -1.0f), new Vector3(0, 0, 1.0f)};
                        Vector3[] p =
                        {
                            new Vector3(0, 0, -0.5f * cylinder.Height), new Vector3(0, 0, 0.5f * cylinder.Height)
                        };
                        for (uint i = 0; i < 2; i++)
                            AddAnchor(context, cylinder, p[i], d[i], i, RvmConnection.Flags.HasCircularSide);
                        break;
                    }

                    case RvmSphere:
                    case RvmFacetGroup:
                    case RvmLine:
                        break;

                    default:
                        throw new NotImplementedException("Unsupported primitive ");
                }
            }


            Connect(context, offset);
        }


        public static void Connect(RvmStore store)
        {
            var context = new Context {store = store};


            var stopwatch = Stopwatch.StartNew();
            foreach (var root in store.RvmFiles.SelectMany(file => file.Model.children))
            {
                Recurse(context, root);
            }

            if (context.anchors.Any(a => a.matched == true))
            {
                throw new Exception("Matched connections left in context");
            }

            var e0 = stopwatch.Elapsed;
            Console.WriteLine($"Matched {context.anchors_matched} of {context.anchors_total} anchors {e0}.");
        }
    }
}