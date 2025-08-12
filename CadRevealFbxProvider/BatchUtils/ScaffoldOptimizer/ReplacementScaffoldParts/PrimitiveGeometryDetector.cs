namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealComposer.Tessellation;

public class PrimitiveGeometryDetector
{
    public enum PrimitiveGeometry
    {
        Cylinder,
        Cuboid,
        Ellipsoid,
        Unknown
    }

    public PrimitiveGeometry DetectedGeometry { get; private set; }
    public Vector3 MajorAxis { get; private set; }
    public Vector3 SemiMajorAxis { get; private set; }
    public Vector3 CenterPosition { get; private set; }
    public float CylinderRadiusMinor { get; private set; }
    public float CylinderRadiusMajor { get; private set; }
    public float CylinderHeight { get; private set; }
    public float CuboidShortestEdgeLength { get; private set; }
    public float CuboidIntermediateEdgeLength { get; private set; }
    public float CuboidLongestEdgeLength { get; private set; }
    public float EllipsoidRadiusMinor { get; private set; }
    public float EllipsoidRadiusSemiMajor { get; private set; }
    public float EllipsoidRadiusMajor { get; private set; }

    public PrimitiveGeometryDetector(Mesh mesh)
    {
        List<Vector3> vertices = mesh.Vertices.ToList();
        PcaResult3 pca = PrincipalComponentAnalyzer.Invoke(vertices);
        DoPrimitiveGeometryDetection(vertices, pca);
    }

