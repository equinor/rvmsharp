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
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
