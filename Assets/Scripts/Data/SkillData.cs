// Assets/Scripts/Data/SkillData.cs
using System;
using UnityEngine;

namespace PixelClash.Data
{
    /// <summary>
    /// 스킬 정의용 ScriptableObject.
    /// - 수치(쿨타임, 배수), 메타(이름, 설명, 아이콘), unlock 레벨 등만 보유
    /// - 실제 효과는 SkillBehaviour에서 처리 (Strategy 패턴 권장)
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Skill Data", order = 52)]
    public class SkillData : ScriptableObject
    {
        [Header("Meta")]
        public string id;                 // 내부 키 (ex: "fireball_lv1")
        public string displayName;        // UI 노출 이름
        [TextArea] public string description;
        public Sprite icon;

        [Header("Skill Type")]
        public SkillType type = SkillType.Active;
        public TargetType target = TargetType.SingleEnemy;

        [Header("Numeric Values")]
        [Tooltip("쿨타임(초). Passive일 경우 0으로 남겨 둔다.")]
        public float cooldown = 5f;

        [Tooltip("스킬 계수(데미지 or 회복량 배수). 1 = 100% 기본공격과 동일")]
        public float powerMultiplier = 1.5f;

        [Tooltip("에너지·궁극기 게이지 필요치 등 특수 비용 (필요 없다면 0)")]
        public float cost = 0f;

        [Header("Unlock & Progression")]
        [Tooltip("기본 스킬이면 0, 특정 레벨에서 해금된다면 그 레벨 입력")]
        public int requiredLevel = 1;

        /// <summary>
        /// 향후 업그레이드용: 동일 스킬의 다음 레벨 데이터 참조
        /// </summary>
        public SkillData nextLevel;
    }

    // -------------------- ENUMS --------------------
    public enum SkillType
    {
        Passive,    // 항상 발동되는 버프/효과
        Active,     // 쿨타임 기반
        Ultimate    // 게이지 기반, 강력
    }

    public enum TargetType
    {
        Self,
        Ally,
        AllAllies,
        SingleEnemy,
        AllEnemies,
        Area          // 범위 (중앙 좌표 필요)
    }
}