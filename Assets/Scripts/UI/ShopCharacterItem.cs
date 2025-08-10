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
    [SerializeField] private Image backgroundImage;  // 배경 이미지 (보유 중일 때 어둡게 만들기 위함)

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

        // 캐릭터 정보 표시 (+ 포지션 배지)
        if (characterNameText != null)
        {
            string baseName = string.IsNullOrEmpty(character.displayName) ? character.name : character.displayName;
            characterNameText.text = baseName + GetPositionBadge(character.position);
        }

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

        // 배경 색상 변경 (보유 중인 캐릭터는 어둡게)
        if (backgroundImage != null)
        {
            if (unlocked)
            {
                // 보유 중인 캐릭터: 어두운 배경
                backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // 회색으로 어둡게
            }
            else
            {
                // 미보유 캐릭터: 기본 배경
                backgroundImage.color = Color.white;
            }
        }

        // 캐릭터 아이콘과 텍스트 색상도 조정
        Color contentColor = unlocked ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        
        if (characterIcon != null)
        {
            characterIcon.color = contentColor;
        }
        
        if (characterNameText != null)
        {
            characterNameText.color = contentColor;
        }
        
        if (characterStatsText != null)
        {
            characterStatsText.color = contentColor;
        }
        
        if (priceText != null)
        {
            priceText.color = contentColor;
        }

        // 구매 버튼 상태
        if (purchaseButton != null)
        {
            purchaseButton.interactable = !unlocked;
            
            // 버튼 텍스트 변경
            TMPro.TextMeshProUGUI buttonText = purchaseButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            // if (buttonText != null)
            // {
            //     buttonText.text = unlocked ? "보유 중" : "구매";
            // }
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
