namespace CadRevealFbxProvider.UserFriendlyLogger;

using System;

public class UserFriendlyLogException(string userFriendlyMessage, Exception? innerException = null)
    : Exception(userFriendlyMessage, innerException);
