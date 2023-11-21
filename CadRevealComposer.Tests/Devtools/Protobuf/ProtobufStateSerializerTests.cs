namespace CadRevealComposer.Tests.Devtools.Protobuf;

using CadRevealComposer.Devtools.Protobuf;
using Primitives;
using System.Drawing;
using System.Numerics;
using Tessellation;

public class ProtobufStateSerializerTests
{
    private static readonly BoundingBox SampleAxisAlignedBoundingBox = new BoundingBox(
        new Vector3(1, 2, 3),
        new Vector3(4, 5, 6)
    );
    private static readonly Color SampleColor = Color.FromArgb(1, 2, 3, 4);

    [Test]
    public void TestProtobufRoundtrip_Cone()
    {
        var box = new Cone(
            0,
            0,
            new Vector3(1, 2, 3),
            new Vector3(1, 2, 3),
            Vector3.UnitX,
            0.1f,
            0.2f,
            1337,
            SampleColor,
            SampleAxisAlignedBoundingBox,
            "HA"
        );

        APrimitive[] inputArray = { box };
        APrimitive[] outputArray = RoundtripSerializeDeserialize(inputArray);

        Assert.That(outputArray, Is.EqualTo(inputArray));
    }

    [Test]
    public void TestProtobufRoundtrip_Box()
    {
        var box = new Box(Matrix4x4.Identity, 1337, Color.FromArgb(255, 255, 0, 255), SampleAxisAlignedBoundingBox, "HA");

        APrimitive[] inputArray = { box };
        APrimitive[] outputArray = RoundtripSerializeDeserialize(inputArray);

        Assert.That(outputArray, Is.EqualTo(inputArray));
    }

    [Test]
    public void TestProtobufRoundtrip_Mesh()
    {
        var mesh = new TriangleMesh(
            new Mesh(
                new Vector3[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9) },
                new uint[] { 0, 1, 2 },
                0.0f
            ),
            1337,
            SampleColor,
            SampleAxisAlignedBoundingBox,
            "HA"
        );

        APrimitive[] inputArray = { mesh };
        APrimitive[] outputArray = RoundtripSerializeDeserialize(inputArray);

        Assert.That(outputArray, Is.EqualTo(inputArray));
    }

    private static APrimitive[] RoundtripSerializeDeserialize(APrimitive[] inputArray)
    {
        using var memoryStream = new MemoryStream();
        ProtobufStateSerializer.WriteAPrimitiveStateToStream(memoryStream, inputArray);
        memoryStream.Position = 0; // Set position to start of stream to read from it.
        var outputArray = ProtobufStateSerializer.ReadAPrimitiveStateFromStream(memoryStream);
        return outputArray;
    }

    [Test]
    public void ProtoMatrixTest()
    {
        var realMatrix = Matrix4x4.Identity;

        ProtobufStateSerializer.ProtoMatrix4x4 proto = realMatrix;

        Matrix4x4 matrixAgain = proto;
        Assert.That(matrixAgain, Is.EqualTo(realMatrix));
    }
}
