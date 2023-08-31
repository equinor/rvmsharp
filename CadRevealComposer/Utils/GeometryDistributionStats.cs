namespace CadRevealComposer.Utils;

using Primitives;
using System;
using System.Collections.Generic;

[Serializable]
public class GeometryDistributionStats
{
    public int Boxes { get; }
    public int Circles { get; }
    public int Cones { get; }
    public int EccentricCones { get; }
    public int EllipsoidSegments { get; }
    public int GeneralCylinders { get; }
    public int GeneralRings { get; }
    public int Nuts { get; }
    public int Quads { get; }
    public int TorusSegments { get; }
    public int Trapeziums { get; }
    public int InstancedMeshes { get; }
    public int TriangleMeshes { get; }

    public GeometryDistributionStats(IEnumerable<APrimitive> primitives)
    {
        foreach (APrimitive primitive in primitives)
        {
            switch (primitive)
            {
                case Box:
                    Boxes++;
                    break;
                case Circle:
                    Circles++;
                    break;
                case Cone:
                    Cones++;
                    break;
                case EccentricCone:
                    EccentricCones++;
                    break;
                case EllipsoidSegment:
                    EllipsoidSegments++;
                    break;
                case GeneralCylinder:
                    GeneralCylinders++;
                    break;
                case GeneralRing:
                    GeneralRings++;
                    break;
                case InstancedMesh:
                    InstancedMeshes++;
                    break;
                case Nut:
                    Nuts++;
                    break;
                case Quad:
                    Quads++;
                    break;
                case TorusSegment:
                    TorusSegments++;
                    break;
                case Trapezium:
                    Trapeziums++;
                    break;
                case TriangleMesh:
                    TriangleMeshes++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive));
            }
        }
    }
}
