using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AtmosphereWindow : EditorWindow
{
    private Light directionalLight;
    private float timeOfDay = 12f;

    // 시간 변화에 따른 자동 보간 데이터 (무대 조명 큐 데이터에 해당)
    private Gradient sunColorGradient = new Gradient();
    private AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0f, 24f, 1.5f);
    private Gradient ambientColorGradient = new Gradient();

    [MenuItem("Tools/Atmosphere")]
    public static void ShowWindow()
    {
        GetWindow<AtmosphereWindow>("Atmosphere");
    }

    private void OnGUI()
    {
        GUILayout.Label("Atmosphere Controller", EditorStyles.boldLabel);

        directionalLight = (Light)EditorGUILayout.ObjectField("Sun (Directional Light)", directionalLight, typeof(Light), true);

        if (directionalLight == null)
        {
            EditorGUILayout.HelpBox("제어할 Directional Light를 할당하십시오.", MessageType.Error);
            return;
        }

        // 마스터 페이더 감지 시작
        EditorGUI.BeginChangeCheck();

        GUILayout.Space(10);
        timeOfDay = EditorGUILayout.Slider("Time Of Day", timeOfDay, 0f, 24f);

        GUILayout.Space(10);
        GUILayout.Label("Automated Lighting Data", EditorStyles.boldLabel);
        sunColorGradient = EditorGUILayout.GradientField("Sun Color Gradient", sunColorGradient);
        sunIntensityCurve = EditorGUILayout.CurveField("Sun Intensity Curve", sunIntensityCurve);
        ambientColorGradient = EditorGUILayout.GradientField("Ambient Color Gradient", ambientColorGradient);

        // 변경 사항이 발생한 즉시 로직 실행
        if (EditorGUI.EndChangeCheck())
        {
            ApplyAtmosphere();
        }
    }

    private void ApplyAtmosphere()
    {
        // 0.0 ~ 1.0으로 정규화하여 Gradient 처리에 사용
        float timePercent = timeOfDay / 24f;

        // 1. 태양(무빙 라이트) 모터 회전
        // 0시: -90도(아래), 12시: 90도(위), 24시: 270도(아래)
        float sunAngle = Mathf.Lerp(-90f, 270f, timePercent);

        // Undo 시스템 등록 (Ctrl+Z 지원)
        Undo.RecordObject(directionalLight.transform, "Atmosphere Transform");
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0f);

        // 2. 태양 색상 및 강도(셀로판지 및 전력 조절)
        Undo.RecordObject(directionalLight, "Atmosphere Light");
        directionalLight.color = sunColorGradient.Evaluate(timePercent);
        directionalLight.intensity = sunIntensityCurve.Evaluate(timeOfDay);

        // 3. URP 환경광 강제 플랫 컬러 모드 적용 및 갱신
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColorGradient.Evaluate(timePercent);

        // 연산 결과를 즉시 화면에 렌더링
        SceneView.RepaintAll();
    }
}