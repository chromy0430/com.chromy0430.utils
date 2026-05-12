using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class CustomLogger
{
    private static string GetColor(string name)
    {
        var hue = (uint)name.GetHashCode() / (float)uint.MaxValue;
        var color = Color.HSVToRGB(hue, 0.6f, 1f);
        return ColorUtility.ToHtmlStringRGB(color);
    }
    
    public static void Log(object message, [CallerFilePath] string file = "")
    {
        var className = Path.GetFileNameWithoutExtension(file);
        var color = GetColor(className);
        Debug.Log($"<color=#{color}><b>[{className}]</b></color> {message}");
    }
}
