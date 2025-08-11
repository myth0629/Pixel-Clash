using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 업그레이드 화면의 전역 스킬(힐 등) 업그레이드 항목
/// </summary>
public class SkillUpgradeItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI valueText;   // 예: 현재 힐량
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject maxIndicator;

    [Header("설정")]
    [SerializeField] private string skillDisplayName = "힐 스킬";
    [SerializeField] private int maxLevel = -1; // -1이면 제한 없음

    private GameUIManager uiManager;

    public void Setup(GameUIManager manager)
    {
        uiManager = manager;
        if (skillNameText != null)
            skillNameText.text = skillDisplayName;

        SetupButton();
        Refresh();
    }

    private void OnEnable()
    {
        // 골드 변경 시 버튼 활성화 갱신을 위해 새로고침
        Refresh();
    }

    private void SetupButton()
    {
        if (upgradeButton == null) return;
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
    }

    private void OnUpgradeClicked()
    {
        // 캐릭터 업그레이드와 동일하게 확인 팝업을 GameUIManager에서 띄우도록 위임
        if (uiManager != null)
        {
            uiManager.ShowSkillUpgradeConfirmPopup();
        }
    }

    public void Refresh()
    {
        if (GameDataManager.Instance == null) return;

        int level = GameDataManager.Instance.HealSkillLevel;
        int amount = GameDataManager.Instance.GetCurrentHealAmount();
        int cost = GameDataManager.Instance.GetHealSkillUpgradeCost();

        if (levelText != null)
            levelText.text = $"Lv.{level}";

        if (valueText != null)
            valueText.text = $"힐량: {amount}";

        bool isMax = (maxLevel >= 0 && level >= maxLevel);
        if (costText != null)
            costText.text = isMax ? "MAX" : $"{cost} G";

        if (maxIndicator != null)
            maxIndicator.SetActive(isMax);

        if (upgradeButton != null)
        {
            bool canAfford = !isMax && GameDataManager.Instance.CurrentGold >= cost;
            upgradeButton.interactable = canAfford;
        }
    }
}
