using System.IO;

namespace STranslate.Plugin.Ocr.SimpleTex;

public class Settings
{
    private static string _defaultPath = string.Empty;
    public static string DefaultPath
    {
        get => _defaultPath;
        set
        {
            if (!Directory.Exists(value))
                Directory.CreateDirectory(value);

            _defaultPath = value;
        }
    }
    
    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
}