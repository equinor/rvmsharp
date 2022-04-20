namespace RvmSharp.Operations;

using Containers;
using Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

/// <summary>
/// Make connection between geometries that share a boundary and are aligned.
/// Tessellation use the connections to:
/// - Avoid adding internal caps between adjacent shapes.
/// - Align circumferential sample points between adjacent shapes.
/// </summary>
public static class RvmConnect
{
    class Anchor
    {
        public RvmPrimitive Geo { get; } // Not sure if this can be null.
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public uint Offset { get; }
        public RvmConnection.ConnectionType ConnectionTypeFlags { get; }
            
        public bool Matched { get; set; }
            
        public Anchor(RvmPrimitive geo, Vector3 position, Vector3 direction, uint offset,
            RvmConnection.ConnectionType connectionTypeFlags)
        {
            Geo = geo;
            Position = position;
            Direction = direction;
            Offset = offset;
            ConnectionTypeFlags = connectionTypeFlags;
        }
    };

    class Context
    {
        public RvmStore store;
        public readonly List<Anchor> Anchors = new List<Anchor>();
        public const float epsilon = 0.001f;

        public Context(RvmStore store)
        {
            this.store = store;
        }

        public int AnchorsCount => Anchors.Count;
        public uint AnchorsMatched { get; set; }
    };

    private static void Connect(Context context, int offset)
    {
        var anchorsCount = context.AnchorsCount;

        const float epsilon = Context.epsilon;
        const float epsilonSquared = epsilon * epsilon;

        if (offset > anchorsCount)
            throw new ArgumentOutOfRangeException(nameof(offset), offset,
                $"{nameof(offset)} is greater than {nameof(anchorsCount)} {anchorsCount}");

        var anchors = context.Anchors.Skip(offset).OrderBy(e => e.Position.X).ToArray();


        for (int j = 0; j < anchors.Length; j++)
        {
            if (anchors[j].Matched)
                continue;

            for (int i = j + 1; i < anchors.Length && anchors[i].Position.X <= anchors[j].Position.X + epsilon; i++)
            {
                bool canMatch = anchors[i].Matched == false && !ReferenceEquals(anchors[i].Geo, anchors[j].Geo);
                bool close = Vector3.DistanceSquared(anchors[j].Position, anchors[i].Position) <= epsilonSquared;

                const float alignedThreshold = -0.98f;
                bool aligned = Vector3.Dot(anchors[j].Direction, anchors[i].Direction) < alignedThreshold;

                if (canMatch && close && aligned)
                {
                    RvmConnection connection = new RvmConnection
                    (
                        primitive1: anchors[j].Geo,
                        primitive2: anchors[i].Geo,
                        offsetX: anchors[j].Offset,
                        offsetY: anchors[i].Offset,
                        position: anchors[j].Position,
                        direction: anchors[j].Direction,
                        connectionTypeFlags: anchors[i].ConnectionTypeFlags | anchors[j].ConnectionTypeFlags
                    );

                    context.store.Connections.Add(connection);

                    anchors[j].Geo.Connections[anchors[j].Offset] = connection;
                    anchors[i].Geo.Connections[anchors[i].Offset] = connection;

                    anchors[j].Matched = true;
                    anchors[i].Matched = true;
                    context.AnchorsMatched += 2;


                    //context->store->addDebugLine((a[j].p + 0.03f*a[j].d).data,
                    //                             (a[i].p + 0.03f*a[i].d).data,
                    //                             0x0000ff);
                }
            }
        }

        // Remove matched anchors.
        for (var j = offset; j < context.Anchors.Count;)
        {
            if (context.Anchors[j].Matched)
            {
                context.Anchors.RemoveAt(j);
            }
            else
            {
                j++;
            }
        }

        if (offset > context.Anchors.Count)
            throw new Exception("Something went wrong");
    }

    private static void AddAnchor(Context context, RvmPrimitive geo, Vector3 position, Vector3 direction, uint offset,
        RvmConnection.ConnectionType flags)
    {
        Anchor a = new Anchor(
            geo: geo,
            position: Vector3.Transform(position, geo.Matrix),
            direction: Vector3.Normalize(Vector3.TransformNormal(direction, geo.Matrix)),
            offset: offset,
            connectionTypeFlags: flags
        );

        //Console.WriteLine($"add {a.p.X:0.00000} {a.p.Y:0.00000} {a.p.Z:0.00000}");

        context.Anchors.Add(a);
    }

