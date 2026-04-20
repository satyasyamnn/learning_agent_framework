internal static class EndpointUtilities
{
    public static string NormalizeAzureOpenAiEndpoint(string endpoint)
    {
        return new Uri(endpoint).GetLeftPart(UriPartial.Authority);
    }
}
