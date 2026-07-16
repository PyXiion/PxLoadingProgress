// TextField.cs
// Copyright Karel Kroeze, 2018-2018

namespace ru.pyxiion.modrim.LoadingProgress.ColorPicker;

internal sealed class TextField<T>(
    T? value,
    string id,
    Action<T?> callback,
    Func<string, T>? parser = null,
    Func<string, bool>? validator = null,
    Func<T?, string>? toString = null
)
{
    private T? _value = value;
    private readonly string _id = id;
    private string _temp = value?.ToString() ?? "<null>";
    private readonly Func<string, bool>? _validator = validator;
    private readonly Func<string, T>? _parser = parser;
    private readonly Func<T?, string>? _toString = toString;
    private readonly Action<T?> _callback = callback;

    public T? Value
    {
        get => _value;
        set
        {
            _value = value;
            _temp = _toString?.Invoke(value) ?? value?.ToString() ?? "<null>";
        }
    }

    public static TextField<float> Float01(float value, string id, Action<float> callback) =>
        new(
            value,
            id,
            callback,
            float.Parse,
            Validate01,
            f => Round(f).ToString(CultureInfo.InvariantCulture)
        );

    public static TextField<string?> Hex(string? value, string id, Action<string?> callback) =>
        new(value, id, callback, hex => hex, ValidateHex);

    public void Draw(Rect rect)
    {
        var valid = _validator?.Invoke(_temp) ?? true;
        GUI.color = valid ? Color.white : Color.red;
        GUI.SetNextControlName(_id);
        var temp = Widgets.TextField(rect, _temp);
        GUI.color = Color.white;

        if (temp != _temp)
        {
            _temp = temp;
            if (_validator?.Invoke(_temp) ?? true)
            {
                _value = _parser != null ? _parser(_temp) : default;
                _callback?.Invoke(_value);
            }
        }
    }

    private static bool Validate01(string value) =>
        float.TryParse(value, out var parsed) && parsed is >= 0f and <= 1f;

    private static bool ValidateHex(string value) => ColorUtility.TryParseHtmlString(value, out _);

    private static float Round(float value, int digits = 2)
    {
        var exponent = Mathf.Pow(10, digits);
        return Mathf.RoundToInt(value * exponent) / exponent;
    }
}
