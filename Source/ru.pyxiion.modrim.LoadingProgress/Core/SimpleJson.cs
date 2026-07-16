using System.Globalization;
using System.Text;

namespace ru.pyxiion.modrim.LoadingProgress;

internal static class SimpleJson
{
#pragma warning disable IDE0058 // StringBuilder.Append return value intentionally discarded

    internal static string Serialize(StartupImpact.Dialog.StartupImpactSessionData data)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        Field(sb, "timestamp", data.Timestamp.ToString("O"));
        sb.Append(',');
        Field(sb, "loadingTime", data.LoadingTime);
        sb.Append(',');
        DictField(sb, "metrics", data.Metrics);
        sb.Append(',');
        Field(sb, "totalImpact", data.TotalImpact);
        sb.Append(',');
        DictField(sb, "offThreadMetrics", data.OffThreadMetrics);
        sb.Append(',');
        Field(sb, "offThreadTotalImpact", data.OffThreadTotalImpact);
        sb.Append(',');
        sb.Append("\"mods\":[");
        var first = true;
        foreach (var mod in data.Mods)
        {
            if (!first) { sb.Append(','); }
            first = false;
            sb.Append('{');
            Field(sb, "modName", mod.ModName);
            sb.Append(',');
            Field(sb, "modPackageId", mod.ModPackageId);
            sb.Append(',');
            DictField(sb, "metrics", mod.Metrics);
            sb.Append(',');
            Field(sb, "totalImpact", mod.TotalImpact);
            sb.Append(',');
            DictField(sb, "offThreadMetrics", mod.OffThreadMetrics);
            sb.Append(',');
            Field(sb, "offThreadTotalImpact", mod.OffThreadTotalImpact);
            sb.Append('}');
        }
        sb.Append(']');
        sb.Append('}');
        return sb.ToString();
    }

    internal static StartupImpact.Dialog.StartupImpactSessionData? DeserializeSession(
        string json)
    {
        var reader = new Reader(json);
        if (!reader.SkipWhitespace() || reader.Peek() != '{') { return null; }
        reader.Read(); // skip '{'

        DateTimeOffset? timestamp = null;
        var loadingTime = 0f;
        Dictionary<string, float>? metrics = null;
        var totalImpact = 0f;
        Dictionary<string, float>? offThreadMetrics = null;
        var offThreadTotalImpact = 0f;
        List<StartupImpact.Dialog.StartupImpactSessionModData>? mods = null;

        while (reader.SkipWhitespace() && reader.Peek() != '}')
        {
            var key = reader.ReadString();
            if (key == null) { break; }
            reader.SkipWhitespace();
            if (reader.Read() != ':') { break; }
            reader.SkipWhitespace();

            switch (key)
            {
                case "timestamp":
                    timestamp = DateTimeOffset.Parse(
                        reader.ReadString()!, CultureInfo.InvariantCulture);
                    break;
                case "loadingTime":
                    loadingTime = (float)reader.ReadNumber();
                    break;
                case "metrics":
                    metrics = ReadDict(reader);
                    break;
                case "totalImpact":
                    totalImpact = (float)reader.ReadNumber();
                    break;
                case "offThreadMetrics":
                    offThreadMetrics = ReadDict(reader);
                    break;
                case "offThreadTotalImpact":
                    offThreadTotalImpact = (float)reader.ReadNumber();
                    break;
                case "mods":
                    mods = [];
                    if (reader.Peek() == '[')
                    {
                        reader.Read();
                        while (reader.SkipWhitespace() && reader.Peek() != ']')
                        {
                            var mod = ReadMod(reader);
                            if (mod != null) { mods.Add(mod); }
                            reader.SkipWhitespace();
                            if (reader.Peek() == ',') { reader.Read(); }
                        }
                        if (reader.Peek() == ']') { reader.Read(); }
                    }
                    break;
                default:
                    reader.SkipValue();
                    break;
            }

            reader.SkipWhitespace();
            if (reader.Peek() == ',') { reader.Read(); }
        }
        if (reader.Peek() == '}') { reader.Read(); }

        return new StartupImpact.Dialog.StartupImpactSessionData(
            timestamp ?? DateTimeOffset.UtcNow,
            loadingTime,
            metrics ?? [],
            totalImpact,
            offThreadMetrics ?? [],
            offThreadTotalImpact,
            mods ?? []
        );
    }

    private static StartupImpact.Dialog.StartupImpactSessionModData? ReadMod(Reader reader)
    {
        if (reader.Peek() != '{') { return null; }
        reader.Read();

        var modName = "";
        var modPackageId = "";
        var metrics = new Dictionary<string, float>();
        var totalImpact = 0f;
        var offThreadMetrics = new Dictionary<string, float>();
        var offThreadTotalImpact = 0f;

        while (reader.SkipWhitespace() && reader.Peek() != '}')
        {
            var key = reader.ReadString();
            if (key == null) { break; }
            reader.SkipWhitespace();
            if (reader.Read() != ':') { break; }
            reader.SkipWhitespace();

            switch (key)
            {
                case "modName":
                    modName = reader.ReadString() ?? "";
                    break;
                case "modPackageId":
                    modPackageId = reader.ReadString() ?? "";
                    break;
                case "metrics":
                    metrics = ReadDict(reader);
                    break;
                case "totalImpact":
                    totalImpact = (float)reader.ReadNumber();
                    break;
                case "offThreadMetrics":
                    offThreadMetrics = ReadDict(reader);
                    break;
                case "offThreadTotalImpact":
                    offThreadTotalImpact = (float)reader.ReadNumber();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }

            reader.SkipWhitespace();
            if (reader.Peek() == ',') { reader.Read(); }
        }
        if (reader.Peek() == '}') { reader.Read(); }

        return new StartupImpact.Dialog.StartupImpactSessionModData(
            modName, modPackageId, metrics, totalImpact, offThreadMetrics, offThreadTotalImpact
        );
    }

    private static Dictionary<string, float> ReadDict(Reader reader)
    {
        var dict = new Dictionary<string, float>();
        if (reader.Peek() != '{') { return dict; }
        reader.Read();

        while (reader.SkipWhitespace() && reader.Peek() != '}')
        {
            var key = reader.ReadString();
            if (key == null) { break; }
            reader.SkipWhitespace();
            if (reader.Read() != ':') { break; }
            reader.SkipWhitespace();
            dict[key] = (float)reader.ReadNumber();
            reader.SkipWhitespace();
            if (reader.Peek() == ',') { reader.Read(); }
        }
        if (reader.Peek() == '}') { reader.Read(); }

        return dict;
    }

    private static void Field(StringBuilder sb, string name, string value)
    {
        sb.Append('"');
        sb.Append(name);
        sb.Append("\":\"");
        Escape(sb, value);
        sb.Append('"');
    }

    private static void Field(StringBuilder sb, string name, float value)
    {
        sb.Append('"');
        sb.Append(name);
        sb.Append("\":");
        sb.Append(value.ToString("G", CultureInfo.InvariantCulture));
    }

    private static void DictField(
        StringBuilder sb, string name, IReadOnlyDictionary<string, float> dict)
    {
        sb.Append('"');
        sb.Append(name);
        sb.Append("\":{");
        var first = true;
        foreach (var kvp in dict)
        {
            if (!first) { sb.Append(','); }
            first = false;
            sb.Append('"');
            Escape(sb, kvp.Key);
            sb.Append("\":");
            sb.Append(kvp.Value.ToString("G", CultureInfo.InvariantCulture));
        }
        sb.Append('}');
    }

    private static void Escape(StringBuilder sb, string s)
    {
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
    }

