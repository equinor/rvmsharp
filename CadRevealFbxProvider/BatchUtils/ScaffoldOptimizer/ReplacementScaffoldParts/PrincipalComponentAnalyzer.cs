namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNetVector = MathNet.Numerics.LinearAlgebra.Vector<float>;

public class PcaResult3
{
    public PcaResult3(Vector3 v1, Vector3 v2, Vector3 v3, float lambda1, float lambda2, float lambda3)
    {
        var sortedEigenvectors = new List<(float lambda, Vector3 v)> { (lambda1, v1), (lambda2, v2), (lambda3, v3) };

        sortedEigenvectors.Sort((x1, x2) => x1.lambda <= x2.lambda ? 1 : 0);

        _v1 = sortedEigenvectors[0].v;
        _v2 = sortedEigenvectors[1].v;
        _v3 = sortedEigenvectors[2].v;

        _lambda1 = sortedEigenvectors[0].lambda;
        _lambda2 = sortedEigenvectors[1].lambda;
        _lambda3 = sortedEigenvectors[2].lambda;
    }

    public Vector3 V(int index)
    {
        return index switch
        {
            0 => _v1,
            1 => _v2,
            2 => _v3,
            _ => new Vector3(0, 0, 0),
        };
    }

    public float Lambda(int index)
    {
        return index switch
        {
            0 => _lambda1,
            1 => _lambda2,
            2 => _lambda3,
            _ => 0.0f,
        };
    }

    private readonly Vector3 _v1;
    private readonly Vector3 _v2;
    private readonly Vector3 _v3;
    private readonly float _lambda1;
    private readonly float _lambda2;
    private readonly float _lambda3;
}

public static class PrincipalComponentAnalyzer
{
    public static PcaResult3 Invoke(List<Vector3> dataList)
    {
        int N = dataList.Count;

        // Create data matrix, X, from list of points
        var X = new DenseMatrix(N, 3);
        for (int i = 0; i < N; i++)
        {
            X[i, 0] = dataList[i].X;
            X[i, 1] = dataList[i].Y;
            X[i, 2] = dataList[i].Z;
        }

        // Find column means
        var xMean = new Vector3
        {
            X = dataList.Sum(x => x.X) / (float)N,
            Y = dataList.Sum(x => x.Y) / (float)N,
            Z = dataList.Sum(x => x.Z) / (float)N,
        };

        // Find covariances and store in covariance matrix, S
        var s = new DenseMatrix(3, 3);
        for (int a = 0; a < 3; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                for (int i = 0; i < N; i++)
                {
                    s[a, b] += (X[i, a] - xMean[a]) * (X[i, b] - xMean[b]);
                }

                s[a, b] /= N;
            }
        }

        // Perform the Eigenvalue decomposition
        var evd = s.Evd();

        // Return result
        return (
            new PcaResult3(
                ToVector3(evd.EigenVectors.Column(0)),
                ToVector3(evd.EigenVectors.Column(1)),
                ToVector3(evd.EigenVectors.Column(2)),
                (float)evd.EigenValues[0].Real,
                (float)evd.EigenValues[1].Real,
                (float)evd.EigenValues[2].Real
            )
        );

        Vector3 ToVector3(MathNetVector v)
        {
            return new Vector3(v[0], v[1], v[2]);
        }
    }
}
