namespace CadRevealFbxProvider.UserFriendlyLogger;

using CadRevealComposer.Utils;

public class UserFriendlyLoggerExceptionHandler
{
    public static void HandleException(Exception ex)
    {
        // Default error message, meaning that we have not anticipated this error and did not write explicitly a more detailed message.
        var message =
            "An error occurred while processing the FBX/CSV files. Please notify the Echo developing team to check the logs for more details.";

        // Error has a more detailed message intended for the user.
        if (ex is UserFriendlyLogException)
        {
            message = ex.Message;
        }

        TeamCityServiceMessages.WriteMessage("Fbx parsing failed: Error: " + ex);
        TeamCityServiceMessages.SetParameter("Scaffolding_ErrorMessage", message);
    }
}
