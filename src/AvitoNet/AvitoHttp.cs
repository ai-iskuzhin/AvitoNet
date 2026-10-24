namespace AvitoNet;

/// <summary>Shared constants for the AvitoNet HTTP stack.</summary>
public static class AvitoHttp
{
    /// <summary>
    /// Name of the <see cref="System.Net.Http.HttpClient"/> registered by <c>AddAvito</c> and used by every API
    /// surface and the token endpoint. Configure it (proxy, handlers) with <c>services.AddHttpClient(AvitoHttp.ClientName)</c>.
    /// </summary>
    public const string ClientName = "AvitoNet";
}
