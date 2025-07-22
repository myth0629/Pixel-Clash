using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 스테이지와 라운드 시작 시 나타나는 UI
/// </summary>
public class RoundUI : MonoBehaviour
{
    [Header("Round Text UI")]
    [SerializeField] private GameObject roundPanel;
    [SerializeField] private TextMeshProUGUI stageText; // "STAGE" 라벨
    [SerializeField] private TextMeshProUGUI stageNumberText; // "1-1" 형식의 숫자
    [SerializeField] private Image backgroundImage;

    [Header("애니메이션 설정")]
    [SerializeField] private float showDuration = 2f;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("텍스트 설정")]
    [SerializeField] private string stageFormat = "STAGE";
    [SerializeField] private string stageNumberFormat = "{0}-{1}";
    [SerializeField] private Color stageTextColor = Color.white;
    [SerializeField] private Color stageNumberColor = Color.yellow;

    private bool isAnimating = false;

    private void Start()
    {
        // 이벤트 구독
        StageManager.OnStageStart += OnStageStart;
        StageManager.OnRoundStart += OnRoundStart;

        // 초기 상태는 비활성화
        if (roundPanel != null)
            roundPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        StageManager.OnStageStart -= OnStageStart;
        StageManager.OnRoundStart -= OnRoundStart;
    }

    #region ▶ 이벤트 핸들러 ◀
    /// <summary>스테이지 시작 시 호출</summary>
    private void OnStageStart(int stageNumber, int roundNumber)
    {
        ShowRoundText(stageNumber, roundNumber, true);
    }

    /// <summary>라운드 시작 시 호출</summary>
    private void OnRoundStart(int stageNumber, int roundNumber)
    {
        if (roundNumber > 1) // 첫 번째 라운드는 스테이지 시작에서 이미 표시
        {
            ShowRoundText(stageNumber, roundNumber, false);
        }
    }
    #endregion

    #region ▶ UI 표시 ◀
    /// <summary>Round Text 표시</summary>
    public void ShowRoundText(int stage, int round, bool isNewStage = false)
    {
        if (isAnimating) return;

        StartCoroutine(ShowRoundTextCoroutine(stage, round, isNewStage));
    }

    private IEnumerator ShowRoundTextCoroutine(int stage, int round, bool isNewStage)
    {
        isAnimating = true;

        // UI 설정
        SetupUI(stage, round, isNewStage);

        // 패널 활성화
        if (roundPanel != null)
            roundPanel.SetActive(true);

        // 페이드 인 애니메이션
        yield return StartCoroutine(FadeInAnimation());

        // 표시 시간 대기
        yield return new WaitForSeconds(showDuration);

        // 페이드 아웃 애니메이션
        yield return StartCoroutine(FadeOutAnimation());

        // 패널 비활성화
        if (roundPanel != null)
            roundPanel.SetActive(false);

        isAnimating = false;
    }

    /// <summary>UI 요소 설정</summary>
    private void SetupUI(int stage, int round, bool isNewStage)
    {
        // 스테이지 라벨 텍스트
        if (stageText != null)
        {
            stageText.text = stageFormat;
            stageText.color = stageTextColor;
        }

        // 스테이지 번호 텍스트 (1-1 형식)
        if (stageNumberText != null)
        {
            stageNumberText.text = string.Format(stageNumberFormat, stage, round);
            stageNumberText.color = stageNumberColor;
        }

        // 새로운 스테이지일 때는 텍스트를 더 강조
        if (isNewStage)
        {
            if (stageText != null)
                stageText.fontSize *= 1.2f;
            if (stageNumberText != null)
                stageNumberText.fontSize *= 1.2f;
        }
    }
    #endregion

    #region ▶ 애니메이션 ◀
    /// <summary>페이드 인 애니메이션</summary>
    private IEnumerator FadeInAnimation()
    {
        float elapsed = 0f;

        // 초기 투명도 설정
        SetAlpha(0f);

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInTime;
            float curveValue = fadeCurve.Evaluate(progress);

            SetAlpha(curveValue);

            // 약간의 스케일 효과
            float scale = Mathf.Lerp(0.8f, 1f, curveValue);
            if (roundPanel != null)
                roundPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        SetAlpha(1f);
        if (roundPanel != null)
            roundPanel.transform.localScale = Vector3.one;
    }

    /// <summary>페이드 아웃 애니메이션</summary>
    private IEnumerator FadeOutAnimation()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutTime;
            float curveValue = fadeCurve.Evaluate(1f - progress);

            SetAlpha(curveValue);

            // 약간의 스케일 효과
            float scale = Mathf.Lerp(1f, 1.1f, progress);
            if (roundPanel != null)
                roundPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        SetAlpha(0f);
        if (roundPanel != null)
            roundPanel.transform.localScale = Vector3.one;
    }

    /// <summary>모든 UI 요소의 투명도 설정</summary>
    private void SetAlpha(float alpha)
    {
        if (stageText != null)
        {
            Color color = stageText.color;
            color.a = alpha;
            stageText.color = color;
        }

        if (stageNumberText != null)
        {
            Color color = stageNumberText.color;
            color.a = alpha;
            stageNumberText.color = color;
        }

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = alpha * 0.8f; // 배경은 조금 더 투명하게
            backgroundImage.color = color;
        }
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>수동으로 Round Text 표시 (테스트용)</summary>
    public void TestShowRound(int stage, int round)
    {
        ShowRoundText(stage, round, false);
    }
    #endregion
}
