namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.AlgebraExtensions;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

public static class EllipsoidSegmentTessellator
{
    public static IEnumerable<APrimitive> Tessellate(EllipsoidSegment ellipsoidSegment)
    {
        var horizontalRad = ellipsoidSegment.HorizontalRadius;
        var verticalRadius = ellipsoidSegment.VerticalRadius;
        var height = ellipsoidSegment.Height;
        var center = ellipsoidSegment.Center;
        var normal = ellipsoidSegment.Normal;
        var radius = verticalRadius; // Radius of a sphere (not correct)

        //var scale_z = height / horizontalRad; // horixontalRad is originally baseRadius

        var segments = TessellationUtils.SagittaBasedSegmentCount(Math.PI * 2, horizontalRad, 1f, 0.05f);
        var error = TessellationUtils.SagittaBasedError(Math.PI * 2, horizontalRad, 1f, segments);

        uint numLongitudeLines = 8; // arc <= half_pi ? 2 : 3;
        uint numLatitudeLines = numLongitudeLines;

        //number of vertices
        uint numVertices = (numLatitudeLines * (numLongitudeLines + 1)) + 2;

        Vector3[] positions = new Vector3[numVertices];
        Vector2[] texcoords = new Vector2[numVertices];

        // North pole (or one of the poles)
        positions[0] = new Vector3(0, radius, 0) + center;
        texcoords[0] = new Vector2(0, 1);

        //vertices.Add(center + Vector3.UnitY * horizontalRad);
        //texcoords.Add(Vector2.UnitY);

        // South pole (or one of the poles)
        positions[numVertices - 1] = new Vector3(0, -radius, 0) + center;
        texcoords[numVertices - 1] = new Vector2(0, 0);
        //vertices.Add(center - Vector3.UnitY * horizontalRad);
        //texcoords.Add(Vector2.Zero);

        // +1.0f because there's a gap between the poles and the first parallel.
        float latitudeSpacing = 1.0f / (numLatitudeLines + 1.0f);
        float longitudeSpacing = 1.0f / (numLongitudeLines - 1);

        // start writing new vertices at position 1
        uint v = 1;

        for (int latitude = 0; latitude < numLatitudeLines; latitude++)
        {
            for (int longitude = 0; longitude <= numLongitudeLines; longitude++)
            {
                // Scale coordinates into the 0...1 texture coordinate range,
                // with north at the top (y = 1).

                //var texcoord = new Vector2(longitude * longitudeSpacing, 1.0f - (latitude + 1) * latitudeSpacing); OLD
                //texcoords.Add(texcoord); OLD
                texcoords[v] = new Vector2(longitude * longitudeSpacing, 1.0f - ((latitude + 1) * latitudeSpacing));

                // Convert to spherical coordinates:
                // theta is a longitude angle (around the equator) in radians.
                // phi is a latitude angle (north or south of the equator).

                //float theta = texcoord.X * 2.0f * MathF.PI; OLD
                //float phi = texcoord.Y - 0.5f) * MathF.PI; OLD

                float theta = (float)(texcoords[v].X * 2.0f * Math.PI);
                float phi = (float)((texcoords[v].Y - 0.5f) * Math.PI);

                // This determines the radius of the ring of this line of latitude.
                // It's widest at the equator, and narrows as phi increases/decreases.
                float c = MathF.Cos(phi);

                // Usual formula for a vector in spherical coordinates.
                // You can exchange x & z to wind the opposite way around the sphere.
                //var vec = new Vector3(c * MathF.Cos(theta), MathF.Sin(phi), c * MathF.Sin(theta)) * horizontalRad; OLD
                //vertices.Add(center + vec); OLD

                positions[v] =
                    center
                    + new Vector3((float)(c * Math.Cos(theta)), (float)Math.Sin(phi), (float)(c * Math.Sin(theta)))
                        * radius;

                // Iterate over each quad and add indices for the two triangles that form it

                // Proceed to the next vertex.
                v++;
            }
        }

        uint numTriangles = numLatitudeLines * numLongitudeLines * 2;

        var vertices_1 = new List<uint>();

        // Might need to change the order of the two lines below
        for (uint i = 0; i < numLongitudeLines; i++)
        {
            vertices_1.Add(0);
            vertices_1.Add(i + 2);
            vertices_1.Add(i + 1);
        }

        // Each row has one more unique vertex than there are lines of longitude,
        // since we double a vertex at the texture seam.
        uint rowLength = numLongitudeLines + 1;

        for (uint latitude = 0; latitude < numLatitudeLines - 1; latitude++)
        {
            // Plus one for the pole.
            uint rowStart = (latitude * rowLength + 1);
            for (uint longitude = 0; longitude < numLongitudeLines; longitude++)
            {
                uint firstCorner = (rowStart + longitude);

                // First triangle of quad: Top-Left, Bottom-Left, Bottom-Right
                vertices_1.Add(firstCorner);
                vertices_1.Add(firstCorner + rowLength + 1);
                vertices_1.Add(firstCorner + rowLength);

                // Second triangle of quad: Top-Left, Bottom-Right, Top-Right
                vertices_1.Add(firstCorner);
                vertices_1.Add(firstCorner + 1);
                vertices_1.Add(firstCorner + rowLength + 1);
            }
        }

        uint pole = (uint)(positions.Length - 1);
        uint bottomRow = (numLatitudeLines - 1) * rowLength + 1;

        for (int i = 0; i < numLongitudeLines; i++)
        {
            vertices_1.Add(pole);
            vertices_1.Add((uint)(bottomRow + i));
            vertices_1.Add((uint)(bottomRow + i + 1));
        }

        var vertices = positions;
        var indices = vertices_1;

        var mesh = new Mesh(vertices, indices.ToArray(), error);
        yield return new TriangleMesh(
            mesh,
            ellipsoidSegment.TreeIndex,
            ellipsoidSegment.Color,
            ellipsoidSegment.AxisAlignedBoundingBox
        );
    }
}
