using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>배경 스크롤링 시스템 - 리팩토링 버전</summary>
public class BackgroundScroller : MonoBehaviour
{
    #region Inspector Fields
    [Header("▶ 배경 레이어 설정")]
    [SerializeField] private RectTransform[] backgroundLayers;
    [SerializeField] private float[] layerSpeeds = { 50f, 75f, 100f };
    
    [Header("▶ 스크롤 설정")]
    [SerializeField] private float scrollDuration = 3f;
    [SerializeField] private Vector2 scrollDirection = Vector2.left;
    [SerializeField] private bool enableInfiniteScroll = true;
    
    [Header("▶ 무한 스크롤 고급 설정")]
    [SerializeField, Range(0.5f, 2f)] private float cloneSpacing = 1f;
    [SerializeField, Range(1f, 5f)] private float resetDelayMultiplier = 1.5f;
    #endregion

    #region Events
    public static event Action OnScrollComplete;
    #endregion

    #region Private Fields
    // 상태
    private bool isScrolling = false;
    private bool isStageTransition = false;
    
    // 위치 관리
    private Vector2[] originalPositions;
    private Vector2[] targetPositions;
    
    // 코루틴
    private Coroutine scrollCoroutine;
    private Coroutine autoStopCoroutine;
    
    // 무한 스크롤 시스템
    private InfiniteScrollData[] scrollData;
    #endregion

    #region Infinite Scroll Data Structure
    [System.Serializable]
    private class InfiniteScrollData
    {
        public List<RectTransform> clones = new List<RectTransform>();
        public float width;
        public int firstIndex = 0;

        public InfiniteScrollData()
        {
            clones = new List<RectTransform>();
            width = 0f;
            firstIndex = 0;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializePositions();
        InitializeInfiniteScroll();
    }

    private void Update()
    {
        HandleScrollingState();
    }

