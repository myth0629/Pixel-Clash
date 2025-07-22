using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타이틀 화면 전용 UI 컴포넌트
/// </summary>
public class TitleUI : MonoBehaviour
{
    [Header("타이틀 UI 요소")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("애니메이션")]
    [SerializeField] private bool enableTitleAnimation = true;
    [SerializeField] private float animationSpeed = 1f;

    private void Start()
    {
        SetupUI();
        if (enableTitleAnimation)
            StartTitleAnimation();
    }

    #region ▶ UI 설정 ◀
    /// <summary>타이틀 UI 초기 설정</summary>
    private void SetupUI()
    {
        // 버튼 이벤트는 GameUIManager에서 처리하므로 여기서는 설정하지 않음
        
        // 버전 정보 표시
        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }
    }
    #endregion

    #region ▶ 애니메이션 ◀
    /// <summary>타이틀 텍스트 애니메이션 시작</summary>
    private void StartTitleAnimation()
    {
        if (titleText != null)
        {
            // 간단한 페이드 인/아웃 애니메이션
            StartCoroutine(TitleFadeAnimation());
        }
    }

    private System.Collections.IEnumerator TitleFadeAnimation()
    {
        Color originalColor = titleText.color;
        
        while (true)
        {
            // 페이드 아웃
            float alpha = 1f;
            while (alpha > 0.5f)
            {
                alpha -= Time.deltaTime * animationSpeed;
                Color newColor = originalColor;
                newColor.a = alpha;
                titleText.color = newColor;
                yield return null;
            }

            // 페이드 인
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * animationSpeed;
                Color newColor = originalColor;
                newColor.a = alpha;
                titleText.color = newColor;
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>버튼 활성화/비활성화</summary>
    public void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
        
        if (settingsButton != null)
            settingsButton.interactable = interactable;
        
        if (exitButton != null)
            exitButton.interactable = interactable;
    }

    /// <summary>타이틀 텍스트 변경</summary>
    public void SetTitleText(string text)
    {
        if (titleText != null)
            titleText.text = text;
    }
    #endregion
}
