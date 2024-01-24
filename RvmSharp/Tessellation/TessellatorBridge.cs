namespace RvmSharp.Tessellation;

using Commons.Utils;
using Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using static Primitives.RvmFacetGroup;

// ReSharper disable once UnusedType.Global -- Public API
public static class TessellatorBridge
{
    private const float MinimumThreshold = 1e-7f;

    public static (RvmMesh, Color)[] Tessellate(RvmNode group, float tolerance)
    {
        var meshes = group.Children
            .OfType<RvmPrimitive>()
            .Select(primitive =>
            {
#if DEBUG
                // Assert that the decomposition works.
                if (!Matrix4x4.Decompose(primitive.Matrix, out _, out _, out _))
                {
                    throw new InvalidOperationException($"Could not decompose matrix for {@group.Name}");
                }
#endif

                if (!PdmsColors.TryGetColorByCode(group.MaterialId, out var color))
                {
                    color = Color.Magenta;
                }

                return (mesh: Tessellate(primitive, tolerance), color);
            })
            .Where(mc => mc.mesh != null)
            .Select(m => (m.mesh!, m.color));

        return meshes.ToArray();
    }

    public static RvmMesh? Tessellate(RvmPrimitive primitive, float tolerance)
    {
        if (!Matrix4x4.Decompose(primitive.Matrix, out var scale, out _, out _))
        {
            throw new InvalidOperationException($"Could not decompose matrix for {primitive}");
        }

        var scaleScalar = Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
        var mesh = TessellateWithoutApplyingMatrix(primitive, scaleScalar, tolerance);
        mesh?.Apply(primitive.Matrix);

        return mesh;
    }

