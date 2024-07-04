namespace CadRevealFbxProvider;

using BatchUtils;
using Ben.Collections.Specialized;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using Commons;
using g3;
using System.Diagnostics;
using System.Numerics;
using MIConvexHull;
using Utils;
using System.Diagnostics.Metrics;
using CadRevealComposer.Utils.MeshOptimization;
using System.Linq;
using CadRevealComposer.Operations.Tessellating;
using System.Drawing;

public class FbxProvider : IModelFormatProvider
{
    public (IReadOnlyList<CadRevealNode>, ModelMetadata?) ParseFiles(
        IEnumerable<FileInfo> filesToParse,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        var workload = FbxWorkload.CollectWorkload(filesToParse.Select(x => x.FullName).ToArray());
        if (!workload.Any())
        {
            Console.WriteLine("Found no .fbx files. Skipping FBX Parser.");
            return (new List<CadRevealNode>(), null);
        }

        var fbxTimer = Stopwatch.StartNew();

        var teamCityReadFbxFilesLogBlock = new TeamCityLogBlock("Reading Fbx Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());

        (var nodes, var metadata) = FbxWorkload.ReadFbxData(
            workload,
            treeIndexGenerator,
            instanceIdGenerator,
            nodeNameFiltering,
            progressReport,
            stringInternPool
        );
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.fbxFilename).Length);
        teamCityReadFbxFilesLogBlock.CloseBlock();

        if (workload.Length == 0)
        {
            // returns empty list if there are no rvm files to process
            return (new List<CadRevealNode>(), null);
        }

        Console.WriteLine(
            $"Read FbxData in {fbxTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .fbx files (excluding evtl .csv file size))"
        );