    private static void Recurse(Context context, RvmNode group)
    {
        var offset = context.AnchorsCount;
        foreach (var child in group.Children.OfType<RvmNode>())
            Recurse(context, child);

        foreach (var prim in group.Children.OfType<RvmPrimitive>())
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
                            new Vector3(-h, 0.0f, (-t.X + o.X) - (-b.X - o.X)),
                            new Vector3(0.0f, 0.0f, -1.0f),
                            new Vector3(0.0f, 0.0f, 1.0f)
                        };
                        
                        
                        Vector3[] p =
                        {
                            new Vector3(0.0f, -m.Y, 0.0f),
                            new Vector3(m.X, 0.0f, 0.0f),
                            new Vector3(0.0f, m.Y, 0.0f),
                            new Vector3(-m.X, 0.0f, 0.0f),
                            new Vector3(-o.X, -o.Y, -h),
                            new Vector3(o.X, o.Y, h)
                        };
                        
                        for (uint i = 0; i < 6; i++)
                        {
                            AddAnchor(context, pyramid, p[i], n[i], i, RvmConnection.ConnectionType.HasRectangularSide);
                        }

                        break;
                    }
                case RvmBox box:
                    {
                        Vector3[] n =
                        {
                            new Vector3(-1, 0, 0),
                            new Vector3(1, 0, 0),
                            new Vector3(0, -1, 0),
                            new Vector3(0, 1, 0),
                            new Vector3(0, 0, -1),
                            new Vector3(0, 0, 1)
                        };
                        var xp = 0.5f * box.LengthX;
                        var xm = -xp;
                        var yp = 0.5f * box.LengthY;
                        var ym = -yp;
                        var zp = 0.5f * box.LengthZ;
                        var zm = -zp;
                        Vector3[] p =
                        {
                            new Vector3(xm, 0.0f, 0.0f),
                            new Vector3(xp, 0.0f, 0.0f),
                            new Vector3(0.0f, ym, 0.0f),
                            new Vector3(0.0f, yp, 0.0f),
                            new Vector3(0.0f, 0.0f, zm),
                            new Vector3(0.0f, 0.0f, zp)
                        };
                        for (uint i = 0; i < 6; i++)
                            AddAnchor(context, box, p[i], n[i], i, RvmConnection.ConnectionType.HasRectangularSide);
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
                            AddAnchor(context, rt, p[i], n[i], i, RvmConnection.ConnectionType.HasRectangularSide);
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
                            AddAnchor(context, ct, p[i], n[i], i, RvmConnection.ConnectionType.HasCircularSide);
                        break;
                    }

                case RvmEllipticalDish:
                case RvmSphericalDish:
                    {
                        AddAnchor(context, prim, new Vector3(0, 0, 0), new Vector3(0, 0, -1), 0,
                            RvmConnection.ConnectionType.HasCircularSide);
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
                            AddAnchor(context, sn, p[i], n[i], i, RvmConnection.ConnectionType.HasCircularSide);
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
                            AddAnchor(context, cylinder, p[i], d[i], i, RvmConnection.ConnectionType.HasCircularSide);
                        break;
                    }

                case RvmSphere:
                case RvmFacetGroup:
                case RvmLine:
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Unsupported primitive type: " + prim.GetType());
            }
        }


        Connect(context, offset);
    }


    public static void Connect(RvmStore store)
    {
        var context = new Context(store);


        var stopwatch = Stopwatch.StartNew();
        foreach (var root in store.RvmFiles.SelectMany(file => file.Model.Children))
        {
            Recurse(context, root);
        }

        if (context.Anchors.Any(a => a.Matched))
        {
            throw new Exception("Matched connections left in context");
        }

        var e0 = stopwatch.Elapsed;
        Console.WriteLine($"Matched {context.AnchorsMatched} of {context.AnchorsCount} anchors {e0}.");
    }
}