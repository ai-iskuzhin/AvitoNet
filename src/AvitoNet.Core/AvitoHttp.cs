namespace AvitoNet;

/// <summary>Shared constants for the AvitoNet HTTP stack.</summary>
public static class AvitoHttp
{
    /// <summary>
    /// Name of the <see cref="System.Net.Http.HttpClient"/> registered by <c>AddAvitoCore</c> and shared by every
    /// AvitoNet client (Messenger, Reviews, …) and the token provider — so one token cache serves all of them.
    /// </summary>
    public const string ClientName = "AvitoNet";
}
