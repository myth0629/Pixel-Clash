using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Soldier 캐릭터의 위치별 애니메이션을 설정하는 에디터 스크립트
/// </summary>
public class SoldierAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Animation/Setup Soldier Position Animations")]
    public static void ShowWindow()
    {
        GetWindow<SoldierAnimationSetup>("Soldier Animation Setup");
    }

    private AnimatorController soldierController;

    private void OnGUI()
    {
        GUILayout.Label("Soldier 위치별 애니메이션 설정", EditorStyles.boldLabel);
        
        soldierController = (AnimatorController)EditorGUILayout.ObjectField(
            "Soldier Animator Controller", 
            soldierController, 
            typeof(AnimatorController), 
            false
        );

        EditorGUILayout.Space();

        if (soldierController == null)
        {
            EditorGUILayout.HelpBox(
                "Assets/Assets/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/Characters(100x100)/Player/Soldier/Soldier/Soldier_new.controller 파일을 선택하세요.",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.HelpBox(
            "다음 파라미터와 상태들이 필요합니다:\n\n" +
            "Parameters:\n" +
            "• IsInBackRow (Bool) - 후방 배치 여부\n" +
            "• FrontRowAttack (Trigger) - 전방 공격\n" +
            "• BackRowAttack (Trigger) - 후방 공격\n\n" +
            "States:\n" +
            "• idle (기본) - 공용 대기 상태\n" +
            "• FrontRowAttack - 전방 공격 (근접)\n" +
            "• BackRowAttack - 후방 공격 (원거리)",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("애니메이터 파라미터 자동 추가"))
        {
            AddParametersToAnimator();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "상태(States)와 전환(Transitions)은 Unity Animator 창에서 수동으로 설정해야 합니다.\n\n" +
            "권장 구조:\n" +
            "1. idle (기본 상태) - 공용 대기 애니메이션\n" +
            "2. idle → FrontRowAttack (FrontRowAttack 트리거 + IsInBackRow = false)\n" +
            "3. idle → BackRowAttack (BackRowAttack 트리거 + IsInBackRow = true)\n" +
            "4. 공격 상태들 → idle 상태로 복귀",
            MessageType.Warning
        );
    }

    private void AddParametersToAnimator()
    {
        if (soldierController == null)
        {
            Debug.LogError("Animator Controller가 선택되지 않았습니다!");
            return;
        }

        // 필요한 파라미터들
        string[] boolParameters = { "IsInBackRow" };
        string[] triggerParameters = { "FrontRowAttack", "BackRowAttack" };

        bool modified = false;

        // Bool 파라미터 추가
        foreach (string paramName in boolParameters)
        {
            if (!HasParameter(soldierController, paramName))
            {
                soldierController.AddParameter(paramName, AnimatorControllerParameterType.Bool);
                Debug.Log($"Bool 파라미터 추가: {paramName}");
                modified = true;
            }
        }

        // Trigger 파라미터 추가
        foreach (string paramName in triggerParameters)
        {
            if (!HasParameter(soldierController, paramName))
            {
                soldierController.AddParameter(paramName, AnimatorControllerParameterType.Trigger);
                Debug.Log($"Trigger 파라미터 추가: {paramName}");
                modified = true;
            }
        }

        if (modified)
        {
            EditorUtility.SetDirty(soldierController);
            AssetDatabase.SaveAssets();
            Debug.Log("Soldier Animator Controller에 파라미터들이 추가되었습니다!");
        }
        else
        {
            Debug.Log("모든 파라미터가 이미 존재합니다.");
        }
    }

    private bool HasParameter(AnimatorController controller, string parameterName)
    {
        foreach (var param in controller.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }
}
