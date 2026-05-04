// RecentColors.cs
// Copyright Karel Kroeze, 2018-2018

namespace ilyvion.LoadingProgress.ColorPicker;

internal static class RecentColors
{
    private const int max = 20;
    private static List<Color> _colors = [];

    static RecentColors()
    {
        Read();
    }

    public static List<Color> Colors => _colors;

    public static int Count => _colors.Count;

    public static void Add(Color color)
    {
        _ = _colors.RemoveAll(c => c == color);
        _colors.Insert(0, color);

        while (_colors.Count > max)
        {
            _colors.RemoveAt(_colors.Count - 1);
        }

        Write();
    }

    private static void Read()
    {
        var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ColorPicker.xml");
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            Scribe.loader.InitLoading(path);
            ExposeData();
        }
        catch (Exception ex)
        {
            Log.Error("ColorPicker :: Error loading recent Colors from file:" + ex);
        }
        finally
        {
            Scribe.loader.FinalizeLoading();
        }
    }

    private static void Write()
    {
        try
        {
            var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ColorPicker.xml");
            Scribe.saver.InitSaving(path, "ColorPicker");
            ExposeData();
        }
        catch (Exception ex)
        {
            Log.Error("ColorPicker :: Error saving recent Colors to file:" + ex);
        }
        finally
        {
            Scribe.saver.FinalizeSaving();
        }
    }

    private static void ExposeData()
    {
#pragma warning disable IDE0022 // Use expression body for method
        Scribe_Collections.Look(ref _colors, "RecentColors");
#pragma warning restore IDE0022 // Use expression body for method
    }
}
