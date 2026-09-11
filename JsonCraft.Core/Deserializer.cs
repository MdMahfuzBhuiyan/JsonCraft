namespace JsonCraft.Core;
using System.Collections;
using System.Globalization;
using System.Reflection;

internal static class Deserializer
{
    public static object? Deserialize(object? rawValue, Type targetType)
    {
        if (rawValue is null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new JsonSerializationException($"Cannot assign null to non-nullable value type '{targetType.FullName}'.");
            }
            return null;
        }

        var underlyingNullable = Nullable.GetUnderlyingType(targetType);
        if (underlyingNullable != null)
        {
            return Deserialize(rawValue, underlyingNullable);
        }

        if (targetType == typeof(object))
        {
            return rawValue;
        }

        if (targetType == typeof(string))
        {
            return rawValue.ToString();
        }

        if (targetType == typeof(bool))
        {
            if (rawValue is bool b) return b;
            throw new JsonSerializationException($"Expected boolean value for type '{targetType.FullName}', got '{rawValue.GetType().Name}'.");
        }

        if (targetType.IsPrimitive || targetType == typeof(decimal))
        {
            return ConvertNumber(rawValue, targetType);
        }

        if (targetType == typeof(DateTime))
        {
            if (rawValue is string dateStr && DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                return dt;
            }
            throw new JsonSerializationException($"Invalid DateTime format: '{rawValue}'.");
        }

        if (targetType == typeof(Guid))
        {
            if (rawValue is string guidStr && Guid.TryParse(guidStr, out var guid))
            {
                return guid;
            }
            throw new JsonSerializationException($"Invalid Guid format: '{rawValue}'.");
        }

        if (targetType.IsEnum)
        {
            if (rawValue is string enumStr)
            {
                try
                {
                    return Enum.Parse(targetType, enumStr, ignoreCase: true);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Unknown enum value '{enumStr}' for type '{targetType.FullName}'.", ex);
                }
            }

            if (rawValue is long or int or short or byte)
            {
                return Enum.ToObject(targetType, rawValue);
            }

            throw new JsonSerializationException($"Cannot convert '{rawValue.GetType().Name}' to enum '{targetType.FullName}'.");
        }

        if (typeof(IDictionary).IsAssignableFrom(targetType) || 
            (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
        {
            return PopulateDictionary(rawValue, targetType);
        }

        if (targetType.IsArray)
        {
            return PopulateArray(rawValue, targetType);
        }

        if (typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            return PopulateList(rawValue, targetType);
        }

        if (rawValue is Dictionary<string, object?> rawObj)
        {
            return PopulateObject(rawObj, targetType);
        }

        throw new JsonSerializationException($"Cannot convert raw value of type '{rawValue.GetType().Name}' to '{targetType.FullName}'.");
    }
    private static object ConvertNumber(object rawValue, Type targetType)
    {
        try
        {
            return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Cannot convert value '{rawValue}' to numeric type '{targetType.FullName}'.", ex);
        }
    }
    private static object PopulateDictionary(object rawValue, Type targetType)
    {
        if (rawValue is not Dictionary<string, object?> rawDict)
        {
            throw new JsonSerializationException($"Expected JSON object for dictionary type '{targetType.FullName}'.");
        }

        Type keyType = typeof(string);
        Type valueType = typeof(object);

        if (targetType.IsGenericType)
        {
            var genArgs = targetType.GetGenericArguments();
            keyType = genArgs[0];
            valueType = genArgs[1];
        }

        if (keyType != typeof(string))
        {
            throw new JsonSerializationException("Only dictionaries with string keys are supported.");
        }

        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var instance = (IDictionary)Activator.CreateInstance(dictType)!;

        foreach (var kvp in rawDict)
        {
            var deserializedVal = Deserialize(kvp.Value, valueType);
            instance.Add(kvp.Key, deserializedVal);
        }

        return instance;
    }
    private static object PopulateArray(object rawValue, Type targetType)
    {
        if (rawValue is not List<object?> rawList)
        {
            throw new JsonSerializationException($"Expected JSON array for array type '{targetType.FullName}'.");
        }

        var elemType = targetType.GetElementType()!;
        var array = Array.CreateInstance(elemType, rawList.Count);

        for (var i = 0; i < rawList.Count; i++)
        {
            var item = Deserialize(rawList[i], elemType);
            array.SetValue(item, i);
        }

        return array;
    }
    private static object PopulateList(object rawValue, Type targetType)
    {
        if (rawValue is not List<object?> rawList)
        {
            throw new JsonSerializationException($"Expected JSON array for collection type '{targetType.FullName}'.");
        }

        Type elemType = typeof(object);
        if (targetType.IsGenericType)
        {
            elemType = targetType.GetGenericArguments()[0];
        }

        var listType = typeof(List<>).MakeGenericType(elemType);
        var listInstance = (IList)Activator.CreateInstance(listType)!;

        foreach (var rawItem in rawList)
        {
            var item = Deserialize(rawItem, elemType);
            listInstance.Add(item);
        }

        return listInstance;
    }
    private static object PopulateObject(Dictionary<string, object?> rawObj, Type targetType)
    {
        object instance;
        try
        {
            instance = Activator.CreateInstance(targetType)!;
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Failed to instantiate type '{targetType.FullName}'. Ensure it has a public parameterless constructor.", ex);
        }

        var properties = TypeCache.GetWritableProperties(targetType);

        foreach (var prop in properties)
        {
            if (rawObj.TryGetValue(prop.Name, out var rawPropVal))
            {
                var convertedValue = Deserialize(rawPropVal, prop.PropertyType);
                prop.SetValue(instance, convertedValue);
            }
        }

        return instance;
    }
}