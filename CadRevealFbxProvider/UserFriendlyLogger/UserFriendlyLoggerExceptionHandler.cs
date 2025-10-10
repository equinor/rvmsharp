namespace CadRevealFbxProvider.UserFriendlyLogger;

public class UserFriendlyLoggerExceptionHandler
{
    public static void HandleException(Exception ex)
    {
        var message = "An error occurred while processing the FBX/CSV files. Please check the logs for more details.";
        if (ex is UserFriendlyLogException)
        {
            message = ex.Message;
        }

        var escapedMessage = message.Replace("'", "|'"); // Escape single quotes for TeamCity
        Console.WriteLine("Fbx parsing failed: Error: " + ex);
        Console.WriteLine($"##teamcity[setParameter name='Scaffolding_ErrorMessage' value='{escapedMessage}']");
    }
}
