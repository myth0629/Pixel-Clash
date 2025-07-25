using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ㄴ
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
    
    [Header("파티 슬롯 (고정)")]
    [SerializeField] private Button partySlot1Button; // 전방 슬롯
    [SerializeField] private Button partySlot2Button; // 후방 슬롯
    [SerializeField] private UnityEngine.UI.Image partySlot1Icon;
    [SerializeField] private UnityEngine.UI.Image partySlot2Icon;
    [SerializeField] private TMPro.TextMeshProUGUI partySlot1Text;
    [SerializeField] private TMPro.TextMeshProUGUI partySlot2Text;
    
    [Header("캐릭터 선택")]
    [SerializeField] private GameObject characterSelectionPanel; // 캐릭터 선택 창
    [SerializeField] private Transform availableCharactersContainer; // 선택 가능한 캐릭터 리스트
    [SerializeField] private GameObject characterSelectButtonPrefab; // 캐릭터 선택 버튼 프리팹
    [SerializeField] private Button closeSelectionButton; // 선택창 닫기 버튼
    [SerializeField] private CharacterData[] availableCharacters; // 선택 가능한 캐릭터들

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
    
    // 파티 관리 변수들
    private int selectedSlotIndex = -1; // 현재 선택 중인 파티 슬롯 (-1이면 선택 안됨)
    private List<CharacterData> currentParty = new List<CharacterData>(); // 현재 파티 구성

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
            
        if (closeSelectionButton != null)
            closeSelectionButton.onClick.AddListener(OnCloseSelectionClicked);
            
        // 파티 슬롯 버튼 설정
        if (partySlot1Button != null)
            partySlot1Button.onClick.AddListener(() => OnPartySlotClicked(0));
            
        if (partySlot2Button != null)
            partySlot2Button.onClick.AddListener(() => OnPartySlotClicked(1));
            
        // 초기 파티 설정
        InitializeParty();
    }

    /// <summary>초기 파티 설정</summary>
    private void InitializeParty()
    {
        currentParty.Clear();
        
        // BattleManager에서 기본 파티 정보 가져오기 (기존 테스트 파티)
        if (BattleManager.Instance != null)
        {
            var testParty = BattleManager.Instance.GetTestPartyInfo();
            foreach (var (characterData, level) in testParty)
            {
                if (characterData != null)
                {
                    currentParty.Add(characterData);
                }
            }
        }
        
        // 파티가 비어있으면 기본값으로 채우기
        while (currentParty.Count < 2)
        {
            currentParty.Add(null); // 빈 슬롯
        }
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
        // 고정 슬롯 방식으로 업데이트
        UpdatePartySlot(0, partySlot1Icon, partySlot1Text);
        UpdatePartySlot(1, partySlot2Icon, partySlot2Text);
    }

    /// <summary>개별 파티 슬롯 업데이트</summary>
    private void UpdatePartySlot(int slotIndex, UnityEngine.UI.Image iconImage, TMPro.TextMeshProUGUI nameText)
    {
        if (slotIndex >= currentParty.Count) return;
        
        var character = currentParty[slotIndex];
        
        if (character != null)
        {
            // 캐릭터가 있는 경우
            if (nameText != null)
                nameText.text = $"{character.name} Lv.1";
                
            if (iconImage != null && character.icon != null)
                iconImage.sprite = character.icon;
        }
        else
        {
            // 빈 슬롯인 경우
            if (nameText != null)
                nameText.text = slotIndex == 0 ? "전방\n(클릭하여 선택)" : "후방\n(클릭하여 선택)";
                
            if (iconImage != null)
                iconImage.sprite = null; // 기본 이미지 또는 빈 슬롯 이미지
        }
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
        
        // 파티에 캐릭터가 있는지 확인
        bool hasCharacters = false;
        foreach (var character in currentParty)
        {
            if (character != null)
            {
                hasCharacters = true;
                break;
            }
        }
        
        if (!hasCharacters)
        {
            Debug.LogWarning("파티에 캐릭터가 없습니다. 캐릭터를 선택해주세요.");
            return;
        }
        
        // UI를 먼저 게임 화면으로 전환
        ShowGameUI();
        
        // StageManager를 통해 게임 시작 (현재 파티 정보 사용)
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

    #region 파티 관리

    /// <summary>파티 슬롯 클릭 이벤트</summary>
    private void OnPartySlotClicked(int slotIndex)
    {
        Debug.Log($"파티 슬롯 {slotIndex} 클릭됨");
        selectedSlotIndex = slotIndex;
        ShowCharacterSelection();
    }

    /// <summary>캐릭터 선택창 표시</summary>
    private void ShowCharacterSelection()
    {
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
            UpdateCharacterSelectionList();
        }
    }

    /// <summary>캐릭터 선택창 숨기기</summary>
    private void HideCharacterSelection()
    {
        if (characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);
            
        selectedSlotIndex = -1;
    }

    /// <summary>선택 가능한 캐릭터 리스트 업데이트</summary>
    private void UpdateCharacterSelectionList()
    {
        if (availableCharactersContainer == null || characterSelectButtonPrefab == null) return;

        // 기존 버튼들 제거
        foreach (Transform child in availableCharactersContainer)
        {
            Destroy(child.gameObject);
        }

        // 선택 가능한 캐릭터들의 버튼 생성
        foreach (var character in availableCharacters)
        {
            if (character != null)
            {
                CreateCharacterSelectButton(character);
            }
        }

        // 빈 슬롯 버튼도 추가 (캐릭터 제거용)
        CreateEmptySlotButton();
    }

    /// <summary>캐릭터 선택 버튼 생성</summary>
    private void CreateCharacterSelectButton(CharacterData character)
    {
        var buttonObj = Instantiate(characterSelectButtonPrefab, availableCharactersContainer);
        var button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(() => OnCharacterSelected(character));
        }

        // 이름으로 정확히 찾기
        var nameText = buttonObj.transform.Find("IconContainer/NameText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = character.name;

        var iconImage = buttonObj.transform.Find("IconContainer/Icon")?.GetComponent<UnityEngine.UI.Image>();
        if (iconImage != null && character.icon != null)
        {
            iconImage.sprite = character.icon;
            
            // 이미지 비율 유지 설정
            iconImage.preserveAspect = true;
            iconImage.type = Image.Type.Simple;
        }
    }

    /// <summary>빈 슬롯 버튼 생성 (캐릭터 제거용)</summary>
    private void CreateEmptySlotButton()
    {
        var buttonObj = Instantiate(characterSelectButtonPrefab, availableCharactersContainer);
        var button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(() => OnCharacterSelected(null));
        }

        // 이름으로 정확히 찾기
        var nameText = buttonObj.transform.Find("IconContainer/NameText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = "빈 슬롯";
    }

    /// <summary>캐릭터 선택 완료</summary>
    private void OnCharacterSelected(CharacterData selectedCharacter)
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < currentParty.Count)
        {
            currentParty[selectedSlotIndex] = selectedCharacter;
            UpdatePartyInfo(); // UI 업데이트
            HideCharacterSelection();
            
            Debug.Log($"슬롯 {selectedSlotIndex}에 {(selectedCharacter?.name ?? "빈 슬롯")} 배치");
        }
    }

    /// <summary>선택창 닫기 버튼 클릭</summary>
    private void OnCloseSelectionClicked()
    {
        HideCharacterSelection();
    }

    /// <summary>현재 파티 정보 반환 (BattleManager용)</summary>
    public List<(CharacterData, int)> GetCurrentPartyInfo()
    {
        var partyInfo = new List<(CharacterData, int)>();
        
        foreach (var character in currentParty)
        {
            if (character != null)
            {
                partyInfo.Add((character, 1)); // 레벨은 기본 1로 설정
            }
        }
        
        return partyInfo;
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
