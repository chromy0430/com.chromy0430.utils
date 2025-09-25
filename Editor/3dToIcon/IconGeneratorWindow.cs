using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IconGeneratorWindow : EditorWindow
{
    // --- 기본 설정값 정의 ---
    private const int DEFAULT_IMAGE_SIZE = 512;
    private static readonly Vector3 DEFAULT_CAMERA_ROTATION = new Vector3(30, -135, 0);
    private const float DEFAULT_PADDING = 0.1f;
    private const string DEFAULT_SAVE_PATH = "Assets/GeneratedIcons";

    // 설정 변수
    private GameObject[] selectedPrefabs;
    private int imageSize = DEFAULT_IMAGE_SIZE;
    private Vector3 cameraRotation = DEFAULT_CAMERA_ROTATION;
    private float padding = DEFAULT_PADDING;
    private string savePath = DEFAULT_SAVE_PATH;

    // 내부 관리 변수
    private PreviewRenderUtility previewRenderUtility;
    private Texture2D previewTexture;
    private GameObject currentPrefab;

    [MenuItem("Tools/Icon Generator")]
    private static void ShowWindow()
    {
        GetWindow<IconGeneratorWindow>("Icon Generator").Show();
    }

    private void OnEnable()
    {
        previewRenderUtility = new PreviewRenderUtility();
        previewRenderUtility.camera.fieldOfView = 30.0f;
        Light mainLight = previewRenderUtility.lights[0];
        mainLight.intensity = 1.2f;
        mainLight.transform.rotation = Quaternion.Euler(50f, 50f, 0);
    }

    private void OnDisable()
    {
        if (previewRenderUtility != null)
        {
            previewRenderUtility.Cleanup();
            previewRenderUtility = null;
        }
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    // ▼▼▼ 리셋 메소드 추가 ▼▼▼
    /// <summary>
    /// 모든 설정을 기본값으로 되돌립니다.
    /// </summary>
    private void ResetSettings()
    {
        imageSize = DEFAULT_IMAGE_SIZE;
        cameraRotation = DEFAULT_CAMERA_ROTATION;
        padding = DEFAULT_PADDING;
        savePath = DEFAULT_SAVE_PATH;

        // 변경된 설정으로 미리보기를 업데이트합니다.
        UpdatePreview();
        // 키보드 포커스를 해제하여 UI가 깔끔하게 보이도록 합니다.
        GUI.FocusControl(null);
    }
    // ▲▲▲ 리셋 메소드 추가 ▲▲▲

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1. Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        imageSize = EditorGUILayout.IntPopup("Icon Size", imageSize, new string[] { "128", "256", "512", "1024" }, new int[] { 128, 256, 512, 1024 });
        cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);
        padding = EditorGUILayout.Slider("Padding", padding, 0f, 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }

        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Save Path", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("경로 오류", "반드시 현재 Unity 프로젝트 내부의 폴더를 선택해야 합니다.", "확인");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // ▼▼▼ 리셋 버튼 추가 ▼▼▼
        if (GUILayout.Button("Reset Settings to Default"))
        {
            // 사용자에게 초기화 여부를 다시 한번 확인받습니다.
            if (EditorUtility.DisplayDialog("Reset Settings",
                                            "모든 설정을 기본값으로 되돌리시겠습니까?",
                                            "Yes, Reset", "Cancel"))
            {
                ResetSettings();
            }
        }
        // ▲▲▲ 리셋 버튼 추가 ▲▲▲

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("2. Preview & Generate", EditorStyles.boldLabel);
        if (selectedPrefabs == null || selectedPrefabs.Length == 0)
        {
            EditorGUILayout.HelpBox("Project 창에서 아이콘을 생성할 프리팹을 하나 이상 선택해주세요.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Selected: {selectedPrefabs.Length} prefab(s)");
        }

        if (previewTexture != null)
        {
            GUILayout.Box(previewTexture, GUILayout.Width(256), GUILayout.Height(256), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
        }

        GUI.enabled = selectedPrefabs != null && selectedPrefabs.Length > 0;
        if (GUILayout.Button($"Generate ({selectedPrefabs?.Length ?? 0}) Icon(s)"))
        {
            GenerateAllIcons();
        }
        GUI.enabled = true;
    }

    private void OnSelectionChange()
    {
        List<GameObject> prefabs = new List<GameObject>();
        foreach (var obj in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
        {
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                prefabs.Add(obj);
            }
        }
        selectedPrefabs = prefabs.ToArray();

        if (selectedPrefabs.Length > 0 && selectedPrefabs[0] != currentPrefab)
        {
            currentPrefab = selectedPrefabs[0];
            UpdatePreview();
        }
        else if (selectedPrefabs.Length == 0)
        {
            currentPrefab = null;
        }

        Repaint();
    }

    private void UpdatePreview()
    {
        if (currentPrefab == null)
        {
            if (previewTexture != null) DestroyImmediate(previewTexture);
            return;
        }

        GameObject instance = null;
        try
        {
            instance = previewRenderUtility.InstantiatePrefabInScene(currentPrefab);
            Bounds bounds = CalculateBounds(instance);
            PositionCamera(bounds);
            previewTexture = RenderToTexture(256);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"미리보기 생성 중 오류 발생: {e.Message}");
        }
        finally
        {
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
        }
    }

    private void GenerateAllIcons()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        try
        {
            for (int i = 0; i < selectedPrefabs.Length; i++)
            {
                GameObject prefab = selectedPrefabs[i];
                EditorUtility.DisplayProgressBar("Generating Icons", $"Processing {prefab.name}...", (float)i / selectedPrefabs.Length);

                GenerateSingleIcon(prefab);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"{selectedPrefabs.Length}개의 아이콘이 성공적으로 생성되었습니다.\n경로: {savePath}", "확인");
        }
    }

    private void GenerateSingleIcon(GameObject prefab)
    {
        GameObject instance = null;
        try
        {
            instance = previewRenderUtility.InstantiatePrefabInScene(prefab);
            Bounds bounds = CalculateBounds(instance);
            PositionCamera(bounds);
            Texture2D finalTexture = RenderToTexture(imageSize);

            byte[] bytes = finalTexture.EncodeToPNG();
            string filePath = $"{savePath}/{prefab.name}.png";
            File.WriteAllBytes(filePath, bytes);

            DestroyImmediate(finalTexture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"'{prefab.name}' 아이콘 생성 중 오류 발생: {e.Message}");
        }
        finally
        {
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
        }
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    private void PositionCamera(Bounds bounds)
    {
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = maxSize * 2.0f;

        previewRenderUtility.camera.transform.rotation = Quaternion.Euler(cameraRotation);
        previewRenderUtility.camera.transform.position = bounds.center - (previewRenderUtility.camera.transform.forward * distance);
        previewRenderUtility.camera.orthographic = true;
        previewRenderUtility.camera.orthographicSize = (maxSize / 2f) * (1f + padding);
        previewRenderUtility.camera.nearClipPlane = 0.1f;
        previewRenderUtility.camera.farClipPlane = distance * 2f;
    }

    private Texture2D RenderToTexture(int size)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
        previewRenderUtility.camera.targetTexture = renderTexture;
        previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
        previewRenderUtility.camera.backgroundColor = new Color(0, 0, 0, 0);

        previewRenderUtility.camera.Render();

        RenderTexture.active = renderTexture;
        Texture2D resultTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        resultTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        resultTexture.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(renderTexture);
        previewRenderUtility.camera.targetTexture = null;

        return resultTexture;
    }
}