    private void DoPrimitiveGeometryDetection(List<Vector3> vertices, PcaResult3 pca, bool recursiveCall = false)
    {
        var u = new List<(int a, int b)> { (0, 1), (0, 2), (1, 2) };
        var ellipseDetectedInPlane = new List<(bool y, float rMinor, float rMajor)>
        {
            (false, 0, 0),
            (false, 0, 0),
            (false, 0, 0)
        };
        var rectangleDetectedInPlane = new List<(bool y, float hMin, float hMax, float vMin, float vMax)>
        {
            (false, 0, 0, 0, 0),
            (false, 0, 0, 0, 0),
            (false, 0, 0, 0, 0)
        };

        // Check the shape of the projected points in the planes formed by the principal components
        for (int k = 0; k < 3; k++)
        {
            // Calculate the projections onto the plane
            List<Vector3> local = ProjectToLocal3DCoordinates(vertices, pca, u[k].a, u[k].b);

            // Calculate the projections along axis in each plane, in the plane 2D coordinate system
            var projections = local
                .Select(x => (a: Vector3.Dot(x, pca.V(u[k].a)), b: Vector3.Dot(x, pca.V(u[k].b))))
                .ToList();

            // Check if the points in the plane form an ellipse
            ellipseDetectedInPlane[k] = IsEllipse(local, pca, projections, u[k].a, u[k].b);
            rectangleDetectedInPlane[k] = IsRectangle(local, pca, projections, u[k].a, u[k].b);
        }

        // For each plane, determine the shape
        CenterPosition = vertices.Aggregate(Vector3.Zero, (acc, x) => acc + x) / vertices.Count;
        MajorAxis = pca.V(0);
        SemiMajorAxis = pca.V(1);

        if (ellipseDetectedInPlane.Count(x => x.y) == 3) // Ellipsoid detected
        {
            DetectedGeometry = PrimitiveGeometry.Ellipsoid;
            EllipsoidRadiusMinor = ellipseDetectedInPlane.Min(x => x.rMinor);
            EllipsoidRadiusSemiMajor = ellipseDetectedInPlane.Min(x => x.rMajor);
            EllipsoidRadiusMajor = ellipseDetectedInPlane.Max(x => x.rMajor);
        }
        else if (rectangleDetectedInPlane.Count(x => x.y) == 3) // Cuboid detected
        {
            DetectedGeometry = PrimitiveGeometry.Cuboid;
            float shortestVerticalEdge = rectangleDetectedInPlane.Min(x => x.vMax - x.vMin);
            float shortestHorizontalEdge = rectangleDetectedInPlane.Min(x => x.hMax - x.hMin);
            float longestVerticalEdge = rectangleDetectedInPlane.Max(x => x.vMax - x.vMin);
            float longestHorizontalEdge = rectangleDetectedInPlane.Max(x => x.hMax - x.hMin);
            CuboidShortestEdgeLength = Math.Min(shortestVerticalEdge, shortestHorizontalEdge);
            CuboidIntermediateEdgeLength = Math.Max(shortestVerticalEdge, shortestHorizontalEdge);
            CuboidLongestEdgeLength = Math.Max(longestVerticalEdge, longestHorizontalEdge);
        }
        else if (ellipseDetectedInPlane.Count(x => x.y) == 1 && rectangleDetectedInPlane.Count(x => x.y) == 2) // Cylinder detected
        {
            DetectedGeometry = PrimitiveGeometry.Cylinder;

            var ellipsePlane = ellipseDetectedInPlane.Select((v, i) => new { v = v, i = i }).First(x => x.v.y);
            CylinderRadiusMinor = ellipsePlane.v.rMinor;
            CylinderRadiusMajor = ellipsePlane.v.rMajor;

            var rectangleContainingHeight = rectangleDetectedInPlane[(ellipsePlane.i + 1) % 3];
            float heightH = rectangleContainingHeight.hMax - rectangleContainingHeight.hMin;
            float heightV = rectangleContainingHeight.vMax - rectangleContainingHeight.vMin;
            CylinderHeight = Math.Max(heightH, heightV);
        }
        else if (!recursiveCall && ArePrincipalComponentsSymmetricInExactlyTwoAxis(pca)) // May be cuboid with one quadratic face
        {
            (int equal1, int equal2, int different) = DetermineEqualAndDifferentPrincipalComponents(pca);
            pca = RedoPcaAssumingCuboidWithOneQuadraticFace(pca, vertices, different, equal1, equal2);
            DoPrimitiveGeometryDetection(vertices, pca, true);
        }
        else if (!recursiveCall && ArePrincipalComponentsSymmetricInAllAxis(pca)) // May be a cube (i.e., all quadratic faces)
        {
            pca = RedoPcaAssumingACube(pca, vertices);
            DoPrimitiveGeometryDetection(vertices, pca, true);
        }
        else
        {
            DetectedGeometry = PrimitiveGeometry.Unknown;
            CylinderRadiusMinor = 0.0f;
            CylinderRadiusMajor = 0.0f;
            CylinderHeight = 0.0f;
        }
    }

