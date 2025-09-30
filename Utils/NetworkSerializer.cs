using System.Text.Json;

public static class NetworkSerializer
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
    };
}
