using UnityEngine;
using System;
using System.Collections.Generic;
using PixelClash.Data;

/// <summary>
/// 게임 데이터 (골드, 경험치, 캐릭터 해금 등)를 관리하고 저장하는 매니저
/// 싱글톤 패턴으로 구현되어 있으며 PlayerPrefs를 통해 데이터를 영구 저장합니다.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    #region Constants
    private const string PREF_GOLD = "CurrentGold";
    private const string PREF_EXP = "CurrentExp";
    private const string PREF_LEVEL = "PlayerLevel";
    private const string PREF_HEAL_SKILL = "HealSkillLevel";
    private const string PREF_CHARACTER_UNLOCKED = "Character_{0}_Unlocked";
    private const string PREF_CHARACTER_LEVEL = "Character_{0}_Level";
    #endregion

    #region Serialized Fields
    [Header("게임 데이터")]
    [SerializeField] private int currentGold = 0;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int playerLevel = 1;

    [Header("레벨업 설정")]
    [SerializeField] private int baseExpToLevelUp = 100;
    [SerializeField] private float expGrowthRate = 1.2f;

    [Header("힐 스킬 설정")]
    [SerializeField] private int healSkillLevel = 0;
    [SerializeField] private int healBaseAmount = 50;
    [SerializeField] private int healPerLevel = 10;
    [SerializeField] private int healUpgradeBaseCost = 100;
    [SerializeField] private float healUpgradeCostGrowth = 1.5f;

    [Header("캐릭터 업그레이드 설정")]
    [SerializeField] private int baseUpgradeCost = 50;
    [SerializeField] private int costPerLevel = 25;
    #endregion

    #region Events
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnExpChanged;
    public static event Action<int> OnLevelUp;
    public static event Action<CharacterData> OnCharacterUnlocked;
    public static event Action<CharacterData, int> OnCharacterLevelChanged;
    #endregion

    #region Properties
    public int Gold => currentGold;
    public int CurrentExp => currentExp;
    public int PlayerLevel => playerLevel;
    public int ExpToNextLevel => GetExpForNextLevel() - currentExp;
    public int HealSkillLevel => healSkillLevel;
    #endregion

    #region Runtime Data
    private readonly Dictionary<CharacterData, bool> unlockedCharacters = new();
    private readonly Dictionary<CharacterData, int> characterLevels = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveGameData();
    }

    private void OnDestroy()
    {
        SaveGameData();
    }
    #endregion

    #region Initialization
    /// <summary>싱글톤 초기화</summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadGameData();
            Debug.Log("[GameDataManager] 인스턴스 생성 및 데이터 로드 완료");
        }
        else
        {
            Debug.LogWarning("[GameDataManager] 중복 생성 감지 - 오브젝트 파괴");
            Destroy(gameObject);
        }
    }
    #endregion

    #region Gold Management
    /// <summary>골드 추가</summary>
    /// <param name="amount">추가할 골드 양</param>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[GameDataManager] 잘못된 골드 양: {amount}");
            return;
        }

        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();

        Debug.Log($"[GameDataManager] 골드 획득: +{amount} (총 {currentGold})");
    }

    /// <summary>골드 차감</summary>
    /// <param name="amount">차감할 골드 양</param>
    /// <returns>차감 성공 여부</returns>
    public bool SubtractGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[GameDataManager] 잘못된 골드 양: {amount}");
            return false;
        }

        if (currentGold < amount)
        {
            Debug.LogWarning($"[GameDataManager] 골드 부족: 현재 {currentGold}, 필요 {amount}");
            return false;
        }

        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();

        Debug.Log($"[GameDataManager] 골드 사용: -{amount} (남은 골드: {currentGold})");
        return true;
    }

    /// <summary>골드 직접 설정 (디버그용)</summary>
    /// <param name="amount">설정할 골드 양</param>
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();
        Debug.Log($"[GameDataManager] 골드 설정: {currentGold}");
    }

    /// <summary>골드 보유량 확인</summary>
    /// <param name="requiredAmount">필요한 골드 양</param>
    /// <returns>보유 여부</returns>
    public bool HasEnoughGold(int requiredAmount) => currentGold >= requiredAmount;

    /// <summary>골드를 포맷된 문자열로 반환</summary>
    public string GetFormattedGold()
    {
        return currentGold switch
        {
            >= 1000000 => $"{currentGold / 1000000f:F1}M",
            >= 1000 => $"{currentGold / 1000f:F1}K",
            _ => currentGold.ToString()
        };
    }
    #endregion

    #region Experience Management
    /// <summary>경험치 추가</summary>
    /// <param name="amount">추가할 경험치 양</param>
    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[GameDataManager] 잘못된 경험치 양: {amount}");
            return;
        }

        currentExp += amount;
        OnExpChanged?.Invoke(currentExp);

        Debug.Log($"[GameDataManager] 경험치 획득: +{amount} (총 {currentExp})");

        CheckLevelUp();
        SaveGameData();
    }

    /// <summary>레벨업 체크 및 처리</summary>
    private void CheckLevelUp()
    {
        int expForNextLevel = GetExpForNextLevel();
        
        while (currentExp >= expForNextLevel)
        {
            currentExp -= expForNextLevel;
            playerLevel++;
            
            OnLevelUp?.Invoke(playerLevel);
            OnExpChanged?.Invoke(currentExp);
            Debug.Log($"[GameDataManager] 레벨업! 현재 레벨: {playerLevel}, 남은 경험치: {currentExp}");
            
            expForNextLevel = GetExpForNextLevel();
        }
    }

    /// <summary>현재 레벨에서 다음 레벨로 올라가는 데 필요한 경험치 계산</summary>
    private int GetExpForNextLevel()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expGrowthRate, playerLevel - 1));
    }

    /// <summary>경험치 진행률 (0~1)</summary>
    public float GetExpProgress()
    {
        int expForNextLevel = GetExpForNextLevel();
        return expForNextLevel <= 0 ? 1f : (float)currentExp / expForNextLevel;
    }
    #endregion

    #region Character Management
    /// <summary>캐릭터 해금</summary>
    /// <param name="characterData">해금할 캐릭터</param>
    public void UnlockCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[GameDataManager] null 캐릭터 해금 시도");
            return;
        }

        if (IsCharacterUnlocked(characterData))
        {
            Debug.LogWarning($"[GameDataManager] 이미 해금된 캐릭터: {characterData.displayName}");
            return;
        }

        unlockedCharacters[characterData] = true;
        characterLevels[characterData] = 1; // 기본 레벨 1로 설정
        
        PlayerPrefs.SetInt(string.Format(PREF_CHARACTER_UNLOCKED, characterData.name), 1);
        PlayerPrefs.SetInt(string.Format(PREF_CHARACTER_LEVEL, characterData.name), 1);
        PlayerPrefs.Save();

        OnCharacterUnlocked?.Invoke(characterData);
        Debug.Log($"[GameDataManager] 캐릭터 해금: {characterData.displayName}");
    }

    /// <summary>캐릭터 해금 여부 확인</summary>
    /// <param name="characterData">확인할 캐릭터</param>
    /// <returns>해금 여부</returns>
    public bool IsCharacterUnlocked(CharacterData characterData)
    {
        if (characterData == null) return false;
        return unlockedCharacters.TryGetValue(characterData, out bool unlocked) && unlocked;
    }

    /// <summary>캐릭터 레벨 설정</summary>
    /// <param name="characterData">대상 캐릭터</param>
    /// <param name="level">설정할 레벨</param>
    public void SetCharacterLevel(CharacterData characterData, int level)
    {
        if (characterData == null || level < 1)
        {
            Debug.LogWarning($"[GameDataManager] 잘못된 캐릭터 레벨 설정: {characterData?.displayName}, level: {level}");
            return;
        }

        characterLevels[characterData] = level;
        PlayerPrefs.SetInt(string.Format(PREF_CHARACTER_LEVEL, characterData.name), level);
        PlayerPrefs.Save();

        OnCharacterLevelChanged?.Invoke(characterData, level);
        Debug.Log($"[GameDataManager] 캐릭터 레벨 설정: {characterData.displayName} → 레벨 {level}");
    }

    /// <summary>캐릭터 레벨 가져오기</summary>
    /// <param name="characterData">대상 캐릭터</param>
    /// <returns>캐릭터 레벨</returns>
    public int GetCharacterLevel(CharacterData characterData)
    {
        if (characterData == null) return 1;
        return characterLevels.TryGetValue(characterData, out int level) ? level : 1;
    }

    /// <summary>캐릭터 업그레이드 비용 계산</summary>
    /// <param name="characterData">대상 캐릭터</param>
    /// <returns>업그레이드 비용</returns>
    public int GetCharacterUpgradeCost(CharacterData characterData)
    {
        int currentLevel = GetCharacterLevel(characterData);
        return baseUpgradeCost + (currentLevel * costPerLevel);
    }

    /// <summary>캐릭터 업그레이드 시도</summary>
    /// <param name="characterData">업그레이드할 캐릭터</param>
    /// <returns>업그레이드 성공 여부</returns>
    public bool TryUpgradeCharacter(CharacterData characterData)
    {
        if (characterData == null || !IsCharacterUnlocked(characterData))
        {
            Debug.LogWarning($"[GameDataManager] 업그레이드 불가능한 캐릭터: {characterData?.displayName}");
            return false;
        }

        int cost = GetCharacterUpgradeCost(characterData);
        if (!SubtractGold(cost)) return false;

        int newLevel = GetCharacterLevel(characterData) + 1;
        SetCharacterLevel(characterData, newLevel);
        
        Debug.Log($"[GameDataManager] 캐릭터 업그레이드 성공: {characterData.displayName} → 레벨 {newLevel}");
        return true;
    }
    #endregion
    #region Heal Skill Management
    /// <summary>현재 힐량 계산</summary>
    /// <returns>기본 힐량 + (레벨 × 레벨당 힐량)</returns>
    public int GetCurrentHealAmount()
    {
        return Mathf.Max(0, healBaseAmount + healSkillLevel * healPerLevel);
    }

    /// <summary>힐 스킬 업그레이드 비용 계산</summary>
    /// <returns>다음 레벨업에 필요한 골드</returns>
    public int GetHealSkillUpgradeCost()
    {
        double cost = healUpgradeBaseCost * System.Math.Pow(healUpgradeCostGrowth, healSkillLevel);
        return Mathf.Max(1, Mathf.RoundToInt((float)cost));
    }

    /// <summary>힐 스킬 업그레이드 시도</summary>
    /// <returns>업그레이드 성공 여부</returns>
    public bool TryUpgradeHealSkill()
    {
        int cost = GetHealSkillUpgradeCost();
        if (!SubtractGold(cost)) return false;

        healSkillLevel++;
        PlayerPrefs.SetInt(PREF_HEAL_SKILL, healSkillLevel);
        PlayerPrefs.Save();

        Debug.Log($"[GameDataManager] 힐 스킬 업그레이드! 레벨: {healSkillLevel}, 현재 힐량: {GetCurrentHealAmount()}");
        return true;
    }
    #endregion

    #region Data Persistence
    /// <summary>게임 데이터 저장</summary>
    public void SaveGameData()
    {
        PlayerPrefs.SetInt(PREF_GOLD, currentGold);
        PlayerPrefs.SetInt(PREF_EXP, currentExp);
        PlayerPrefs.SetInt(PREF_LEVEL, playerLevel);
        PlayerPrefs.SetInt(PREF_HEAL_SKILL, healSkillLevel);
        PlayerPrefs.Save();

        Debug.Log($"[GameDataManager] 데이터 저장 완료 - 골드: {currentGold}, 레벨: {playerLevel}");
    }

    /// <summary>게임 데이터 로드</summary>
    public void LoadGameData()
    {
        currentGold = PlayerPrefs.GetInt(PREF_GOLD, 0);
        currentExp = PlayerPrefs.GetInt(PREF_EXP, 0);
        playerLevel = PlayerPrefs.GetInt(PREF_LEVEL, 1);
        healSkillLevel = PlayerPrefs.GetInt(PREF_HEAL_SKILL, 0);

        LoadCharacterData();

        // 이벤트 발생
        OnGoldChanged?.Invoke(currentGold);
        OnExpChanged?.Invoke(currentExp);

        Debug.Log($"[GameDataManager] 데이터 로드 완료 - 골드: {currentGold}, 레벨: {playerLevel}");
    }

    /// <summary>캐릭터 데이터 로드</summary>
    private void LoadCharacterData()
    {
        // 이 메서드는 게임 시작 시 사용 가능한 모든 캐릭터 데이터를 로드해야 합니다
        // 현재는 런타임에서 캐릭터별로 개별 로드하는 방식을 사용
        unlockedCharacters.Clear();
        characterLevels.Clear();
        
        Debug.Log("[GameDataManager] 캐릭터 데이터 로드 준비 완료");
    }

    /// <summary>특정 캐릭터 데이터 로드</summary>
    /// <param name="characterData">로드할 캐릭터</param>
    public void LoadCharacterData(CharacterData characterData)
    {
        if (characterData == null) return;

        bool isUnlocked = PlayerPrefs.GetInt(string.Format(PREF_CHARACTER_UNLOCKED, characterData.name), 0) == 1;
        int level = PlayerPrefs.GetInt(string.Format(PREF_CHARACTER_LEVEL, characterData.name), 1);

        unlockedCharacters[characterData] = isUnlocked;
        characterLevels[characterData] = level;
    }

    /// <summary>게임 데이터 리셋</summary>
    public void ResetGameData()
    {
        currentGold = 0;
        currentExp = 0;
        playerLevel = 1;
        healSkillLevel = 0;

        unlockedCharacters.Clear();
        characterLevels.Clear();

        PlayerPrefs.DeleteKey(PREF_GOLD);
        PlayerPrefs.DeleteKey(PREF_EXP);
        PlayerPrefs.DeleteKey(PREF_LEVEL);
        PlayerPrefs.DeleteKey(PREF_HEAL_SKILL);

        // 모든 캐릭터 데이터 삭제는 현재 구현에서는 개별적으로 처리
        PlayerPrefs.Save();

        OnGoldChanged?.Invoke(currentGold);
        OnExpChanged?.Invoke(currentExp);
        OnLevelUp?.Invoke(playerLevel);

        Debug.Log("[GameDataManager] 게임 데이터 리셋 완료");
    }
    #endregion

    #region Debug & Testing
    /// <summary>테스트용: 캐릭터 구매 취소 및 골드 환불</summary>
    /// <param name="refundAmount">환불할 골드 양</param>
    public void RefundCharacterPurchase(int refundAmount)
    {
        AddGold(refundAmount);
        Debug.Log($"[GameDataManager] 테스트: 캐릭터 구매 취소 - 골드 환불: +{refundAmount}");
    }

    /// <summary>개발자 콘솔용: 모든 캐릭터 해금</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUnlockAllCharacters()
    {
        Debug.Log("[GameDataManager] 개발자 모드: 모든 캐릭터 해금 기능은 개별 캐릭터 데이터가 필요합니다");
    }

    /// <summary>개발자 콘솔용: 골드 대량 지급</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAddGold(int amount = 10000)
    {
        AddGold(amount);
        Debug.Log($"[GameDataManager] 개발자 모드: 골드 {amount} 지급");
    }
    #endregion
}
