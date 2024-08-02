namespace CadRevealComposer.Tests.Operations.Tessellating;

using System.Numerics;

public static class TessellatorTestUtils
{
    public static float CalculateDeterminant(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Can calculate the determinant with this formula from:
        // https://www.geeksforgeeks.org/determinant-of-a-matrix/
        //   | a d g |
        //   | b e h |
        //   | c f i |
        // determinant = a(ei - fh) - b(di - gf) + c(dh - eg)

        float a = v1.X;
        float b = v1.Y;
        float c = v1.Z;
        float d = v2.X;
        float e = v2.Y;
        float f = v2.Z;
        float g = v3.X;
        float h = v3.Y;
        float i = v3.Z;

        float determinant = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);

        return determinant;
    }
}
