using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 업그레이드 화면의 개별 캐릭터 아이템 UI 컴포넌트
/// </summary>
public class UpgradeCharacterItem : MonoBehaviour
{
    [Header("UI 요소들")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject maxLevelIndicator; // 최대 레벨 표시
    
    [Header("스탯 표시")]
    [SerializeField] private TextMeshProUGUI currentAttackText; // 현재 공격력 표시
    [SerializeField] private TextMeshProUGUI currentHealthText; // 현재 체력 표시

    [Header("레벨업 설정")]
    [SerializeField] private int maxLevel = 10; // 최대 레벨

    private CharacterData characterData;
    private int currentLevel;
    private GameUIManager gameUIManager;

    /// <summary>아이템 설정</summary>
    public void SetupItem(CharacterData character, int level, GameUIManager uiManager)
    {
        characterData = character;
        currentLevel = level;
        gameUIManager = uiManager;

        if (character == null)
        {
            Debug.LogWarning("UpgradeCharacterItem: character가 null입니다!");
            return;
        }

        UpdateUIState();
        SetupButton();
        
        Debug.Log($"UpgradeCharacterItem 설정: {character.displayName}, 레벨: {level}");
    }

    /// <summary>캐릭터의 현재 레벨 기준 스탯 계산</summary>
    private (int hp, int atk) CalculateCharacterStats(CharacterData characterData, int level)
    {
        if (characterData == null) return (0, 0);
        
        // 곱연산 성장: base * (1 + growth)^(level - 1)
        int lv = Mathf.Max(1, level);
        float hpMultiplier = Mathf.Pow(1f + characterData.hpGrowth, lv - 1);
        float atkMultiplier = Mathf.Pow(1f + characterData.atkGrowth, lv - 1);
        int hp = Mathf.RoundToInt(characterData.baseHp * hpMultiplier);
        int atk = Mathf.RoundToInt(characterData.baseAtk * atkMultiplier);
        
        return (hp, atk);
    }

    /// <summary>UI 상태 업데이트</summary>
    private void UpdateUIState()
    {
        if (characterData == null) return;

        // 캐릭터 이름 표시 (+ 포지션 배지)
        if (characterNameText != null)
        {
            string baseName = string.IsNullOrEmpty(characterData.displayName) ? characterData.name : characterData.displayName;
            characterNameText.text = baseName + GetPositionBadge(characterData.position);
        }

        // 캐릭터 아이콘 표시
        if (characterIcon != null && characterData.icon != null)
            characterIcon.sprite = characterData.icon;

        // 현재 레벨 표시
        if (currentLevelText != null)
            currentLevelText.text = $"Lv.{currentLevel}";

        // 캐릭터 스탯 계산 및 표시
        var (currentHp, currentAtk) = CalculateCharacterStats(characterData, currentLevel);
        
        // 공격력 표시
        if (currentAttackText != null)
            currentAttackText.text = $"{currentAtk}";
            
        // 체력 표시
        if (currentHealthText != null)
            currentHealthText.text = $"{currentHp}";

        // 최대 레벨 체크
        bool isMaxLevel = currentLevel >= maxLevel;

        // 업그레이드 비용 표시
        if (upgradeCostText != null)
        {
            if (isMaxLevel)
            {
                upgradeCostText.text = "MAX Lv";
                upgradeCostText.color = Color.yellow;
            }
            else
            {
                int cost = gameUIManager?.GetUpgradeCost(characterData, currentLevel) ?? 0;
                upgradeCostText.text = $"{cost} G";
                upgradeCostText.color = Color.white;
            }
        }

        // 최대 레벨 표시
        if (maxLevelIndicator != null)
            maxLevelIndicator.SetActive(isMaxLevel);

        // 업그레이드 버튼 상태
        if (upgradeButton != null)
        {
            upgradeButton.interactable = !isMaxLevel;
            
            // 골드 부족 체크
            if (!isMaxLevel && gameUIManager != null)
            {
                int cost = gameUIManager.GetUpgradeCost(characterData, currentLevel);
                bool canAfford = GameDataManager.Instance != null && 
                               GameDataManager.Instance.CurrentGold >= cost;
                upgradeButton.interactable = canAfford;
            }
        }

        // 배경 색상 조정 (최대 레벨이면 다른 색상)
        if (backgroundImage != null)
        {
            if (isMaxLevel)
            {
                backgroundImage.color = new Color(1f, 1f, 0.5f, 0.8f); // 황금색
            }
            else
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0.8f); // 기본 흰색
            }
        }
    }

    /// <summary>버튼 이벤트 설정</summary>
    private void SetupButton()
    {
        if (upgradeButton == null) return;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
    }

    /// <summary>업그레이드 버튼 클릭</summary>
    private void OnUpgradeButtonClicked()
    {
        if (characterData == null || gameUIManager == null)
        {
            Debug.LogWarning("UpgradeCharacterItem: characterData 또는 gameUIManager가 null입니다!");
            return;
        }

        // 최대 레벨 체크
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{characterData.displayName}는 이미 최대 레벨입니다.");
            return;
        }

        Debug.Log($"업그레이드 버튼 클릭: {characterData.displayName} (Lv.{currentLevel})");
        
        // GameUIManager에 업그레이드 요청
        gameUIManager.OnUpgradeCharacter(characterData);
    }

    /// <summary>아이템 새로고침 (레벨업 후 호출)</summary>
    public void RefreshItem()
    {
        if (gameUIManager != null && characterData != null)
        {
            currentLevel = gameUIManager.GetCharacterLevel(characterData);
            UpdateUIState();
        }
    }

    /// <summary>
    /// 전방/후방 배지 문자열 반환
    /// </summary>
    private string GetPositionBadge(PositionType pos)
    {
        switch (pos)
        {
            case PositionType.Front: return " <color=#FFD700>[전방]</color>";
            case PositionType.Back:  return " <color=#FFD700>[후방]</color>";
            default: return string.Empty;
        }
    }
}
