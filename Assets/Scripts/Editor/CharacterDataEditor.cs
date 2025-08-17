using UnityEngine;
using UnityEditor;

/// <summary>
/// 캐릭터 데이터 설정을 위한 에디터 스크립트
/// </summary>
public class CharacterDataEditor : EditorWindow
{
    [MenuItem("Tools/Character Data/Setup Soldier Position Unlock")]
    public static void SetupSoldierPositionUnlock()
    {
        // Soldier 캐릭터 데이터 로드
        CharacterData soldierData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/CharacterData/Soldier.asset");
        
        if (soldierData == null)
        {
            Debug.LogError("Soldier.asset을 찾을 수 없습니다!");
            return;
        }
        
        // 기존 위치 해금 설정 확인
        if (soldierData.positionUnlocks == null)
        {
            soldierData.positionUnlocks = new System.Collections.Generic.List<PositionUnlock>();
        }
        
        // 후방 배치 해금이 이미 있는지 확인
        bool hasBackUnlock = false;
        foreach (var unlock in soldierData.positionUnlocks)
        {
            if (unlock.unlockedPosition == PositionType.Back)
            {
                hasBackUnlock = true;
                break;
            }
        }
        
        // 후방 배치 해금 추가
        if (!hasBackUnlock)
        {
            PositionUnlock backPositionUnlock = new PositionUnlock
            {
                requiredLevel = 3,
                unlockedPosition = PositionType.Back,
                unlockMessage = "병사가 후방 지원 전술을 익혔습니다! 이제 후방에서도 전투할 수 있습니다."
            };
            
            soldierData.positionUnlocks.Add(backPositionUnlock);
            
            // 변경사항 저장
            EditorUtility.SetDirty(soldierData);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Soldier 캐릭터에 후방 배치 해금이 추가되었습니다! (레벨 3에서 해금)");
        }
        else
        {
            Debug.Log("Soldier 캐릭터에 이미 후방 배치 해금이 설정되어 있습니다.");
        }
    }
}
