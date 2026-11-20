namespace RvmSharp.Exe.Tests;

using System.Numerics;
using RvmSharp.Primitives;

[TestFixture]
public class NativePrimitiveFitterTests
{
    private const float Length = 2.0f;

    private static readonly Matrix4x4 Placement =
        Matrix4x4.CreateFromYawPitchRoll(0.7f, -1.1f, 2.3f) * Matrix4x4.CreateTranslation(0.3f, -0.2f, 0.1f);

    private static readonly PrimitiveFitOptions Options = new();

    /// <summary>A prism along Z of the given outline (and optional hole), with square or mitred ends.</summary>
    private static RvmFacetGroup Prism(
        IReadOnlyList<Vector2> outline,
        IReadOnlyList<Vector2>? hole = null,
        float length = Length,
        float mitre = 0f
    )
    {
        Vector3 Bottom(Vector2 p) => new(p.X, p.Y, mitre * p.X);
        Vector3 Top(Vector2 p) => new(p.X, p.Y, length);
        var polygons = new List<RvmFacetGroup.RvmPolygon>();
        foreach (var loop in hole == null ? new[] { outline } : new[] { outline, hole })
        {
            for (var index = 0; index < loop.Count; index++)
            {
                var a = loop[index];
                var b = loop[(index + 1) % loop.Count];
                polygons.Add(Polygon([Bottom(a), Bottom(b), Top(b), Top(a)]));
            }
        }
        var bottomNormal = Normal(outline.Reverse().Select(Bottom).ToArray());
        var bottom = new List<RvmFacetGroup.RvmContour> { Contour(outline.Reverse().Select(Bottom), bottomNormal) };
        var top = new List<RvmFacetGroup.RvmContour> { Contour(outline.Select(Top), Vector3.UnitZ) };
        if (hole != null)
        {
            bottom.Add(Contour(hole.Reverse().Select(Bottom), bottomNormal));
            top.Add(Contour(hole.Select(Top), Vector3.UnitZ));
        }
        polygons.Add(new RvmFacetGroup.RvmPolygon(bottom.ToArray()));
        polygons.Add(new RvmFacetGroup.RvmPolygon(top.ToArray()));
        return new RvmFacetGroup(1, Placement, new RvmBoundingBox(Vector3.Zero, Vector3.One), polygons.ToArray());
    }

    private static RvmFacetGroup.RvmContour Contour(IEnumerable<Vector3> points, Vector3 normal) =>
        new(points.Select(point => (point, normal)).ToArray());

    /// <summary>A planar face; its vertex normals follow the winding, as in RVM files.</summary>
    private static RvmFacetGroup.RvmPolygon Polygon(Vector3[] points) => new([Contour(points, Normal(points))]);

    private static Vector3 Normal(Vector3[] points)
    {
        var sum = Vector3.Zero;
        for (var index = 0; index < points.Length; index++)
            sum += Vector3.Cross(points[index], points[(index + 1) % points.Length]);
        return Vector3.Normalize(sum);
    }

