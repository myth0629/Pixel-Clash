using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 전체 UI 상태를 관리하는 매니저
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("타이틀 화면")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("게임 UI")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject[] gameUIElements; // 게임 중 활성화할 UI들

    [Header("설정")]
    [SerializeField] private bool showTitleOnStart = true;

    private bool isGameStarted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupButtons();
        InitializeUI();
    }

    #region ▶ 초기화 ◀
    /// <summary>버튼 이벤트 설정</summary>
    private void SetupButtons()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    /// <summary>초기 UI 상태 설정</summary>
    private void InitializeUI()
    {
        if (showTitleOnStart)
        {
            ShowTitleScreen();
        }
        else
        {
            ShowGameUI();
        }
    }
    #endregion

    #region ▶ UI 상태 관리 ◀
    /// <summary>타이틀 화면 표시</summary>
    public void ShowTitleScreen()
    {
        if (titlePanel != null)
            titlePanel.SetActive(true);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);

        // 게임 UI 요소들 비활성화
        foreach (var element in gameUIElements)
        {
            if (element != null)
                element.SetActive(false);
        }

        isGameStarted = false;
        Debug.Log("타이틀 화면 표시");
    }

    /// <summary>게임 UI 표시</summary>
    public void ShowGameUI()
    {
        if (titlePanel != null)
            titlePanel.SetActive(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);

        // 게임 UI 요소들 활성화
        foreach (var element in gameUIElements)
        {
            if (element != null)
                element.SetActive(true);
        }

        isGameStarted = true;
        Debug.Log("게임 UI 표시");
    }
    #endregion

    #region ▶ 버튼 이벤트 ◀
    /// <summary>게임 시작 버튼 클릭</summary>
    private void OnStartGameClicked()
    {
        Debug.Log("게임 시작!");
        
        // 게임 UI 표시
        ShowGameUI();

        // StageManager에게 게임 시작 알림
        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartGame();
        }
        else
        {
            Debug.LogWarning("StageManager가 없습니다!");
        }
    }

    /// <summary>설정 버튼 클릭</summary>
    private void OnSettingsClicked()
    {
        Debug.Log("설정 화면 (미구현)");
        // TODO: 설정 패널 표시
    }

    /// <summary>종료 버튼 클릭</summary>
    private void OnExitClicked()
    {
        Debug.Log("게임 종료");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region ▶ 게임 상태 이벤트 ◀
    /// <summary>게임 오버 시 호출</summary>
    public void OnGameOver()
    {
        Debug.Log("게임 오버!");
        
        // 게임 오버 처리 후 타이틀로 돌아가기
        Invoke(nameof(ReturnToTitle), 3f);
    }

    /// <summary>게임 완료 시 호출</summary>
    public void OnGameComplete()
    {
        Debug.Log("게임 완료!");
        
        // 게임 완료 처리 후 타이틀로 돌아가기
        Invoke(nameof(ReturnToTitle), 5f);
    }

    /// <summary>타이틀로 돌아가기</summary>
    public void ReturnToTitle()
    {
        ShowTitleScreen();
        
        // 필요시 씬 리로드
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>현재 게임이 시작되었는지 확인</summary>
    public bool IsGameStarted => isGameStarted;

    /// <summary>특정 UI 요소 토글</summary>
    public void ToggleUIElement(GameObject element)
    {
        if (element != null)
            element.SetActive(!element.activeSelf);
    }

    /// <summary>특정 UI 요소 활성화/비활성화</summary>
    public void SetUIElementActive(GameObject element, bool active)
    {
        if (element != null)
            element.SetActive(active);
    }
    #endregion
}
