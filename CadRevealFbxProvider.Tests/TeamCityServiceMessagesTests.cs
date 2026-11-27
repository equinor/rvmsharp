namespace CadRevealFbxProvider.Tests;

using CadRevealComposer.Utils;
using NUnit.Framework;
using UserFriendlyLogger;

[TestFixture]
public class TeamCityServiceMessagesTests
{
    [TestCase("plain", "plain")]
    [TestCase("", "")]
    [TestCase("|'\r\n[]", "|||'|r|n|[|]")]
    [TestCase("|n|r|'|[|]", "||n||r|||'|||[|||]")]
    public void SetParameter_WhenAttributesContainSpecialCharacters_WritesEscapedServiceMessage(
        string input,
        string expected
    )
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);

            TeamCityServiceMessages.SetParameter(input, input);

            Assert.That(
                output.ToString(),
                Is.EqualTo($"##teamcity[setParameter name='{expected}' value='{expected}']{Environment.NewLine}")
            );
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestCase("Maker's [part]|\r\n", "Maker|'s |[part|]|||r|n")]
    [TestCase("100% of [%build.number%] (%%)", "100%% of |[%%build.number%%|] (%%%%)")]
    [TestCase(
        "\r\n##teamcity[buildProblem description='injected']",
        "|r|n##teamcity|[buildProblem description=|'injected|'|]",
        TestName = "HandleException_WhenUserFriendlyMessageContainsServiceMessage_WritesEscapedErrorParameter"
    )]
    public void HandleException_WhenUserFriendlyMessageContainsSpecialCharacters_WritesEscapedErrorParameter(
        string message,
        string expected
    )
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);

            UserFriendlyLoggerExceptionHandler.HandleException(new UserFriendlyLogException(message));

            Assert.That(
                output.ToString(),
                Does.EndWith(
                    $"##teamcity[setParameter name='Scaffolding_ErrorMessage' value='{expected}']{Environment.NewLine}"
                )
            );
            Assert.That(output.ToString(), Does.Not.Contain("##teamcity[buildProblem"));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void HandleException_WhenInnerExceptionContainsServiceMessage_WritesOnlyEscapedDiagnostics()
    {
        var exception = new Exception(
            "Parsing failed",
            new Exception("\r\n##teamcity[buildProblem description='injected']")
        );
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);

            UserFriendlyLoggerExceptionHandler.HandleException(exception);

            Assert.That(output.ToString(), Does.Not.Contain("##teamcity[buildProblem"));
            Assert.That(
                output.ToString(),
                Does.StartWith("##teamcity[message text='Fbx parsing failed: Error: System.Exception: Parsing failed")
                    .And.Contain("|r|n##teamcity|[buildProblem description=|'injected|'|]")
            );
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestCase("plain", "plain")]
    [TestCase("", "")]
    [TestCase("|'\r\n[]", "|||'|r|n|[|]")]
    [TestCase("50% [%build.number%]", "50% |[%build.number%|]")]
    public void WriteMessage_WhenTextContainsSpecialCharacters_WritesEscapedLiteralMessage(string text, string expected)
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);

            TeamCityServiceMessages.WriteMessage(text);

            Assert.That(output.ToString(), Is.EqualTo($"##teamcity[message text='{expected}']{Environment.NewLine}"));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
