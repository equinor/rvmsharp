namespace RvmSharp.Tessellation
{
    using Primitives;
    using System;
    using System.Numerics;

    internal class ConnectionInterface
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

        private ConnectionInterface() { }

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

                    iface.InterfaceType = Type.Square;
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
                    iface.InterfaceType = Type.Circular;
                    iface.CircularRadius = scale * circularTorus.Radius;
                    break;

                case RvmEllipticalDish ellipticalDish:
                    iface.InterfaceType = Type.Circular;
                    iface.CircularRadius = scale * ellipticalDish.BaseRadius;
                    break;

                case RvmSphericalDish sphericalDish:
                {
                    float r_circ = sphericalDish.BaseRadius;
                    var h = sphericalDish.Height;
                    float r_sphere = (r_circ * r_circ + h * h) / (2.0f * h);
                    iface.InterfaceType = Type.Circular;
                    iface.CircularRadius = scale * r_sphere;
                    break;
                }
                case RvmSnout snout:
                    iface.InterfaceType = Type.Circular;
                    var offset = ix == 0 ? connection.OffsetX : connection.OffsetY;
                    iface.CircularRadius = scale * (offset == 0 ? snout.RadiusBottom : snout.RadiusTop);
                    break;
                case RvmCylinder cylinder:
                    iface.InterfaceType = Type.Circular;
                    iface.CircularRadius = scale * cylinder.Radius;
                    break;
                case RvmSphere:
                case RvmLine:
                case RvmFacetGroup:
                    iface.InterfaceType = Type.Undefined;
                    break;
                default:
                    throw new NotSupportedException("Unhandled primitive type");
            }

            return iface;
        }

        internal static bool DoInterfacesMatch(RvmPrimitive primitive, RvmConnection connection)
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

            if (thisIFace.InterfaceType == Type.Circular)
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
    }
}