    private void OnEnable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.OnRoundStart += HandleRoundStart;
            StageManager.OnStageComplete += HandleStageComplete;
        }
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.OnRoundStart -= HandleRoundStart;
            StageManager.OnStageComplete -= HandleStageComplete;
        }
    }
    #endregion

    #region Initialization
    /// <summary>위치 초기화</summary>
    private void InitializePositions()
    {
        if (backgroundLayers == null || backgroundLayers.Length == 0) return;

        originalPositions = new Vector2[backgroundLayers.Length];
        targetPositions = new Vector2[backgroundLayers.Length];

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            if (backgroundLayers[i] != null)
            {
                originalPositions[i] = backgroundLayers[i].anchoredPosition;
                targetPositions[i] = originalPositions[i];
            }
        }

        Debug.Log($"[{gameObject.name}] 배경 위치 초기화 완료 - 레이어 수: {backgroundLayers.Length}");
    }

    /// <summary>무한 스크롤 시스템 초기화</summary>
    private void InitializeInfiniteScroll()
    {
        if (!enableInfiniteScroll || backgroundLayers == null || backgroundLayers.Length == 0) return;

        InitializeAdvancedInfiniteScroll();
    }
    #endregion

    #region Event Handlers
    /// <summary>라운드 시작 핸들러</summary>
    private void HandleRoundStart(int stage, int round)
    {
        Debug.Log($"[{gameObject.name}] 라운드 시작: {stage}-{round}");
        StartScrolling(true);
    }

    /// <summary>스테이지 완료 핸들러</summary>
    private void HandleStageComplete(int stage)
    {
        Debug.Log($"[{gameObject.name}] 스테이지 완료: {stage}");
        isStageTransition = true;
        StopScrollingAndReset();
    }
    #endregion

    #region State Management
    /// <summary>스크롤링 상태 관리</summary>
    private void HandleScrollingState()
    {
        if (isStageTransition) return;
        
        if (isScrolling && ShouldStopScrolling())
        {
            Debug.Log($"[{gameObject.name}] 게임이 중단되어 스크롤을 정지합니다.");
            StopScrolling();
        }
    }

    /// <summary>스크롤링을 중단해야 하는지 확인</summary>
    private bool ShouldStopScrolling()
    {
        if (BattleManager.Instance == null) return false;
        if (BattleManager.Instance.IsBattleRunning) return false;
        
        return StageManager.Instance == null;
    }
    #endregion

    #region Public API
    /// <summary>배경 스크롤링 시작</summary>
    public void StartScrolling() => StartScrolling(false);
    
    /// <summary>배경 스크롤링 시작</summary>
    /// <param name="forceStart">true면 BattleManager 상태와 무관하게 강제 시작</param>
    public void StartScrolling(bool forceStart)
    {
        Debug.Log($"[{gameObject.name}] StartScrolling 호출 - isScrolling: {isScrolling}, forceStart: {forceStart}");
        
        if (!ValidateScrollingStart(forceStart) || isScrolling) return;
        if (!ValidateBackgroundLayers()) return;

        isScrolling = true;
        scrollCoroutine = StartCoroutine(ScrollingCoroutine());
        
        StartCoroutine(DelayedSetWalkingAnimation(true));
        
        Debug.Log($"[{gameObject.name}] 배경 스크롤링 시작 - 레이어 수: {backgroundLayers.Length}, 지속시간: {scrollDuration}초");
    }

    /// <summary>배경 스크롤링 정지</summary>
    public void StopScrolling()
    {
        Debug.Log($"[{gameObject.name}] StopScrolling 호출 - isScrolling: {isScrolling}");
        
        if (!isScrolling) return;

        CleanupScrolling();
        TriggerScrollComplete();
        
        Debug.Log($"[{gameObject.name}] 배경 스크롤링 정지");
    }

    /// <summary>배경 스크롤링 정지 후 위치 리셋</summary>
    public void StopScrollingAndReset()
    {
        StopScrolling();
        ResetPositions();
        Debug.Log($"[{gameObject.name}] 배경 스크롤링 정지 및 위치 리셋 완료");
    }

    /// <summary>배경 위치 리셋</summary>
    public void ResetPositions()
    {
        if (backgroundLayers == null || originalPositions == null) return;

        for (int i = 0; i < backgroundLayers.Length && i < originalPositions.Length; i++)
        {
            ResetLayerPosition(i);
        }

        Debug.Log("배경 위치 리셋 완료");
    }

    /// <summary>스크롤링 상태 확인</summary>
    public bool IsScrolling => isScrolling;
    #endregion

    #region Character Animation
    /// <summary>캐릭터 걷기 애니메이션 설정</summary>
    private void SetCharacterWalkingAnimation(bool isWalking)
    {
        if (BattleManager.Instance == null) 
        {
            Debug.LogWarning("[BackgroundScroller] BattleManager.Instance가 null입니다!");
            return;
        }
        
        int playerCount = 0;
        int enemyCount = 0;
        
        var players = BattleManager.Instance.GetAllPlayers();
        foreach (var player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                player.SetWalkingAnimation(isWalking);
                playerCount++;
            }
        }
        
        var enemies = BattleManager.Instance.GetAllEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.SetWalkingAnimation(isWalking);
                enemyCount++;
            }
        }
        
        Debug.Log($"[{gameObject.name}] 걷기 애니메이션 적용 완료 - 플레이어: {playerCount}명, 적: {enemyCount}명");
    }
    
    /// <summary>지연된 걷기 애니메이션 설정</summary>
    private IEnumerator DelayedSetWalkingAnimation(bool isWalking)
    {
        yield return new WaitForSeconds(0.1f);
        SetCharacterWalkingAnimation(isWalking);
    }
    #endregion

    #region Private Helpers
    /// <summary>스크롤링 시작 검증</summary>
    private bool ValidateScrollingStart(bool forceStart)
    {
        if (!forceStart && BattleManager.Instance != null && !BattleManager.Instance.IsBattleRunning)
        {
            Debug.Log($"[{gameObject.name}] 게임이 실행 중이 아니므로 스크롤을 시작하지 않습니다.");
            return false;
        }
        return true;
    }

    /// <summary>배경 레이어 검증</summary>
    private bool ValidateBackgroundLayers()
    {
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] backgroundLayers가 null이거나 비어있습니다!");
            return false;
        }
        return true;
    }

    /// <summary>스크롤링 정리</summary>
    private void CleanupScrolling()
    {
        isScrolling = false;
        
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
            Debug.Log($"[{gameObject.name}] 스크롤링 코루틴 정지");
        }
        
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
            Debug.Log($"[{gameObject.name}] 자동 정지 코루틴 정리");
        }
    }

    /// <summary>스크롤 완료 트리거</summary>
    private void TriggerScrollComplete()
    {
        Debug.Log($"[{gameObject.name}] 스크롤 완료 이벤트 발생!");
        OnScrollComplete?.Invoke();

        SetCharacterWalkingAnimation(false);
        isStageTransition = false;
    }

    /// <summary>레이어 위치 리셋</summary>
    private void ResetLayerPosition(int layerIndex)
    {
        if (backgroundLayers[layerIndex] != null)
        {
            backgroundLayers[layerIndex].anchoredPosition = originalPositions[layerIndex];
            
            if (targetPositions != null && layerIndex < targetPositions.Length)
            {
                targetPositions[layerIndex] = originalPositions[layerIndex];
            }

            if (scrollData != null && layerIndex < scrollData.Length && scrollData[layerIndex].clones != null)
            {
                SortLayerImages(layerIndex);
            }
        }
    }

    /// <summary>지정된 시간 후 자동 정지</summary>
    private IEnumerator AutoStopAfterDuration()
    {
        Debug.Log($"[{gameObject.name}] 자동 정지 타이머 시작: {scrollDuration}초");
        yield return new WaitForSeconds(scrollDuration);
        
        if (isScrolling)
        {
            Debug.Log($"[{gameObject.name}] {scrollDuration}초 후 자동 정지 실행");
            StopScrolling();
        }
        
        autoStopCoroutine = null;
    }
    #endregion

    #region Infinite Scroll Management
    /// <summary>개선된 무한 스크롤 시스템 초기화</summary>
    private void InitializeAdvancedInfiniteScroll()
    {
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogWarning("배경 레이어가 설정되지 않았습니다.");
            return;
        }

        scrollData = new InfiniteScrollData[backgroundLayers.Length];

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            scrollData[i] = new InfiniteScrollData();
            InitializeScrollLayer(i);
        }

        Debug.Log($"개선된 무한 스크롤 시스템 초기화 완료 - 레이어 수: {backgroundLayers.Length}");
    }

    /// <summary>개별 스크롤 레이어 초기화</summary>
    private void InitializeScrollLayer(int layerIndex)
    {
        var layer = backgroundLayers[layerIndex];
        if (layer == null) return;

        var scrollLayer = scrollData[layerIndex];
        scrollLayer.width = CalculateLayerWidth(layer);
        scrollLayer.clones = new List<RectTransform>();
        scrollLayer.firstIndex = 0;

        CreateInitialClones(layerIndex);
        
        Debug.Log($"레이어 {layerIndex} 초기화 완료 - 너비: {scrollLayer.width}, 복제본 수: {scrollLayer.clones.Count}");
    }

    /// <summary>레이어 너비 계산</summary>
    private float CalculateLayerWidth(RectTransform layer)
    {
        var imageComponent = layer.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent?.sprite != null)
        {
            var sprite = imageComponent.sprite;
            return sprite.bounds.size.x * layer.localScale.x;
        }
        
        return layer.rect.width;
    }

    /// <summary>초기 복제본 생성</summary>
    private void CreateInitialClones(int layerIndex)
    {
        var layer = backgroundLayers[layerIndex];
        var scrollLayer = scrollData[layerIndex];
        
        float canvasWidth = GetCanvasWidth();
        int clonesNeeded = Mathf.CeilToInt(canvasWidth / scrollLayer.width) + 2;

        for (int i = 0; i < clonesNeeded; i++)
        {
            CreateClone(layerIndex, i);
        }
    }

    /// <summary>복제본 생성</summary>
    private RectTransform CreateClone(int layerIndex, int cloneIndex)
    {
        var originalLayer = backgroundLayers[layerIndex];
        var scrollLayer = scrollData[layerIndex];
        
        var clone = Instantiate(originalLayer, originalLayer.parent);
        clone.name = $"{originalLayer.name}_Clone_{cloneIndex}";
        clone.anchoredPosition = new Vector2(scrollLayer.width * cloneIndex, originalLayer.anchoredPosition.y);
        
        scrollLayer.clones.Add(clone);
        return clone;
    }

    /// <summary>캔버스 너비 획득</summary>
    private float GetCanvasWidth()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas?.worldCamera != null)
        {
            return Screen.width / canvas.scaleFactor;
        }
        return Screen.width;
    }

    /// <summary>레이어 이미지 정렬</summary>
    private void SortLayerImages(int layerIndex)
    {
        if (scrollData == null || layerIndex >= scrollData.Length || scrollData[layerIndex].clones == null) return;

        float width = scrollData[layerIndex].width;

        for (int i = 0; i < scrollData[layerIndex].clones.Count; i++)
        {
            var clone = scrollData[layerIndex].clones[i];
            if (clone == null) continue;

            float currentX = clone.anchoredPosition.x;
            float newX = currentX;

            if (currentX <= -width)
            {
                float rightmostX = currentX;
                foreach (var otherClone in scrollData[layerIndex].clones)
                {
                    if (otherClone != null && otherClone != clone)
                    {
                        rightmostX = Mathf.Max(rightmostX, otherClone.anchoredPosition.x);
                    }
                }
                newX = rightmostX + width;
            }
            else if (currentX >= width * 2)
            {
                float leftmostX = currentX;
                foreach (var otherClone in scrollData[layerIndex].clones)
                {
                    if (otherClone != null && otherClone != clone)
                    {
                        leftmostX = Mathf.Min(leftmostX, otherClone.anchoredPosition.x);
                    }
                }
                newX = leftmostX - width;
            }

            if (Mathf.Abs(newX - currentX) > 0.1f)
            {
                clone.anchoredPosition = new Vector2(newX, clone.anchoredPosition.y);
            }
        }
    }
    #endregion

    #region Scrolling Logic
    /// <summary>스크롤링 메인 코루틴</summary>
    private IEnumerator ScrollingCoroutine()
    {
        autoStopCoroutine = StartCoroutine(AutoStopAfterDuration());
        
        while (isScrolling)
        {
            UpdateScroll();
            yield return null;
        }
    }

    /// <summary>스크롤 업데이트</summary>
    private void UpdateScroll()
    {
        if (backgroundLayers == null) return;

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            var layer = backgroundLayers[i];
            if (layer == null) continue;

            float speed = i < layerSpeeds.Length ? layerSpeeds[i] : layerSpeeds[layerSpeeds.Length - 1];

            if (enableInfiniteScroll)
            {
                UpdateAdvancedInfiniteScroll(i, speed);
            }
            else
            {
                UpdateSimpleScroll(layer, speed);
            }
        }
    }

    /// <summary>개선된 무한 스크롤 업데이트</summary>
    private void UpdateAdvancedInfiniteScroll(int layerIndex, float speed)
    {
        if (scrollData == null || layerIndex >= scrollData.Length || scrollData[layerIndex].clones == null) return;

        float move = Time.deltaTime * speed;
        float width = scrollData[layerIndex].width;
        float actualSpacing = width * cloneSpacing;
        float resetDistance = actualSpacing * resetDelayMultiplier;

        // 원본 레이어도 함께 스크롤
        var originalLayer = backgroundLayers[layerIndex];
        if (originalLayer != null)
        {
            if (scrollDirection.x < 0)
            {
                originalLayer.anchoredPosition += Vector2.left * move;
                
                // 원본이 화면 밖으로 나가면 가장 오른쪽으로 이동
                if (originalLayer.anchoredPosition.x <= originalPositions[layerIndex].x - resetDistance)
                {
                    float rightmostX = float.MinValue;
                    foreach (var clone in scrollData[layerIndex].clones)
                    {
                        if (clone != null)
                        {
                            rightmostX = Mathf.Max(rightmostX, clone.anchoredPosition.x);
                        }
                    }
                    originalLayer.anchoredPosition = new Vector2(rightmostX + actualSpacing, originalLayer.anchoredPosition.y);
                }
            }
            else if (scrollDirection.x > 0)
            {
                originalLayer.anchoredPosition += Vector2.right * move;
                
                // 원본이 화면 밖으로 나가면 가장 왼쪽으로 이동
                if (originalLayer.anchoredPosition.x >= originalPositions[layerIndex].x + resetDistance)
                {
                    float leftmostX = float.MaxValue;
                    foreach (var clone in scrollData[layerIndex].clones)
                    {
                        if (clone != null)
                        {
                            leftmostX = Mathf.Min(leftmostX, clone.anchoredPosition.x);
                        }
                    }
                    originalLayer.anchoredPosition = new Vector2(leftmostX - actualSpacing, originalLayer.anchoredPosition.y);
                }
            }
        }

        // 복제본들도 함께 스크롤
        for (int i = 0; i < scrollData[layerIndex].clones.Count; i++)
        {
            var clone = scrollData[layerIndex].clones[i];
            if (clone == null) continue;

            if (scrollDirection.x < 0)
            {
                clone.anchoredPosition += Vector2.left * move;

                if (clone.anchoredPosition.x <= originalPositions[layerIndex].x - resetDistance)
                {
                    float rightmostX = originalLayer.anchoredPosition.x;
                    foreach (var otherClone in scrollData[layerIndex].clones)
                    {
                        if (otherClone != null && otherClone != clone)
                        {
                            rightmostX = Mathf.Max(rightmostX, otherClone.anchoredPosition.x);
                        }
                    }
                    
                    clone.anchoredPosition = new Vector2(rightmostX + actualSpacing, clone.anchoredPosition.y);
                }
            }
            else if (scrollDirection.x > 0)
            {
                clone.anchoredPosition += Vector2.right * move;

                if (clone.anchoredPosition.x >= originalPositions[layerIndex].x + resetDistance)
                {
                    float leftmostX = originalLayer.anchoredPosition.x;
                    foreach (var otherClone in scrollData[layerIndex].clones)
                    {
                        if (otherClone != null && otherClone != clone)
                        {
                            leftmostX = Mathf.Min(leftmostX, otherClone.anchoredPosition.x);
                        }
                    }
                    
                    clone.anchoredPosition = new Vector2(leftmostX - actualSpacing, clone.anchoredPosition.y);
                }
            }
        }
    }

    /// <summary>단순 스크롤 업데이트</summary>
    private void UpdateSimpleScroll(RectTransform layer, float speed)
    {
        float move = Time.deltaTime * speed;
        layer.anchoredPosition += scrollDirection * move;
    }
    #endregion
}
