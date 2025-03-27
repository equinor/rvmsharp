namespace Commons.Tests.Utils;

using Commons.Utils;

[TestFixture]
public class ByteUtilsTests
{
    [Test]
    [TestCase(1024 * 1024, ExpectedResult = 1.0)]
    [TestCase(0, ExpectedResult = 0)]
    public double BytesToMegabytesTests(int bytes) => ByteUtils.BytesToMegabytes(bytes);
}
