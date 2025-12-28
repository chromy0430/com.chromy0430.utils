using UnityEngine;
using System;
using System.Collections.Generic;

// [신뢰] 데이터 직렬화를 위한 SO 정의
// 메모리 효율성을 위해 class 대신 struct 사용
public class SceneViewBookmarkData : ScriptableObject
{
    [Serializable]
    public struct Bookmark
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public float size; // 카메라 줌(Zoom) 거리

        public Bookmark(string n, Vector3 p, Quaternion r, float s)
        {
            name = n;
            position = p;
            rotation = r;
            size = s;
        }
    }

    // 리스트 탐색 시 GC 발생을 줄이기 위해 단순 List 사용
    public List<Bookmark> bookmarks = new List<Bookmark>();
}