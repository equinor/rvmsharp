namespace RvmSharp.Tessellation;

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
        var connectionInterface = new ConnectionInterface();
        var connection = geo.Connections[o];

        if (connection == null)
            throw new Exception($"Got Unexpected Null-Connection in 'geo.Connections' for index: {o}");

        var ix = connection.Primitive1 == geo ? 1 : 0;

        if (!Matrix4x4.Decompose(geo.Matrix, out var geoScale, out _, out _))
            throw new Exception();

        var scale = Math.Max(geoScale.X, Math.Max(geoScale.Y, geoScale.Z));
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
                    Vector3[,] quad =
                    {
                        {
                            new Vector3(-bx - ox, -by - oy, -h2),
                            new Vector3(bx - ox, -by - oy, -h2),
                            new Vector3(bx - ox, by - oy, -h2),
                            new Vector3(-bx - ox, by - oy, -h2)
                        },
                        {
                            new Vector3(-tx + ox, -ty + oy, h2),
                            new Vector3(tx + ox, -ty + oy, h2),
                            new Vector3(tx + ox, ty + oy, h2),
                            new Vector3(-tx + ox, ty + oy, h2)
                        },
                    };

                    connectionInterface.InterfaceType = Type.Square;
                    if (o < 4)
                    {
                        var oo = (o + 1) & 3;
                        connectionInterface.SquareConnectionPoints[0] = Vector3.Transform(quad[0, o], geo.Matrix);
                        connectionInterface.SquareConnectionPoints[1] = Vector3.Transform(quad[0, oo], geo.Matrix);
                        connectionInterface.SquareConnectionPoints[2] = Vector3.Transform(quad[1, oo], geo.Matrix);
                        connectionInterface.SquareConnectionPoints[3] = Vector3.Transform(quad[1, o], geo.Matrix);
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            connectionInterface.SquareConnectionPoints[k] =
                                Vector3.Transform(quad[o - 4, k], geo.Matrix);
                        }
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
                            new Vector3(xm, ym, zp),
                            new Vector3(xm, yp, zp),
                            new Vector3(xm, yp, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xp, ym, zm),
                            new Vector3(xp, yp, zm),
                            new Vector3(xp, yp, zp),
                            new Vector3(xp, ym, zp)
                        },
                        {
                            new Vector3(xp, ym, zm),
                            new Vector3(xp, ym, zp),
                            new Vector3(xm, ym, zp),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, yp, zm),
                            new Vector3(xm, yp, zp),
                            new Vector3(xp, yp, zp),
                            new Vector3(xp, yp, zm)
                        },
                        {
                            new Vector3(xm, yp, zm),
                            new Vector3(xp, yp, zm),
                            new Vector3(xp, ym, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, ym, zp),
                            new Vector3(xp, ym, zp),
                            new Vector3(xp, yp, zp),
                            new Vector3(xm, yp, zp)
                        }
                    };

                    for (var k = 0; k < 4; k++)
                    {
                        connectionInterface.SquareConnectionPoints[k] = Vector3.Transform(V[o, k], geo.Matrix);
                    }

                    break;
                }
            case RvmRectangularTorus tor:
                {
                    var h2 = 0.5f * tor.Height;
                    float[,] square =
                    {
                        {tor.RadiusOuter, -h2},
                        {tor.RadiusInner, -h2},
                        {tor.RadiusInner, h2},
                        {tor.RadiusOuter, h2},
                    };
                    if (o == 0)
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            connectionInterface.SquareConnectionPoints[k] =
                                Vector3.Transform(new Vector3(square[k, 0], 0.0f, square[k, 1]), geo.Matrix);
                        }
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            connectionInterface.SquareConnectionPoints[k] = Vector3.Transform(new Vector3(
                                (float)(square[k, 0] * Math.Cos(tor.Angle)),
                                (float)(square[k, 0] * Math.Sin(tor.Angle)),
                                square[k, 1]), geo.Matrix);
                        }
                    }

                    break;
                }
            case RvmCircularTorus circularTorus:
                connectionInterface.InterfaceType = Type.Circular;
                connectionInterface.CircularRadius = scale * circularTorus.Radius;
                break;

            case RvmEllipticalDish ellipticalDish:
                connectionInterface.InterfaceType = Type.Circular;
                connectionInterface.CircularRadius = scale * ellipticalDish.BaseRadius;
                break;

            case RvmSphericalDish sphericalDish:
                {
                    float baseRadius = sphericalDish.BaseRadius;
                    var height = sphericalDish.Height;
                    float sphereRadius = (baseRadius * baseRadius + height * height) / (2.0f * height);
                    connectionInterface.InterfaceType = Type.Circular;
                    connectionInterface.CircularRadius = scale * sphereRadius;
                    break;
                }
            case RvmSnout snout:
                connectionInterface.InterfaceType = Type.Circular;
                var offset = ix == 0 ? connection.ConnectionIndex1 : connection.ConnectionIndex2;
                connectionInterface.CircularRadius = scale * (offset == 0 ? snout.RadiusBottom : snout.RadiusTop);
                break;
            case RvmCylinder cylinder:
                connectionInterface.InterfaceType = Type.Circular;
                connectionInterface.CircularRadius = scale * cylinder.Radius;
                break;
            case RvmSphere:
            case RvmLine:
            case RvmFacetGroup:
                connectionInterface.InterfaceType = Type.Undefined;
                break;
            default:
                throw new NotSupportedException("Unhandled primitive type");
        }

        return connectionInterface;
    }

    internal static bool DoInterfacesMatch(RvmPrimitive primitive, RvmConnection connection)
    {
        bool isFirst = primitive == connection.Primitive1;

        var thisGeo = isFirst ? connection.Primitive1 : connection.Primitive2;
        var thisOffset = isFirst ? connection.ConnectionIndex1 : connection.ConnectionIndex2;
        var thisIFace = GetInterface(thisGeo, (int)thisOffset);

        var thatGeo = isFirst ? connection.Primitive2 : connection.Primitive1;
        var thatOffset = isFirst ? connection.ConnectionIndex2 : connection.ConnectionIndex1;
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
                if (Vector3.DistanceSquared(thisIFace.SquareConnectionPoints[j],
                        thatIFace.SquareConnectionPoints[i]) < 0.001f * 0.001f)
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