    private static (bool y, float hMin, float hMax, float vMin, float vMax) IsRectangle(
        List<Vector3> local,
        PcaResult3 pca,
        List<(float a, float b)> projections,
        int ua,
        int ub
    )
    {
        // Calculate the rectangle extents in case of a rectangular shape of the points in the plane
        float horizontalMax = projections.Max(p => p.a);
        float horizontalMin = projections.Min(p => p.a);
        float verticalMax = projections.Max(p => p.b);
        float verticalMin = projections.Min(p => p.b);
        const float border = 0.1f;
        float hMaxWithBorder = horizontalMax + border;
        float hMinWithBorder = horizontalMin - border;
        float vMaxWithBorder = verticalMax + border;
        float vMinWithBorder = verticalMin - border;

        // Calculate bin indices for each angle for each plane
        const int binCount = 8;
        var hBinIndices = projections
            .Select(b => (int)Math.Round((b.a - horizontalMin) * (binCount - 1) / (horizontalMax - horizontalMin)))
            .ToList();
        var vBinIndices = projections
            .Select(b => (int)Math.Round((b.b - verticalMin) * (binCount - 1) / (verticalMax - verticalMin)))
            .ToList();

        // For each plane, perform the binning procedure
        const float epsilonV = 0.01f;
        const float epsilonH = 0.01f;
        var satisfiedRectangleRequirementsTop = Enumerable.Repeat(0, (int)binCount).ToList();
        var satisfiedRectangleRequirementsBottom = Enumerable.Repeat(0, (int)binCount).ToList();
        var satisfiedRectangleRequirementsLeft = Enumerable.Repeat(0, (int)binCount).ToList();
        var satisfiedRectangleRequirementsRight = Enumerable.Repeat(0, (int)binCount).ToList();
        var satisfiedRectangleRequirementsWithinH = Enumerable.Repeat(0, (int)binCount).ToList();
        var satisfiedRectangleRequirementsWithinV = Enumerable.Repeat(0, (int)binCount).ToList();
        for (int i = 0; i < hBinIndices.Count; i++)
        {
            satisfiedRectangleRequirementsTop[hBinIndices[i]] +=
                (projections[i].b >= vMinWithBorder && projections[i].b <= vMaxWithBorder)
                && Math.Abs(projections[i].b - verticalMax) < epsilonV
                    ? 1
                    : 0;
            satisfiedRectangleRequirementsBottom[hBinIndices[i]] +=
                (projections[i].b >= vMinWithBorder && projections[i].b <= vMaxWithBorder)
                && Math.Abs(projections[i].b - verticalMin) < epsilonV
                    ? 1
                    : 0;
            satisfiedRectangleRequirementsWithinH[hBinIndices[i]] +=
                (projections[i].b >= vMinWithBorder && projections[i].b <= vMaxWithBorder) ? 1 : 0;
        }
        for (int i = 0; i < vBinIndices.Count; i++)
        {
            satisfiedRectangleRequirementsLeft[vBinIndices[i]] +=
                (projections[i].a >= hMinWithBorder && projections[i].a <= hMaxWithBorder)
                && Math.Abs(projections[i].a - horizontalMin) < epsilonH
                    ? 1
                    : 0;
            satisfiedRectangleRequirementsRight[vBinIndices[i]] +=
                (projections[i].a >= hMinWithBorder && projections[i].a <= hMaxWithBorder)
                && Math.Abs(projections[i].a - horizontalMax) < epsilonH
                    ? 1
                    : 0;
            satisfiedRectangleRequirementsWithinV[vBinIndices[i]] +=
                (projections[i].a >= hMinWithBorder && projections[i].a <= hMaxWithBorder) ? 1 : 0;
        }

        // Check if the rectangle requirements are satisfied for each plane
        var reqList = new List<bool>();
        reqList.Add(satisfiedRectangleRequirementsTop[0] >= 1);
        reqList.Add(satisfiedRectangleRequirementsTop[binCount - 1] >= 1);
        reqList.Add(satisfiedRectangleRequirementsBottom[0] >= 1);
        reqList.Add(satisfiedRectangleRequirementsBottom[binCount - 1] >= 1);
        reqList.Add(satisfiedRectangleRequirementsLeft[0] >= 1);
        reqList.Add(satisfiedRectangleRequirementsLeft[binCount - 1] >= 1);
        reqList.Add(satisfiedRectangleRequirementsRight[0] >= 1);
        reqList.Add(satisfiedRectangleRequirementsRight[binCount - 1] >= 1);
        reqList.Add(
            satisfiedRectangleRequirementsTop
                .Select((x, i) => new { val = x, index = i })
                .All(x => satisfiedRectangleRequirementsWithinH[x.index] == 0 || x.val >= 1)
        );
        reqList.Add(
            satisfiedRectangleRequirementsBottom
                .Select((x, i) => new { val = x, index = i })
                .All(x => satisfiedRectangleRequirementsWithinH[x.index] == 0 || x.val >= 1)
        );
        reqList.Add(
            satisfiedRectangleRequirementsLeft
                .Select((x, i) => new { val = x, index = i })
                .All(x => satisfiedRectangleRequirementsWithinV[x.index] == 0 || x.val >= 1)
        );
        reqList.Add(
            satisfiedRectangleRequirementsRight
                .Select((x, i) => new { val = x, index = i })
                .All(x => satisfiedRectangleRequirementsWithinV[x.index] == 0 || x.val >= 1)
        );

        return (reqList.All(x => x), horizontalMin, horizontalMax, verticalMin, verticalMax);
    }

