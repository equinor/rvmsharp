namespace CadRevealFbxProvider.UserFriendlyLogger;

public class UserFriendlyLoggerExceptionHandler
{
    public static void HandleException(Exception ex)
    {
        var message = "An error occurred while processing the FBX files. Please check the logs for more details.";
        if (ex is UserFriendlyLogException)
        {
            message = ex.Message;
        }
        else if (
            ex is ArgumentException argEx
            && argEx.Message.Contains("An item with the same key has already been added")
        )
        {
            message =
                "There are duplicate column names in the CSV file. Please ensure that all column names are unique.";
        }

        var escapedMessage = message.Replace("'", "|'"); // Escape single quotes for TeamCity
        Console.WriteLine("Fbx parsing failed: Error: " + ex);
        Console.WriteLine($"##teamcity[setParameter name='Scaffolding_ErrorMessage' value='{escapedMessage}']");
    }
}
