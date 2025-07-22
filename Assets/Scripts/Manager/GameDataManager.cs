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

    // 이벤트
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnExpChanged;
    public static event Action<int> OnLevelUp;

    // 프로퍼티
    public int CurrentGold => currentGold;
    public int CurrentExp => currentExp;
    public int PlayerLevel => playerLevel;
    public int ExpToNextLevel => GetExpRequiredForLevel(playerLevel + 1) - currentExp;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameData();
        }
        else
        {
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
        int expRequired = GetExpRequiredForLevel(playerLevel + 1);
        
        while (currentExp >= expRequired)
        {
            playerLevel++;
            OnLevelUp?.Invoke(playerLevel);
            Debug.Log($"레벨업! 현재 레벨: {playerLevel}");
            
            expRequired = GetExpRequiredForLevel(playerLevel + 1);
        }
    }

    /// <summary>특정 레벨에 필요한 총 경험치 계산</summary>
    private int GetExpRequiredForLevel(int level)
    {
        if (level <= 1) return 0;
        
        int totalExp = 0;
        for (int i = 2; i <= level; i++)
        {
            totalExp += Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expGrowthRate, i - 2));
        }
        return totalExp;
    }
    #endregion

    #region ▶ 데이터 저장/로드 ◀
    /// <summary>게임 데이터 저장</summary>
    public void SaveGameData()
    {
        PlayerPrefs.SetInt("CurrentGold", currentGold);
        PlayerPrefs.SetInt("CurrentExp", currentExp);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        PlayerPrefs.Save();
    }

    /// <summary>게임 데이터 로드</summary>
    public void LoadGameData()
    {
        currentGold = PlayerPrefs.GetInt("CurrentGold", 0);
        currentExp = PlayerPrefs.GetInt("CurrentExp", 0);
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

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

        PlayerPrefs.DeleteKey("CurrentGold");
        PlayerPrefs.DeleteKey("CurrentExp");
        PlayerPrefs.DeleteKey("PlayerLevel");

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
        int currentLevelExp = GetExpRequiredForLevel(playerLevel);
        int nextLevelExp = GetExpRequiredForLevel(playerLevel + 1);
        
        if (nextLevelExp <= currentLevelExp) return 1f;
        
        return (float)(currentExp - currentLevelExp) / (nextLevelExp - currentLevelExp);
    }
    #endregion
}