    private static PcaResult3 RedoPcaAssumingCuboidWithOneQuadraticFace(
        PcaResult3 pca,
        List<Vector3> vertices,
        int principleComp,
        int equalComp1,
        int equalComp2
    )
    {
        // Calculate the local coordinates of the vertices in the plane formed by the two equal principal components
        List<Vector3> local = ProjectToLocal3DCoordinates(vertices, pca, equalComp1, equalComp2);

        // Find the largest radial component. This will correspond to the corners of the square face.
        float maxR2 = local.Select(x => x.LengthSquared()).Max();

        // Making an assumption the vertices form a cuboid with one quadratic face, we attempt finding its corner vertices
        var cornerVertices = local
            .Select((x, i) => new { x, i })
            .Where(x => Math.Abs(x.x.LengthSquared() - maxR2) < 0.01f)
            .ToList();

        // Find two linearly independent corner radial vectors
        Vector3 v1 = (cornerVertices.Count > 0) ? cornerVertices[0].x : new Vector3();
        Vector3 v2 =
            cornerVertices.FirstOrDefault(x => Vector3.Cross(x.x, v1).LengthSquared() > 0.01f)?.x ?? new Vector3();

        // Add and subtract the corner vectors to find the vectors pointing normal to the quadratic faces
        Vector3 u1 = Vector3.Normalize(v1 + v2);
        Vector3 u2 = Vector3.Normalize(v1 - v2);

        // Update the principal components to reflect the assumption of a cuboid with one quadratic face
        return new PcaResult3(
            pca.V(principleComp),
            u1,
            u2,
            pca.Lambda(principleComp),
            pca.Lambda(equalComp1),
            pca.Lambda(equalComp2)
        );
    }

    private static (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) ExtractFourLinearlyIndependentCornerVectors(
        List<Vector3> vectors
    )
    {
        if (vectors.Count < 4)
            throw new ArgumentException(
                "At least four vectors are required to extract enough linearly independent vectors."
            );

        var uniqueVectors = new List<Vector3>();
        foreach (var vector in vectors)
        {
            // Continue if the vector is collinear with any of the already added unique vectors
            foreach (Vector3 uniqueVector in uniqueVectors)
            {
                if (Math.Abs(Vector3.Cross(vector, uniqueVector).LengthSquared()) < 0.01f)
                    continue;
            }

            // Add the vector to the unique list if it is not collinear with any of the existing unique vectors
            uniqueVectors.Add(vector);
        }

        if (uniqueVectors.Count < 4)
            throw new ArgumentException("At least four unique vectors are required to continue.");

        return (uniqueVectors[0], uniqueVectors[1], uniqueVectors[2], uniqueVectors[3]);
    }

