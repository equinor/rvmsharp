namespace CadRevealFbxProvider.UserFriendlyLogger;

public class UserFriendlyLogException(string userFriendlyMessage, Exception? innerException = null)
    : Exception(userFriendlyMessage, innerException);
