using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 골드, 경험치, 레벨을 표시하는 UI 매니저
/// </summary>
public class GameDataUI : MonoBehaviour
{
    [Header("골드 UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Image goldIcon;

    [Header("경험치/레벨 UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFill;

    [Header("레벨업 이펙트")]
    [SerializeField] private GameObject levelUpEffect;
    [SerializeField] private float effectDuration = 2f;

    [Header("골드 획득 이펙트")]
    [SerializeField] private GameObject goldGainEffect;
    [SerializeField] private TextMeshProUGUI goldGainText;

    private void Start()
    {
        // 이벤트 구독
        GameDataManager.OnGoldChanged += UpdateGoldUI;
        GameDataManager.OnExpChanged += UpdateExpUI;
        GameDataManager.OnLevelUp += OnLevelUp;

        // 초기 UI 업데이트
        if (GameDataManager.Instance != null)
        {
            UpdateGoldUI(GameDataManager.Instance.CurrentGold);
            UpdateExpUI(GameDataManager.Instance.CurrentExp);
            UpdateLevelUI(GameDataManager.Instance.PlayerLevel);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        GameDataManager.OnGoldChanged -= UpdateGoldUI;
        GameDataManager.OnExpChanged -= UpdateExpUI;
        GameDataManager.OnLevelUp -= OnLevelUp;
    }

    #region ▶ UI 업데이트 ◀
    /// <summary>골드 UI 업데이트</summary>
    private void UpdateGoldUI(int newGold)
    {
        if (goldText != null)
        {
            goldText.text = GameDataManager.Instance.GetFormattedGold() + " G";
        }
    }

    /// <summary>경험치 UI 업데이트</summary>
    private void UpdateExpUI(int newExp)
    {
        if (GameDataManager.Instance == null) return;

        // 경험치 텍스트
        if (expText != null)
        {
            int expToNext = GameDataManager.Instance.ExpToNextLevel;
            expText.text = $"Exp {newExp}/{newExp + expToNext}";
        }

        // 경험치 슬라이더
        if (expSlider != null)
        {
            expSlider.value = GameDataManager.Instance.GetExpProgress();
        }

        UpdateLevelUI(GameDataManager.Instance.PlayerLevel);
    }

    /// <summary>레벨 UI 업데이트</summary>
    private void UpdateLevelUI(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{newLevel}";
        }
    }
    #endregion

    #region ▶ 이펙트 ◀
    /// <summary>레벨업 이펙트</summary>
    private void OnLevelUp(int newLevel)
    {
        UpdateLevelUI(newLevel);

        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
            Invoke(nameof(HideLevelUpEffect), effectDuration);
        }
    }

    private void HideLevelUpEffect()
    {
        if (levelUpEffect != null)
            levelUpEffect.SetActive(false);
    }

    /// <summary>골드 획득 이펙트 표시</summary>
    public void ShowGoldGainEffect(int amount)
    {
        if (goldGainEffect != null && goldGainText != null)
        {
            goldGainText.text = $"+{amount}";
            goldGainEffect.SetActive(true);
            
            // 간단한 페이드 아웃 애니메이션
            StartCoroutine(FadeOutGoldEffect());
        }
    }

    private System.Collections.IEnumerator FadeOutGoldEffect()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        
        Color originalColor = goldGainText.color;
        Vector3 originalPos = goldGainEffect.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 페이드 아웃
            Color newColor = originalColor;
            newColor.a = 1f - progress;
            goldGainText.color = newColor;

            // 위로 이동
            Vector3 newPos = originalPos + Vector3.up * (progress * 50f);
            goldGainEffect.transform.localPosition = newPos;

            yield return null;
        }

        // 원상복구
        goldGainText.color = originalColor;
        goldGainEffect.transform.localPosition = originalPos;
        goldGainEffect.SetActive(false);
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>골드 획득 시 호출 (외부에서 사용)</summary>
    public static void ShowGoldReward(int amount)
    {
        var instance = FindObjectOfType<GameDataUI>();
        if (instance != null)
        {
            instance.ShowGoldGainEffect(amount);
        }
    }
    #endregion
}
