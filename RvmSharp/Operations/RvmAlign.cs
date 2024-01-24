namespace RvmSharp.Operations;

using Containers;
using Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

// ReSharper disable once UnusedType.Global -- This is public API
public static class RvmAlign
{
    private record QueueItem(RvmPrimitive? From, RvmConnection Connection, Vector3 UpWorld);

    private class Context
    {
        public List<QueueItem> Queue = [];
        public int Front;
        public int Back;
        public int ConnectedComponents;
        public int CircularConnections;
        public int Connections;
    };

    private static void Enqueue(Context context, RvmPrimitive? from, RvmConnection connection, Vector3 upWorld)
    {
        connection.IsEnqueued = true;

        if (context.Back >= context.Connections)
            throw new Exception("Expected Back to be Less than the total connections count.");

        context.Queue.Add(new QueueItem(From: from, Connection: connection, UpWorld: upWorld));
        context.Back++;
    }

    private static void HandleCircularTorus(Context context, RvmCircularTorus ct, uint offset, Vector3 upWorld)
    {
        var m = ct.Matrix;
        //var  N = Mat3f(M.data);
        //var  N_inv = inverse(N);
        if (!Matrix4x4.Invert(m, out var nInv))
            throw new Exception("Inversion failed");

        var c = (float)Math.Cos(ct.Angle);
        var s = (float)Math.Sin(ct.Angle);

        var upLocal = Vector3.Normalize(Vector3.TransformNormal(upWorld, nInv));

        if (offset == 1)
        {
            // rotate back to xz
            upLocal = new Vector3(c * upLocal.X + s * upLocal.Y, -s * upLocal.X + c * upLocal.Y, upLocal.Z);
        }

        ct.SampleStartAngle = (float)Math.Atan2(upLocal.Z, upLocal.X);
        if (!ct.SampleStartAngle.IsFinite())
        {
            ct.SampleStartAngle = 0.0f;
        }

        var ci = (float)Math.Cos(ct.SampleStartAngle);
        var si = (float)Math.Sin(ct.SampleStartAngle);
        // var co = (float)Math.Cos(ct.Angle);
        // var so = (float)Math.Sin(ct.Angle);

        Vector3 upNew = new Vector3(ci, 0.0f, si);

        Vector3[] upNewWorld = new Vector3[2];
        upNewWorld[0] = Vector3.TransformNormal(upNew, m);
        upNewWorld[1] = Vector3.TransformNormal(
            new Vector3(c * upNew.X - s * upNew.Y, s * upNew.X + c * upNew.Y, upNew.Z),
            m
        );

        // TODO: Redundant. Why?
        // if (true)
        // {
        //     Vector3 p0 = new Vector3(ct.Radius * ci + ct.Offset, 0.0f, ct.Radius * si);
        //
        //     Vector3 p1 = new Vector3(
        //         (ct.Radius * ci + ct.Offset) * co,
        //         (ct.Radius * ci + ct.Offset) * so,
        //         ct.Radius * si
        //     );
        //
        //     var a0 = Vector3.Transform(p0, ct.Matrix);
        //
        //     var b0 = a0 + 1.5f * ct.Radius * upNewWorld[0];
        //
        //     var a1 = Vector3.Transform(p1, ct.Matrix);
        //
        //     var b1 = a1 + 1.5f * ct.Radius * upNewWorld[1];
        // }

        for (uint k = 0; k < 2; k++)
        {
            var con = ct.Connections[k];

            if (
                con != null
                && !con.HasConnectionType(RvmConnection.ConnectionType.HasRectangularSide)
                && !con.IsEnqueued
            )
            {
                Enqueue(context, ct, con, upNewWorld[k]);
            }
        }
    }

