using UnityEngine;

[CreateAssetMenu(fileName = "New Monster Data", menuName = "Game Data/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName = "몬스터";
    [TextArea(2, 4)]
    public string description = "몬스터 설명";
    public Sprite icon;
    public GameObject prefab;

    [Header("기본 스탯")]
    [Tooltip("기본 체력")]
    public int baseHp = 30;
    [Tooltip("기본 공격력")]
    public int baseAtk = 5;
    [Tooltip("공격 간격 (초)")]
    public float attackInterval = 1.2f;

    [Header("스케일링")]
    [Tooltip("웨이브당 체력 증가량")]
    public int hpPerWave = 10;
    [Tooltip("웨이브당 공격력 증가량")]
    public int atkPerWave = 3;
    [Tooltip("최대 스케일링 웨이브 수")]
    public int maxScalingWave = 10;

    [Header("보상")]
    [Tooltip("처치 시 경험치")]
    public int expReward = 15;
    [Tooltip("처치 시 골드")]
    public int goldReward = 10;

    [Header("AI 설정")]
    [Tooltip("공격 우선 타겟 (Front: 전방 우선, Random: 랜덤)")]
    public TargetPriority targetPriority = TargetPriority.Front;

    public enum TargetPriority
    {
        Front,    // 전방 우선
        Random,   // 랜덤
        Weakest,  // 체력이 낮은 적 우선
        Strongest // 체력이 높은 적 우선
    }

    /// <summary>
    /// 웨이브에 따른 스탯 계산
    /// </summary>
    public (int hp, int atk) GetScaledStats(int waveNumber)
    {
        int scalingWave = Mathf.Min(waveNumber, maxScalingWave);
        
        int scaledHp = baseHp + (hpPerWave * scalingWave);
        int scaledAtk = baseAtk + (atkPerWave * scalingWave);
        
        return (scaledHp, scaledAtk);
    }

    /// <summary>
    /// 웨이브에 따른 보상 계산
    /// </summary>
    public (int exp, int gold) GetScaledRewards(int waveNumber)
    {
        float multiplier = 1f + (waveNumber * 0.1f); // 웨이브당 10% 증가
        
        int scaledExp = Mathf.RoundToInt(expReward * multiplier);
        int scaledGold = Mathf.RoundToInt(goldReward * multiplier);
        
        return (scaledExp, scaledGold);
    }
}
