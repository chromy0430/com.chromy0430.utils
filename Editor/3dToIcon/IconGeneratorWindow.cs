using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IconGeneratorWindow : EditorWindow
{
    // --- 기본 설정값 정의 ---
    private const int DEFAULT_IMAGE_SIZE = 512;
    private static readonly Vector3 DEFAULT_CAMERA_ROTATION = new Vector3(30, -135, 0);
    private static readonly Vector3 DEFAULT_CAMERA_OFFSET = Vector3.zero;
    private const float DEFAULT_PADDING = 0.1f;
    private const string DEFAULT_SAVE_PATH = "Assets/GeneratedIcons";

    private AnimationClip targetAnimation;
    private float animationNormalizedTime = 0f; // 0.0 (시작) ~ 1.0 (끝)
    // 설정 변수
    private GameObject[] selectedPrefabs;
    private int imageSize = DEFAULT_IMAGE_SIZE;
    private Vector3 cameraRotation = DEFAULT_CAMERA_ROTATION;
    private Vector3 cameraPositionOffset = DEFAULT_CAMERA_OFFSET;
    private float padding = DEFAULT_PADDING;
    private string savePath = DEFAULT_SAVE_PATH;

    // 내부 관리 변수
    private PreviewRenderUtility previewRenderUtility;
    private Texture2D previewTexture;
    private GameObject currentPrefab;
    // OnSelectionChange에서 매번 List를 생성하는 것을 방지하여 GC를 줄입니다.
    private List<GameObject> prefabListCache = new List<GameObject>();

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

    /// <summary>
    /// 모든 설정을 기본값으로 되돌립니다.
    /// </summary>
    private void ResetSettings()
    {
        imageSize = DEFAULT_IMAGE_SIZE;
        cameraRotation = DEFAULT_CAMERA_ROTATION;
        cameraPositionOffset = DEFAULT_CAMERA_OFFSET;
        padding = DEFAULT_PADDING;
        savePath = DEFAULT_SAVE_PATH;

        targetAnimation = null;
        animationNormalizedTime = 0f;

        // 변경된 설정으로 미리보기를 업데이트합니다.
        UpdatePreview();
        // 키보드 포커스를 해제하여 UI가 깔끔하게 보이도록 합니다.
        GUI.FocusControl(null);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1. Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        imageSize = EditorGUILayout.IntPopup("Icon Size", imageSize, new string[] { "128", "256", "512", "1024" }, new int[] { 128, 256, 512, 1024 });
        cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);        
        cameraPositionOffset = EditorGUILayout.Vector3Field("Camera Position Offset", cameraPositionOffset);
        
        padding = EditorGUILayout.Slider("Padding", padding, -1f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Settings (Optional)", EditorStyles.boldLabel);

        // 애니메이션 클립 선택 필드
        targetAnimation = (AnimationClip)EditorGUILayout.ObjectField("Target Animation", targetAnimation, typeof(AnimationClip), false);

        // targetAnimation이 할당된 경우에만 슬라이더 활성화
        GUI.enabled = targetAnimation != null;
        animationNormalizedTime = EditorGUILayout.Slider("Animation Time", animationNormalizedTime, 0f, 1f);
        GUI.enabled = true; // GUI.enabled 상태 원복

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
        // List<GameObject> prefabs = new List<GameObject>(); // 기존: 매번 리스트 생성 (GC 발생)
        prefabListCache.Clear(); // 수정: 캐시된 리스트 초기화

        foreach (var obj in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
        {
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                // prefabs.Add(obj); // 기존 코드
                prefabListCache.Add(obj); // 수정: 캐시된 리스트에 추가
            }
        }
        // selectedPrefabs = prefabs.ToArray(); // 기존 코드
        selectedPrefabs = prefabListCache.ToArray(); // 수정: 리스트를 배열로 변환 (ToArray()는 GC 발생하나, List 생성보다는 효율적)

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
            ApplyAnimationPose(instance);
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
            ApplyAnimationPose(instance);
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

        Vector3 basePosition = bounds.center - (previewRenderUtility.camera.transform.forward * distance);

        previewRenderUtility.camera.transform.position = basePosition + cameraPositionOffset;

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

    /// <summary>
    /// 인스턴스에 선택된 애니메이션의 특정 포즈를 적용합니다.
    /// </summary>
    /// <param name="instance">포즈를 적용할 게임오브젝트 인스턴스</param>
    private void ApplyAnimationPose(GameObject instance)
    {
        // 선택된 애니메이션 클립이 없으면 아무것도 하지 않음
        if (targetAnimation == null || instance == null)
        {
            return;
        }

        // 애니메이션 클립의 총 길이 (0으로 나눠지는 것을 방지)
        float clipLength = targetAnimation.length;
        if (clipLength <= 0f)
        {
            // 클립 길이가 0이면 (예: 단일 포즈 클립) 0초 지점을 샘플링
            clipLength = 1f;
            animationNormalizedTime = 0f;
        }

        // 샘플링할 정확한 시간 계산 (예: 0.5f -> 클립의 절반 시간)
        float sampleTime = clipLength * animationNormalizedTime;

        // 'SampleAnimation'은 Animator나 Animation 컴포넌트 유무와 관계없이
        // GameObject의 트랜스폼 계층 구조에 직접 애니메이션 데이터를 샘플링합니다.
        try
        {
            // 이 시점에서 인스턴스의 트랜스폼이 지정된 시간의 포즈로 변경됩니다.
            targetAnimation.SampleAnimation(instance, sampleTime);
        }
        catch (System.Exception e)
        {
            // (예외 처리) 레거시 클립인데 'Animation' 컴포넌트가 없는 경우 등
            Debug.LogWarning($"애니메이션 포즈 적용 중 오류 발생 ('{instance.name}'): {e.Message}. 클립이 'Legacy'로 설정되어 있다면 'Animation' 컴포넌트가 필요할 수 있습니다.");
        }
    }
}