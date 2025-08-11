using UnityEngine;
using System;

/// <summary>
/// 게임 데이터 (골드, 경험치 등)를 관리하고 저장하는 매니저
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("게임 데이터")]
    [SerializeField] private int currentGold = 0;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int playerLevel = 1;

    [Header("레벨업 설정")]
    [SerializeField] private int baseExpToLevelUp = 100;
    [SerializeField] private float expGrowthRate = 1.2f;

    [Header("힐 스킬 설정")]
    [SerializeField] private int healSkillLevel = 0;           // 힐 스킬 레벨
    [SerializeField] private int healBaseAmount = 50;          // 기본 힐량
    [SerializeField] private int healPerLevel = 10;            // 레벨당 추가 힐량
    [SerializeField] private int healUpgradeBaseCost = 100;    // 업그레이드 기본 비용
    [SerializeField] private float healUpgradeCostGrowth = 1.5f; // 업그레이드 비용 성장률

    // 이벤트
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnExpChanged;
    public static event Action<int> OnLevelUp;

    // 프로퍼티
    public int CurrentGold => currentGold;
    public int CurrentExp => currentExp;
    public int PlayerLevel => playerLevel;
    public int ExpToNextLevel => GetExpForNextLevel() - currentExp;
    public int HealSkillLevel => healSkillLevel;

    private void Awake()
    {
        // 단일 씬 게임이므로 DontDestroyOnLoad 제거
        if (Instance == null)
        {
            Instance = this;
            LoadGameData();
            Debug.Log("GameDataManager 인스턴스 생성");
        }
        else
        {
            Debug.LogWarning("GameDataManager 중복 생성 감지!");
            Destroy(gameObject);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGameData();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveGameData();
    }

    private void OnDestroy()
    {
        SaveGameData();
    }

    #region ▶ 골드 관리 ◀
    /// <summary>골드 추가</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();

        Debug.Log($"골드 획득: +{amount} (총 {currentGold})");
    }

    /// <summary>골드 사용</summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || currentGold < amount) 
            return false;

        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();

        Debug.Log($"골드 사용: -{amount} (남은 골드: {currentGold})");
        return true;
    }

    /// <summary>골드 설정 (치트용)</summary>
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();
    }

    /// <summary>테스트용: 캐릭터 구매를 취소하고 골드 환불</summary>
    public void RefundCharacterPurchase(int refundAmount)
    {
        currentGold += refundAmount;
        OnGoldChanged?.Invoke(currentGold);
        SaveGameData();
        
        Debug.Log($"테스트: 캐릭터 구매 취소 - 골드 환불: +{refundAmount} (총 {currentGold})");
    }
    #endregion

    #region ▶ 경험치 관리 ◀
    /// <summary>경험치 추가</summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        currentExp += amount;
        OnExpChanged?.Invoke(currentExp);

        Debug.Log($"경험치 획득: +{amount} (총 {currentExp})");

        // 레벨업 체크
        CheckLevelUp();
        SaveGameData();
    }

    /// <summary>레벨업 체크 및 처리</summary>
    private void CheckLevelUp()
    {
        int expForNextLevel = GetExpForNextLevel();
        
        while (currentExp >= expForNextLevel)
        {
            // 레벨업에 사용된 경험치 차감
            currentExp -= expForNextLevel;
            playerLevel++;
            
            OnLevelUp?.Invoke(playerLevel);
            OnExpChanged?.Invoke(currentExp);
            Debug.Log($"레벨업! 현재 레벨: {playerLevel}, 남은 경험치: {currentExp}");
            
            // 다음 레벨업에 필요한 경험치 재계산
            expForNextLevel = GetExpForNextLevel();
        }
    }

    /// <summary>현재 레벨에서 다음 레벨로 올라가는 데 필요한 경험치</summary>
    private int GetExpForNextLevel()
    {
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expGrowthRate, playerLevel - 1));
    }
    #endregion

    #region ▶ 데이터 저장/로드 ◀
    /// <summary>게임 데이터 저장</summary>
    public void SaveGameData()
    {
        PlayerPrefs.SetInt("CurrentGold", currentGold);
        PlayerPrefs.SetInt("CurrentExp", currentExp);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
    PlayerPrefs.SetInt("HealSkillLevel", healSkillLevel);
        PlayerPrefs.Save();
    }

    /// <summary>게임 데이터 로드</summary>
    public void LoadGameData()
    {
        currentGold = PlayerPrefs.GetInt("CurrentGold", 0);
        currentExp = PlayerPrefs.GetInt("CurrentExp", 0);
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
    healSkillLevel = PlayerPrefs.GetInt("HealSkillLevel", 0);

        // 이벤트 발생
        OnGoldChanged?.Invoke(currentGold);
        OnExpChanged?.Invoke(currentExp);
    }

    /// <summary>게임 데이터 리셋 (치트용)</summary>
    public void ResetGameData()
    {
        currentGold = 0;
        currentExp = 0;
        playerLevel = 1;
    healSkillLevel = 0;

        PlayerPrefs.DeleteKey("CurrentGold");
        PlayerPrefs.DeleteKey("CurrentExp");
        PlayerPrefs.DeleteKey("PlayerLevel");
    PlayerPrefs.DeleteKey("HealSkillLevel");

        OnGoldChanged?.Invoke(currentGold);
        OnExpChanged?.Invoke(currentExp);
        OnLevelUp?.Invoke(playerLevel);

        Debug.Log("게임 데이터가 리셋되었습니다.");
    }
    #endregion

    #region ▶ 유틸리티 ◀
    /// <summary>골드를 포맷된 문자열로 반환</summary>
    public string GetFormattedGold()
    {
        if (currentGold >= 1000000)
            return $"{currentGold / 1000000f:F1}M";
        else if (currentGold >= 1000)
            return $"{currentGold / 1000f:F1}K";
        else
            return currentGold.ToString();
    }

    /// <summary>경험치 진행률 (0~1)</summary>
    public float GetExpProgress()
    {
        int expForNextLevel = GetExpForNextLevel();
        
        if (expForNextLevel <= 0) return 1f;
        
        return (float)currentExp / expForNextLevel;
    }

    // ===== 힐 스킬 API =====
    /// <summary>현재 힐량 계산 (기본 + 레벨당 증가)</summary>
    public int GetCurrentHealAmount()
    {
        return Mathf.Max(0, healBaseAmount + healSkillLevel * healPerLevel);
    }

    /// <summary>다음 힐 스킬 업그레이드 비용</summary>
    public int GetHealSkillUpgradeCost()
    {
        // 기하급수 성장 비용
        double cost = healUpgradeBaseCost * System.Math.Pow(healUpgradeCostGrowth, healSkillLevel);
        return Mathf.Max(1, Mathf.RoundToInt((float)cost));
    }

    /// <summary>힐 스킬 업그레이드 시도 (성공 시 true)</summary>
    public bool TryUpgradeHealSkill()
    {
        int cost = GetHealSkillUpgradeCost();
        if (!SpendGold(cost)) return false;
        healSkillLevel++;
        PlayerPrefs.SetInt("HealSkillLevel", healSkillLevel);
        PlayerPrefs.Save();
        Debug.Log($"힐 스킬 업그레이드! 레벨: {healSkillLevel}, 현재 힐량: {GetCurrentHealAmount()}");
        return true;
    }
    #endregion
}
