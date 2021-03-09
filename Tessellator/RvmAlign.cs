using rvmsharp.Rvm;
using rvmsharp.Rvm.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace rvmsharp.Tessellator
{
    class RvmAlign
    {
        class QueueItem
        {
            public RvmPrimitive From;
            public RvmConnection Connection;
            public Vector3 UpWorld;
        };


        class Context
        {
            public List<QueueItem> Queue = new List<QueueItem>();
            public int Front = 0;
            public int Back = 0;
            public int ConnectedComponents = 0;
            public int CircularConnections = 0;
            public int Connections = 0;
        };

        private static void Enqueue(Context context, RvmPrimitive from, RvmConnection connection, Vector3 upWorld)
        {
            connection.temp = 1;

            if (context.Back >= context.Connections)
                throw new Exception();
            context.Queue[context.Back].From = from;
            context.Queue[context.Back].Connection = connection;
            context.Queue[context.Back].UpWorld = upWorld;

            context.Back++;
        }

        private static void HandleCircularTorus(Context context, RvmCircularTorus ct, uint offset, Vector3 upWorld)
        {
            var M = ct.Matrix;
            //var  N = Mat3f(M.data);
            //var  N_inv = inverse(N);
            if (!Matrix4x4.Invert(M, out var N_inv))
                throw new Exception("Inversion failed");
            var c = (float)Math.Cos(ct.Angle);
            var s = (float)Math.Sin(ct.Angle);

            var upLocal = Vector3.Normalize(Vector3.TransformNormal(upWorld, N_inv));

            if (offset == 1)
            {
                // rotate back to xz
                upLocal = new Vector3(c * upLocal.X + s * upLocal.Y,
                    -s * upLocal.X + c * upLocal.Y,
                    upLocal.Z);
            }

            ct.SampleStartAngle = (float)Math.Atan2(upLocal.Z, upLocal.X);
            if (!float.IsFinite(ct.SampleStartAngle))
            {
                ct.SampleStartAngle = 0.0f;
            }

            var ci = (float)Math.Cos(ct.SampleStartAngle);
            var si = (float)Math.Sin(ct.SampleStartAngle);
            var co = (float)Math.Cos(ct.Angle);
            var so = (float)Math.Sin(ct.Angle);

            Vector3 upNew = new Vector3(ci, 0.0f, si);

            Vector3[] upNewWorld = new Vector3[2];
            upNewWorld[0] = Vector3.TransformNormal(upNew, M);
            upNewWorld[1] = Vector3.TransformNormal(new Vector3(c * upNew.X - s * upNew.Y,
                s * upNew.X + c * upNew.Y,
                upNew.Z), M);

            if (true)
            {
                Vector3 p0 = new Vector3(ct.Radius * ci + ct.Offset,
                    0.0f,
                    ct.Radius * si);

                Vector3 p1 = new Vector3((ct.Radius * ci + ct.Offset) * co,
                    (ct.Radius * ci + ct.Offset) * so,
                    ct.Radius * si);


                var a0 = Vector3.Transform(p0, ct.Matrix);
                var b0 = a0 + 1.5f * ct.Radius * upNewWorld[0];

                var a1 = Vector3.Transform(p1, ct.Matrix);
                var b1 = a1 + 1.5f * ct.Radius * upNewWorld[1];

                //if (context.front == 1) {
                //  if (geo.connections[0]) context.store.addDebugLine(a0.data, b0.data, 0x00ffff);
                //  if (geo.connections[1]) context.store.addDebugLine(a1.data, b1.data, 0x00ff88);
                //}
                //else if (offset == 0) {
                //  if (geo.connections[0]) context.store.addDebugLine(a0.data, b0.data, 0x0000ff);
                //  if (geo.connections[1]) context.store.addDebugLine(a1.data, b1.data, 0x000088);
                //}
                //else {
                //  if (geo.connections[0]) context.store.addDebugLine(a0.data, b0.data, 0x000088);
                //  if (geo.connections[1]) context.store.addDebugLine(a1.data, b1.data, 0x0000ff);
                //}
            }

            for (uint k = 0; k < 2; k++)
            {
                var con = ct.Connections[k];
                if (con != null && !con.HasFlag(RvmConnection.Flags.HasRectangularSide) && con.temp == 0)
                {
                    Enqueue(context, ct, con, upNewWorld[k]);
                }
            }
        }


        private static void HandleCylinderSnoutAndDish(Context context, RvmPrimitive geo, uint offset, Vector3 upWorld)
        {
            if (!Matrix4x4.Invert(geo.Matrix, out var M_inv))
                throw new Exception();
            //var  M_inv = inverse(Mat3f(geo.M_3x4.data));

            var upn = Vector3.Normalize(upWorld);

            var upLocal = Vector3.TransformNormal(upn, M_inv);
            upLocal.Z = 0.0f; // project to xy-plane

            geo.SampleStartAngle = (float)Math.Atan2(upLocal.Y, upLocal.X);
            if (!float.IsFinite(geo.SampleStartAngle))
            {
                geo.SampleStartAngle = 0.0f;
            }

            Vector3 upNewWorld = Vector3.TransformNormal(new Vector3((float)Math.Cos(geo.SampleStartAngle),
                (float)Math.Sin(geo.SampleStartAngle),
                0.0f), geo.Matrix);

            for (var k = 0; k < 2; k++)
            {
                var con = geo.Connections[k];
                if (con != null && !con.HasFlag(RvmConnection.Flags.HasRectangularSide) && con.temp == 0)
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
                if (item.From != item.Connection.p1)
                {
                    var geo = item.Connection.p2;
                    switch (geo)
                    {
                        case RvmPyramid:
                        case RvmBox:
                        case RvmRectangularTorus:
                        case RvmSphere:
                        case RvmLine:
                        case RvmFacetGroup:
                            throw new Exception("Got geometry with non-circular intersection.");
                            break;

                        case RvmSnout:
                        case RvmEllipticalDish:
                        case RvmSphericalDish:
                        case RvmCylinder:
                        {
                            var offset = i == 0 ? item.Connection.OffsetX : item.Connection.OffsetY;
                            HandleCylinderSnoutAndDish(context, geo, offset, item.UpWorld);
                        }
                            break;

                        case RvmCircularTorus ct:
                        {
                            var offset = i == 0 ? item.Connection.OffsetX : item.Connection.OffsetY;
                            HandleCircularTorus(context, ct, offset, item.UpWorld);
                        }
                            break;

                        default:
                            throw new InvalidOperationException("Illegal kind");
                    }
                }
            }
        }


        public static void Align(RvmStore store)
        {
            var context = new Context();
            var time0 = Stopwatch.StartNew();
            ;
            foreach (var connection in store.Connections)
            {
                connection.temp = 0;

                if (connection.flags == RvmConnection.Flags.HasCircularSide)
                {
                    context.CircularConnections++;
                }

                context.Connections++;
            }

            context.Queue = Enumerable.Repeat<QueueItem>(new QueueItem(), store.Connections.Count).ToList();
            foreach (var connection in store.Connections)
            {
                if (connection.temp != 0 || connection.HasFlag(RvmConnection.Flags.HasRectangularSide)) continue;

                // Create an arbitrary vector in plane of intersection as seed.
                var d = connection.d;
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
                if (!float.IsFinite(upWorld.LengthSquared()))
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

            var e0 = time0.Elapsed;
            ;

            Console.WriteLine(
                $"{context.ConnectedComponents} connected components in {context.CircularConnections} circular connections {e0}.");
        }
    }
}