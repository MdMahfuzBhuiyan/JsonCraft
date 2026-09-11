namespace JsonCraft.Core;
using System.Globalization;
using System.Text;

internal class JsonParser
{
    private readonly string _json;
    private int _index;

    private JsonParser(string json)
    {
        _json = json ?? throw new ArgumentNullException(nameof(json));
        _index = 0;
    }

    public static object? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new JsonSerializationException("JSON input is empty or contains only whitespace.");
        }

        var parser = new JsonParser(json);
        parser.SkipWhitespace();
        var result = parser.ParseValue();
        parser.SkipWhitespace();

        if (parser._index < parser._json.Length)
        {
            throw new JsonSerializationException($"Unexpected character '{parser._json[parser._index]}' at index {parser._index} after root element.");
        }

        return result;
    }
    private object? ParseValue()
    {
        SkipWhitespace();
        if (_index >= _json.Length)
        {
            throw new JsonSerializationException("Unexpected end of input while expecting a value.");
        }

        var ch = _json[_index];
        if (ch == '{')
        {
            return ParseObject();
        }
        if (ch == '[')
        {
            return ParseArray();
        }
        if (ch == '"')
        {
            return ParseString();
        }
        if (ch == 't' || ch == 'f')
        {
            return ParseBoolean();
        }
        if (ch == 'n')
        {
            return ParseNull();
        }
        if (ch == '-' || char.IsAsciiDigit(ch))
        {
            return ParseNumber();
        }

        throw new JsonSerializationException($"Unexpected character '{ch}' at index {_index}.");
    }
    private Dictionary<string, object?> ParseObject()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _index++;
        SkipWhitespace();

        if (_index < _json.Length && _json[_index] == '}')
        {
            _index++;
            return dict;
        }

        while (true)
        {
            SkipWhitespace();
            if (_index >= _json.Length)
            {
                throw new JsonSerializationException("Unterminated object: expected string key or '}'.");
            }

            if (_json[_index] != '"')
            {
                throw new JsonSerializationException($"Expected string key in object at index {_index}.");
            }

            var key = ParseString();
            SkipWhitespace();

            if (_index >= _json.Length || _json[_index] != ':')
            {
                throw new JsonSerializationException($"Expected ':' after property name at index {_index}.");
            }

            _index++;
            var value = ParseValue();
            dict[key] = value;

            SkipWhitespace();
            if (_index >= _json.Length)
            {
                throw new JsonSerializationException("Unterminated object: expected ',' or '}'.");
            }

            if (_json[_index] == ',')
            {
                _index++;
                SkipWhitespace();
                if (_index < _json.Length && _json[_index] == '}')
                {
                    throw new JsonSerializationException($"Trailing comma detected in object at index {_index}.");
                }
            }
            else if (_json[_index] == '}')
            {
                _index++;
                break;
            }
            else
            {
                throw new JsonSerializationException($"Expected ',' or '}}' in object at index {_index}.");
            }
        }

        return dict;
    }
    private List<object?> ParseArray()
    {
        var list = new List<object?>();
        _index++;
        SkipWhitespace();

        if (_index < _json.Length && _json[_index] == ']')
        {
            _index++;
            return list;
        }

        while (true)
        {
            var value = ParseValue();
            list.Add(value);

            SkipWhitespace();
            if (_index >= _json.Length)
            {
                throw new JsonSerializationException("Unterminated array: expected ',' or ']'.");
            }

            if (_json[_index] == ',')
            {
                _index++;
                SkipWhitespace();
                if (_index < _json.Length && _json[_index] == ']')
                {
                    throw new JsonSerializationException($"Trailing comma detected in array at index {_index}.");
                }
            }
            else if (_json[_index] == ']')
            {
                _index++;
                break;
            }
            else
            {
                throw new JsonSerializationException($"Expected ',' or ']' in array at index {_index}.");
            }
        }

        return list;
    }
    private string ParseString()
    {
        _index++;
        var sb = new StringBuilder();

        while (_index < _json.Length)
        {
            var ch = _json[_index++];
            if (ch == '"')
            {
                return sb.ToString();
            }

            if (ch == '\\')
            {
                if (_index >= _json.Length)
                {
                    throw new JsonSerializationException("Incomplete escape sequence at end of string.");
                }

                var esc = _json[_index++];
                switch (esc)
                {
                    case '"':
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case '/':
                        sb.Append('/');
                        break;
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'u':
                        if (_index + 4 > _json.Length)
                        {
                            throw new JsonSerializationException("Incomplete unicode escape sequence.");
                        }
                        var hex = _json.Substring(_index, 4);
                        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                        {
                            throw new JsonSerializationException($"Invalid unicode escape sequence '\\u{hex}'.");
                        }
                        sb.Append((char)codePoint);
                        _index += 4;
                        break;
                    default:
                        throw new JsonSerializationException($"Unsupported escape character '\\{esc}'.");
                }
            }
            else
            {
                if (char.IsControl(ch))
                {
                    throw new JsonSerializationException($"Unescaped control character (0x{(int)ch:x2}) in string.");
                }
                sb.Append(ch);
            }
        }

        throw new JsonSerializationException("Unterminated string.");
    }
    private object ParseNumber()
    {
        var start = _index;
        if (_json[_index] == '-')
        {
            _index++;
        }

        if (_index >= _json.Length || !char.IsAsciiDigit(_json[_index]))
        {
            throw new JsonSerializationException($"Malformed number at index {start}.");
        }

        while (_index < _json.Length && char.IsAsciiDigit(_json[_index]))
        {
            _index++;
        }

        var isFloatingPoint = false;
        if (_index < _json.Length && _json[_index] == '.')
        {
            isFloatingPoint = true;
            _index++;
            if (_index >= _json.Length || !char.IsAsciiDigit(_json[_index]))
            {
                throw new JsonSerializationException($"Malformed decimal in number at index {start}.");
            }
            while (_index < _json.Length && char.IsAsciiDigit(_json[_index]))
            {
                _index++;
            }
        }

        if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
        {
            isFloatingPoint = true;
            _index++;
            if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-'))
            {
                _index++;
            }
            if (_index >= _json.Length || !char.IsAsciiDigit(_json[_index]))
            {
                throw new JsonSerializationException($"Malformed exponent in number at index {start}.");
            }
            while (_index < _json.Length && char.IsAsciiDigit(_json[_index]))
            {
                _index++;
            }
        }

        var numStr = _json.Substring(start, _index - start);

        if (isFloatingPoint)
        {
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }
            throw new JsonSerializationException($"Invalid floating point number '{numStr}'.");
        }

        if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
        {
            return l;
        }

        if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fallbackDouble))
        {
            return fallbackDouble;
        }

        throw new JsonSerializationException($"Invalid number '{numStr}'.");
    }
    private bool ParseBoolean()
    {
        if (_index + 4 <= _json.Length && _json.Substring(_index, 4) == "true")
        {
            _index += 4;
            return true;
        }

        if (_index + 5 <= _json.Length && _json.Substring(_index, 5) == "false")
        {
            _index += 5;
            return false;
        }

        throw new JsonSerializationException($"Invalid boolean token at index {_index}.");
    }
    private object? ParseNull()
    {
        if (_index + 4 <= _json.Length && _json.Substring(_index, 4) == "null")
        {
            _index += 4;
            return null;
        }

        throw new JsonSerializationException($"Invalid null token at index {_index}.");
    }
    private void SkipWhitespace()
    {
        while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
        {
            _index++;
        }
    }
}