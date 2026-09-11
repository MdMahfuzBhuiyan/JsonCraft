namespace JsonCraft.Core;

public static class JsonSerializer
{
    public static string Serialize<T>(T? value)
    {
        return Serialize((object?)value);
    }

    public static string Serialize(object? value)
    {
        var serializer = new Serializer();
        return serializer.Serialize(value);
    }

    public static T? Deserialize<T>(string json)
    {
        var result = Deserialize(json, typeof(T));
        return (T?)result;
    }

    public static object? Deserialize(string json, Type targetType)
    {
        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        var parsedNode = JsonParser.Parse(json);
        return Deserializer.Deserialize(parsedNode, targetType);
    }
}