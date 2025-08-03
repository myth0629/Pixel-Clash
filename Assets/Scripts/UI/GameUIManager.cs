using System.Collections.Generic;
using System;
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
    [SerializeField] private Button shopButton;  // 상점 버튼 추가
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

    [Header("상점")]
    [SerializeField] private GameObject shopPanel; // 상점 패널
    [SerializeField] private Transform shopCharacterContainer; // 상점 캐릭터 리스트 컨테이너 (Scroll View의 Content)
    [SerializeField] private GameObject shopCharacterItemPrefab; // 상점 캐릭터 아이템 프리팹
    [SerializeField] private Button closeShopButton; // 상점 닫기 버튼
    [SerializeField] private TMPro.TextMeshProUGUI shopGoldText; // 상점에서 보여줄 골드 텍스트
    [SerializeField] private CharacterData[] shopCharacters; // 상점에서 판매할 캐릭터들
    
    [Header("테스트 기능")]
    [SerializeField] private Button testRefundButton; // 테스트용 구매 취소 버튼
    [SerializeField] private GameObject testPanel; // 테스트 기능 패널

    [Header("구매 확인 팝업")]
    [SerializeField] private GameObject purchaseConfirmPanel;  // 구매 확인 팝업
    [SerializeField] private TMPro.TextMeshProUGUI confirmMessageText;  // 확인 메시지
    [SerializeField] private TMPro.TextMeshProUGUI confirmCharacterNameText;  // 캐릭터 이름
    [SerializeField] private TMPro.TextMeshProUGUI confirmPriceText;  // 가격 텍스트
    [SerializeField] private Image confirmCharacterIcon;  // 캐릭터 아이콘
    [SerializeField] private Button confirmPurchaseButton;  // 확인 버튼
    [SerializeField] private Button cancelPurchaseButton;   // 취소 버튼

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
    
    // 상점 관리 변수들
    private HashSet<string> unlockedCharacters = new HashSet<string>(); // 해금된 캐릭터 이름들
    private CharacterData pendingPurchaseCharacter; // 구매 대기 중인 캐릭터

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
        InitializeDefaultParty(); // 기본 파티 설정
        LoadUnlockedCharacters(); // 해금된 캐릭터 로드
        
        // 골드 변경 이벤트 구독
        if (GameDataManager.Instance != null)
        {
            GameDataManager.OnGoldChanged += OnGoldChanged;
        }
        
        if (showTitleOnStart)
        {
            ShowTitleScreen();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameDataManager.Instance != null)
        {
            GameDataManager.OnGoldChanged -= OnGoldChanged;
        }
    }

    /// <summary>골드 변경 시 호출</summary>
    private void OnGoldChanged(int newGold)
    {
        // 상점이 열려있으면 골드 표시 업데이트
        if (shopPanel != null && shopPanel.activeSelf)
        {
            UpdateShopGoldDisplay();
            RefreshShopItemsAffordability();
        }
    }

    /// <summary>상점 아이템들의 구매 가능 여부만 업데이트</summary>
    private void RefreshShopItemsAffordability()
    {
        if (shopCharacterContainer == null) return;

        // 모든 ShopCharacterItem 컴포넌트를 찾아서 RefreshItem 호출
        var shopItems = shopCharacterContainer.GetComponentsInChildren<MonoBehaviour>();
        foreach (var item in shopItems)
        {
            if (item.GetType().Name == "ShopCharacterItem")
            {
                var refreshMethod = item.GetType().GetMethod("RefreshItem");
                refreshMethod?.Invoke(item, null);
            }
        }
    }

    private void SetupButtons()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopClicked);
        
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
            {
                iconImage.sprite = character.icon;
                // 투명도 복원 (캐릭터가 있으면 불투명)
                var color = iconImage.color;
                color.a = 1f;
                iconImage.color = color;
            }
        }
        else
        {
            // 빈 슬롯인 경우
            if (nameText != null)
                nameText.text = "클릭하여 선택";
                
            if (iconImage != null)
            {
                iconImage.sprite = null; // 스프라이트 제거
                // 투명하게 만들기
                var color = iconImage.color;
                color.a = 0f;
                iconImage.color = color;
            }
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

        // 선택 가능한 캐릭터들의 버튼 생성 (해금된 캐릭터만)
        CharacterData[] unlockedChars = GetUnlockedCharacters();
        foreach (var character in unlockedChars)
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
    
    /// <summary>기본 파티 초기화</summary>
    private void InitializeDefaultParty()
    {
        // currentParty가 비어있고 availableCharacters가 있으면 첫 번째 캐릭터로 기본 파티 설정
        if (currentParty.Count == 0 && availableCharacters != null && availableCharacters.Length > 0)
        {
            // 첫 번째 사용 가능한 캐릭터를 첫 번째 슬롯에 추가
            var firstCharacter = availableCharacters[0];
            if (firstCharacter != null)
            {
                // currentParty를 적절한 크기로 초기화
                while (currentParty.Count < 2)
                {
                    currentParty.Add(null);
                }
                
                currentParty[0] = firstCharacter;
                Debug.Log($"기본 파티 설정: {firstCharacter.name}");
                
                // UI 업데이트
                UpdatePartyInfo();
            }
        }
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

    #region 상점 시스템

    /// <summary>상점 버튼 클릭 시 호출</summary>
    private void OnShopClicked()
    {
        Debug.Log("상점 열기");
        ShowShopScreen();
    }

    /// <summary>상점 화면 표시</summary>
    private void ShowShopScreen()
    {
        // 모든 패널 비활성화
        if (titlePanel != null) titlePanel.SetActive(false);
        //if (preparePanel != null) preparePanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);

        // 상점 패널 활성화
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            SetupShopButtons();
            RefreshShopItems();
            UpdateShopGoldDisplay();
        }
    }

    /// <summary>상점 버튼들 설정</summary>
    private void SetupShopButtons()
    {
        if (closeShopButton != null)
        {
            closeShopButton.onClick.RemoveAllListeners();
            closeShopButton.onClick.AddListener(CloseShop);
        }

        // 구매 확인 팝업 버튼들 설정
        if (confirmPurchaseButton != null)
        {
            confirmPurchaseButton.onClick.RemoveAllListeners();
            confirmPurchaseButton.onClick.AddListener(OnConfirmPurchase);
        }

        if (cancelPurchaseButton != null)
        {
            cancelPurchaseButton.onClick.RemoveAllListeners();
            cancelPurchaseButton.onClick.AddListener(OnCancelPurchase);
        }

        // 구매 확인 팝업 초기 상태 설정
        if (purchaseConfirmPanel != null)
            purchaseConfirmPanel.SetActive(false);

        // 테스트 버튼 설정
        if (testRefundButton != null)
        {
            testRefundButton.onClick.RemoveAllListeners();
            testRefundButton.onClick.AddListener(ShowTestRefundMenu);
        }
    }

    /// <summary>상점 닫기</summary>
    private void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
        
        ShowTitleScreen();
    }

    /// <summary>상점 아이템 목록 새로고침</summary>
    private void RefreshShopItems()
    {
        if (shopCharacterContainer == null || shopCharacters == null) return;

        // 기존 아이템들 제거
        foreach (Transform child in shopCharacterContainer)
        {
            Destroy(child.gameObject);
        }

        // 상점 캐릭터들을 순회하며 아이템 생성
        foreach (var character in shopCharacters)
        {
            if (character == null) continue;
            CreateShopCharacterItem(character);
        }
    }

    /// <summary>상점 캐릭터 아이템 생성</summary>
    private void CreateShopCharacterItem(CharacterData character)
    {
        if (shopCharacterItemPrefab == null) return;

        GameObject itemObject = Instantiate(shopCharacterItemPrefab, shopCharacterContainer);
        var shopItem = itemObject.GetComponent("ShopCharacterItem");
        
        if (shopItem != null)
        {
            bool isUnlocked = IsCharacterUnlocked(character);
            // Reflection을 사용하여 SetupItem 메서드 호출
            var setupMethod = shopItem.GetType().GetMethod("SetupItem");
            if (setupMethod != null)
            {
                setupMethod.Invoke(shopItem, new object[] { character, isUnlocked, (Action<CharacterData>)OnPurchaseCharacter });
            }
        }
    }

    /// <summary>캐릭터 구매 버튼 클릭 시 호출 - 확인 팝업 표시</summary>
    private void OnPurchaseCharacter(CharacterData character)
    {
        if (character == null || IsCharacterUnlocked(character)) return;

        // 구매 대기 캐릭터 설정
        pendingPurchaseCharacter = character;

        // 구매 확인 팝업 표시
        ShowPurchaseConfirmPopup(character);
    }

    /// <summary>구매 확인 팝업 표시</summary>
    private void ShowPurchaseConfirmPopup(CharacterData character)
    {
        if (character == null || purchaseConfirmPanel == null) return;

        // 캐릭터 정보 표시
        if (confirmCharacterNameText != null)
            confirmCharacterNameText.text = character.displayName;

        if (confirmPriceText != null)
            confirmPriceText.text = $"{character.unlockCost} 골드";

        if (confirmCharacterIcon != null && character.icon != null)
            confirmCharacterIcon.sprite = character.icon;

        if (confirmMessageText != null)
            confirmMessageText.text = $"'{character.displayName}' 캐릭터를 구매하시겠습니까?";

        // 골드 부족 체크
        bool canAfford = GameDataManager.Instance != null && 
                        GameDataManager.Instance.CurrentGold >= character.unlockCost;
        
        if (confirmPurchaseButton != null)
            confirmPurchaseButton.interactable = canAfford;

        // 팝업 표시
        purchaseConfirmPanel.SetActive(true);
    }

    /// <summary>구매 확인 버튼 클릭</summary>
    private void OnConfirmPurchase()
    {
        if (pendingPurchaseCharacter == null) return;

        int price = pendingPurchaseCharacter.unlockCost;
        
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGold >= price)
        {
            // 골드 차감
            GameDataManager.Instance.SpendGold(price);
            
            // 캐릭터 해금
            UnlockCharacter(pendingPurchaseCharacter);
            
            // UI 새로고침
            RefreshShopItems();
            UpdateShopGoldDisplay();
            
            Debug.Log($"캐릭터 '{pendingPurchaseCharacter.name}' 구매 완료! (가격: {price} 골드)");
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
        }

        // 팝업 닫기
        ClosePurchaseConfirmPopup();
    }

    /// <summary>구매 취소 버튼 클릭</summary>
    private void OnCancelPurchase()
    {
        ClosePurchaseConfirmPopup();
    }

    /// <summary>구매 확인 팝업 닫기</summary>
    private void ClosePurchaseConfirmPopup()
    {
        if (purchaseConfirmPanel != null)
            purchaseConfirmPanel.SetActive(false);

        pendingPurchaseCharacter = null;
    }

    /// <summary>캐릭터 해금</summary>
    private void UnlockCharacter(CharacterData character)
    {
        if (character == null) return;
        
        unlockedCharacters.Add(character.name);
        SaveUnlockedCharacters();
    }

    /// <summary>캐릭터 해금 여부 확인</summary>
    private bool IsCharacterUnlocked(CharacterData character)
    {
        if (character == null) return false;
        return unlockedCharacters.Contains(character.name);
    }

    /// <summary>상점 골드 표시 업데이트</summary>
    private void UpdateShopGoldDisplay()
    {
        if (shopGoldText != null && GameDataManager.Instance != null)
        {
            shopGoldText.text = $"{GameDataManager.Instance.GetFormattedGold()} G";
        }
    }

    /// <summary>해금된 캐릭터 저장</summary>
    private void SaveUnlockedCharacters()
    {
        string unlockedList = string.Join(",", unlockedCharacters);
        PlayerPrefs.SetString("UnlockedCharacters", unlockedList);
        PlayerPrefs.Save();
    }

    /// <summary>해금된 캐릭터 로드</summary>
    private void LoadUnlockedCharacters()
    {
        string unlockedList = PlayerPrefs.GetString("UnlockedCharacters", "");
        unlockedCharacters.Clear();
        
        if (!string.IsNullOrEmpty(unlockedList))
        {
            string[] characterNames = unlockedList.Split(',');
            foreach (string name in characterNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    unlockedCharacters.Add(name);
                }
            }
        }
        
        // 기본 캐릭터는 항상 해금상태로 설정
        if (availableCharacters != null && availableCharacters.Length > 0)
        {
            unlockedCharacters.Add(availableCharacters[0].name);
        }
    }

    /// <summary>해금된 캐릭터만 반환 (파티 선택에서 사용)</summary>
    public CharacterData[] GetUnlockedCharacters()
    {
        if (availableCharacters == null) return new CharacterData[0];
        
        List<CharacterData> unlocked = new List<CharacterData>();
        foreach (var character in availableCharacters)
        {
            if (character != null && IsCharacterUnlocked(character))
            {
                unlocked.Add(character);
            }
        }
        
        return unlocked.ToArray();
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

    #region ▶ 테스트 기능 ◀

    /// <summary>테스트용 구매 취소 메뉴 표시</summary>
    private void ShowTestRefundMenu()
    {
        if (unlockedCharacters.Count == 0)
        {
            Debug.Log("테스트: 구매 취소할 캐릭터가 없습니다.");
            return;
        }

        Debug.Log("=== 테스트: 구매 취소 메뉴 ===");
        for (int i = 0; i < shopCharacters.Length; i++)
        {
            var character = shopCharacters[i];
            if (IsCharacterUnlocked(character))
            {
                Debug.Log($"{i + 1}. {character.displayName} - {character.unlockCost} 골드 환불 가능");
            }
        }
        
        Debug.Log("Unity 콘솔에서 TestRefundCharacterByIndex(인덱스) 메서드를 호출하여 환불하세요.");
        Debug.Log("예: TestRefundCharacterByIndex(0) - 첫 번째 해금된 캐릭터 환불");
    }

    /// <summary>테스트용: 인덱스로 캐릭터 구매 취소</summary>
    public void TestRefundCharacterByIndex(int index)
    {
        if (shopCharacters == null || index < 0 || index >= shopCharacters.Length)
        {
            Debug.LogError($"테스트: 잘못된 인덱스입니다. (0-{shopCharacters.Length - 1} 사이의 값을 입력하세요)");
            return;
        }

        var character = shopCharacters[index];
        TestRefundCharacter(character);
    }

    /// <summary>테스트용: 캐릭터 구매 취소 및 골드 환불</summary>
    public void TestRefundCharacter(CharacterData character)
    {
        if (character == null)
        {
            Debug.LogError("테스트: 캐릭터 데이터가 null입니다.");
            return;
        }

        if (!IsCharacterUnlocked(character))
        {
            Debug.Log($"테스트: '{character.displayName}' 캐릭터는 구매하지 않았습니다.");
            return;
        }

        // 캐릭터 잠금
        LockCharacter(character);
        
        // 골드 환불
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.RefundCharacterPurchase(character.unlockCost);
        }

        // UI 새로고침
        RefreshShopItems();
        UpdateShopGoldDisplay();
        
        Debug.Log($"테스트: '{character.displayName}' 구매 취소 완료! {character.unlockCost} 골드 환불됨");
    }

    /// <summary>테스트용: 캐릭터 잠금</summary>
    private void LockCharacter(CharacterData character)
    {
        if (character != null && unlockedCharacters.Contains(character.name))
        {
            unlockedCharacters.Remove(character.name);
            
            // PlayerPrefs에서도 제거
            string key = $"Character_Unlocked_{character.name}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>테스트용: 모든 구매 취소</summary>
    public void TestRefundAllCharacters()
    {
        if (unlockedCharacters.Count == 0)
        {
            Debug.Log("테스트: 구매 취소할 캐릭터가 없습니다.");
            return;
        }

        int totalRefund = 0;
        var charactersToRefund = new List<string>(unlockedCharacters);
        
        foreach (var characterName in charactersToRefund)
        {
            // 상점 캐릭터 목록에서 해당 캐릭터 찾기
            var character = System.Array.Find(shopCharacters, c => c.name == characterName);
            if (character != null)
            {
                totalRefund += character.unlockCost;
                LockCharacter(character);
            }
        }

        // 골드 환불
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.RefundCharacterPurchase(totalRefund);
        }

        // UI 새로고침
        RefreshShopItems();
        UpdateShopGoldDisplay();
        
        Debug.Log($"테스트: 모든 캐릭터 구매 취소 완료! 총 {totalRefund} 골드 환불됨");
    }

    #endregion
}