    /// <summary>
    /// Tessellate a RvmPrimitive into a Mesh representation.
    /// </summary>
    /// <param name="primitive">The primitive to tessellate</param>
    /// <param name="scale">The scale is used in combination with tolerance for meshes that will be scaled large. If you dont care just use 1 as the value.</param>
    /// <param name="tolerance">The accepted tolerance. Used in combination with scale for some primitives.</param>
    /// <returns>Tessellated Mesh (Or null, if we cannot tessellate it.)</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static RvmMesh? TessellateWithoutApplyingMatrix(RvmPrimitive primitive, float scale, float tolerance)
    {
        switch (primitive)
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
                return Tessellate(sphere, sphere.Radius, (float)Math.PI, 0.0f, 1.0f, scale, tolerance);
            case RvmEllipticalDish ellipticalDish:
                return Tessellate(
                    ellipticalDish,
                    ellipticalDish.BaseRadius,
                    (float)Math.PI / 2,
                    0.0f,
                    ellipticalDish.Height / ellipticalDish.BaseRadius,
                    scale,
                    tolerance
                );
            case RvmSphericalDish sphericalDish:
            {
                var baseRadius = sphericalDish.BaseRadius;
                var height = sphericalDish.Height;
                var sphereRadius = (baseRadius * baseRadius + height * height) / (2.0f * height);
                var sinval = Math.Min(1.0f, Math.Max(-1.0, (baseRadius / sphereRadius)));
                var arc = (float)Math.Asin(sinval);
                if (baseRadius < height)
                {
                    arc = (float)Math.PI - arc;
                }

                return Tessellate(sphericalDish, sphereRadius, arc, height - sphereRadius, 1.0f, scale, tolerance);
            }
            default:
                throw new ArgumentOutOfRangeException(
                    $"(Currently) Unsupported type for tessellation: {primitive.Kind}"
                );
        }
    }

    private static RvmMesh Tessellate(RvmPyramid pyramid)
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
                new Vector3(-bx - ox, -by - oy, -halfHeight),
                new Vector3(bx - ox, -by - oy, -halfHeight),
                new Vector3(bx - ox, by - oy, -halfHeight),
                new Vector3(-bx - ox, by - oy, -halfHeight)
            },
            {
                new Vector3(-tx + ox, -ty + oy, halfHeight),
                new Vector3(tx + ox, -ty + oy, halfHeight),
                new Vector3(tx + ox, ty + oy, halfHeight),
                new Vector3(-tx + ox, ty + oy, halfHeight)
            },
        };

        // Avoid the normal in the height direction ever being Zero. (This can lead to Normals without direction)
        float heightNormal = Math.Abs(halfHeight) < 0.00001f ? 1 : halfHeight;

        Vector3[] n =
        {
            new Vector3(0.0f, -heightNormal, (quad[1, 0].Y - quad[0, 0].Y)),
            new Vector3(heightNormal, 0.0f, -(quad[1, 1].X - quad[0, 1].X)),
            new Vector3(0.0f, heightNormal, -(quad[1, 2].Y - quad[0, 2].Y)),
            new Vector3(-heightNormal, 0.0f, (quad[1, 3].X - quad[0, 3].X)),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1),
        };

        bool[] cap =
        {
            true,
            true,
            true,
            true,
            MinimumThreshold <= Math.Min(Math.Abs(pyramid.BottomX), Math.Abs(pyramid.BottomY)),
            MinimumThreshold <= Math.Min(Math.Abs(pyramid.TopX), Math.Abs(pyramid.TopY))
        };

        for (var i = 0; i < 6; i++)
        {
            var con = pyramid.Connections[i];
            if (
                cap[i] == false
                || con == null
                || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasRectangularSide
            )
                continue;

            if (ConnectionInterface.DoInterfacesMatch(pyramid, con))
            {
                cap[i] = false;
            }
        }

        var capCount = cap.Count(c => c);

        const float error = 0.0f;

        var vertices = new float[3 * 4 * capCount];
        var normals = new float[3 * 4 * capCount];

        var arrayPosition = 0;
        for (var i = 0; i < 4; i++)
        {
            if (cap[i] == false)
                continue;

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
            if (cap[i] == false)
                continue;
            arrayPosition = TessellationHelpers.QuadIndices(
                indices,
                arrayPosition,
                o /*4 * i*/
                ,
                0,
                1,
                2,
                3
            );
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

        return new RvmMesh(vertices, normals, indices, error);
    }

    private static RvmMesh Tessellate(RvmRectangularTorus rectangularTorus, float scale, float tolerance)
    {
        var segments = SagittaUtils.SagittaBasedSegmentCount(
            rectangularTorus.Angle,
            rectangularTorus.RadiusOuter,
            scale,
            tolerance
        );
        var samples = segments + 1; // Assumed to be open, add extra sample.

        var error = SagittaUtils.SagittaBasedError(
            rectangularTorus.Angle,
            rectangularTorus.RadiusOuter,
            scale,
            segments
        );
        Debug.Assert(error <= tolerance);

        bool shell = true;
        bool[] cap = [true, true];

        for (var i = 0; i < 2; i++)
        {
            var con = rectangularTorus.Connections[i];
            if (con == null || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasRectangularSide)
            {
                continue;
            }

            if (ConnectionInterface.DoInterfacesMatch(rectangularTorus, con))
            {
                cap[i] = false;
            }
        }

        var h2 = 0.5f * rectangularTorus.Height;
        float[,] square =
        {
            { rectangularTorus.RadiusOuter, -h2 },
            { rectangularTorus.RadiusInner, -h2 },
            { rectangularTorus.RadiusInner, h2 },
            { rectangularTorus.RadiusOuter, h2 },
        };

        // Not closed
        var t0 = new float[2 * samples + 1];
        for (var i = 0; i < samples; i++)
        {
            t0[2 * i + 0] = (float)Math.Cos((rectangularTorus.Angle / segments) * i);
            t0[2 * i + 1] = (float)Math.Sin((rectangularTorus.Angle / segments) * i);
        }

        var l = 0;

        var verticesN = (4 * 2 * samples) + (cap[0] ? 4 : 0) + (cap[1] ? 4 : 0);

        var vertices = new float[3 * verticesN];
        var normals = new float[3 * verticesN];

        if (shell)
        {
            for (var i = 0; i < samples; i++)
            {
                float[,] n =
                {
                    { 0.0f, 0.0f, -1.0f },
                    { -t0[2 * i + 0], -t0[2 * i + 1], 0.0f },
                    { 0.0f, 0.0f, 1.0f },
                    { t0[2 * i + 0], t0[2 * i + 1], 0.0f },
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

        if (l != 3 * verticesN)
            throw new Exception();

        l = 0;
        var o = 0;

        var trianglesN = (4 * 2 * (samples - 1)) + (cap[0] ? 2 : 0) + (cap[1] ? 2 : 0);
        var indices = new int[3 * trianglesN];

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

        if (o != verticesN || l != 3 * trianglesN)
            throw new Exception();

        return new RvmMesh(vertices, normals, indices, error);
    }

    private static RvmMesh Tessellate(RvmCircularTorus circularTorus, float scale, float tolerance)
    {
        var segmentsL = SagittaUtils.SagittaBasedSegmentCount(
            circularTorus.Angle,
            circularTorus.Offset + circularTorus.Radius,
            scale,
            tolerance
        ); // large radius, toroidal direction
        // FIXME: some assets have negative circularTorus.Radius. Find out if this is the correct solution
        var segmentsS = SagittaUtils.SagittaBasedSegmentCount(
            Math.PI * 2,
            Math.Abs(circularTorus.Radius),
            scale,
            tolerance
        ); // small radius, poloidal direction

        var error = Math.Max(
            SagittaUtils.SagittaBasedError(
                circularTorus.Angle,
                circularTorus.Offset + circularTorus.Radius,
                scale,
                segmentsL
            ),
            SagittaUtils.SagittaBasedError(Math.PI * 2, circularTorus.Radius, scale, segmentsS)
        );
        Debug.Assert(error <= tolerance);

        var samplesL = segmentsL + 1; // Assumed to be open, add extra sample
        var samplesS = segmentsS; // Assumed to be closed

        const bool shell = true;
        bool[] cap = { true, true };
        for (var i = 0; i < 2; i++)
        {
            var con = circularTorus.Connections[i];
            if (con == null || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasCircularSide)
            {
                continue;
            }

            if (ConnectionInterface.DoInterfacesMatch(circularTorus, con))
            {
                cap[i] = false;
            }
            // else
            // {
            //     store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0x00ffff);
            // }
        }

        var t0 = new float[2 * samplesL];
        for (var i = 0; i < samplesL; i++)
        {
            t0[2 * i + 0] = (float)Math.Cos((circularTorus.Angle / (samplesL - 1.0f)) * i);
            t0[2 * i + 1] = (float)Math.Sin((circularTorus.Angle / (samplesL - 1.0f)) * i);
        }

        var t1 = new float[2 * samplesS];
        for (var i = 0; i < samplesS; i++)
        {
            t1[2 * i + 0] = (float)Math.Cos((Math.PI * 2 / samplesS) * i + circularTorus.SampleStartAngle);
            t1[2 * i + 1] = (float)Math.Sin((Math.PI * 2 / samplesS) * i + circularTorus.SampleStartAngle);
        }

        var verticesN = ((samplesL) + (cap[0] ? 1 : 0) + (cap[1] ? 1 : 0)) * samplesS;
        var vertices = new float[3 * verticesN];
        var normals = new float[3 * verticesN];

        var trianglesN =
            (2 * (samplesL - 1) * samplesS) + (samplesS - 2) * ((cap[0] ? 1 : 0) + (cap[1] ? 1 : 0));
        var indices = new int[3 * trianglesN];

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
            for (var u = 0; u < samplesL; u++)
            {
                for (var v = 0; v < samplesS; v++)
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
            for (var v = 0; v < samplesS; v++)
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
            var m = 2 * (samplesL - 1);
            for (var v = 0; v < samplesS; v++)
            {
                normals[l] = -t0[m + 1];
                vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[m + 0]);
                normals[l] = t0[m + 0];
                vertices[l++] = ((circularTorus.Radius * t1[2 * v + 0] + circularTorus.Offset) * t0[m + 1]);
                normals[l] = 0.0f;
                vertices[l++] = circularTorus.Radius * t1[2 * v + 1];
            }
        }

        Debug.Assert(l == 3 * verticesN, "l == 3*vertices_n");

        // generate indices
        l = 0;
        var o = 0;
        if (shell)
        {
            for (var u = 0; u + 1 < samplesL; u++)
            {
                for (var v = 0; v + 1 < samplesS; v++)
                {
                    indices[l++] = samplesS * (u + 0) + (v + 0);
                    indices[l++] = samplesS * (u + 1) + (v + 0);
                    indices[l++] = samplesS * (u + 1) + (v + 1);

                    indices[l++] = samplesS * (u + 1) + (v + 1);
                    indices[l++] = samplesS * (u + 0) + (v + 1);
                    indices[l++] = samplesS * (u + 0) + (v + 0);
                }

                indices[l++] = samplesS * (u + 0) + (samplesS - 1);
                indices[l++] = samplesS * (u + 1) + (samplesS - 1);
                indices[l++] = samplesS * (u + 1) + 0;
                indices[l++] = samplesS * (u + 1) + 0;
                indices[l++] = samplesS * (u + 0) + 0;
                indices[l++] = samplesS * (u + 0) + (samplesS - 1);
            }

            o += samplesL * samplesS;
        }

        var u1 = new int[samplesS];
        var u2 = new int[samplesS];
        if (cap[0])
        {
            for (var i = 0; i < samplesS; i++)
            {
                u1[i] = o + i;
            }

            l = TessellateCircle(indices, l, u2, u1, samplesS);
            o += samplesS;
        }

        if (cap[1])
        {
            for (var i = 0; i < samplesS; i++)
            {
                u1[i] = o + (samplesS - 1) - i;
            }

            l = TessellateCircle(indices, l, u2, u1, samplesS);
            o += samplesS;
        }

        Debug.Assert(l == 3 * trianglesN);
        Debug.Assert(o == verticesN);

        return new RvmMesh(vertices, normals, indices, error);
    }

    private static RvmMesh Tessellate(RvmBox box)
    {
        var xp = 0.5f * box.LengthX;
        var xm = -xp;
        var yp = 0.5f * box.LengthY;
        var ym = -yp;
        var zp = 0.5f * box.LengthZ;
        var zm = -zp;

        Vector3[,] v = new Vector3[,]
        {
            { new Vector3(xm, ym, zp), new Vector3(xm, yp, zp), new Vector3(xm, yp, zm), new Vector3(xm, ym, zm) },
            { new Vector3(xp, ym, zm), new Vector3(xp, yp, zm), new Vector3(xp, yp, zp), new Vector3(xp, ym, zp) },
            { new Vector3(xp, ym, zm), new Vector3(xp, ym, zp), new Vector3(xm, ym, zp), new Vector3(xm, ym, zm) },
            { new Vector3(xm, yp, zm), new Vector3(xm, yp, zp), new Vector3(xp, yp, zp), new Vector3(xp, yp, zm) },
            { new Vector3(xm, yp, zm), new Vector3(xp, yp, zm), new Vector3(xp, ym, zm), new Vector3(xm, ym, zm) },
            { new Vector3(xm, ym, zp), new Vector3(xp, ym, zp), new Vector3(xp, yp, zp), new Vector3(xm, yp, zp) }
        };

        Vector3[] n =
        {
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1)
        };

        bool[] faces =
        {
            1e-5 <= box.LengthX,
            1e-5 <= box.LengthX,
            1e-5 <= box.LengthY,
            1e-5 <= box.LengthY,
            1e-5 <= box.LengthZ,
            1e-5 <= box.LengthZ,
        };

        for (var i = 0; i < 6; i++)
        {
            var con = box.Connections[i];
            if (
                faces[i] == false
                || con == null
                || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasRectangularSide
            )
                continue;

            if (ConnectionInterface.DoInterfacesMatch(box, con))
            {
                faces[i] = false;
                //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0xff0000);
            }
        }

        var facesN = 0;
        for (var i = 0; i < 6; i++)
        {
            if (faces[i])
                facesN++;
        }

        if (facesN <= 0)
        {
            return new RvmMesh(Array.Empty<float>(), Array.Empty<float>(), Array.Empty<int>(), 0);
        }

        {
            var verticesN = 4 * facesN;
            var vertices = new float[3 * verticesN];
            var normals = new float[3 * verticesN];

            var trianglesN = 2 * facesN;
            var indices = new int[3 * trianglesN];

            var o = 0;
            var iV = 0;
            var iP = 0;
            for (var f = 0; f < 6; f++)
            {
                if (!faces[f])
                    continue;

                for (var i = 0; i < 4; i++)
                {
                    iV = TessellationHelpers.Vertex(normals, vertices, iV, n[f], v[f, i]);
                }

                iP = TessellationHelpers.QuadIndices(indices, iP, o, 0, 1, 2, 3);

                o += 4;
            }

            var tri = new RvmMesh(vertices, normals, indices, 0.0f);

            if (iV != 3 * verticesN || iP != 3 * trianglesN || o != verticesN)
            {
                throw new Exception();
            }

            return tri;
        }

    }

    private static RvmMesh Tessellate(RvmFacetGroup facetGroup)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        foreach (var poly in facetGroup.Polygons)
        {
            var (bMin, bMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            foreach (var cont in poly.Contours)
            {
                foreach (var vn in cont.Vertices)
                {
                    (bMin.X, bMin.Y, bMin.Z) = (
                        Math.Min(bMin.X, vn.Vertex.X),
                        Math.Min(bMin.Y, vn.Vertex.Y),
                        Math.Min(bMin.Z, vn.Vertex.Z)
                    );
                    (bMax.X, bMax.Y, bMax.Z) = (
                        Math.Max(bMax.X, vn.Vertex.X),
                        Math.Max(bMax.Y, vn.Vertex.Y),
                        Math.Max(bMax.Z, vn.Vertex.Z)
                    );
                }
            }

            var m = 0.5f * (bMin + bMax);

            var vo = vertices.Count;

            var adjustedContours = poly.Contours
                .Select(v => new RvmContour(v.Vertices.Select(x => (x.Vertex - m, n: x.Normal)).ToArray()))
                .ToArray();

            var outJob = TessNet.Tessellate(adjustedContours);

            vertices.AddRange(outJob.VertexData.Select(v => v + m));
            normals.AddRange(outJob.NormalData);
            indices.AddRange(outJob.Indices.Select(i => i + vo));

            if (vertices.Count != normals.Count)
                throw new Exception();
        }

        return new RvmMesh(vertices.ToArray(), normals.ToArray(), indices.Select(x => (uint)x).ToArray(), 0);
    }

    private static RvmMesh TessellateCylinder(RvmCylinder cylinder, float scale, float tolerance)
    {
        //if (cullTiny && cy.radius*scale < tolerance) {
        //  tri.error = cy.radius * scale;
        //  return;
        //}

        int segments = SagittaUtils.SagittaBasedSegmentCount(Math.PI * 2, cylinder.Radius, scale, tolerance);
        int samples = segments; // Assumed to be closed

        var error = SagittaUtils.SagittaBasedError(Math.PI * 2, cylinder.Radius, scale, segments);

        const bool shell = true;
        bool[] shouldCap = { true, true };

        for (int i = 0; i < 2; i++)
        {
            var con = cylinder.Connections[i];
            if (con == null || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasCircularSide)
            {
                continue;
            }

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

        int vertCount = (2 * samples) + (shouldCap[0] ? samples : 0) + (shouldCap[1] ? samples : 0);
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];

        int trianglesN =
            (2 * samples) + (shouldCap[0] ? samples - 2 : 0) + (shouldCap[1] ? samples - 2 : 0);
        var indices = new int[trianglesN * 3];

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
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    t0[2 * i + 0],
                    t0[2 * i + 1],
                    0,
                    t1[2 * i + 0],
                    t1[2 * i + 1],
                    -h2
                );
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    t0[2 * i + 0],
                    t0[2 * i + 1],
                    0,
                    t1[2 * i + 0],
                    t1[2 * i + 1],
                    h2
                );
            }
        }

        if (shouldCap[0])
        {
            for (int i = 0; i < samples; i++)
            {
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    new Vector3(0, 0, -1),
                    new Vector3(t1[2 * i + 0], t1[2 * i + 1], -h2)
                );
            }
        }

        if (shouldCap[1])
        {
            for (int i = 0; i < samples; i++)
            {
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    new Vector3(0, 0, 1),
                    new Vector3(t1[2 * i + 0], t1[2 * i + 1], h2)
                );
            }
        }

        Debug.Assert(l == vertCount);

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

        Debug.Assert(l == trianglesN * 3);
        Debug.Assert(o == vertCount);

        return new RvmMesh(vertices, normals, indices.Select(x => (uint)x).ToArray(), error);
    }

    private static RvmMesh Tessellate(RvmSnout snout, float scale, float tolerance)
    {
        var radiusMax = Math.Max(snout.RadiusBottom, snout.RadiusTop);
        var segments = SagittaUtils.SagittaBasedSegmentCount(Math.PI * 2, radiusMax, scale, tolerance);
        var samples = segments; // assumed to be closed

        var error = SagittaUtils.SagittaBasedError(Math.PI * 2, radiusMax, scale, segments);

        const bool shell = true;
        bool[] cap = { true, true };
        for (var i = 0; i < 2; i++)
        {
            var con = snout.Connections[i];
            if (con == null || con.ConnectionTypeFlags != RvmConnection.ConnectionType.HasCircularSide)
            {
                continue;
            }

            if (ConnectionInterface.DoInterfacesMatch(snout, con))
            {
                cap[i] = false;
            }
            // else
            // {
            //     store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0x00ffff);
            // }
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
        float[] mb = { (float)Math.Tan(snout.BottomShearX), (float)Math.Tan(snout.BottomShearY) };
        float[] mt = { (float)Math.Tan(snout.TopShearX), (float)Math.Tan(snout.TopShearY) };

        var verticesN = (2 * samples) + (cap[0] ? samples : 0) + (cap[1] ? samples : 0);
        var vertices = new float[3 * verticesN];
        var normals = new float[3 * verticesN];

        var trianglesN = (2 * samples) + (cap[0] ? samples - 2 : 0) + (cap[1] ? samples - 2 : 0);
        var indices = new int[3 * trianglesN];

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
                float nz =
                    Math.Abs(snout.Height) < 0.00001f ? 0 : -(snout.RadiusTop - snout.RadiusBottom + s) / snout.Height;

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
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    nx,
                    ny,
                    nz,
                    t1[2 * i + 0] - ox,
                    t1[2 * i + 1] - oy,
                    -h2 + mb[0] * t1[2 * i + 0] + mb[1] * t1[2 * i + 1]
                );
            }
        }

        if (cap[1])
        {
            var nx = (float)(-Math.Sin(snout.TopShearX) * Math.Cos(snout.TopShearY));
            var ny = (float)(-Math.Sin(snout.TopShearY));
            var nz = (float)(Math.Cos(snout.TopShearX) * Math.Cos(snout.TopShearY));
            for (var i = 0; i < samples; i++)
            {
                l = TessellationHelpers.Vertex(
                    normals,
                    vertices,
                    l,
                    nx,
                    ny,
                    nz,
                    t2[2 * i + 0] + ox,
                    t2[2 * i + 1] + oy,
                    h2 + mt[0] * t2[2 * i + 0] + mt[1] * t2[2 * i + 1]
                );
            }
        }

        Debug.Assert(l == verticesN * 3);

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

        Debug.Assert(l == trianglesN * 3);
        Debug.Assert(o == verticesN);

        return new RvmMesh(vertices, normals, indices, error);
    }

    private static RvmMesh Tessellate(
        RvmPrimitive sphereBasedPrimitive,
        float radius,
        float arc,
        float shiftZ,
        float scaleZ,
        float scale,
        float tolerance
    )
    {
        var segments = SagittaUtils.SagittaBasedSegmentCount(Math.PI * 2, radius, scale, tolerance);
        var samples = segments; // Assumed to be closed

        var error = SagittaUtils.SagittaBasedError(Math.PI * 2, radius, scale, samples);

        bool isSphere = false;
        if (Math.PI - 1e-3 <= arc)
        {
            arc = (float)Math.PI;
            isSphere = true;
        }

        const int minRings = 3; // arc <= half_pi ? 2 : 3;
        var rings = (int)(Math.Max(minRings, scaleZ * samples * arc * (1.0f / Math.PI * 2)));

        var u0 = new int[rings];
        var t0 = new float[2 * rings];
        var thetaScale = arc / (rings - 1);
        for (var r = 0; r < rings; r++)
        {
            float theta = thetaScale * r;
            t0[2 * r + 0] = (float)Math.Cos(theta);
            t0[2 * r + 1] = (float)Math.Sin(theta);
            u0[r] = (int)(Math.Max(3.0f, t0[2 * r + 1] * samples)); // samples in this ring
        }

        u0[0] = 1;
        if (isSphere)
        {
            u0[rings - 1] = 1;
        }

        var s = 0;
        for (var r = 0; r < rings; r++)
        {
            s += u0[r];
        }

        var verticesN = s;
        var vertices = new float[3 * verticesN];
        var normals = new float[3 * verticesN];

        var l = 0;
        for (var r = 0; r < rings; r++)
        {
            var nz = t0[2 * r + 0];
            var z = radius * scaleZ * nz + shiftZ;
            var w = t0[2 * r + 1];
            var n = u0[r];

            var phiScale = Math.PI * 2 / n;
            for (var i = 0; i < n; i++)
            {
                var phi = (float)(phiScale * i + sphereBasedPrimitive.SampleStartAngle);
                var nx = (float)(w * Math.Cos(phi));
                var ny = (float)(w * Math.Sin(phi));
                l = TessellationHelpers.Vertex(normals, vertices, l, nx, ny, nz / scaleZ, radius * nx, radius * ny, z);
            }
        }

        Debug.Assert(l == verticesN * 3);

        var oC = 0;
        var indices = new List<int>();
        for (var r = 0; r + 1 < rings; r++)
        {
            var nC = u0[r];
            var nN = u0[r + 1];
            var oN = oC + nC;

            if (nC < nN)
            {
                for (var iN = 0; iN < nN; iN++)
                {
                    var iiN = (iN + 1);
                    var iC = (nC * (iN + 1)) / nN;
                    var iiC = (nC * (iiN + 1)) / nN;

                    iC %= nC;
                    iiC %= nC;
                    iiN %= nN;

                    if (iC != iiC)
                    {
                        indices.Add(oC + iC);
                        indices.Add(oN + iiN);
                        indices.Add(oC + iiC);
                    }

                    Debug.Assert(iN != iiN, $"{nameof(iN)} should not equal {nameof(iiN)}");

                    indices.Add(oC + iC);
                    indices.Add(oN + iN);
                    indices.Add(oN + iiN);
                }
            }
            else
            {
                for (var iC = 0; iC < nC; iC++)
                {
                    var iiC = (iC + 1);
                    var iN = (nN * (iC + 0)) / nC;
                    var iiN = (nN * (iiC + 0)) / nC;

                    iN %= nN;
                    iiN %= nN;
                    iiC %= nC;

                    Debug.Assert(iC != iiC, $"{nameof(iC)} should not equal {nameof(iiC)}");

                    indices.Add(oC + iC);
                    indices.Add(oN + iiN);
                    indices.Add(oC + iiC);

                    if (iN != iiN)
                    {
                        indices.Add(oC + iC);
                        indices.Add(oN + iN);
                        indices.Add(oN + iiN);
                    }
                }
            }

            oC = oN;
        }

        return new RvmMesh(vertices, normals, indices.ToArray(), error);
    }

    private static int TessellateCircle(IList<int> indices, int l, int[] t, int[] src, int n)
    {
        while (3 <= n)
        {
            int m = 0;
            int i;
            for (i = 0; i + 2 < n; i += 2)
            {
                indices[l++] = src[i];
                indices[l++] = src[i + 1];
                indices[l++] = src[i + 2];
                t[m++] = src[i];
            }

            for (; i < n; i++)
            {
                t[m++] = src[i];
            }

            n = m;

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
    private static void Swap<T>(ref T lhs, ref T rhs)
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}