    private static PcaResult3 RedoPcaAssumingACube(PcaResult3 pca, List<Vector3> vertices)
    {
        // Find center of the three dimensional point cloud (i.e., not of the projected points)
        Vector3 center = vertices.Aggregate(Vector3.Zero, (acc, x) => acc + x) / vertices.Count;

        // Find the local vertices, relative to the center
        List<Vector3> local3 = vertices.Select(x => x - center).ToList();

        // Calculate the radial maximum of the vertices, which corresponds to a cube corner if the shape is a cube
        float maxR2 = local3.Select(x => x.LengthSquared()).Max();

        // Find the corners of the three-dimensional cube (making the assumption that the points form a cube)
        var cornerVertices = local3.Where(x => Math.Abs(x.LengthSquared() - maxR2) < 0.01f).ToList();

        // Find four linearly independent corner radial vectors
        var (a, b, c, d) = ExtractFourLinearlyIndependentCornerVectors(cornerVertices);

        // Find the general direction towards the face from the first two independent vectors
        Vector3 s = Vector3.Normalize(a + b);

        // Find the corner vectors that point in the same general direction as the face pointed to by s
        Vector3 v3 = Vector3.Dot(s, c) > 0 ? c : -c;
        Vector3 v4 = Vector3.Dot(s, d) > 0 ? d : -d;

        // Find the first principal component, which will be the sum of all components pointing in the same direction as s
        Vector3 u1 = Vector3.Normalize(a + b + v3 + v4);

        // We will only find some vectors that are orthogonal to u1 and to each other as a starting point
        Vector3 u2 = Vector3.Normalize(Vector3.Cross(u1, a));
        Vector3 u3 = Vector3.Normalize(Vector3.Cross(u2, u1));

        // We now have three orthogonal vectors, where only one is aligned with the cube geometry, which allows us to invoke the algorithm for a cuboid with one quadratic face
        var pcaIntermediate = new PcaResult3(u1, u2, u3, pca.Lambda(0), pca.Lambda(1), pca.Lambda(2));
        return RedoPcaAssumingCuboidWithOneQuadraticFace(pcaIntermediate, vertices, 0, 1, 2);
    }

    private static bool ArePrincipalComponentsSymmetricInExactlyTwoAxis(PcaResult3 pca)
    {
        // Check if two of the principal components are equal and the third one is different
        return (Math.Abs(pca.Lambda(0) - pca.Lambda(1)) < 0.01f && Math.Abs(pca.Lambda(2) - pca.Lambda(0)) > 0.01f)
            || (Math.Abs(pca.Lambda(0) - pca.Lambda(2)) < 0.01f && Math.Abs(pca.Lambda(1) - pca.Lambda(0)) > 0.01f)
            || (Math.Abs(pca.Lambda(1) - pca.Lambda(2)) < 0.01f && Math.Abs(pca.Lambda(0) - pca.Lambda(1)) > 0.01f);
    }

    private static bool ArePrincipalComponentsSymmetricInAllAxis(PcaResult3 pca)
    {
        // Check if two of the principal components are equal and the third one is different
        return (Math.Abs(pca.Lambda(0) - pca.Lambda(1)) < 0.01f && Math.Abs(pca.Lambda(0) - pca.Lambda(2)) < 0.01f);
    }

    private static (int equa1, int equal2, int different) DetermineEqualAndDifferentPrincipalComponents(PcaResult3 pca)
    {
        // Determine which principal components are equal and which one is different
        if (Math.Abs(pca.Lambda(0) - pca.Lambda(1)) < 0.01f && Math.Abs(pca.Lambda(2) - pca.Lambda(0)) > 0.01f)
            return (0, 1, 2);
        if (Math.Abs(pca.Lambda(0) - pca.Lambda(2)) < 0.01f && Math.Abs(pca.Lambda(1) - pca.Lambda(0)) > 0.01f)
            return (0, 2, 1);
        if (Math.Abs(pca.Lambda(1) - pca.Lambda(2)) < 0.01f && Math.Abs(pca.Lambda(0) - pca.Lambda(1)) > 0.01f)
            return (1, 2, 0);

        return (-1, -1, -1); // No two components are equal
    }

    private static List<Vector3> ProjectToLocal3DCoordinates(
        List<Vector3> vertices,
        PcaResult3 pca,
        int pcaProjAxisA,
        int pcaProjAxisB
    )
    {
        // Project all points onto the planes formed by the principal components
        var plane = vertices
            .Select(p =>
                Vector3.Dot(p, pca.V(pcaProjAxisA)) * pca.V(pcaProjAxisA)
                + Vector3.Dot(p, pca.V(pcaProjAxisB)) * pca.V(pcaProjAxisB)
            )
            .ToList();

        // Calculate the center of each plane
        var center = plane.Aggregate(Vector3.Zero, (acc, x) => acc + x) / plane.Count;

        // Calculate the local coordinates of the points in each plane
        return plane.Select(p => p - center).ToList();
    }

