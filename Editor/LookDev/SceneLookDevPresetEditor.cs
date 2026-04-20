using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EditorLookDev
{
    public class LookDevPresetManagerWindow : EditorWindow
    {
        private List<LookDevPreset> _cachedPresets = new List<LookDevPreset>();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Environment/LookDev Preset Manager")]
        public static void ShowWindow()
        {
            GetWindow<LookDevPresetManagerWindow>("LookDev Presets");
        }

        private void OnEnable()
        {
            RefreshPresetList();
        }

        private void OnGUI()
        {
            GUILayout.Label("저장된 룩뎁(LookDev) 프리셋 목록", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("현재 씬 설정을 프리셋으로 저장", GUILayout.Height(40)))
            {
                SaveCurrentSceneAsPreset();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("새 프리셋 파일 생성하기 (기본값)", GUILayout.Height(25)))
            {
                CreateNewPresetAsset();
            }

            if (GUILayout.Button("목록 새로고침", GUILayout.Height(20)))
            {
                RefreshPresetList();
            }

            EditorGUILayout.Space();

            if (_cachedPresets.Count == 0)
            {
                EditorGUILayout.HelpBox("저장된 프리셋이 없습니다.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var preset in _cachedPresets)
            {
                if (preset == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button(preset.name, EditorStyles.label, GUILayout.Width(200)))
                {
                    EditorGUIUtility.PingObject(preset);
                }

                if (GUILayout.Button("씬에 적용", GUILayout.Height(25)))
                {
                    ApplyPresetToScene(preset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshPresetList()
        {
            _cachedPresets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LookDevPreset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LookDevPreset preset = AssetDatabase.LoadAssetAtPath<LookDevPreset>(path);
                if (preset != null)
                {
                    _cachedPresets.Add(preset);
                }
            }
        }

        private void CreateNewPresetAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            LookDevPreset newPreset = ScriptableObject.CreateInstance<LookDevPreset>();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Editor/NewLookDevPreset.asset");

            AssetDatabase.CreateAsset(newPreset, uniquePath);
            AssetDatabase.SaveAssets();

            RefreshPresetList();
            Selection.activeObject = newPreset;
        }

        private void SaveCurrentSceneAsPreset()
        {
            LookDevPreset newPreset = ScriptableObject.CreateInstance<LookDevPreset>();

            // 1. Directional Light 데이터 추출
            Light dirLight = FindAnyObjectByType<Light>(FindObjectsInactive.Exclude);
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                newPreset.lightColor = dirLight.color;
                newPreset.lightRotation = dirLight.transform.rotation.eulerAngles;
                newPreset.shadowStrength = dirLight.shadowStrength;
                newPreset.shadowType = dirLight.shadows;
            }

            // 2. GI & Environment 데이터 추출
            newPreset.useBakedGI = Lightmapping.bakedGI;
            newPreset.indirectIntensity = RenderSettings.ambientIntensity;
            newPreset.reflectionIntensity = RenderSettings.reflectionIntensity;
            newPreset.enableFog = RenderSettings.fog;
            newPreset.fogColor = RenderSettings.fogColor;
            newPreset.fogDensity = RenderSettings.fogDensity;

            // 3. Post Processing (Volume) 데이터 추출
            Volume globalVolume = FindAnyObjectByType<Volume>(FindObjectsInactive.Exclude);
            if (globalVolume != null && globalVolume.profile != null)
            {
                var profile = globalVolume.profile;
                if (profile.TryGet(out Tonemapping tone)) newPreset.tonemappingMode = tone.mode.value;
                if (profile.TryGet(out ColorAdjustments color))
                {
                    newPreset.saturation = color.saturation.value;
                    newPreset.contrast = color.contrast.value;
                }
                if (profile.TryGet(out Bloom bloom))
                {
                    newPreset.bloomIntensity = bloom.intensity.value;
                    newPreset.bloomThreshold = bloom.threshold.value;
                }
                if (profile.TryGet(out WhiteBalance wb))
                {
                    newPreset.colorTemperature = wb.temperature.value;
                    newPreset.colorTint = wb.tint.value;
                }
                if (profile.TryGet(out Vignette vig)) newPreset.vignetteIntensity = vig.intensity.value;
            }

            // 에셋 파일로 저장
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Editor/CapturedLookDevPreset.asset");
            AssetDatabase.CreateAsset(newPreset, uniquePath);
            AssetDatabase.SaveAssets();

            RefreshPresetList();
            EditorGUIUtility.PingObject(newPreset);
            Debug.Log($"[LookDev] 현재 씬 설정을 '{uniquePath}' 프리셋으로 저장했습니다.");
        }

        private void ApplyPresetToScene(LookDevPreset preset)
        {
            ApplyDirectionalLight(preset);
            ApplyGI(preset);
            ApplyPostProcessing(preset);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[LookDev] '{preset.name}' 프리셋 적용 완료.");
        }

        private void ApplyDirectionalLight(LookDevPreset preset)
        {
            Light dirLight = FindAnyObjectByType<Light>(FindObjectsInactive.Exclude);
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(preset.lightRotation);
            dirLight.color = preset.lightColor;
            dirLight.shadows = preset.shadowType;
            dirLight.shadowStrength = preset.shadowStrength;
        }

        private void ApplyGI(LookDevPreset preset)
        {
            Lightmapping.bakedGI = preset.useBakedGI;
            Lightmapping.realtimeGI = !preset.useBakedGI;
            RenderSettings.ambientIntensity = preset.indirectIntensity;
            RenderSettings.reflectionIntensity = preset.reflectionIntensity;
            RenderSettings.fog = preset.enableFog;
            if (preset.enableFog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = preset.fogColor;
                RenderSettings.fogDensity = preset.fogDensity;
            }
        }

        private void ApplyPostProcessing(LookDevPreset preset)
        {
            Volume globalVolume = FindAnyObjectByType<Volume>(FindObjectsInactive.Exclude);
            if (globalVolume == null)
            {
                GameObject volumeObj = new GameObject("Global PostProcessing Volume");
                globalVolume = volumeObj.AddComponent<Volume>();
                globalVolume.isGlobal = true;
            }
            VolumeProfile profile = globalVolume.HasInstantiatedProfile() ? globalVolume.profile : ScriptableObject.CreateInstance<VolumeProfile>();
            globalVolume.profile = profile;

            if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(preset.tonemappingMode);

            if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>();
            colorAdjustments.saturation.Override(preset.saturation);
            colorAdjustments.contrast.Override(preset.contrast);

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>();
            bloom.intensity.Override(preset.bloomIntensity);
            bloom.threshold.Override(preset.bloomThreshold);

            if (!profile.TryGet(out WhiteBalance whiteBalance)) whiteBalance = profile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(preset.colorTemperature);
            whiteBalance.tint.Override(preset.colorTint);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>();
            vignette.intensity.Override(preset.vignetteIntensity);
        }
    }
}