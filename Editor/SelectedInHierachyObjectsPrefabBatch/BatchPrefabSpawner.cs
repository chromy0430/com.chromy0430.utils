using UnityEngine;
using UnityEditor;

public class BatchPrefabSpawner : EditorWindow
{
    private GameObject _targetPrefab;
    private bool _resetTransform = true;

    [MenuItem("Tools/Batch Prefab Spawner")]
    public static void ShowWindow()
    {
        GetWindow<BatchPrefabSpawner>("Batch Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Spawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 프리팹 선택 필드
        _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab",
            _targetPrefab,
            typeof(GameObject),
            false // false: 프로젝트 뷰의 에셋만 허용 (씬 오브젝트 제외)
        );

        // 2. 옵션: 트랜스폼 리셋 여부
        _resetTransform = EditorGUILayout.Toggle("Reset Local Transform", _resetTransform);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Currently Selected Objects: {Selection.gameObjects.Length}", MessageType.Info);

        // 3. 실행 버튼
        // 유효성 검사: 프리팹이 없거나 선택된 오브젝트가 없으면 버튼 비활성화
        bool isValid = _targetPrefab != null && Selection.gameObjects.Length > 0;

        GUI.enabled = isValid;

        if (GUILayout.Button("Spawn as Child to Selected"))
        {
            SpawnPrefabs();
        }

        GUI.enabled = true;
    }

    private void SpawnPrefabs()
    {
        // 선택된 게임 오브젝트 배열 캐싱 (GC 방지: 매번 접근하지 않음)
        GameObject[] selectedParents = Selection.gameObjects;
        int count = selectedParents.Length;

        // 대량 작업 시 Undo 그룹 생성 (한 번의 Ctrl+Z로 모두 되돌리기 위함)
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < count; i++)
        {
            GameObject parent = selectedParents[i];

            // 유효성 검사: 부모가 null이 아닌지 확인
            if (parent == null) continue;

            // 핵심: 프리팹 링크를 유지하며 인스턴스화
            GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(_targetPrefab);

            if (newChild != null)
            {
                // Undo 시스템에 등록 (생성된 오브젝트 추적)
                Undo.RegisterCreatedObjectUndo(newChild, "Batch Spawn Prefab");

                // 부모 설정 (Transform 변경 시 Undo 기록을 위해 Transform 부모 설정보다 Undo 지원 메서드 권장되나, 
                // 생성 직후 부모 설정은 아래와 같이 처리해도 무방함. 엄격하게는 Undo.SetTransformParent 사용 가능)
                newChild.transform.SetParent(parent.transform);

                // 옵션: 로컬 트랜스폼 초기화
                if (_resetTransform)
                {
                    newChild.transform.localPosition = Vector3.zero;
                    newChild.transform.localRotation = Quaternion.identity;
                    newChild.transform.localScale = Vector3.one;
                }
            }
        }

        // Undo 그룹 닫기
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[BatchSpawner] Successfully spawned '{_targetPrefab.name}' under {count} objects.");
    }
}