    private static (bool y, float rMinor, float rMajor) IsEllipse(
        List<Vector3> local,
        PcaResult3 pca,
        List<(float a, float b)> projections,
        int ua,
        int ub
    )
    {
        // Calculate the ellipse radii in case of an elliptical shape of the points in the plane
        float maxA = projections.Max(p => p.a * p.a);
        float maxB = projections.Max(p => p.b * p.b);
        float rMinorSqr = Math.Min(maxA, maxB);
        float rMajorSqr = Math.Max(maxA, maxB);
        float rMinor = (float)Math.Sqrt(rMinorSqr);
        float rMajor = (float)Math.Sqrt(rMajorSqr);

        // Calculate the relative angle of each point in the planes
        var relAngles = projections
            .Select(xi => xi.a == 0 ? Math.PI / 2.0f : Math.Atan(Math.Abs(xi.b) / Math.Abs(xi.a)))
            .ToList();

        // Calculate the absolute angle of each point in the planes
        var absAngles = relAngles
            .Select((angle, i) => RelAngleToAbsoluteAngle(projections[i].a, projections[i].b, (float)angle))
            .ToList();

        // Calculate bin indices for each angle for each plane
        const float binCount = 8.0f;
        var binIndices = absAngles
            .Select(theta => (int)Math.Round(theta * (binCount - 1.0f) / (2.0f * Math.PI)))
            .ToList();

        // For each plane, perform the binning procedure
        const float epsilon2 = 0.01f;
        const float border = 0.5f;
        var satisfiedEllipseRequirements = Enumerable.Repeat(0, (int)binCount).ToList();
        for (int i = 0; i < binIndices.Count; i++)
        {
            var g = CalcIdealEllipsePoint(projections[i].a, projections[i].b, rMinor, rMajor, pca.V(ua), pca.V(ub));
            var dist = local[i] - g;
            var distSqr = Vector3.Dot(dist, dist);
            var gSqr = Vector3.Dot(g, g);
            var localSqr = Vector3.Dot(local[i], local[i]);

            satisfiedEllipseRequirements[binIndices[i]] += (distSqr < epsilon2 && localSqr <= (gSqr + border)) ? 1 : 0;
        }

        // Check if the ellipse requirements are satisfied for each plane
        return (satisfiedEllipseRequirements.All(x => x >= 1), rMinor, rMajor);

        // Local functions
        float RelAngleToAbsoluteAngle(float a, float b, float relAngle)
        {
            if (a > 0 && b > 0)
                return relAngle;
            if (a == 0 && b > 0)
                return (float)(Math.PI / 2.0);
            if (a < 0 && b > 0)
                return (float)(Math.PI - relAngle);
            if (a < 0 && b == 0)
                return (float)Math.PI;
            if (a < 0 && b < 0)
                return (float)(Math.PI + relAngle);
            if (a == 0 && b < 0)
                return (float)(3.0 * Math.PI / 2.0);
            if (a > 0 && b < 0)
                return (float)(2.0 * Math.PI - relAngle);
            if (a > 0 && b == 0)
                return 0.0f;

            return 0.0f;
        }

        Vector3 CalcIdealEllipsePoint(float projU1, float projU2, float r, float R, Vector3 u1, Vector3 u2)
        {
            float q = (R * projU2) / (r * projU1);
            float qInv = 1.0f / q;
            float sinT = Math.Sign(projU2) * float.Sqrt(1.0f / (1.0f + qInv * qInv));
            float cosT = Math.Sign(projU1) * float.Sqrt(1.0f / (1.0f + q * q));
            return R * cosT * u1 + r * sinT * u2;
        }
    }
}
