using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using System.Linq;

// [신뢰] Unity 6000+ Overlay API 적용
[Overlay(typeof(SceneView), "Quick Bookmarks", true)]
public class SceneBookmarkOverlay : ToolbarOverlay
{
    // 툴바에 추가될 버튼들의 ID 등록
    SceneBookmarkOverlay() : base(
        SaveBookmarkButton.id,
        BookmarkListDropdown.id
    )
    { }
}

// ---------------------------------------------------------
// 1. 북마크 저장 버튼 (Save Button)
// ---------------------------------------------------------
[EditorToolbarElement(id, typeof(SceneView))]
class SaveBookmarkButton : EditorToolbarButton
{
    public const string id = "SceneBookmark/Save";

    public SaveBookmarkButton()
    {
        text = "Save";
        tooltip = "Save current Scene View camera transform";
        icon = EditorGUIUtility.IconContent("SaveAs").image as Texture2D;

        clicked += OnClick;
    }

    void OnClick()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return;

        // 데이터 로드 또는 생성
        var data = BookmarkManager.LoadOrCreateData();

        // 현재 뷰 정보 캡처
        string defaultName = $"View_{data.bookmarks.Count + 1}";
        var newBookmark = new SceneViewBookmarkData.Bookmark(
            defaultName,
            view.pivot,
            view.rotation,
            view.size
        );

        data.bookmarks.Add(newBookmark);
        EditorUtility.SetDirty(data); // 저장 플래그 설정
        AssetDatabase.SaveAssets();   // 에셋 파일 쓰기

        Debug.Log($"[Bookmark] Saved: {defaultName} at {view.pivot}");
    }
}

// ---------------------------------------------------------
// 2. 북마크 목록 드롭다운 (Dropdown List)
// ---------------------------------------------------------
[EditorToolbarElement(id, typeof(SceneView))]
class BookmarkListDropdown : EditorToolbarDropdown
{
    public const string id = "SceneBookmark/Dropdown";

    public BookmarkListDropdown()
    {
        text = "Jump To...";
        tooltip = "Select a saved bookmark to move camera";
        clicked += ShowDropdown;
    }

    void ShowDropdown()
    {
        var data = BookmarkManager.LoadOrCreateData();
        var menu = new GenericMenu();

        if (data.bookmarks.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No bookmarks saved"));
        }
        else
        {
            // GC 최소화를 위해 foreach 대신 for문 사용 권장
            for (int i = 0; i < data.bookmarks.Count; i++)
            {
                int index = i; // 클로저 캡처용 로컬 변수
                var b = data.bookmarks[i];

                // 메뉴 항목 생성: 이름, 체크여부, 콜백함수
                menu.AddItem(new GUIContent($"{index + 1}. {b.name}"), false, () =>
                {
                    MoveCamera(b);
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear All Bookmarks"), false, () =>
            {
                data.bookmarks.Clear();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            });
        }

        menu.ShowAsContext();
    }

    void MoveCamera(SceneViewBookmarkData.Bookmark b)
    {
        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            // [핵심] 뷰 이동 로직
            // LookAtDirect는 즉시 이동, LookAt은 부드러운 이동(옵션)
            view.LookAt(b.position, b.rotation, b.size);
            view.Repaint();
        }
    }
}

// ---------------------------------------------------------
// 3. 데이터 관리자 (Helper Class)
// ---------------------------------------------------------
public static class BookmarkManager
{
    private const string AssetPath = "Assets/Editor/SceneBookmarks.asset";

    public static SceneViewBookmarkData LoadOrCreateData()
    {
        var data = AssetDatabase.LoadAssetAtPath<SceneViewBookmarkData>(AssetPath);

        if (data == null)
        {
            // Editor 폴더가 없으면 생성
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            data = ScriptableObject.CreateInstance<SceneViewBookmarkData>();
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssets();
        }

        return data;
    }
}