        return (nodes, metadata);
    }

    // NOTE: This method is a copy from somewhere else in the code and should be
    // placed a common place, if it is to be used here!!!!
    private static DMesh3 ConvertMeshToDMesh3(Mesh mesh)
    {
        return DMesh3Builder.Build<
            Vector3d,
            int,
            object /* Normals */
        >(mesh.Vertices.Select(Vec3ToVec3d), mesh.Indices.Select(x => (int)x));
    }

    // NOTE: This method is a copy from somewhere else in the code and should be
    // placed a common place, if it is to be used here!!!!
    static Vector3d Vec3ToVec3d(Vector3 vec3f)
    {
        return new Vector3d(vec3f.X, vec3f.Y, vec3f.Z);
    }

    // NOTE: This method is a copy from somewhere else in the code and should be
    // placed a common place, if it is to be used here!!!!
    static Vector3 Vec3fToVec3(Vector3f vec3f)
    {
        return new Vector3(vec3f.x, vec3f.y, vec3f.z);
    }

    // NOTE: This method is a copy from somewhere else in the code and should be
    // placed a common place, if it is to be used here!!!!
    private static Mesh ConvertDMesh3ToMesh(DMesh3 dMesh3)
    {
        var vertices = new Vector3[dMesh3.VertexCount];

        for (int vertexIndex = 0; vertexIndex < dMesh3.VertexCount; vertexIndex++)
        {
            vertices[vertexIndex] = Vec3fToVec3(dMesh3.GetVertexf(vertexIndex));
        }

        var mesh = new Mesh(vertices, dMesh3.Triangles().SelectMany(x => x.array).Select(x => (uint)x).ToArray(), 0.0f);
        return mesh;
    }

    private APrimitive[] ConvertAllToAxisAlignedBoundaries(APrimitive[] geometries)
    {
        int triangleCount = 0;
        int originalTriangleCount = 0;
        APrimitive[] geometriesOut = new APrimitive[geometries.Length];
        for (int i=0; i<geometries.Length; i++)
        {
            var tG = geometries[i];
            Console.WriteLine($"Type is {tG.GetType()}");
            if (tG is InstancedMesh)
            {
                var t = (InstancedMesh)tG;
                originalTriangleCount += t.TemplateMesh.TriangleCount;

                Console.WriteLine($"{t.GetType().Name}: {t.TemplateMesh.Vertices.Length}");

                Vector3[] boundingBoxVertices = new Vector3[8];

                BoundingBox boundingBox = t.TemplateMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
                Vector3 d = boundingBox.Max - boundingBox.Min;
                boundingBoxVertices[0] = boundingBox.Min;
                boundingBoxVertices[1] = boundingBox.Min + new Vector3(d.X, 0.0f, 0.0f);
                boundingBoxVertices[2] = boundingBox.Min + new Vector3(0.0f, d.Y, 0.0f);
                boundingBoxVertices[3] = boundingBox.Min + new Vector3(d.X, d.Y, 0.0f);

                boundingBoxVertices[4] = boundingBox.Min + new Vector3(0.0f, 0.0f, d.Z);
                boundingBoxVertices[5] = boundingBox.Min + new Vector3(d.X, 0.0f, d.Z);
                boundingBoxVertices[6] = boundingBox.Min + new Vector3(0.0f, d.Y, d.Z);
                boundingBoxVertices[7] = boundingBox.Min + new Vector3(d.X, d.Y, d.Z);

                uint[] indices = new uint[36];
                // Surface X-Y
                indices[0] = 0;
                indices[1] = 2;
                indices[2] = 3;

                indices[3] = 0;
                indices[4] = 3;
                indices[5] = 1;

                // Surface X-Z-near min
                indices[6] = 0;
                indices[7] = 1;
                indices[8] = 4;

                indices[9] = 1;
                indices[10] = 5;
                indices[11] = 4;

                // Surface Y-Z-near min
                indices[12] = 0;
                indices[13] = 6;
                indices[14] = 2;

                indices[15] = 0;
                indices[16] = 4;
                indices[17] = 6;

                // Surface X-Z-far min
                indices[18] = 3;
                indices[19] = 2;
                indices[20] = 6;

                indices[21] = 3;
                indices[22] = 6;
                indices[23] = 7;

                // Surface Y-Z-far min
                indices[24] = 3;
                indices[25] = 5;
                indices[26] = 1;

                indices[27] = 3;
                indices[28] = 7;
                indices[29] = 1;

                // Surface X-Y-far min
                indices[30] = 4;
                indices[31] = 5;
                indices[32] = 7;

                indices[33] = 4;
                indices[34] = 7;
                indices[35] = 6;

                triangleCount += 6;

                geometriesOut[i] = new InstancedMesh(t.InstanceId,
                    new Mesh(boundingBoxVertices, indices, t.TemplateMesh.Error), t.InstanceMatrix, t.TreeIndex,
                    t.Color,
                    t.AxisAlignedBoundingBox);
            }
            else if (tG is TriangleMesh)
            {
                var t = (TriangleMesh)tG;
                triangleCount += t.Mesh.TriangleCount;
                originalTriangleCount += t.Mesh.TriangleCount;
                geometriesOut[i] = geometries[i];
            }
            else
            {
                geometriesOut[i] = geometries[i];
            }
        }

        Console.WriteLine($"TriCount: {triangleCount}");
        Console.WriteLine($"OriginalTriCount: {originalTriangleCount}");
        return geometriesOut;
    }
    private APrimitive[] ConvertAllToConvexHull(APrimitive[] geometries, bool onlyOptimizeSmallVolumes)
    {
        APrimitive[] geometriesOut = new APrimitive[geometries.Length];
        int triangleCount = 0;
        for (int i=0; i<geometries.Length; i++)
        {
            var tG = geometries[i];
            Console.WriteLine($"Type is {tG.GetType()}");
            if (tG is InstancedMesh)
            {
                var t = (InstancedMesh)tG;
                var originalMeshCount = t.TemplateMesh.TriangleCount;

                Mesh reducedMesh;

                BoundingBox bbox = t.TemplateMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
                float dX = bbox.Max.X - bbox.Min.X;
                float dY = bbox.Max.Y - bbox.Min.Y;
                float dZ = bbox.Max.Z - bbox.Min.Z;
                float V = dX * dY * dZ;
                if (V < 8.0f || !onlyOptimizeSmallVolumes)
                {
                    Console.WriteLine($"{t.GetType().Name}: {t.TemplateMesh.Vertices.Length}");

                    // Build vertex list to hand to the convex hull algorithm
                    var meshVertexCount = t.TemplateMesh.Vertices.Length;
                    var meshVertices = new double[meshVertexCount][];
                    for (int j = 0; j < meshVertexCount; j++)
                    {
                        var coordinate = new double[3];
                        coordinate[0] = t.TemplateMesh.Vertices[j].X;
                        coordinate[1] = t.TemplateMesh.Vertices[j].Y;
                        coordinate[2] = t.TemplateMesh.Vertices[j].Z;
                        meshVertices[j] = coordinate;
                    }

                    // Create the convex hull
                    var convexHullOfMesh = ConvexHull.Create(meshVertices/*, 1E-1*/);

                    // Create the convex hull vertices and indices in CadRevealComposer internal format
                    uint index = 0;
                    var cadRevealVertices = new List<Vector3>();
                    var cadRevealIndices = new List<uint>();
                    foreach (DefaultConvexFace<DefaultVertex> face in convexHullOfMesh.Result.Faces)
                    {
                        triangleCount++;
                        var facePoints = face.Vertices.ToArray();
                        foreach (var r in facePoints)
                        {
                            double x = r.Position[0];
                            double y = r.Position[1];
                            double z = r.Position[2];
                            cadRevealIndices.Add(index++);
                            cadRevealVertices.Add(new Vector3((float)x, (float)y, (float)z));
                        }
                    }

                    reducedMesh = new Mesh(cadRevealVertices.ToArray(), cadRevealIndices.ToArray(),
                        t.TemplateMesh.Error);
                }
                else
                {
                    var t2 = (InstancedMesh)geometries[i];
                    reducedMesh = t2.TemplateMesh;

                    /*
                    // The below code does not work due to failure of the first CheckValidity()
                    var meshCopy = MeshTools.OptimizeMesh(t2.TemplateMesh);
                    var dMesh = ConvertMeshToDMesh3(meshCopy);
                    dMesh.CheckValidity();
                    var reducer = new Reducer(dMesh);
                    reducer.ReduceToTriangleCount(50);
                    dMesh.CheckValidity();
                    reducedMesh = ConvertDMesh3ToMesh(dMesh);
                    */

                    triangleCount+= reducedMesh.TriangleCount;
                }

                // Replace the instanced mesh
                geometriesOut[i] = new InstancedMesh(
                    t.InstanceId,
                    reducedMesh,
                    t.InstanceMatrix, t.TreeIndex,
                    t.Color,
                    t.AxisAlignedBoundingBox);
            }
            else if (tG is TriangleMesh)
            {
                var t = (TriangleMesh)tG;
                triangleCount += t.Mesh.TriangleCount;
                geometriesOut[i] = geometries[i];
            }
            else
            {
                geometriesOut[i] = geometries[i];
            }
        }

        Console.WriteLine($"TriCount: {triangleCount}");
        return geometriesOut;
    }

    private (Mesh mesh, int triangleCount) ConvertToConvexHull(Mesh inputMesh)
    {
        // Build vertex list to hand to the convex hull algorithm
        var meshVertexCount = inputMesh.Vertices.Length;
        var meshVertices = new double[meshVertexCount][];
        for (int j = 0; j < meshVertexCount; j++)
        {
            var coordinate = new double[3];
            coordinate[0] = inputMesh.Vertices[j].X;
            coordinate[1] = inputMesh.Vertices[j].Y;
            coordinate[2] = inputMesh.Vertices[j].Z;
            meshVertices[j] = coordinate;
        }

        // Create the convex hull
        var convexHullOfMesh = ConvexHull.Create(meshVertices, 1E-1);
        if (convexHullOfMesh.ErrorMessage.Length != 0)
        {
            Console.WriteLine($"Convex hull simplification failed with error : {convexHullOfMesh.ErrorMessage}");
            return (inputMesh, inputMesh.TriangleCount);
        }

        // Create the convex hull vertices and indices in CadRevealComposer internal format
        uint index = 0;
        int triangleCount = 0;
        var cadRevealVertices = new List<Vector3>();
        var cadRevealIndices = new List<uint>();
        foreach (DefaultConvexFace<DefaultVertex> face in convexHullOfMesh.Result.Faces)
        {
            triangleCount++;
            var facePoints = face.Vertices.ToArray();
            foreach (var r in facePoints)
            {
                double x = r.Position[0];
                double y = r.Position[1];
                double z = r.Position[2];
                cadRevealIndices.Add(index++);
                cadRevealVertices.Add(new Vector3((float)x, (float)y, (float)z));
            }
        }

        Mesh reducedMesh = MeshTools.OptimizeMesh(new Mesh(cadRevealVertices.ToArray(), cadRevealIndices.ToArray(), inputMesh.Error));
        return (reducedMesh, triangleCount);
    }

    private (Mesh mesh, int triangleCount) ConvertToBoundingBox(BoundingBox boundingBox, float error)
    {
        int triangleCount = 0;
        Vector3[] boundingBoxVertices = new Vector3[8];

        Vector3 d = boundingBox.Max - boundingBox.Min;
        boundingBoxVertices[0] = boundingBox.Min;
        boundingBoxVertices[1] = boundingBox.Min + new Vector3(d.X, 0.0f, 0.0f);
        boundingBoxVertices[2] = boundingBox.Min + new Vector3(0.0f, d.Y, 0.0f);
        boundingBoxVertices[3] = boundingBox.Min + new Vector3(d.X, d.Y, 0.0f);

        boundingBoxVertices[4] = boundingBox.Min + new Vector3(0.0f, 0.0f, d.Z);
        boundingBoxVertices[5] = boundingBox.Min + new Vector3(d.X, 0.0f, d.Z);
        boundingBoxVertices[6] = boundingBox.Min + new Vector3(0.0f, d.Y, d.Z);
        boundingBoxVertices[7] = boundingBox.Min + new Vector3(d.X, d.Y, d.Z);

        uint[] indices = new uint[36];
        // Surface X-Y
        indices[0] = 0;
        indices[1] = 2;
        indices[2] = 3;

        indices[3] = 0;
        indices[4] = 3;
        indices[5] = 1;

        // Surface X-Z-near min
        indices[6] = 0;
        indices[7] = 1;
        indices[8] = 4;

        indices[9] = 1;
        indices[10] = 5;
        indices[11] = 4;

        // Surface Y-Z-near min
        indices[12] = 0;
        indices[13] = 6;
        indices[14] = 2;

        indices[15] = 0;
        indices[16] = 4;
        indices[17] = 6;

        // Surface X-Z-far min
        indices[18] = 3;
        indices[19] = 2;
        indices[20] = 6;

        indices[21] = 3;
        indices[22] = 6;
        indices[23] = 7;

        // Surface Y-Z-far min
        indices[24] = 3;
        indices[25] = 5;
        indices[26] = 1;

        indices[27] = 3;
        indices[28] = 7;
        indices[29] = 1;

        // Surface X-Y-far min
        indices[30] = 4;
        indices[31] = 5;
        indices[32] = 7;

        indices[33] = 4;
        indices[34] = 7;
        indices[35] = 6;

        triangleCount += 6;

        Mesh reducedMesh = new Mesh(boundingBoxVertices, indices, error);
        return (reducedMesh, triangleCount);
    }

    private (Mesh mesh, int triangleCount) ConvertToBoundingBox(Mesh inputMesh)
    {
        BoundingBox boundingBox = inputMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
        return ConvertToBoundingBox(boundingBox, inputMesh.Error);
    }

    private (Mesh mesh, int triangleCount) ConvertToDecimatedMesh(Mesh inputMesh, float thresholdInMeshUnits=0.01f)
    {
        var reducedMesh = Simplify.SimplifyMeshLossy(inputMesh, new SimplificationLogObject(), thresholdInMeshUnits);
        return (reducedMesh, reducedMesh.TriangleCount);
    }

    private (Mesh mesh, int triangleCount) ConvertToLedgerBeam(Mesh inputMesh)
    {
        BoundingBox bbox = inputMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);

        float lx = bbox.Max.X - bbox.Min.X;
        float ly = bbox.Max.Y - bbox.Min.Y;
        float lz = bbox.Max.Z - bbox.Min.Z;

        // Find largest, smallest, and the middle side lengths
        (float l, int i) maxLen = (lx > ly) ? ((lx > lz) ? (lx, 0) : (lz, 2)) : ((ly > lz) ? (ly, 1) : (lz, 2));
        (float l, int i) minLen = (lx < ly) ? ((lx < lz) ? (lx, 0) : (lz, 2)) : ((ly < lz) ? (ly, 1) : (lz, 2));
        (float l, int i) middleLen = ((lx > minLen.l && lx < maxLen.l) ? (lx, 0) : ((ly > minLen.l && ly < maxLen.l) ? (ly, 1) : (lz, 2)));

        // Find radius of cylinder
        float cylinderRadius = minLen.l / 1.5f; // Make the cylinder slightly thicker by dividing by 1.5 instead of 2.0

        // Find the start vector of the upper cylinder
        var centerTopMin = new Vector3();
        centerTopMin[minLen.i] = (bbox.Max[minLen.i] + bbox.Min[minLen.i]) / 2.0f;
        centerTopMin[middleLen.i] = bbox.Max[middleLen.i] - cylinderRadius;
        centerTopMin[maxLen.i] = bbox.Min[maxLen.i];

        // Find the end vector of the upper cylinder
        var centerTopMax = new Vector3();
        centerTopMax[minLen.i] = centerTopMin[minLen.i];
        centerTopMax[middleLen.i] = centerTopMin[middleLen.i];
        centerTopMax[maxLen.i] = bbox.Max[maxLen.i];

        // Find displacement vector needed to displace the top cylinder to the bottom cylinder
        Vector3 displacement = new Vector3(0.0f, 0.0f, 0.0f);
        displacement[middleLen.i] = middleLen.l + cylinderRadius;

        // Find the start and end vector of the lower cylinder
        var centerBottomMin = centerTopMin - displacement;
        var centerBottomMax = centerTopMax - displacement;

        // Tessellate the cylinders
        Vector3 lengthVec = centerTopMax - centerTopMin;
        Vector3 unitNormal = lengthVec * (1.0f / lengthVec.Length());
        EccentricCone cone1A = new EccentricCone(centerTopMin, centerTopMax, unitNormal, cylinderRadius, cylinderRadius, 0, Color.Brown, bbox);
        EccentricCone cone1B = new EccentricCone(centerTopMin, centerTopMax, -unitNormal, cylinderRadius, cylinderRadius, 0, Color.Brown, bbox);
        EccentricCone cone2A = new EccentricCone(centerBottomMin, centerBottomMax, unitNormal, cylinderRadius, cylinderRadius, 0, Color.Brown, bbox);
        EccentricCone cone2B = new EccentricCone(centerBottomMin, centerBottomMax, -unitNormal, cylinderRadius, cylinderRadius, 0, Color.Brown, bbox);
        var cylinder1A = EccentricConeTessellator.Tessellate(cone1A);
        var cylinder1B = EccentricConeTessellator.Tessellate(cone1B);
        var cylinder2A = EccentricConeTessellator.Tessellate(cone2A);
        var cylinder2B = EccentricConeTessellator.Tessellate(cone2B);

        // Create a thin box below upper cylinder
        var depthOfBoxVec = new Vector3();
        depthOfBoxVec[middleLen.i] = 0.11f; // [m]
        var thicknessOfBoxVec = new Vector3();
        thicknessOfBoxVec[middleLen.i] = 0.005f; // [m]
        displacement[middleLen.i] = cylinderRadius - 0.01f;
        BoundingBox bboxUpper = new BoundingBox(centerTopMin - displacement, centerTopMax - displacement - depthOfBoxVec + thicknessOfBoxVec);
        var boxUpper = ConvertToBoundingBox(bboxUpper, inputMesh.Error);

        // Create a thin box above lower cylinder
        BoundingBox bboxLower = new BoundingBox(centerBottomMin + displacement, centerBottomMax + displacement + depthOfBoxVec + thicknessOfBoxVec);
        var boxLower = ConvertToBoundingBox(bboxLower, inputMesh.Error);

        // Create thin support boxes, 8cm wide, 12cm apart
        float supportWidth = 4.0f*0.08f; // [m]?
        float openingWidth = 4.0f*0.12f; // [m]?
        float supportPlusOpeningWidth = supportWidth + openingWidth;                                                            // Can be thought of as opening with half a support on each side
        int numOpeningsWith2HalfSupport = (int)(maxLen.l / supportPlusOpeningWidth);
        float missingWidthOfEndSupport = (maxLen.l - (float)numOpeningsWith2HalfSupport * supportPlusOpeningWidth) / 2.0f;      // Need to extend end supports to account for beams not being an exact integer multiple of supportPlusOpeingWisth
        int numNonEndSupports = numOpeningsWith2HalfSupport - 1;

        var widthOfEndSupportVec = new Vector3(0.0f, 0.0f, 0.0f);
        widthOfEndSupportVec[maxLen.i] = missingWidthOfEndSupport + 0.5f * supportWidth;

        var supportWidthVec = new Vector3(0.0f, 0.0f, 0.0f);
        supportWidthVec[maxLen.i] = supportWidth;

        var heightOfSupportVec = new Vector3(0.0f, 0.0f, 0.0f);
        heightOfSupportVec[middleLen.i] = middleLen.l - cylinderRadius;

        var startPosLeftEndSupport = centerTopMin;
        var endPosLeftEndSupport = centerTopMin + widthOfEndSupportVec + thicknessOfBoxVec - heightOfSupportVec;
        var startPosRightEndSupport = centerTopMax - widthOfEndSupportVec;
        var endPosRightEndSupport = centerTopMax + thicknessOfBoxVec - heightOfSupportVec;

        var startPosCenterFirstNonEndSupportVec = centerTopMin + widthOfEndSupportVec - supportWidthVec*0.5f;
        var supportPlusOpeningWidthVec = new Vector3(0.0f, 0.0f, 0.0f);
        supportPlusOpeningWidthVec[maxLen.i] = supportPlusOpeningWidth;

        List<Mesh> boxes = new List<Mesh>();
        var bboxBox = new BoundingBox(startPosLeftEndSupport, endPosLeftEndSupport);
        boxes.Add(ConvertToBoundingBox(bboxBox, inputMesh.Error).mesh);
        for (int i = 0; i < numNonEndSupports; i++)
        {
            var startVec = startPosCenterFirstNonEndSupportVec + (float)(i+1) * supportPlusOpeningWidthVec;
            bboxBox = new BoundingBox(startVec - supportWidthVec * 0.5f, startVec + supportWidthVec * 0.5f + thicknessOfBoxVec - heightOfSupportVec);
            boxes.Add(ConvertToBoundingBox(bboxBox, inputMesh.Error).mesh);
        }
        bboxBox = new BoundingBox(startPosRightEndSupport, endPosRightEndSupport);
        boxes.Add(ConvertToBoundingBox(bboxBox, inputMesh.Error).mesh);

        // Combine meshed parts
        List<Mesh> combinedMeshes = new ([
            cylinder1A.Mesh, cylinder1B.Mesh, cylinder2A.Mesh, cylinder2B.Mesh, boxUpper.mesh, boxLower.mesh
        ]);
        foreach (var box in boxes)
        {
            combinedMeshes.Add(box);
        }
        Mesh combinedMesh = CombineMeshes(combinedMeshes.ToArray(), inputMesh.Error);

        return (combinedMesh, combinedMesh.TriangleCount);
    }

    Mesh CombineMeshes(Mesh[] meshes, float error)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var indexDisplacements = new List<uint>();
        indexDisplacements.Add(0);
        uint indexCounter = 0;
        foreach (var mesh in meshes)
        {
            foreach (var v in mesh.Vertices)
            {
                vertices.Add(v);
                indexCounter++;
            }
            indexDisplacements.Add(indexCounter);
        }

        for (int i=0; i<meshes.Length; i++)
        {
            var mesh = meshes[i];
            foreach (var j in mesh.Indices) indices.Add(j + indexDisplacements[i]);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), error);
    }

    private Mesh ReduceMeshPart(Mesh inputMesh, string name, ref int triangleCountAfter, int histogramIndex, ref List<(int fb, int fa)> histogramValues)
    {
        Mesh reducedMesh;

        if (name.Contains("Plank"))
        {
            var reducedMeshAndTriCount = ConvertToBoundingBox(inputMesh);
            reducedMesh = reducedMeshAndTriCount.mesh;
            triangleCountAfter += reducedMeshAndTriCount.triangleCount;
            histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb, histogramValues[histogramIndex].fa + reducedMeshAndTriCount.triangleCount);
        }
        else if (name.Contains("FS") || name.Contains("Stair") || name.Contains("Base Element"))
        {
            Mesh[] disjointMeshes = MeshUtils.SplitDisjointPieces(inputMesh);
            var reducedDisjointMeshes = new List<Mesh>();
            foreach (var disjointMesh in disjointMeshes)
            {
                var reducedMeshAndTriCount = ConvertToConvexHull(disjointMesh);
                reducedDisjointMeshes.Add(reducedMeshAndTriCount.mesh);
                triangleCountAfter += reducedMeshAndTriCount.triangleCount;
                histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb, histogramValues[histogramIndex].fa + reducedMeshAndTriCount.triangleCount);
            }
            reducedMesh = CombineMeshes(reducedDisjointMeshes.ToArray(), inputMesh.Error);
        }
        else if (name.Contains("Beam"))
        {
            var reducedMeshAndTriCount = ConvertToLedgerBeam/*ConvertToDecimatedMesh*/(inputMesh/*, 0.05f*/);
            reducedMesh = reducedMeshAndTriCount.mesh;
            triangleCountAfter += reducedMeshAndTriCount.triangleCount;
            histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb, histogramValues[histogramIndex].fa + reducedMeshAndTriCount.triangleCount);
        }
        else if (name.Contains("Kick Board") || name.Contains("BRM"))
        {
            var reducedMeshAndTriCount = ConvertToConvexHull(inputMesh);
            reducedMesh = reducedMeshAndTriCount.mesh;
            triangleCountAfter += reducedMeshAndTriCount.triangleCount;
            histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb, histogramValues[histogramIndex].fa + reducedMeshAndTriCount.triangleCount);
        }
        else
        {
            var reducedMeshAndTriCount = ConvertToDecimatedMesh(inputMesh, 0.01f);
            reducedMesh = reducedMeshAndTriCount.mesh;
            triangleCountAfter+= reducedMeshAndTriCount.triangleCount;
            histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb, histogramValues[histogramIndex].fa + reducedMeshAndTriCount.triangleCount);
        }

        return reducedMesh;
    }

    private APrimitive[] ConvertBasedOnPartType(APrimitive[] geometries, string[] names)
    {
        // Prepare to produce histogram data (reduced name along x and frequency along y)
        var histogramLabels = names
            .Select(str => new string(str.Where(c => !char.IsDigit(c)).ToArray()))
            .GroupBy(name => name);
        List<(int fb, int fa)> histogramValues = new (histogramLabels.ToArray().Length);
        for (int k = 0; k < histogramLabels.ToArray().Length; k++) histogramValues.Add(new (0, 0));

        APrimitive[] geometriesOut = new APrimitive[geometries.Length];
        int triangleCountBefore = 0;
        int triangleCountAfter = 0;
        for (int i=0; i<geometries.Length; i++)
        {
            // Find histogram index
            var reducedName = new string(names[i].Where(c => !char.IsDigit(c)).ToArray());
            int histogramIndex = histogramLabels
                .Select((v, index) => new { v, index })
                .First(q => q.v.Key == reducedName).index;

            var tG = geometries[i];
            Console.WriteLine($"Type is {tG.GetType()}");
            if (tG is InstancedMesh)
            {
                // Optimize the instanced mesh
                var t = (InstancedMesh)tG;
                triangleCountBefore += t.TemplateMesh.TriangleCount;
                histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb + t.TemplateMesh.TriangleCount, histogramValues[histogramIndex].fa);
                Mesh reducedMesh = ReduceMeshPart(t.TemplateMesh, names[i], ref triangleCountAfter, histogramIndex, ref histogramValues);

                // Replace the instanced mesh
                geometriesOut[i] = new InstancedMesh(
                    t.InstanceId,
                    reducedMesh,
                    t.InstanceMatrix, t.TreeIndex,
                    t.Color,
                    t.AxisAlignedBoundingBox);
            }
            else if (tG is TriangleMesh)
            {
                // Optimize the triangle mesh
                var t = (TriangleMesh)tG;
                triangleCountBefore += t.Mesh.TriangleCount;
                histogramValues[histogramIndex] = (histogramValues[histogramIndex].fb + t.Mesh.TriangleCount, histogramValues[histogramIndex].fa);
                Mesh reducedMesh = ReduceMeshPart(t.Mesh, names[i], ref triangleCountAfter, histogramIndex, ref histogramValues);

                // Replace the triangle mesh
                geometriesOut[i] = new TriangleMesh(
                    reducedMesh,
                    t.TreeIndex,
                    t.Color,
                    t.AxisAlignedBoundingBox);
            }
            else
            {
                geometriesOut[i] = geometries[i];
            }
        }

        var improvement = (float)(triangleCountBefore - triangleCountAfter) * 100.0f / (float)triangleCountBefore;
        Console.WriteLine($"TriCountBefore: {triangleCountBefore}");
        Console.WriteLine($"TriCountAfter: {triangleCountAfter} => {improvement}%");

        var histogramLabelsArray = histogramLabels.ToArray();
        for (int k = 0; k < histogramLabelsArray.Length; k++)
        {
            var label = histogramLabelsArray[k].Key;
            var valueBefore = histogramValues[k].fb;
            var valueAfter = histogramValues[k].fa;
            Console.WriteLine($"{label};{valueBefore};{valueAfter}");
        }

        return geometriesOut;
    }

    private void CalculateVolumeDistribution(APrimitive[] geometries)
    {
        int N = 1000;
        float minV = float.MaxValue;
        float maxV = -float.MaxValue;
        for (int i = 0; i < geometries.Length; i++)
        {
            var tG = geometries[i];
            float[] histogram = new float[N];
            if (tG is InstancedMesh)
            {
                var t = (InstancedMesh)tG;
                BoundingBox boundingBox = t.TemplateMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);

                float dX = boundingBox.Max.X - boundingBox.Min.X;
                float dY = boundingBox.Max.Y - boundingBox.Min.Y;
                float dZ = boundingBox.Max.Z - boundingBox.Min.Z;

                float V = dX * dY * dZ;

                if (V > maxV) maxV = V;
                if (V < minV) minV = V;
            }
        }

        float[] hist = new float[N];
        for (int j = 0; j < N; j++) hist[j] = 0;
        for (int i = 0; i < geometries.Length; i++)
        {
            var tG = geometries[i];
            if (tG is InstancedMesh)
            {
                var t = (InstancedMesh)tG;
                BoundingBox boundingBox = t.TemplateMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);

                float dX = boundingBox.Max.X - boundingBox.Min.X;
                float dY = boundingBox.Max.Y - boundingBox.Min.Y;
                float dZ = boundingBox.Max.Z - boundingBox.Min.Z;

                float V = dX * dY * dZ;

                int k = (int)((V - minV) * (float)(N - 1) / (maxV - minV));
                if (k < N && k >= 0) hist[k]++;
            }
        }

        Console.WriteLine($"Range: {minV} - {maxV}");
        for (int i = 0; i < N; i++)
        {
            Console.WriteLine($"{hist[i]}");
        }
    }

    public APrimitive[] ProcessGeometries(
        APrimitive[] geometries,
        string[] names,
        ComposerParameters composerParameters,
        ModelParameters modelParameters,
        InstanceIdGenerator instanceIdGenerator
    )
    {
//        CalculateVolumeDistribution(geometries);
//        return geometries;
//        return ConvertAllToAxisAlignedBoundaries(geometries);
//        return ConvertAllToConvexHull(geometries, true);
        return ConvertBasedOnPartType(geometries, names);
    }
}
