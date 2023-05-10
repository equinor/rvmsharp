﻿namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using CadRevealFbxProvider.Attributes;
using NUnit.Framework;

[TestFixture]
public class FbxProviderAttributeParserTests
{
    private DirectoryInfo attributeDirectory = new DirectoryInfo(@".\TestSamples\attributes");

    [Test]
    public void ParseCorrectAttributesTest()
    {
        string infoTextFilename = attributeDirectory.FullName.ToString() + "\\fbx_test_model.csv";
        var lines = File.ReadAllLines(infoTextFilename);
        new ScaffoldingAttributeParser().ParseAttributes(lines);
    }

    [Test]
    public void MissingKeyAttributesTest()
    {
        Assert.Throws<Exception>(
            () =>
            {
                string infoTextFilename = attributeDirectory.FullName.ToString() + "\\missing_key_attribute.csv";
                var lines = File.ReadAllLines(infoTextFilename);
                new ScaffoldingAttributeParser().ParseAttributes(lines);
            },
            "Was expecting an exception saying that the key attribute is missing, but got none"
        );
    }

    [Test]
    public void WrongNumberAttributesTest()
    {
        Assert.Throws<Exception>(
            () =>
            {
                string infoTextFilename = attributeDirectory.FullName.ToString() + "\\wrong_attribute_count.csv";
                var lines = File.ReadAllLines(infoTextFilename);
                new ScaffoldingAttributeParser().ParseAttributes(lines);
            },
            "Was expecting an exception saying that the attribute count is off, but got none"
        );
    }
}
