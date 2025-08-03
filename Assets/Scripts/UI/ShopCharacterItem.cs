using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 상점에서 표시되는 개별 캐릭터 아이템 UI
/// </summary>
public class ShopCharacterItem : MonoBehaviour
{
    [Header("UI 요소들")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TMPro.TextMeshProUGUI characterNameText;
    [SerializeField] private TMPro.TextMeshProUGUI characterStatsText;
    [SerializeField] private TMPro.TextMeshProUGUI priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private GameObject lockedOverlay;  // 잠금 상태 오버레이
    [SerializeField] private GameObject unlockedIndicator;  // 해금 상태 표시

    private CharacterData characterData;
    private bool isUnlocked;
    private Action<CharacterData> onPurchaseCallback;

    /// <summary>
    /// 상점 아이템 설정
    /// </summary>
    /// <param name="character">캐릭터 데이터</param>
    /// <param name="unlocked">해금 여부</param>
    /// <param name="purchaseCallback">구매 콜백</param>
    public void SetupItem(CharacterData character, bool unlocked, Action<CharacterData> purchaseCallback)
    {
        characterData = character;
        isUnlocked = unlocked;
        onPurchaseCallback = purchaseCallback;

        if (character == null) return;

        // 캐릭터 정보 표시
        if (characterNameText != null)
            characterNameText.text = character.name;

        if (characterStatsText != null)
        {
            characterStatsText.text = $"HP: {character.baseHp}\nATK: {character.baseAtk}";
        }

        // 캐릭터 아이콘 설정 (있다면)
        if (characterIcon != null && character.icon != null)
        {
            characterIcon.sprite = character.icon;
        }

        // 가격 표시 (CharacterData의 unlockCost 사용)
        if (priceText != null)
        {
            priceText.text = $"{character.unlockCost} G";
        }

        // 해금 상태에 따른 UI 설정
        UpdateUIState(unlocked, character.unlockCost);

        // 구매 버튼 이벤트 설정
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            if (!unlocked)
            {
                purchaseButton.onClick.AddListener(() => onPurchaseCallback?.Invoke(characterData));
            }
        }
    }

    /// <summary>
    /// 해금 상태에 따른 UI 업데이트
    /// </summary>
    private void UpdateUIState(bool unlocked, int price)
    {
        // 잠금 오버레이
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);

        // 해금 표시
        if (unlockedIndicator != null)
            unlockedIndicator.SetActive(unlocked);

        // 구매 버튼 상태
        if (purchaseButton != null)
        {
            purchaseButton.interactable = !unlocked;
            
            // 버튼 텍스트 변경
            TMPro.TextMeshProUGUI buttonText = purchaseButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = unlocked ? "보유 중" : "구매";
            }
        }

        // 골드 부족 체크
        if (!unlocked && GameDataManager.Instance != null)
        {
            bool canAfford = GameDataManager.Instance.CurrentGold >= price;
            if (purchaseButton != null)
            {
                purchaseButton.interactable = canAfford;
                
                // 골드 부족 시 버튼 색상 변경
                ColorBlock colors = purchaseButton.colors;
                if (canAfford)
                {
                    colors.normalColor = Color.white;
                }
                else
                {
                    colors.normalColor = Color.red;
                    
                    // 버튼 텍스트도 변경
                    TMPro.TextMeshProUGUI buttonText = purchaseButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = "골드 부족";
                    }
                }
                purchaseButton.colors = colors;
            }
        }
    }

    /// <summary>
    /// 아이템 새로고침 (골드 변경 시 호출)
    /// </summary>
    public void RefreshItem()
    {
        if (characterData != null)
        {
            UpdateUIState(isUnlocked, characterData.unlockCost);
        }
    }
}
