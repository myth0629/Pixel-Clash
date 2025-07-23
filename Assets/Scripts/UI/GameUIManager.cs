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

    [Header("준비 화면")]
    [SerializeField] private GameObject preparePanel;
    [SerializeField] private Button battleStartButton;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private TMPro.TextMeshProUGUI prepareTitleText;
    [SerializeField] private TMPro.TextMeshProUGUI instructionText;
    [SerializeField] private TMPro.TextMeshProUGUI stageInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI difficultyText;
    [SerializeField] private Transform partyMemberContainer;
    [SerializeField] private GameObject partyMemberPrefab;

    [Header("게임 UI")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject[] gameUIElements; // 게임 중 활성화할 UI들

    [Header("설정")]
    [SerializeField] private bool showTitleOnStart = true;

    public enum UIState
    {
        Title,      // 타이틀 화면
        Prepare,    // 준비 화면
        Game        // 게임 중
    }

    private UIState currentState = UIState.Title;
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
            return;
        }
    }

    private void Start()
    {
        SetupButtons();
        
        if (showTitleOnStart)
        {
            ShowTitleScreen();
        }
    }

    private void SetupButtons()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (battleStartButton != null)
            battleStartButton.onClick.AddListener(OnBattleStartClicked);
        
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(OnBackToTitleClicked);
    }

    /// <summary>타이틀 화면 표시</summary>
    public void ShowTitleScreen()
    {
        currentState = UIState.Title;
        
        if (titlePanel != null)
            titlePanel.SetActive(true);

        if (preparePanel != null)
            preparePanel.SetActive(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);

        isGameStarted = false;
        Debug.Log("타이틀 화면 표시");
    }

    /// <summary>준비 화면 표시</summary>
    public void ShowPrepareScreen()
    {
        currentState = UIState.Prepare;
        
        if (titlePanel != null)
            titlePanel.SetActive(false);

        if (preparePanel != null)
            preparePanel.SetActive(true);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);

        // 준비 화면 정보 업데이트
        UpdatePrepareScreenInfo();

        isGameStarted = false;
        Debug.Log("준비 화면 표시");
    }

    /// <summary>준비 화면 정보 업데이트</summary>
    private void UpdatePrepareScreenInfo()
    {
        // 기본 텍스트 설정
        if (prepareTitleText != null)
            prepareTitleText.text = "전투 준비";

        if (instructionText != null)
            instructionText.text = "파티 상태를 확인하고 전투를 시작하세요!";

        // 스테이지 정보 업데이트
        UpdateStageInfo();

        // 파티 정보 업데이트
        UpdatePartyInfo();
    }

    /// <summary>스테이지 정보 업데이트</summary>
    private void UpdateStageInfo()
    {
        if (StageManager.Instance != null)
        {
            var stageManager = StageManager.Instance;
            
            if (stageInfoText != null)
                stageInfoText.text = $"Stage {stageManager.CurrentStage}-{stageManager.CurrentRound}";

            if (difficultyText != null)
            {
                string difficulty = GetDifficultyText(stageManager.CurrentStage);
                difficultyText.text = $"난이도: {difficulty}";
            }
        }
    }

    /// <summary>스테이지에 따른 난이도 텍스트 반환</summary>
    private string GetDifficultyText(int stage)
    {
        switch (stage)
        {
            case 1: return "쉬움";
            case 2:
            case 3: return "보통";
            case 4:
            case 5: return "어려움";
            case 6:
            case 7: return "매우 어려움";
            default: return "극한";
        }
    }

    /// <summary>파티 정보 업데이트</summary>
    private void UpdatePartyInfo()
    {
        if (partyMemberContainer == null) return;

        // 기존 파티 멤버 UI 제거
        foreach (Transform child in partyMemberContainer)
        {
            Destroy(child.gameObject);
        }

        // BattleManager에서 파티 정보 가져오기
        if (BattleManager.Instance != null)
        {
            var partyInfo = BattleManager.Instance.GetTestPartyInfo();
            
            foreach (var (characterData, level) in partyInfo)
            {
                if (characterData != null)
                {
                    CreatePartyMemberUI(characterData, level);
                }
            }
        }
    }

    /// <summary>파티 멤버 UI 생성</summary>
    private void CreatePartyMemberUI(CharacterData characterData, int level)
    {
        if (partyMemberPrefab == null || partyMemberContainer == null) return;

        var memberUI = Instantiate(partyMemberPrefab, partyMemberContainer);
        
        // 캐릭터 이름
        var nameText = memberUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = $"{characterData.name} Lv.{level}";

        // 캐릭터 아이콘
        var iconImage = memberUI.GetComponentInChildren<UnityEngine.UI.Image>();
        if (iconImage != null && characterData.icon != null)
            iconImage.sprite = characterData.icon;
    }

    /// <summary>게임 UI 표시</summary>
    public void ShowGameUI()
    {
        currentState = UIState.Game;
        
        if (titlePanel != null)
            titlePanel.SetActive(false);

        if (preparePanel != null)
            preparePanel.SetActive(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);

        // 추가 게임 UI 활성화
        if (gameUIElements != null)
        {
            foreach (var element in gameUIElements)
            {
                if (element != null)
                    element.SetActive(true);
            }
        }

        isGameStarted = true;
        Debug.Log("게임 UI 표시 - 전투 시작");
    }

    #region 버튼 이벤트 핸들러

    private void OnStartGameClicked()
    {
        Debug.Log("게임 시작 버튼 클릭");
        ShowPrepareScreen();
    }

    private void OnBattleStartClicked()
    {
        Debug.Log("전투 시작 버튼 클릭");
        
        // UI를 먼저 게임 화면으로 전환
        ShowGameUI();
        
        // StageManager를 통해 게임 시작
        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartGame();
        }
        else
        {
            Debug.LogWarning("StageManager가 없습니다.");
        }
    }

    private void OnBackToTitleClicked()
    {
        Debug.Log("타이틀로 돌아가기 버튼 클릭");
        ShowTitleScreen();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("설정 버튼 클릭");
        // 설정 화면 로직 추가 예정
    }

    private void OnExitClicked()
    {
        Debug.Log("게임 종료 버튼 클릭");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 게임 상태 관리

    public void OnGameOver()
    {
        Debug.Log("게임 오버");
        // 게임 오버 UI 표시 로직
        
        isGameStarted = false;
    }

    public void OnGameComplete()
    {
        Debug.Log("게임 완료");
        // 게임 완료 UI 표시 로직
        
        isGameStarted = false;
    }

    public void ReturnToTitle()
    {
        Debug.Log("타이틀로 복귀");
        
        // 게임 상태 초기화
        isGameStarted = false;
        
        ShowTitleScreen();
    }

    #endregion

    #region 유틸리티

    public void ToggleUIElement(GameObject element)
    {
        if (element != null)
            element.SetActive(!element.activeSelf);
    }

    public void SetUIElementActive(GameObject element, bool active)
    {
        if (element != null)
            element.SetActive(active);
    }

    #endregion
}