#pragma warning restore IDE0058

    private sealed class Reader(string text)
    {
        private readonly string _text = text;
        private int _pos;

        internal bool SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
            return _pos < _text.Length;
        }

        internal char Peek() => _pos < _text.Length ? _text[_pos] : '\0';

        internal char Read() => _pos < _text.Length ? _text[_pos++] : '\0';

        internal string? ReadString()
        {
            if (Peek() != '"') { return null; }
            _pos++; // skip opening quote
            var sb = new StringBuilder();
            while (_pos < _text.Length)
            {
                var c = _text[_pos++];
                if (c == '"') { return sb.ToString(); }
                if (c == '\\')
                {
                    if (_pos >= _text.Length) { return sb.ToString(); }
                    var esc = _text[_pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 <= _text.Length
                                && int.TryParse(
                                    _text.Substring(_pos, 4),
                                    NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture,
                                    out var code))
                            {
                                sb.Append((char)code);
                                _pos += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        internal double ReadNumber()
        {
            var start = _pos;
            if (_pos < _text.Length && _text[_pos] == '-') { _pos++; }
            while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9')
            {
                _pos++;
            }
            if (_pos < _text.Length && _text[_pos] == '.')
            {
                _pos++;
                while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9')
                {
                    _pos++;
                }
            }
            if (_pos < _text.Length && (_text[_pos] == 'e' || _text[_pos] == 'E'))
            {
                _pos++;
                if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) { _pos++; }
                while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9')
                {
                    _pos++;
                }
            }
            var numStr = _text.Substring(start, _pos - start);
            return double.Parse(numStr, CultureInfo.InvariantCulture);
        }

        internal void SkipValue()
        {
            if (_pos >= _text.Length) { return; }
            var c = _text[_pos];
            if (c == '"') { _ = ReadString(); return; }
            if (c == '{' || c == '[')
            {
                var depth = 1;
                _pos++;
                while (_pos < _text.Length && depth > 0)
                {
                    if (_text[_pos] == '{' || _text[_pos] == '[') { depth++; }
                    else if (_text[_pos] == '}' || _text[_pos] == ']') { depth--; }
                    _pos++;
                }
                return;
            }
            while (_pos < _text.Length
                && _text[_pos] != ','
                && _text[_pos] != '}'
                && _text[_pos] != ']'
                && !char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }
    }
}
