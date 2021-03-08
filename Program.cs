using rvmsharp.Rvm;
using rvmsharp.Tessellator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rvmsharp
{
    class Program
    {
        const string win_path = @"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.RVM";
        static void Main(string[] args)
        {
            var fileStream = File.OpenRead(@"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.RVM");
            var rvm = RvmParser.ReadRvm(fileStream);
            var p = PdmsTextParser.GetAllPdmsNodesInFile(@"e:\gush\projects\rvmparser\tests\snout\RC110-HVAC.txt");

            var store = new RvmStore();
            store.RvmFiles.Add(rvm);
            RvmConnect.Connect(store);
            RvmAlign.Align(store);
            
            AssignRecursive(p, rvm.Model.children);
            var leafs = rvm.Model.children.SelectMany((c) => CollectGeometryNodes(c)).ToArray();
            var dic = new Dictionary<int, List<RvmGroup>>();
            var templates = new List<RvmGroup>();
            foreach (var leaf in leafs)
            {
                if (templates.Any((t) => IsEqual(t, leaf)))
                {} else { templates.Add(leaf);}
            }

            foreach (var t in templates)
            {
                var pc = t.Primitives.Count;
                if (!dic.TryGetValue(pc, out var list)){
                    list = new List<RvmGroup>();
                    dic.Add(pc, list);
                }
                list.Add(t);
            }

            Console.WriteLine("Done!");
        }

        private static void AssignRecursive(IList<PdmsTextParser.PdmsNode> attributes, IList<RvmGroup> groups)
        {
            if (attributes.Count != groups.Count)
                Console.Error.WriteLine("Length of attribute nodes does not match group length");
            var copy = new List<RvmGroup>(groups);
            for (var i = 0; i < attributes.Count; i++)
            {
                var pdms = attributes[i];
                for (var k = 0; k < copy.Count; k++)
                {
                    var group = copy[k];
                    if (group.Name == pdms.Name)
                    {
                        // todo attr
                        foreach (var kvp in  pdms.MetadataDict)
                            group.Attributes.Add(kvp.Key, kvp.Value);
                        AssignRecursive(pdms.Children, group.Children);
                        break;
                    }
                }
            }
        }

        private static IEnumerable<RvmGroup> CollectGeometryNodes(RvmGroup root) {
            if (root.Primitives.Count > 0)
                yield return root;
            foreach (var child in root.Children)
                foreach (var p in CollectGeometryNodes(child))
                    yield return p;
        }

        private static bool IsEqual(RvmGroup g1, RvmGroup g2)
        {
            if (g1.Primitives.Count != g2.Primitives.Count)
                return false;
            for (var i = 0; i < g1.Primitives.Count; i++) {
                var p1 = g1.Primitives[i];
                var p2 = g2.Primitives[i];
                if (p1.Kind != p2.Kind)
                    return false;
                if (!IsEqual(p1, p2))
                    return false;
            }
            return true;
        }

        private static bool IsEqual(RvmPrimitive p1, RvmPrimitive p2)
        {
            if (p1.Kind != p2.Kind)
                return false;
            switch (p1.Kind) {
                case RvmPrimitiveKind.Box:
                    return true;
                case RvmPrimitiveKind.Cylinder:
                    return true;
                case RvmPrimitiveKind.Line:
                    return true;
                case RvmPrimitiveKind.Sphere:
                    return true;
                case RvmPrimitiveKind.Pyramid:
                {
                    var o1 = p1 as RvmPyramid;
                    var o2 = p2 as RvmPyramid;
                    return o1.BottomX == o2.BottomX && o1.BottomY == o2.BottomY &&
                        o1.TopX == o2.TopX && o1.TopY == o2.TopY &&
                        o1.OffsetX == o2.OffsetX && o1.OffsetY == o2.OffsetY &&
                        o1.Height == o2.Height;
                }
                case RvmPrimitiveKind.RectangularTorus: 
                {
                    var o1 = p1 as RvmRectangularTorus;
                    var o2 = p2 as RvmRectangularTorus;
                    return o1.RadiusInner == o2.RadiusInner && o1.RadiusOuter == o2.RadiusOuter &&
                        o1.Height == o2.Height && o1.Angle == o2.Angle;
                }
                case RvmPrimitiveKind.CircularTorus:
                {
                    var o1 = p1 as RvmCircularTorus;
                    var o2 = p2 as RvmCircularTorus;
                    return o1.Offset == o2.Offset && o1.Radius == o2.Radius &&
                        o1.Angle == o2.Angle;
                }
                case RvmPrimitiveKind.EllipticalDish:
                {
                    var o1 = p1 as RvmEllipticalDish;
                    var o2 = p2 as RvmEllipticalDish;
                    return o1.BaseRadius == o2.BaseRadius && o1.Height == o2.Height;
                }
                case RvmPrimitiveKind.SphericalDish:
                {
                    var o1 = p1 as RvmSphericalDish;
                    var o2 = p2 as RvmSphericalDish;
                    return o1.BaseRadius == o2.BaseRadius && o1.Height == o2.Height;
                }
                case RvmPrimitiveKind.Snout:
                {
                    var o1 = p1 as RvmSnout;
                    var o2 = p2 as RvmSnout;
                    return o1.RadiusBottom == o2.RadiusBottom && 
                    o1.RadiusTop == o2.RadiusTop &&
                    o1.Height == o2.Height &&
                    o1.OffsetX == o2.OffsetX &&
                    o1.OffsetY == o2.OffsetY &&
                    o1.BottomShearX == o2.BottomShearX &&
                    o1.BottomShearY == o2.BottomShearY &&
                    o1.TopShearX == o2.TopShearX &&
                    o1.TopShearY == o2.TopShearY;
                }
                case RvmPrimitiveKind.FacetGroup:
                {
                    var o1 = p1 as RvmFacetGroup;
                    var o2 = p2 as RvmFacetGroup;
                    if (o1._polygons.Length != o2._polygons.Length)
                        return false;
                    for (var i = 0; i < o1._polygons.Length; i++)
                        if (!IsEqual(o1._polygons[i], o2._polygons[i]))
                            return false;
                    return true;
                }
            }
            return false;
        }

        private static bool IsEqual(RvmFacetGroup.RvmPolygon p1, RvmFacetGroup.RvmPolygon p2)
        {
            if (p1._contours.Length != p2._contours.Length)
                return false;
            for (var i = 0; i < p1._contours.Length; i++)
            {
                if (!IsEqual(p1._contours[i], p2._contours[i]))
                    return false;
            }
            return true;
        }

        private static bool IsEqual(RvmFacetGroup.RvmContour c1, RvmFacetGroup.RvmContour c2)
        {
            if (c1._vertices.Length != c2._vertices.Length)
                return false;
            var offsetC1 = -c1._vertices[0].v;
            var offsetC2 = -c2._vertices[0].v;
            for (var i = 0; i < c1._vertices.Length; i++)
            {
                var v1 = c1._vertices[i];
                var v2 = c2._vertices[i];
                if (v1.v + offsetC1 != v2.v + offsetC2)
                    return false;
                if (v1.n != v2.n)
                    return false;
            }
            return true;
        }
    }
}
