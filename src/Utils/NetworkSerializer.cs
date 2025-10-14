using System.Text.Json;
using System.Text.Json.Serialization;

public static class NetworkSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        //ReferenceHandler = ReferenceHandler.Preserve,

    };
}
