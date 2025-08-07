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

    /// <summary>UI 상태 업데이트</summary>
    private void UpdateUIState()
    {
        if (characterData == null) return;

        // 캐릭터 이름 표시
        if (characterNameText != null)
            characterNameText.text = characterData.displayName;

        // 캐릭터 아이콘 표시
        if (characterIcon != null && characterData.icon != null)
            characterIcon.sprite = characterData.icon;

        // 현재 레벨 표시
        if (currentLevelText != null)
            currentLevelText.text = $"Lv.{currentLevel}";

        // 최대 레벨 체크
        bool isMaxLevel = currentLevel >= maxLevel;

        // 업그레이드 비용 표시
        if (upgradeCostText != null)
        {
            if (isMaxLevel)
            {
                upgradeCostText.text = "MAX LEVEL";
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
}
