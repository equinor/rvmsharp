namespace CadRevealComposer.Primitives.Instancing
{
    using Newtonsoft.Json;
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Utils;


    // https://github.com/equinor/ModelOptimizationPipeline/blob/fed1215dfa26372ff0d0cb26e959cd1e8e8e85c8/tools/mop/Mop/ModelExtensions/MOPQuaternion.cs#L150
    public static class RvmFacetGroupMatcher
    {
        /// <summary>
        /// Matches a to b and returns true if meshes are alike and sets transform so that a * transform = b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="outputTransform"></param>
        /// <returns></returns>
        public static bool Match(RvmFacetGroup a, RvmFacetGroup b, [NotNullWhen(true)] out Matrix4x4? outputTransform)
        {
            // probably a bad assumption: polygons are ordered, contours are ordered, vertexes are ordered

            // TODO: order polygons by contour count, order subtractive contours by vertex count
            // TODO: contour count and vertex count are not unique, must be handled properly

            if (a.Polygons.Length != b.Polygons.Length)
            {
                outputTransform = default;
                return false;
            }

            // create transform matrix
            if (!TryGetTransform(a, b, out var transform))
            {
                outputTransform = default;
                return false;
            }

            // check all polygons with transform
            for (var i = 0; i < a.Polygons.Length; i++)
            {
                var aPolygon = a.Polygons[i];
                var bPolygon = b.Polygons[i];

                if (aPolygon.Contours.Length != bPolygon.Contours.Length)
                {
                    outputTransform = default;
                    return false;
                }

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    if (aContour.Vertices.Length != bContour.Vertices.Length)
                    {
                        outputTransform = default;
                        return false;
                    }

                    for (var k = 0; k < aContour.Vertices.Length; k++)
                    {
                        if (!MatchVertexApproximately(aContour.Vertices[k].Vertex, bContour.Vertices[k].Vertex, transform.Value))
                        {
                            outputTransform = default;
                            return false;
                        }
                    }
                }
            }

            outputTransform = transform;
            return true;
        }

        public static bool TryGetTransform(RvmFacetGroup a, RvmFacetGroup b, [NotNullWhen(true)] out Matrix4x4? transform)
        {
            for (var i = 0; i < a.Polygons.Length; i++)
            {
                var aPolygon = a.Polygons[i];
                var bPolygon = b.Polygons[i];

                if (aPolygon.Contours.Length != bPolygon.Contours.Length)
                {
                    transform = default;
                    return false;
                }

                for (var j = 0; j < aPolygon.Contours.Length; j++)
                {
                    var aContour = aPolygon.Contours[j];
                    var bContour = bPolygon.Contours[j];

                    if (aContour.Vertices.Length != bContour.Vertices.Length)
                    {
                        transform = default;
                        return false;
                    }

                    if (aContour.Vertices.Length < 3)
                    {
                        continue; // need at least 3 vertexes to calculate transform
                    }

                    // TODO: special case if only one contour
                    // TODO: assumption: all vertices of a contour are in the same plane

                    // find a fourth vertex not in the same plane as the 3 first
                    var pa1 = aContour.Vertices[0].Vertex;
                    var va12 = aContour.Vertices[1].Vertex - aContour.Vertices[0].Vertex;
                    var va13 = aContour.Vertices[2].Vertex - aContour.Vertices[0].Vertex;

                    var fourthVertex = a.Polygons
                        .Select((p, index) => (Polygon: p, PolygonIndex: index))
                        .SelectMany(
                            x => x.Polygon.Contours.Select((c, i) => (Contour: c, CountourIndex: i)),
                            (x, c) => (x.Polygon, x.PolygonIndex, c.Contour, c.CountourIndex))
                        .SelectMany(
                            x => x.Contour.Vertices.Select((c, i) => (Vertex: c, VertexIndex: i)),
                            (x, v) => (x.Polygon, x.PolygonIndex, x.Contour, x.CountourIndex, v.Vertex.Vertex, v.VertexIndex))
                        .FirstOrDefault(x => !Determinant(va12, va13, x.Vertex - pa1).ApproximatelyEquals(0f, 0.001f));

                    if (fourthVertex == default)
                    {
                        transform = default;
                        return false;
                    }

                    // calculate transform from first 3 vertices
                    return TryCalculateTransform(
                        aContour.Vertices[0].Vertex,
                        aContour.Vertices[1].Vertex,
                        aContour.Vertices[2].Vertex,
                        a.Polygons[fourthVertex.PolygonIndex].Contours[fourthVertex.CountourIndex].Vertices[fourthVertex.VertexIndex].Vertex,
                        bContour.Vertices[0].Vertex,
                        bContour.Vertices[1].Vertex,
                        bContour.Vertices[2].Vertex,
                        b.Polygons[fourthVertex.PolygonIndex].Contours[fourthVertex.CountourIndex].Vertices[fourthVertex.VertexIndex].Vertex,
                        out transform
                    );
                }
            }

            transform = default;
            return false;
        }

        public static float Determinant(Vector3 a, Vector3 b, Vector3 c)
        {
            return
                a.X * b.Y * c.Z +
                b.X * c.Y * a.Z +
                c.X * a.Y * b.Z -
                c.X * b.Y * a.Z -
                b.X * a.Y * c.Z -
                a.X * c.Y * b.Z;
        }

        public static bool TryCalculateTransform(Vector3 pa1, Vector3 pa2, Vector3 pa3, Vector3 pa4, Vector3 pb1, Vector3 pb2, Vector3 pb3, Vector3 pb4, [NotNullWhen(true)] out Matrix4x4? transform)
        {
            var va12 = pa2 - pa1;
            var va13 = pa3 - pa1;
            var va14 = pa4 - pa1;
            var vb12 = pb2 - pb1;
            var vb13 = pb3 - pb1;
            var vb14 = pb4 - pb1;

            var vaMatrix = new Matrix4x4(
                va12.X * va12.X,va12.Y * va12.Y,va12.Z * va12.Z, 0,
                va13.X * va13.X,va13.Y * va13.Y,va13.Z * va13.Z, 0,
                va14.X * va14.X,va14.Y * va14.Y,va14.Z * va14.Z, 0,
                0, 0, 0, 1);

            var squaredBLengths = new Vector3(vb12.Length() * vb12.Length(), vb13.Length() * vb13.Length(), vb14.Length() * vb14.Length());
            if (!Matrix4x4.Invert(vaMatrix, out var vaMatrixInverse))
            {
                throw new Exception("Could not invert matrix for scale");
            }
            var scale = new Vector3(
                MathF.Sqrt(vaMatrixInverse.M11 * squaredBLengths.X + vaMatrixInverse.M12 * squaredBLengths.Y + vaMatrixInverse.M13 * squaredBLengths.Z),
                MathF.Sqrt(vaMatrixInverse.M21 * squaredBLengths.X + vaMatrixInverse.M22 * squaredBLengths.Y + vaMatrixInverse.M23 * squaredBLengths.Z),
                MathF.Sqrt(vaMatrixInverse.M31 * squaredBLengths.X + vaMatrixInverse.M32 * squaredBLengths.Y + vaMatrixInverse.M33 * squaredBLengths.Z));

            va12 = va12 * scale;
            va13 = va13 * scale;

            // 2 rotation va'1,va'2 -> vb1,vb2
            var vaNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(va12), Vector3.Normalize(va13)));
            var vbNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(vb12), Vector3.Normalize(vb13)));
            var rot1 = vaNormal.FromToRotation(vbNormal);

            // 3 axis rotation: axis=vb2-vb1 va'3-va'1
            var va12r1 = Vector3.Transform(va12, rot1);
            var va13r1 = Vector3.Transform(va13, rot1);
            var angle2 = va12r1.AngleTo(vb12);
            var rotationNormal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(va12r1), Vector3.Normalize(vb12)));
            var rot2 = Quaternion.CreateFromAxisAngle(rotationNormal, angle2);

            var rotation = rot2 * rot1;

            // scale
            var va12rt = Vector3.Transform(va12, rotation);
            var va13rt = Vector3.Transform(va13, rotation);

            var data = new VisData();
            data.Arrows = new[]
            {
                new VisData.Arrow(Color.Red, pa1, va12),
                new VisData.Arrow(Color.Red, pa1, va13),
                new VisData.Arrow(Color.Blue, pb1, vb12),
                new VisData.Arrow(Color.Blue, pb1, vb13),
                new VisData.Arrow(Color.Pink, pa1, va12r1),
                new VisData.Arrow(Color.Pink, pa1, va13r1),
                new VisData.Arrow(Color.Green, pa1, va12rt),
                new VisData.Arrow(Color.Green, pa1, va13rt),
                //new VisData.Arrow(Color.Aqua, pb1, vbNormal)
            };
            File.WriteAllText("/Users/GUSH/projects/rvmsharp-vis/src/data.json", JsonConvert.SerializeObject(data, Formatting.Indented));


            // translation
            var translation = pb1 - Vector3.Transform(pa1 * scale, rotation);

            transform =
                Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(translation);
            return true;
        }

        public static Vector3 StoreEulers;
        public static Vector3 StoreScale;

        /// <summary>
        /// Match a * transform = b
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MatchVertexApproximately(Vector3 a, Vector3 b, Matrix4x4 transform)
        {
            return Vector3.Transform(a, transform).ApproximatelyEquals(b, tolerance: 0.01f); // TODO: ok tolerance?
        }
    }
}