    private static void HandleCylinderSnoutAndDish(Context context, RvmPrimitive geo, uint offset, Vector3 upWorld)
    {
        if (!Matrix4x4.Invert(geo.Matrix, out var mInv))
            throw new Exception();
        //var  M_inv = inverse(Mat3f(geo.M_3x4.data));

        var upn = Vector3.Normalize(upWorld);

        var upLocal = Vector3.TransformNormal(upn, mInv);
        upLocal.Z = 0.0f; // project to xy-plane

        geo.SampleStartAngle = (float)Math.Atan2(upLocal.Y, upLocal.X);
        if (!geo.SampleStartAngle.IsFinite())
        {
            geo.SampleStartAngle = 0.0f;
        }

        Vector3 upNewWorld = Vector3.TransformNormal(
            new Vector3((float)Math.Cos(geo.SampleStartAngle), (float)Math.Sin(geo.SampleStartAngle), 0.0f),
            geo.Matrix
        );

        for (var k = 0; k < 2; k++)
        {
            var con = geo.Connections[k];
            if (
                con != null
                && !con.HasConnectionType(RvmConnection.ConnectionType.HasRectangularSide)
                && !con.IsEnqueued
            )
            {
                Enqueue(context, geo, con, upNewWorld);
            }
        }
    }

    private static void ProcessItem(Context context)
    {
        var item = context.Queue[context.Front++];

        for (uint i = 0; i < 2; i++)
        {
            if (item.From != item.Connection.Primitive1)
            {
                var geo = item.Connection.Primitive2;
                var offset = i == 0 ? item.Connection.ConnectionIndex1 : item.Connection.ConnectionIndex2;
                switch (geo)
                {
                    case RvmPyramid:
                    case RvmBox:
                    case RvmRectangularTorus:
                    case RvmSphere:
                    case RvmLine:
                    case RvmFacetGroup:
                        throw new Exception($"Got geometry with non-circular intersection: {geo}");

                    case RvmSnout:
                    case RvmEllipticalDish:
                    case RvmSphericalDish:
                    case RvmCylinder:
                        HandleCylinderSnoutAndDish(context, geo, offset, item.UpWorld);
                        break;

                    case RvmCircularTorus ct:
                        HandleCircularTorus(context, ct, offset, item.UpWorld);
                        break;

                    default:
                        throw new InvalidOperationException("Illegal kind");
                }
            }
        }
    }

    // ReSharper disable once UnusedMember.Global -- Align is Public Api
    public static void Align(RvmStore store)
    {
        var context = new Context();
        var stopwatch = Stopwatch.StartNew();

        foreach (var connection in store.Connections)
        {
            connection.IsEnqueued = false;

            if (connection.HasConnectionType(RvmConnection.ConnectionType.HasCircularSide))
            {
                context.CircularConnections++;
            }

            context.Connections++;
        }

        // Clear Queue
        context.Queue = new List<QueueItem>();

        foreach (var connection in store.Connections)
        {
            if (connection.IsEnqueued || connection.HasConnectionType(RvmConnection.ConnectionType.HasRectangularSide))
                continue;

            // Create an arbitrary vector in plane of intersection as seed.
            var d = connection.Direction;
            Vector3 b;
            if (Math.Abs(d.X) > Math.Abs(d.Y) && Math.Abs(d.X) > Math.Abs(d.Z))
            {
                b = new Vector3(0.0f, 1.0f, 0.0f);
            }
            else
            {
                b = new Vector3(1.0f, 0.0f, 0.0f);
            }

            var upWorld = Vector3.Normalize(Vector3.Cross(d, b));
            if (!upWorld.LengthSquared().IsFinite())
                throw new Exception("Invalid world");

            context.Front = 0;
            context.Back = 0;
            Enqueue(context, null, connection, upWorld);
            do
            {
                ProcessItem(context);
            } while (context.Front < context.Back);

            context.ConnectedComponents++;
        }

        var e0 = stopwatch.Elapsed;

        Console.WriteLine(
            $"{context.ConnectedComponents} connected components in {context.CircularConnections} circular connections {e0}."
        );
    }
}
