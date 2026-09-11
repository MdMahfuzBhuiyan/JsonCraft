namespace JsonCraft.Core;

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

internal class Serializer
{
    private readonly HashSet<object> _referenceTracker = new(ReferenceEqualityComparer.Instance);

    public string Serialize(object? value)
    {
        var sb = new StringBuilder();
        SerializeValue(value, sb);
        return sb.ToString();
    }

    private void SerializeValue(object? value, StringBuilder sb)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }

        var type = value.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        if (value is string str)
        {
            WriteString(str, sb);
            return;
        }

        if (value is bool b)
        {
            sb.Append(b ? "true" : "false");
            return;
        }

        if (value is int or long or short or byte or sbyte)
        {
            sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            return;
        }

        if (value is float f)
        {
            sb.Append(f.ToString("G9", CultureInfo.InvariantCulture));
            return;
        }

        if (value is double d)
        {
            sb.Append(d.ToString("G17", CultureInfo.InvariantCulture));
            return;
        }

        if (value is decimal dec)
        {
            sb.Append(dec.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (value is DateTime dt)
        {
            WriteString(dt.ToString("O", CultureInfo.InvariantCulture), sb);
            return;
        }

        if (value is Guid guid)
        {
            WriteString(guid.ToString(), sb);
            return;
        }

        if (type.IsEnum)
        {
            WriteString(value.ToString()!, sb);
            return;
        }

        if (value is IDictionary dict)
        {
            SerializeDictionary(dict, sb);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            SerializeEnumerable(enumerable, sb);
            return;
        }

        SerializeObject(value, sb);
    }
    private void SerializeDictionary(IDictionary dict, StringBuilder sb)
    {
        if (!_referenceTracker.Add(dict))
        {
            throw new JsonSerializationException($"Circular reference detected in type '{dict.GetType().FullName}'.");
        }

        try
        {
            sb.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;

                var keyStr = entry.Key?.ToString() ?? "";
                WriteString(keyStr, sb);
                sb.Append(':');
                SerializeValue(entry.Value, sb);
            }
            sb.Append('}');
        }
        finally
        {
            _referenceTracker.Remove(dict);
        }
    }

    private void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb)
    {
        if (!_referenceTracker.Add(enumerable))
        {
            throw new JsonSerializationException($"Circular reference detected in type '{enumerable.GetType().FullName}'.");
        }

        try
        {
            sb.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;
                SerializeValue(item, sb);
            }
            sb.Append(']');
        }
        finally
        {
            _referenceTracker.Remove(enumerable);
        }
    }

    private void SerializeObject(object obj, StringBuilder sb)
    {
        if (!_referenceTracker.Add(obj))
        {
            throw new JsonSerializationException($"Circular reference detected in object of type '{obj.GetType().FullName}'.");
        }

        try
        {
            sb.Append('{');
            var properties = TypeCache.GetReadableProperties(obj.GetType());
            var first = true;

            foreach (var prop in properties)
            {
                var val = prop.GetValue(obj);
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;

                WriteString(prop.Name, sb);
                sb.Append(':');
                SerializeValue(val, sb);
            }

            sb.Append('}');
        }
        finally
        {
            _referenceTracker.Remove(obj);
        }
    }
    private static void WriteString(string value, StringBuilder sb)
    {
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(ch))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)ch);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        sb.Append('"');
    }
}