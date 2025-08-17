// Assets/Scripts/Data/CharacterData.cs
using System;
using System.Collections.Generic;
using PixelClash.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Meta")]
    public string id;                // 내부용 키
    public string displayName;       // 화면 노출 이름
    public Sprite icon;              // 도트 초상화
    public GameObject prefab;

    [Header("Base Stats")]
    public int baseHp;               // Lv1 체력
    [Range(0f, 1f)] public float hpGrowth;   // 레벨당 성장률
    public int baseAtk;              // Lv1 공격력
    [Range(0f, 1f)] public float atkGrowth;  // 레벨당 성장률

    [Header("Economy")]
    public int unlockCost;           // 골드 해금 비용

    [Header("Battle Placement")]
    [Tooltip("이 캐릭터가 기본적으로 배치될 수 있는 위치")]
    public PositionType position = PositionType.Front;
    
    [Tooltip("스킬 해금으로 추가로 배치 가능해지는 위치들")]
    public List<PositionUnlock> positionUnlocks = new List<PositionUnlock>();

    [Header("Skill Unlocks")]
    public List<SkillUnlock> skills; // (레벨, 스킬) 쌍
}

public enum PositionType
{
    Front,
    Back
}

[Serializable]
public struct SkillUnlock
{
    public int requiredLevel;
    public SkillData skill;
}

[Serializable]
public struct PositionUnlock
{
    public int requiredLevel;
    public PositionType unlockedPosition;
    [Tooltip("위치 해금과 함께 표시할 메시지")]
    public string unlockMessage;
}