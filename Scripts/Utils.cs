using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

using System;
using System.Threading.Tasks;

public static class Utils 
{
    [Tooltip("유니티 Awaitable용 대기 함수")]
    public static Awaitable WaitUntil(this Func<bool> condition, int pollIntervalMs = 33) 
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (pollIntervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(pollIntervalMs));

        var source = new AwaitableCompletionSource();

        if (condition())
        {
            source.SetResult();
            return source.Awaitable;
        }
        
        var interval = TimeSpan.FromMilliseconds(pollIntervalMs);

        async void Poll()
        {
            while (!condition())
            {
                await Awaitable.WaitForSecondsAsync((float)interval.TotalSeconds);
            }
            source.SetResult();
        }

        Poll();
        return source.Awaitable;
    }
    
    // Tasks
    // Default poll interval of 33ms = one frame at 30 FPS (1000 ms / 30 = 33.33ms)
    [Tooltip("C# Task용 대기 + 타임아웃")]
    public static async Task<bool> WaitUntil(this Func<bool> condition, int timeoutMs = -1, int pollIntervalMs = 33)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (pollIntervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(pollIntervalMs));

        var waitTask = RunWaitLoop(condition, pollIntervalMs);

        if (timeoutMs < 0)
        {
            await waitTask;
            return true;
        }
        
        var timeoutTask = Task.Delay(timeoutMs);
        var finished = await Task.WhenAny(waitTask, timeoutTask);
        return finished == waitTask;
    }

    static async Task RunWaitLoop(Func<bool> condition, int pollIntervalMs)
    {
        while (!condition())
        {
            await Task.Delay(pollIntervalMs).ConfigureAwait(false);
        }
    }
    
    // Copy
    public static void BlockCopy<T>(this Span<T> src, int srcOffset, Span<T> dst, int dstOffset, int count)
    {
        if (((uint)(srcOffset + count)) > (uint)src.Length) throw new ArgumentException("Source span is too small");
        if (((uint)(dstOffset + count)) > (uint)dst.Length) throw new ArgumentException("Destination span is too small"); 
        
        src.Slice(srcOffset, count).CopyTo(dst.Slice(dstOffset));
    }
    
    [Tooltip("배열 간 데이터 복사를 위한 헬퍼 함수")]
    public static void BlockCopy<T>(this T[] src, int srcOffset, T[] dst, int dstOffset, int count)
    {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (dst == null) throw new ArgumentNullException(nameof(dst));  
        
        src.AsSpan().BlockCopy(srcOffset, dst.AsSpan(), dstOffset, count);
        //Array.Copy(src, srcOffset, dst, dstOffset, count);
    }
    
    // Math
    [Tooltip("플레이어 입력 값(0~1)을 이동속도(10~100)로 변환 하거나, 체력 바의 백분율을 UI바의 길이로 변환하는 경우")]
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return Mathf.Lerp(from2, to2, Mathf.InverseLerp(from1, to1, value));
    }
    
    // Vector3 Extension
    [Tooltip("EX) transform.position.With(y : number)")]
    public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
    {
        return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
    }

    [Tooltip("EX) transform.position.Add(x:number, y:number) or can position.With.Add(x:number)")]
    public static Vector3 Add(this Vector3 vector, float? x = null, float? y = null, float? z = null)
    {
        return new Vector3(vector.x + (x ?? 0), vector.y + (y ?? 0), vector.z + (z ?? 0));
    }

    // GameObject Extension

    [Tooltip("EX) gameObject.GetOrAdd<Rigidbody>().enable = true;")]
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (!component) component = gameObject.AddComponent<T>();        
        return component;
    }

    // [SerializeField] Light defaultLight
    // Light light = gameObject.GetComponent<Light>().OrNull() ?? defaultLight;
    [Tooltip("EX) var something = gameObject.GetComponent<T>().OrNull()?.someField ?? 0")]
    public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;

    

    // Transform Extension

    // var activeChildren = trnasform.Children().Where(child => child.gameObject.activeInHierarchy).ToList();
    [Tooltip("EX) foreach (Transform child in transform.Children() )")]
    public static IEnumerable<Transform> Children (this Transform parent)
    {
        foreach (Transform child in parent)
        {
            yield return child;
        }
    }

    public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    public static void DestroyChildren(this Transform parent)
    {
        parent.PerformActionOnChildren(child => Object.Destroy(child.gameObject));
    }

    public static void EnableChildren(this Transform parent)
    {
        parent.PerformActionOnChildren(child => child.gameObject.SetActive(true));
    }

    public static void DisableChildren(this Transform parent)
    {
        parent.PerformActionOnChildren(child => child.gameObject.SetActive(false));
    }

    static void PerformActionOnChildren(this Transform parent, System.Action<Transform> action)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            action(parent.GetChild(i));
        }
    }
}

