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

    [Header("Skill Unlocks")]
    public List<SkillUnlock> skills; // (레벨, 스킬) 쌍
}

[Serializable]
public struct SkillUnlock
{
    public int requiredLevel;
    public SkillData skill;
}