    /// <summary>Adds the interior points of an arc, in degrees, between two outline points.</summary>
    private static void Arc(List<Vector2> points, Vector2 centre, float radius, float from, float to, int segments = 4)
    {
        for (var step = 1; step < segments; step++)
        {
            var angle = MathF.PI / 180 * (from + (to - from) * step / segments);
            points.Add(centre + radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle)));
        }
    }

    /// <summary>Rolled I/H section with root fillets: width 0.1, height 0.2, flanges 0.01, web 0.006.</summary>
    private static List<Vector2> IBeam(float radius = 0.008f)
    {
        const float b = 0.05f,
            h = 0.1f,
            f = 0.01f,
            w = 0.003f;
        var points = new List<Vector2> { new(-b, -h), new(b, -h), new(b, -h + f), new(w + radius, -h + f) };
        Arc(points, new Vector2(w + radius, -h + f + radius), radius, -90, -180);
        points.AddRange([new(w, -h + f + radius), new(w, h - f - radius)]);
        Arc(points, new Vector2(w + radius, h - f - radius), radius, 180, 90);
        points.AddRange([new(w + radius, h - f), new(b, h - f), new(b, h), new(-b, h), new(-b, h - f), new(-w - radius, h - f)]);
        Arc(points, new Vector2(-w - radius, h - f - radius), radius, 90, 0);
        points.AddRange([new(-w, h - f - radius), new(-w, -h + f + radius)]);
        Arc(points, new Vector2(-w - radius, -h + f + radius), radius, 0, -90);
        points.AddRange([new(-w - radius, -h + f), new(-b, -h + f)]);
        return points;
    }

    private static List<Vector2> Angle() =>
        [new(0, 0), new(0.1f, 0), new(0.1f, 0.008f), new(0.008f, 0.008f), new(0.008f, 0.075f), new(0, 0.075f)];

    private static List<Vector2> Channel() =>
        [
            new(0, 0),
            new(0.08f, 0),
            new(0.08f, 0.01f),
            new(0.008f, 0.01f),
            new(0.008f, 0.19f),
            new(0.08f, 0.19f),
            new(0.08f, 0.2f),
            new(0, 0.2f),
        ];

    private static List<Vector2> Tee() =>
        [
            new(-0.05f, 0.09f),
            new(-0.004f, 0.09f),
            new(-0.004f, 0),
            new(0.004f, 0),
            new(0.004f, 0.09f),
            new(0.05f, 0.09f),
            new(0.05f, 0.1f),
            new(-0.05f, 0.1f),
        ];

    private static List<Vector2> Rectangle(float halfWidth, float halfHeight) =>
        [
            new(-halfWidth, -halfHeight),
            new(halfWidth, -halfHeight),
            new(halfWidth, halfHeight),
            new(-halfWidth, halfHeight),
        ];

    private static List<Vector2> Circle(float radius, int segments = 24) =>
        Enumerable
            .Range(0, segments)
            .Select(index =>
                radius * new Vector2(MathF.Cos(2 * MathF.PI * index / segments), MathF.Sin(2 * MathF.PI * index / segments))
            )
            .ToList();

    private static PrimitiveOutcome Fit(RvmFacetGroup facetGroup, ProfileFamily hint, PrimitiveFitOptions? options = null) =>
        NativePrimitiveFitter.Fit(
            NativePrimitiveFitter.Faces(facetGroup),
            NativePrimitiveFitter.Mesh(facetGroup),
            hint,
            options ?? Options
        );

    private static float PartVolume(PrimitivePart part) => MathF.Abs(part.Matrix.GetDeterminant());

    /// <summary>World bounds of the parts must equal the source's bounds.</summary>
    private static void AssertSameBounds(RvmFacetGroup facetGroup, PrimitiveFit fit)
    {
        var source = NativePrimitiveFitter.Faces(facetGroup).SelectMany(face => face.SelectMany(contour => contour)).ToArray();
        var corners = fit
            .Parts.SelectMany(part =>
                Enumerable
                    .Range(0, 8)
                    .Select(corner =>
                        Vector3.Transform(
                            new Vector3(
                                (corner & 1) == 0 ? -0.5f : 0.5f,
                                (corner & 2) == 0 ? -0.5f : 0.5f,
                                (corner & 4) == 0 ? -0.5f : 0.5f
                            ),
                            part.Matrix
                        )
                    )
            )
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(corners.Min(c => c.X), Is.EqualTo(source.Min(p => p.X)).Within(1e-4));
            Assert.That(corners.Max(c => c.X), Is.EqualTo(source.Max(p => p.X)).Within(1e-4));
            Assert.That(corners.Min(c => c.Y), Is.EqualTo(source.Min(p => p.Y)).Within(1e-4));
            Assert.That(corners.Max(c => c.Y), Is.EqualTo(source.Max(p => p.Y)).Within(1e-4));
            Assert.That(corners.Min(c => c.Z), Is.EqualTo(source.Min(p => p.Z)).Within(1e-4));
            Assert.That(corners.Max(c => c.Z), Is.EqualTo(source.Max(p => p.Z)).Within(1e-4));
        });
    }

    [Test]
    public void FilletedIBeamBecomesThreeBoxes()
    {
        var beam = Prism(IBeam());
        var outcome = Fit(beam, ProfileFamily.IH);

        Assert.That(outcome.Fit, Is.Not.Null, outcome.Outcome);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Fit!.Replacement, Is.EqualTo("BEAM_IH"));
            Assert.That(outcome.Fit.Parts, Has.Length.EqualTo(3));
            Assert.That(outcome.Fit.Parts.All(part => part.Kind == PrimitivePartKind.Cube));
            // Flanges 0.1 x 0.01 twice and web 0.006 x 0.18, all 2 m long; fillets are dropped.
            Assert.That(
                outcome.Fit.Parts.Sum(PartVolume),
                Is.EqualTo((2 * 0.1 * 0.01 + 0.006 * 0.18) * Length).Within(1e-6)
            );
        });
        AssertSameBounds(beam, outcome.Fit!);
    }

    [TestCase("L", 2)]
    [TestCase("C", 3)]
    [TestCase("T", 2)]
    public void OpenSectionsSplitIntoTheirFlangesAndWeb(string family, int parts)
    {
        var (outline, hint) = family switch
        {
            "L" => (Angle(), ProfileFamily.L),
            "C" => (Channel(), ProfileFamily.C),
            _ => (Tee(), ProfileFamily.T),
        };
        var section = Prism(outline);
        var outcome = Fit(section, hint);

        Assert.That(outcome.Fit, Is.Not.Null, outcome.Outcome);
        Assert.That(outcome.Fit!.Parts, Has.Length.EqualTo(parts));
        AssertSameBounds(section, outcome.Fit);
    }

    [Test]
    public void AngleWithRoundedToesSplitsIntoTwoBoxes()
    {
        var outline = new List<Vector2> { new(0, 0), new(0.096f, 0) };
        Arc(outline, new Vector2(0.096f, 0.004f), 0.004f, -90, 90);
        outline.AddRange([new(0.096f, 0.008f), new(0.008f, 0.008f), new(0.008f, 0.071f)]);
        Arc(outline, new Vector2(0.004f, 0.071f), 0.004f, 0, 180);
        outline.Add(new(0, 0.071f));
        var angle = Prism(outline);
        var outcome = Fit(angle, ProfileFamily.L);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("BEAM_L"), outcome.Outcome);
        Assert.That(outcome.Fit!.Parts, Has.Length.EqualTo(2));
        // The squared toes give legs of 0.1 x 0.008 and 0.008 x 0.067.
        Assert.That(outcome.Fit.Parts.Sum(PartVolume), Is.EqualTo((0.1 * 0.008 + 0.008 * 0.067) * Length).Within(1e-6));
    }

    [Test]
    public void ProfileOfAnotherFamilyIsKept()
    {
        Assert.That(Fit(Prism(IBeam()), ProfileFamily.L).Outcome, Does.StartWith("kept:family"));
    }

    [Test]
    public void UnhintedFlagAcceptsAnyProfile()
    {
        var outcome = Fit(Prism(IBeam()), ProfileFamily.None, Options with { Unhinted = true });

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("BEAM_IH"));
    }

    [Test]
    public void HollowRectangularSectionBecomesItsOuterBox()
    {
        var section = Prism(Rectangle(0.05f, 0.03f), Rectangle(0.045f, 0.025f).AsEnumerable().Reverse().ToList());
        var outcome = Fit(section, ProfileFamily.Hollow);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("CLOSED_SHELL"), outcome.Outcome);
        Assert.That(outcome.Fit!.Parts, Has.Length.EqualTo(1));
        AssertSameBounds(section, outcome.Fit);
    }

    [Test]
    public void RoundSectionsBecomeOneCylinderOfTheOuterDiameter()
    {
        var bar = Fit(Prism(Circle(0.02f)), ProfileFamily.None);
        var tube = Fit(Prism(Circle(0.03f), Circle(0.025f).AsEnumerable().Reverse().ToList()), ProfileFamily.Hollow);

        Assert.Multiple(() =>
        {
            Assert.That(bar.Fit?.Replacement, Is.EqualTo("ROUND"), bar.Outcome);
            Assert.That(tube.Fit?.Replacement, Is.EqualTo("ROUND_HOLLOW"), tube.Outcome);
            var part = tube.Fit!.Parts.Single();
            Assert.That(part.Kind, Is.EqualTo(PrimitivePartKind.Cylinder));
            // The unit cylinder has radius 1 and height 1.
            Assert.That(new Vector3(part.Matrix.M11, part.Matrix.M12, part.Matrix.M13).Length(), Is.EqualTo(0.03f).Within(1e-6));
            Assert.That(new Vector3(part.Matrix.M31, part.Matrix.M32, part.Matrix.M33).Length(), Is.EqualTo(Length).Within(1e-5));
        });
    }

    [Test]
    public void HollowSectionWithRoundedCornersBecomesItsOuterBox()
    {
        // SHS 100x100x6.3 with an outer corner radius of 1.5t: its square corner sticks out 3.9 mm.
        const float half = 0.05f,
            radius = 0.00945f;
        var outline = new List<Vector2>();
        foreach (var (corner, from) in new[] { (new Vector2(1, -1), -90f), (new Vector2(1, 1), 0f), (new Vector2(-1, 1), 90f), (new Vector2(-1, -1), 180f) })
        {
            var centre = corner * (half - radius);
            outline.Add(centre + radius * new Vector2(MathF.Cos(MathF.PI / 180 * from), MathF.Sin(MathF.PI / 180 * from)));
            Arc(outline, centre, radius, from, from + 90);
            outline.Add(centre + radius * new Vector2(MathF.Cos(MathF.PI / 180 * (from + 90)), MathF.Sin(MathF.PI / 180 * (from + 90))));
        }
        var section = Prism(outline, Rectangle(half - 0.0063f, half - 0.0063f).AsEnumerable().Reverse().ToList());
        var outcome = Fit(section, ProfileFamily.Hollow);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("CLOSED_SHELL"), outcome.Outcome);
        Assert.That(PartVolume(outcome.Fit!.Parts.Single()), Is.EqualTo(0.1 * 0.1 * Length).Within(1e-6));
    }

    [Test]
    public void ChannelWithTaperedFlangesSplitsIntoThreeBoxes()
    {
        // Inner flange faces slope by 1:10 from 0.012 at the web to 0.004 at the toe.
        List<Vector2> outline =
        [
            new(0, 0),
            new(0.08f, 0),
            new(0.08f, 0.004f),
            new(0.008f, 0.012f),
            new(0.008f, 0.188f),
            new(0.08f, 0.196f),
            new(0.08f, 0.2f),
            new(0, 0.2f),
        ];
        var channel = Prism(outline);
        var outcome = Fit(channel, ProfileFamily.C);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("BEAM_CTZ"), outcome.Outcome);
        Assert.That(outcome.Fit!.Parts, Has.Length.EqualTo(3));
        // Each flange keeps its mean thickness of 0.008, so the volume is unchanged.
        Assert.That(outcome.Fit.Parts.Sum(PartVolume), Is.EqualTo((0.008 * 0.2 + 2 * 0.072 * 0.008) * Length).Within(1e-6));
    }

    [Test]
    public void LongPlateWithSmallCutOutIsKept()
    {
        // 5 m x 0.75 m x 30 mm with a 90 mm square cut-out near one end, as found in 1P-STRU.
        var hole = new List<Vector2> { new(4.2f, 0.1f), new(4.2f, 0.19f), new(4.29f, 0.19f), new(4.29f, 0.1f) };
        var plate = Prism([new(0, 0), new(5f, 0), new(5f, 0.75f), new(0, 0.75f)], hole, length: 0.03f);

        Assert.That(Fit(plate, ProfileFamily.Any).Fit, Is.Null);
    }

    [Test]
    public void GussetWithLargeInnerRadiusIsNotSplit()
    {
        // An L-shaped gusset whose inner corner is a 200 mm radius, not a root fillet.
        var outline = new List<Vector2> { new(0, 0), new(0.725f, 0), new(0.725f, 0.232f), new(0.395f, 0.232f) };
        Arc(outline, new Vector2(0.395f, 0.432f), 0.2f, -90, -180, 8);
        outline.AddRange([new(0.195f, 0.432f), new(0.195f, 1.45f), new(0, 1.45f)]);
        var outcome = Fit(Prism(outline, length: 0.016f), ProfileFamily.L);

        Assert.That(outcome.Fit, Is.Null, outcome.Outcome);
    }

    [Test]
    public void MitredPlateIsNotSquaredOff()
    {
        // A 2 m x 0.35 m x 20 mm plate with one end cut back 0.27 m across its width: 3.4% of the
        // box is missing, but the box would stick out 270 mm at the mitre.
        List<Vector2> outline = [new(0, 0), new(2f, 0), new(2f, 0.35f), new(0.27f, 0.35f)];
        var outcome = Fit(Prism(outline, length: 0.02f), ProfileFamily.Any);

        Assert.That(outcome.Fit, Is.Null, outcome.Outcome);
        Assert.That(outcome.Outcome, Does.Contain("gap"));
    }

    [Test]
    public void SnipedPlateStillBecomesABox()
    {
        // 6 mm corner snipes stick out of the box by 4.2 mm.
        List<Vector2> outline =
        [
            new(0.006f, 0),
            new(0.494f, 0),
            new(0.5f, 0.006f),
            new(0.5f, 0.3f),
            new(0, 0.3f),
            new(0, 0.006f),
        ];
        var outcome = Fit(Prism(outline, length: 0.012f), ProfileFamily.None);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("CUBELIKE"), outcome.Outcome);
    }

    [Test]
    public void PlateWithLargeHoleIsKept()
    {
        var hole = Circle(0.05f).Select(point => point + new Vector2(0.25f, 0.15f)).Reverse().ToList();
        var outcome = Fit(Prism(Rectangle(0.25f, 0.15f).Select(point => point + new Vector2(0.25f, 0.15f)).ToList(), hole, length: 0.5f), ProfileFamily.None);

        Assert.That(outcome.Fit, Is.Null, outcome.Outcome);
    }

    [Test]
    public void GroovedTreadBecomesABox()
    {
        // A 1.2 m x 0.235 m x 31 mm tread with 40 grip grooves, 2 mm deep, across its top: far over
        // the old 80-vertex limit, but no point of the box is more than 2 mm from the tread.
        const float width = 0.235f,
            depth = 0.031f,
            groove = 0.002f;
        var outline = new List<Vector2> { new(0, 0), new(width, 0) };
        for (var index = 39; index >= 0; index--)
        {
            var x = width * (index + 0.75f) / 40;
            outline.Add(new(x + 0.0015f, depth));
            outline.Add(new(x + 0.0015f, depth - groove));
            outline.Add(new(x - 0.0015f, depth - groove));
            outline.Add(new(x - 0.0015f, depth));
        }
        var tread = Prism(outline, length: 1.2f);
        var outcome = Fit(tread, ProfileFamily.Any);

        Assert.That(NativePrimitiveFitter.Mesh(tread)!.Vertices.Length, Is.GreaterThan(80));
        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("CUBELIKE").Or.EqualTo("BEAM_RECT"), outcome.Outcome);
        Assert.That(outcome.Fit!.Parts, Has.Length.EqualTo(1));
    }

    [Test]
    public void ShortHollowRoundIsKept()
    {
        var collar = Prism(Circle(0.19f), Circle(0.17f).AsEnumerable().Reverse().ToList(), length: 0.19f);

        Assert.That(Fit(collar, ProfileFamily.Hollow).Outcome, Does.StartWith("kept:hollow-short"));
    }

    [Test]
    public void UnhintedHollowRoundIsKept()
    {
        var ring = Fit(Prism(Circle(0.03f), Circle(0.025f).AsEnumerable().Reverse().ToList()), ProfileFamily.None);

        Assert.That(ring.Fit, Is.Null);
    }

    [Test]
    public void UnhintedPlateIsNotSplit()
    {
        var plate = Prism(Angle(), length: 0.01f);

        Assert.That(Fit(plate, ProfileFamily.None).Fit, Is.Null);
    }

    [Test]
    public void MitredBeamIsKept()
    {
        Assert.That(Fit(Prism(IBeam(), mitre: 0.5f), ProfileFamily.IH).Outcome, Is.EqualTo("kept:oblique"));
    }

    [Test]
    public void CurvedPartIsKept()
    {
        var sphere = new List<RvmFacetGroup.RvmPolygon>();
        const int rings = 12,
            sectors = 12;
        Vector3 Point(int ring, int sector)
        {
            var polar = MathF.PI * ring / rings;
            var azimuth = 2 * MathF.PI * sector / sectors;
            return 0.1f * new Vector3(MathF.Sin(polar) * MathF.Cos(azimuth), MathF.Sin(polar) * MathF.Sin(azimuth), MathF.Cos(polar));
        }
        for (var ring = 1; ring < rings - 1; ring++)
        for (var sector = 0; sector < sectors; sector++)
            sphere.Add(Polygon([Point(ring, sector), Point(ring + 1, sector), Point(ring + 1, sector + 1), Point(ring, sector + 1)]));
        for (var sector = 0; sector < sectors; sector++)
        {
            sphere.Add(Polygon([Point(0, 0), Point(1, sector), Point(1, sector + 1)]));
            sphere.Add(Polygon([Point(rings, 0), Point(rings - 1, sector + 1), Point(rings - 1, sector)]));
        }
        var facetGroup = new RvmFacetGroup(1, Placement, new RvmBoundingBox(Vector3.Zero, Vector3.One), sphere.ToArray());

        Assert.That(Fit(facetGroup, ProfileFamily.Any).Outcome, Is.EqualTo("kept:curved"));
    }

    [Test]
    public void BlockWithSmallChamferIsCubeLike()
    {
        List<Vector2> outline = [new(0, 0), new(0.5f, 0), new(0.5f, 0.295f), new(0.495f, 0.3f), new(0, 0.3f)];
        var block = Prism(outline, length: 0.2f);
        var outcome = Fit(block, ProfileFamily.None);

        Assert.That(outcome.Fit?.Replacement, Is.EqualTo("CUBELIKE"), outcome.Outcome);
        Assert.That(PartVolume(outcome.Fit!.Parts.Single()), Is.EqualTo(0.5 * 0.3 * 0.2).Within(1e-6));
    }

    [TestCase("/GB/T11263-HW150X150X7X10-Q235B", "IH")]
    [TestCase("/EURONORM-SPEC/L100x75x7", "L")]
    [TestCase("/US-W40x167", "IH")]
    [TestCase("TS60X5", "Hollow")]
    [TestCase("/DIN-SPEC/UPN200", "C")]
    [TestCase("/STD/CHS168.3", "Hollow")]
    [TestCase("/STD/FB100X10", "Solid")]
    [TestCase("/STD/GRATING-30", "Any")]
    public void SectionReferenceNamesItsProfileFamily(string spref, string family)
    {
        Assert.That(NativeSectionHints.Family(spref).ToString(), Is.EqualTo(family));
    }
}
