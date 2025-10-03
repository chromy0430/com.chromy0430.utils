using System.Collections.Generic;
using UnityEngine;

public static class Utils 
{
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

