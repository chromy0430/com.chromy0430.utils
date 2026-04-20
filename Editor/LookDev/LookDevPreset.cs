using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EditorLookDev
{
    [CreateAssetMenu(fileName = "NewLookDevPreset", menuName = "Tools/LookDev/Create Preset Data")]
    public class LookDevPreset : ScriptableObject
    {
        [Header("Directional Light")]
        public Color lightColor = Color.white;
        public Vector3 lightRotation = new Vector3(50f, -30f, 0f); // 영상 1: 그림자 길이를 결정하는 핵심 요소
        public float shadowStrength = 0.6f;
        public LightShadows shadowType = LightShadows.Soft;

        [Header("Global Illumination & Environment")]
        public bool useBakedGI = true;
        public float indirectIntensity = 1.5f;
        [Range(0f, 1f)] public float reflectionIntensity = 1.0f; // 영상 2: Indirect Specular (스카이박스 반사광) 제어

        [Header("Fog (Atmosphere)")] // 영상 2: 공간의 깊이감을 부여하는 포그
        public bool enableFog = false;
        public Color fogColor = new Color(0.5f, 0.6f, 0.7f);
        public float fogDensity = 0.01f;

        [Header("Post Processing - Base")]
        public TonemappingMode tonemappingMode = TonemappingMode.ACES;
        public float saturation = 15f;
        public float contrast = 10f;

        [Header("Post Processing - Mood & Focus")] // 영상 1: 사계절 온도감 및 시선 유도
        [Range(-100f, 100f)] public float colorTemperature = 0f;
        [Range(-100f, 100f)] public float colorTint = 0f;
        [Range(0f, 1f)] public float vignetteIntensity = 0.2f;

        [Header("Post Processing - Bloom")]
        public float bloomIntensity = 0.5f;
        public float bloomThreshold = 1.0f;
    }
}