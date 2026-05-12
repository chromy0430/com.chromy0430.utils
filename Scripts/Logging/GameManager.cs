using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        Debug.unityLogger.logHandler = new CustomLogHandler();
        
        // 로그를 비활성화 시키기 위한 코드
        // Debug.unityLogger.logEnabled = false;
    }
    
    private void OnGUI() => ((CustomLogHandler)Debug.unityLogger.logHandler).RenderLog();
}

internal class CustomLogHandler : ILogHandler
{
    private string _lastLine = "";
    private readonly ILogHandler _default;

    public CustomLogHandler()
    {
        _default = Debug.unityLogger.logHandler;
    }
    
    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        var caller = GetCaller();
        var color = GetColor(caller);
        
        // GUI에 한 줄씩만 로그 출력
        _lastLine = $"<color=#{color}><b>[{caller}]</b></color> {string.Format(format,args)}";
        
        // Console창에 모든 로그 출력
        _default.LogFormat(logType, context, $"<color=#{color}><b>[{caller}]</b></color> {format}", args);
    }

    public void RenderLog()
    {
        GUI.skin.label.richText = true;
        GUI.skin.label.fontSize = 20;
        GUI.Label(new Rect(10, Screen.height - 40, 800, 30), _lastLine);
    }

    public void LogException(Exception exception, Object context)
    {
        _default.LogException(exception, context);
    }

    private static string GetColor(string name)
    {
        var hue = (uint)name.GetHashCode() / (float)uint.MaxValue;
        var color = Color.HSVToRGB(hue, 0.6f, 1f);
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    private static string GetCaller()
    {
        var stack = new StackTrace(2, false);
        for (int i = 0; i < stack.FrameCount; i++)
        {
            var type = stack.GetFrame(i)?.GetMethod()?.DeclaringType;

            if (type is not null && type.Namespace?.StartsWith("UnityEngine") != true)
            {
                return type.Name;
            }
        }

        return "Default";
    }
}
