namespace RvmSharp.Tests;

using Exporters;
using NUnit.Framework;
using System;
using System.IO;
using System.Numerics;
using Tessellation;

[TestFixture]
public class ObjExporterTests
{
    private string _tempFilePath;
    
    [SetUp]
    public void SetUp()
    {
        _tempFilePath = Path.GetTempFileName();
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(_tempFilePath);
    }
    
    [Test]
    public void Dispose_ClosesWriter()
    {
        var exporter = new ObjExporter(_tempFilePath);
        exporter.StartGroup("test");
        
        exporter.Dispose();

        Assert.Throws<ObjectDisposedException>(() => exporter.StartGroup("test"));
    }

    [Test]
    public void StartGroup_AddsCorrectLine()
    {
        const string testGroupName = "testgroup";

        using (var exporter = new ObjExporter(_tempFilePath))
        {
            exporter.StartGroup(testGroupName);
        }

        var testFileText = File.ReadAllText(_tempFilePath).Trim();
        
        Assert.That(testFileText, Is.EqualTo($"g {testGroupName}"));
    }
    
    [Test]
    public void StartObject_AddsCorrectLine()
    {
        const string testObjectName = "testobject";

        using (var exporter = new ObjExporter(_tempFilePath))
        {
            exporter.StartObject(testObjectName);
        }

        var testFileText = File.ReadAllText(_tempFilePath).Trim();
        Assert.That(testFileText, Is.EqualTo($"o {testObjectName}"));
    }
    
    [Test]
    public void WriteMesh_WithoutColor()
    {
        const string expectedString = """
                                      v 1.000000 3.000000 -2.000000
                                      vn 1.000000 3.000000 -2.000000
                                      s off
                                      f 2//2 3//3 1//1
                                      """;

        Vector3[] vertices = { new(1, 2, 3) };
        Vector3[] normals = { new(1, 2, 3) };
        int[] triangles = { 0, 1, 2};

        var mesh = new Mesh(vertices, normals, triangles, 0);

        using (var exporter = new ObjExporter(_tempFilePath))
        {
            exporter.WriteMesh(mesh);
        }

        var testFileText = File.ReadAllText(_tempFilePath).Trim();
        Console.WriteLine(testFileText);
        Assert.That(testFileText, Is.EqualTo(expectedString));
    }
    
    [Test]
    public void WriteMesh_WithColor()
    {
        const string expectedString = """
                                      v 3.000000 5.000000 -4.000000 0.111111 0.222222 0.333333
                                      vn 6.000000 8.000000 -7.000000
                                      s off
                                      f 2//2 3//3 1//1
                                      """;

        Vector3[] vertices = { new(3, 4, 5) };
        Vector3[] normals = { new(6, 7, 8) };
        Vector3[] vertexColors = { new(0.111111f, 0.222222f, 0.333333f) };
        int[] triangles = { 0, 1, 2};

        var mesh = new Mesh(vertices, normals, triangles, 0)
        {
            VertexColors = vertexColors
        };

        using (var exporter = new ObjExporter(_tempFilePath))
        {
            exporter.WriteMesh(mesh);
        }
        
        var testFileText = File.ReadAllText(_tempFilePath).Trim();
        
        Assert.That(testFileText, Is.EqualTo(expectedString));
    }
    
    [Test]
    public void WriteMesh_WithIncorrectColorArray_ThrowsException()
    {
        Vector3[] vertices = { new(3, 4, 5), new(10, 20, 30) };
        Vector3[] normals = { new(6, 7, 8), new(9, 10, 11)  };
        int[] triangles = { 0, 1, 2};

        var mesh = new Mesh(vertices, normals, triangles, 0)
        {
            VertexColors = new[] { new Vector3(0.111111f, 0.222222f, 0.333333f) }
        };
        
        using var exporter = new ObjExporter(_tempFilePath);
        
        Assert.Throws<ArgumentException>(() => exporter.WriteMesh(mesh));
    }
}