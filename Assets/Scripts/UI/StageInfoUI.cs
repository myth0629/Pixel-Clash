using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상단에 고정으로 표시되는 스테이지 정보 UI
/// </summary>
public class StageInfoUI : MonoBehaviour
{
    [Header("스테이지 정보 UI")]
    [SerializeField] private TextMeshProUGUI stageInfoText;
    [SerializeField] private Slider stageProgressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("텍스트 포맷")]
    [SerializeField] private string stageFormat = "Stage {0}-{1}";
    [SerializeField] private string progressFormat = "{0}/{1}";

    private void Start()
    {
        // 이벤트 구독
        StageManager.OnStageStart += OnStageStart;
        StageManager.OnRoundStart += OnRoundStart;
        StageManager.OnStageComplete += OnStageComplete;

        // 초기 UI 업데이트
        UpdateUI();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        StageManager.OnStageStart -= OnStageStart;
        StageManager.OnRoundStart -= OnRoundStart;
        StageManager.OnStageComplete -= OnStageComplete;
    }

    #region ▶ 이벤트 핸들러 ◀
    private void OnStageStart(int stageNumber, int roundNumber)
    {
        UpdateUI();
    }

    private void OnRoundStart(int stageNumber, int roundNumber)
    {
        UpdateUI();
    }

    private void OnStageComplete(int stageNumber)
    {
        UpdateUI();
    }
    #endregion

    #region ▶ UI 업데이트 ◀
    /// <summary>스테이지 정보 UI 업데이트</summary>
    private void UpdateUI()
    {
        if (StageManager.Instance == null) return;

        var stageManager = StageManager.Instance;

        // 스테이지 텍스트 (1-1 형식)
        if (stageInfoText != null)
        {
            stageInfoText.text = string.Format(stageFormat, stageManager.CurrentStage, stageManager.CurrentRound);
        }

        // 진행률 슬라이더
        if (stageProgressSlider != null)
        {
            float progress = (float)stageManager.CurrentRound / stageManager.RoundsPerStage;
            stageProgressSlider.value = progress;
        }

        // 진행률 텍스트
        if (progressText != null)
        {
            progressText.text = string.Format(progressFormat, 
                stageManager.CurrentRound, 
                stageManager.RoundsPerStage);
        }
    }
    #endregion
}
