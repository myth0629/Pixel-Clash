using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 스테이지 전환 시 배경 스크롤링 효과를 관리
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("스크롤링 설정")]
    [SerializeField] private RectTransform[] backgroundLayers; // 배경 레이어들
    [SerializeField] private float[] scrollSpeeds = { 30f, 50f, 80f }; // 각 레이어별 속도 (패럴랙스) - 개선된 스크롤에 맞게 조정
    [SerializeField] private Vector2 scrollDirection = Vector2.left; // 스크롤 방향

    [Header("스크롤링 동작 설정")]
    [SerializeField] private float scrollDuration = 3f; // 스크롤링 지속 시간 (3초)
    [SerializeField] private bool scrollOnRoundStart = false; // 라운드 시작 시 스크롤 여부 (StageManager에서 관리하므로 비활성화)

    [Header("부드러운 움직임 설정")]
    [SerializeField] private bool useSmoothMovement = false; // 개선된 스크롤에서는 비활성화 권장
    [SerializeField] private float smoothingFactor = 8f; // 부드러움 정도 (높을수록 부드러움) - 더 부드럽게 조정

    [Header("무한 스크롤 설정")]
    [SerializeField] private bool enableInfiniteScroll = true; // 무한 스크롤 활성화
    [SerializeField] private float cloneSpacing = 1.0f; // 복제본 간의 간격 배수 (1.0 = 빈틈없음, 1.5 = 1.5배 간격)
    [SerializeField] private bool synchronizeLayerResets = true; // 레이어별 재배치 동기화 여부
    [SerializeField] private float resetDelayMultiplier = 1.0f; // 재배치 지연 배수 (1.0 = 정확한 타이밍, 높을수록 늦게 사라짐)

    // 스크롤 완료 이벤트
    public static event Action OnScrollComplete;

    private bool isScrolling = false;
    private bool isStageTransition = false; // 스테이지 전환 여부
    private Vector2[] originalPositions; // 각 레이어의 원래 위치
    private Vector2[] targetPositions; // 각 레이어의 목표 위치 (부드러운 움직임용)
    private Coroutine scrollCoroutine;
    private Coroutine autoStopCoroutine; // 자동 정지 코루틴 추적
    
    // 개선된 무한 스크롤용 데이터
    private List<RectTransform>[] layerClones; // 각 레이어별 복제된 UI 요소들
    private float[] layerWidths; // 각 레이어의 너비
    private int[] firstIndices; // 각 레이어의 첫 번째 인덱스

    private void Awake()
    {
        // 원래 위치들 저장
        if (backgroundLayers != null)
        {
            originalPositions = new Vector2[backgroundLayers.Length];
            targetPositions = new Vector2[backgroundLayers.Length];
            
            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                if (backgroundLayers[i] != null)
                {
                    originalPositions[i] = backgroundLayers[i].anchoredPosition;
                    targetPositions[i] = backgroundLayers[i].anchoredPosition;
                }
            }
            
            // 개선된 무한 스크롤 초기화
            InitializeAdvancedInfiniteScroll();
        }
    }

    /// <summary>개선된 무한 스크롤 초기화</summary>
    private void InitializeAdvancedInfiniteScroll()
    {
        if (backgroundLayers == null || backgroundLayers.Length == 0) return;

        layerClones = new List<RectTransform>[backgroundLayers.Length];
        layerWidths = new float[backgroundLayers.Length];
        firstIndices = new int[backgroundLayers.Length];

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            if (backgroundLayers[i] == null) continue;

            layerClones[i] = new List<RectTransform>();
            layerWidths[i] = backgroundLayers[i].rect.width;
            firstIndices[i] = 1;

            // 원본 추가
            layerClones[i].Add(backgroundLayers[i]);

            // 복제본 생성 (2개 추가로 총 3개)
            for (int j = 0; j < 2; j++)
            {
                var clone = Instantiate(backgroundLayers[i], backgroundLayers[i].parent);
                clone.name = $"{backgroundLayers[i].name}_Clone_{j + 1}";
                layerClones[i].Add(clone);
            }

            // 초기 배치
            SortLayerImages(i);
        }

        Debug.Log($"[{gameObject.name}] 개선된 무한 스크롤 초기화 완료 - {backgroundLayers.Length}개 레이어");
    }

    /// <summary>레이어 이미지 정렬</summary>
    private void SortLayerImages(int layerIndex)
    {
        if (layerClones[layerIndex] == null) return;

        float width = layerWidths[layerIndex];
        
        // 배치 간격 계산 - 빈틈없는 배치를 위해 정확한 너비 사용
        float actualSpacing = width * cloneSpacing;
        
        // 복제본들을 순서대로 배치 (0: 원본, 1: 오른쪽 복제본, 2: 왼쪽 복제본)
        for (int i = 0; i < layerClones[layerIndex].Count; i++)
        {
            var clone = layerClones[layerIndex][i];
            if (clone != null)
            {
                Vector2 newPos;
                
                if (i == 0)
                {
                    // 원본: 원래 위치
                    newPos = originalPositions[layerIndex];
                }
                else if (i == 1)
                {
                    // 첫 번째 복제본: 원본의 오른쪽
                    newPos = originalPositions[layerIndex] + Vector2.right * actualSpacing;
                }
                else
                {
                    // 두 번째 복제본: 원본의 왼쪽
                    newPos = originalPositions[layerIndex] + Vector2.left * actualSpacing;
                }
                
                clone.anchoredPosition = newPos;
                
                // 목표 위치도 동일하게 설정
                if (i == 0) // 원본
                {
                    targetPositions[layerIndex] = newPos;
                }
            }
        }
        
        Debug.Log($"[{gameObject.name}] Layer[{layerIndex}] 배치완료: 너비={width:F0}, 간격={actualSpacing:F0}, 클론수={layerClones[layerIndex].Count}");
    }

    #region ▶ 공용 메서드 ◀
    /// <summary>배경 스크롤링 시작</summary>
    public void StartScrolling()
    {
        Debug.Log($"[{gameObject.name}] StartScrolling 호출 - isScrolling: {isScrolling}");
        
        if (isScrolling) return;

        // 배경 레이어 확인
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] backgroundLayers가 null이거나 비어있습니다!");
            return;
        }

        isScrolling = true;
        scrollCoroutine = StartCoroutine(ScrollingCoroutine());
        
        // 캐릭터 걷기 애니메이션 시작 (약간의 지연 후)
        StartCoroutine(DelayedSetWalkingAnimation(true));
        
        Debug.Log($"[{gameObject.name}] 배경 스크롤링 시작 - 레이어 수: {backgroundLayers.Length}, 지속시간: {scrollDuration}초");
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
        
        // 코루틴 참조 정리
        autoStopCoroutine = null;
    }

    /// <summary>배경 스크롤링 정지</summary>
    public void StopScrolling()
    {
        Debug.Log($"[{gameObject.name}] StopScrolling 호출 - isScrolling: {isScrolling}");
        
        if (!isScrolling) return;

        isScrolling = false;
        
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
            Debug.Log($"[{gameObject.name}] 스크롤링 코루틴 정지");
        }
        
        // 자동 정지 코루틴도 정리
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
            Debug.Log($"[{gameObject.name}] 자동 정지 코루틴 정리");
        }

        // 스크롤 완료 이벤트 발생
        Debug.Log($"[{gameObject.name}] 스크롤 완료 이벤트 발생!");
        OnScrollComplete?.Invoke();

        // 캐릭터 걷기 애니메이션 종료
        SetCharacterWalkingAnimation(false);

        // 스테이지 전환 플래그 리셋
        isStageTransition = false;

        Debug.Log($"[{gameObject.name}] 배경 스크롤링 정지");
    }

    /// <summary>배경 스크롤링 정지 후 위치 리셋 (스테이지 전환용)</summary>
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
            if (backgroundLayers[i] != null)
            {
                backgroundLayers[i].anchoredPosition = originalPositions[i];
                
                // 목표 위치도 함께 리셋
                if (targetPositions != null && i < targetPositions.Length)
                {
                    targetPositions[i] = originalPositions[i];
                }

                // 개선된 무한 스크롤의 모든 복제본도 리셋
                if (layerClones != null && i < layerClones.Length && layerClones[i] != null)
                {
                    SortLayerImages(i);
                }
            }
        }

        Debug.Log("배경 위치 리셋 완료");
    }

    /// <summary>스크롤링 상태 확인</summary>
    public bool IsScrolling => isScrolling;
    
    /// <summary>캐릭터들의 걷기 애니메이션 제어</summary>
    private void SetCharacterWalkingAnimation(bool isWalking)
    {
        if (BattleManager.Instance == null) 
        {
            Debug.LogWarning("[BackgroundScroller] BattleManager.Instance가 null입니다!");
            return;
        }
        
        int playerCount = 0;
        int enemyCount = 0;
        
        // 모든 플레이어 캐릭터의 걷기 애니메이션 제어
        var players = BattleManager.Instance.GetAllPlayers();
        Debug.Log($"[{gameObject.name}] 플레이어 수: {players.Count}");
        
        foreach (var player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                player.SetWalkingAnimation(isWalking);
                playerCount++;
                Debug.Log($"[{gameObject.name}] {player.name} 플레이어 걷기 애니메이션: {isWalking}");
            }
        }
        
        // 모든 적 캐릭터의 걷기 애니메이션 제어
        var enemies = BattleManager.Instance.GetAllEnemies();
        Debug.Log($"[{gameObject.name}] 적 수: {enemies.Count}");
        
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.SetWalkingAnimation(isWalking);
                enemyCount++;
                Debug.Log($"[{gameObject.name}] {enemy.name} 적 걷기 애니메이션: {isWalking}");
            }
        }
        
        Debug.Log($"[{gameObject.name}] 걷기 애니메이션 적용 완료 - 플레이어: {playerCount}명, 적: {enemyCount}명");
    }
    
    /// <summary>지연된 걷기 애니메이션 설정</summary>
    private IEnumerator DelayedSetWalkingAnimation(bool isWalking)
    {
        // 0.1초 대기 후 애니메이션 설정 (캐릭터 스폰 대기)
        yield return new WaitForSeconds(0.1f);
        SetCharacterWalkingAnimation(isWalking);
    }
    #endregion

    #region ▶ 스크롤링 로직 ◀
    /// <summary>스크롤링 메인 코루틴</summary>
    private IEnumerator ScrollingCoroutine()
    {
        while (isScrolling)
        {
            UpdateScroll();
            yield return null;
        }
    }

    /// <summary>스크롤 업데이트</summary>
    private void UpdateScroll()
    {
        if (backgroundLayers == null || !enableInfiniteScroll) return;

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            var layer = backgroundLayers[i];
            if (layer == null) 
            {
                Debug.LogWarning($"[{gameObject.name}] backgroundLayers[{i}]가 null입니다!");
                continue;
            }

            // 속도 계산 (배열 범위 체크)
            float speed = i < scrollSpeeds.Length ? scrollSpeeds[i] : scrollSpeeds[scrollSpeeds.Length - 1];
            
            // 개선된 무한 스크롤 업데이트
            UpdateAdvancedInfiniteScroll(i, speed);

            // 첫 번째 프레임에서 위치 변화 로그 (덜 빈번하게)
            if (Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[{gameObject.name}] Layer[{i}] 위치: {layer.anchoredPosition} (목표: {targetPositions[i]}, 속도: {speed})");
            }
        }
    }

    /// <summary>개선된 무한 스크롤 업데이트</summary>
    private void UpdateAdvancedInfiniteScroll(int layerIndex, float speed)
    {
        if (layerClones[layerIndex] == null) return;

        float move = Time.deltaTime * speed;
        float width = layerWidths[layerIndex];
        float actualSpacing = width * cloneSpacing;
        
        // 재배치 거리는 정확히 이미지 너비로 설정
        float resetDistance = actualSpacing * resetDelayMultiplier;

        // 모든 복제본 이동
        for (int i = 0; i < layerClones[layerIndex].Count; i++)
        {
            var clone = layerClones[layerIndex][i];
            if (clone == null) continue;

            // 왼쪽으로 스크롤하는 경우 (scrollDirection.x < 0)
            if (scrollDirection.x < 0)
            {
                clone.anchoredPosition += Vector2.left * move;

                // 화면 왼쪽으로 완전히 나간 경우 오른쪽 끝으로 이동
                if (clone.anchoredPosition.x <= originalPositions[layerIndex].x - resetDistance)
                {
                    // 가장 오른쪽에 있는 복제본 찾기
                    float rightmostX = float.MinValue;
                    foreach (var otherClone in layerClones[layerIndex])
                    {
                        if (otherClone != null && otherClone != clone)
                        {
                            rightmostX = Mathf.Max(rightmostX, otherClone.anchoredPosition.x);
                        }
                    }
                    
                    // 가장 오른쪽 복제본의 오른쪽에 정확히 배치
                    clone.anchoredPosition = new Vector2(rightmostX + actualSpacing, clone.anchoredPosition.y);
                    
                    if (Time.frameCount % 180 == 0) // 3초마다 로그
                    {
                        Debug.Log($"[{gameObject.name}] Layer[{layerIndex}] 왼쪽 재배치: X={clone.anchoredPosition.x:F0}, 간격={actualSpacing:F0}");
                    }
                }
            }
            // 오른쪽으로 스크롤하는 경우
            else if (scrollDirection.x > 0)
            {
                clone.anchoredPosition += Vector2.right * move;

                // 화면 오른쪽으로 완전히 나간 경우 왼쪽 끝으로 이동
                if (clone.anchoredPosition.x >= originalPositions[layerIndex].x + resetDistance)
                {
                    // 가장 왼쪽에 있는 복제본 찾기
                    float leftmostX = float.MaxValue;
                    foreach (var otherClone in layerClones[layerIndex])
                    {
                        if (otherClone != null && otherClone != clone)
                        {
                            leftmostX = Mathf.Min(leftmostX, otherClone.anchoredPosition.x);
                        }
                    }
                    
                    // 가장 왼쪽 복제본의 왼쪽에 정확히 배치
                    clone.anchoredPosition = new Vector2(leftmostX - actualSpacing, clone.anchoredPosition.y);
                    
                    if (Time.frameCount % 180 == 0) // 3초마다 로그
                    {
                        Debug.Log($"[{gameObject.name}] Layer[{layerIndex}] 오른쪽 재배치: X={clone.anchoredPosition.x:F0}, 간격={actualSpacing:F0}");
                    }
                }
            }
        }

        // 원본 레이어 위치 업데이트 (다른 시스템과의 호환성)
        if (layerClones[layerIndex].Count > 0 && layerClones[layerIndex][0] != null)
        {
            targetPositions[layerIndex] = layerClones[layerIndex][0].anchoredPosition;
            backgroundLayers[layerIndex].anchoredPosition = targetPositions[layerIndex];
        }
    }

    #endregion

    #region ▶ 설정 변경 ◀
    /// <summary>스크롤 속도 설정</summary>
    public void SetScrollSpeeds(params float[] speeds)
    {
        if (speeds != null && speeds.Length > 0)
        {
            scrollSpeeds = speeds;
        }
    }

    /// <summary>스크롤 방향 설정</summary>
    public void SetScrollDirection(Vector2 direction)
    {
        scrollDirection = direction.normalized;
    }

    /// <summary>배경 레이어 설정</summary>
    public void SetBackgroundLayers(params RectTransform[] layers)
    {
        backgroundLayers = layers;
        
        // 원래 위치들 다시 저장
        if (backgroundLayers != null)
        {
            originalPositions = new Vector2[backgroundLayers.Length];
            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                if (backgroundLayers[i] != null)
                    originalPositions[i] = backgroundLayers[i].anchoredPosition;
            }
        }
    }
    #endregion

    #region ▶ 이벤트 ◀
    private void OnEnable()
    {
        // StageManager 이벤트 구독 - 라운드 시작과 스테이지 전환에 반응
        Debug.Log($"[{gameObject.name}] BackgroundScroller - StageManager 이벤트 구독");
        StageManager.OnRoundStart += OnRoundStart;
        StageManager.OnStageTransitionStart += OnStageTransitionStart;
        StageManager.OnRoundTransitionStart += OnRoundTransitionStart; // 라운드 전환 이벤트 추가
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        Debug.Log($"[{gameObject.name}] BackgroundScroller - StageManager 이벤트 구독 해제");
        StageManager.OnRoundStart -= OnRoundStart;
        StageManager.OnStageTransitionStart -= OnStageTransitionStart;
        StageManager.OnRoundTransitionStart -= OnRoundTransitionStart; // 라운드 전환 이벤트 해제
    }

    /// <summary>스테이지 전환 시작 시 호출</summary>
    private void OnStageTransitionStart(int stageNumber)
    {
        Debug.Log($"[{gameObject.name}] 스테이지 전환 시작 - Stage {stageNumber} - 배경 스크롤링 시작");
        
        // 배경 레이어 확인
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] backgroundLayers가 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"[{gameObject.name}] 배경 레이어 개수: {backgroundLayers.Length}");
        
        // 스테이지 전환 플래그 설정
        isStageTransition = true;
        
        // 스테이지 전환 시에는 배경 위치 리셋 후 스크롤 시작
        ResetPositions();
        StartScrolling();
        
        // 지정된 시간 후 자동으로 정지
        if (scrollDuration > 0)
        {
            // 기존 자동 정지 코루틴이 있다면 정리
            if (autoStopCoroutine != null)
            {
                StopCoroutine(autoStopCoroutine);
            }
            autoStopCoroutine = StartCoroutine(AutoStopAfterDuration());
        }
    }

    /// <summary>라운드 전환 시작 시 호출 (배경 리셋 없음)</summary>
    private void OnRoundTransitionStart(int stageNumber)
    {
        Debug.Log($"[{gameObject.name}] 라운드 전환 시작 - Stage {stageNumber} - 배경 스크롤링 시작 (리셋 없음)");
        
        // 배경 레이어 확인
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] backgroundLayers가 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"[{gameObject.name}] 배경 레이어 개수: {backgroundLayers.Length}");
        
        // 라운드 전환 플래그 설정 (스테이지 전환이 아님)
        isStageTransition = false;
        
        // 라운드 전환 시에는 배경 위치 리셋 없이 바로 스크롤 시작
        StartScrolling();
        
        // 지정된 시간 후 자동으로 정지
        if (scrollDuration > 0)
        {
            // 기존 자동 정지 코루틴이 있다면 정리
            if (autoStopCoroutine != null)
            {
                StopCoroutine(autoStopCoroutine);
            }
            autoStopCoroutine = StartCoroutine(AutoStopAfterDuration());
        }
    }

    /// <summary>라운드 시작 시 호출</summary>
    private void OnRoundStart(int stage, int round)
    {
        if (!scrollOnRoundStart) return;
        
        Debug.Log($"[{gameObject.name}] Stage {stage}-{round} 시작 - 배경 스크롤링 시작 (OnRoundStart)");
        
        // 배경 레이어 확인
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] backgroundLayers가 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"[{gameObject.name}] 배경 레이어 개수: {backgroundLayers.Length}");
        
        // 라운드 시작 플래그 설정 (스테이지 전환이 아님)
        isStageTransition = false;
        
        StartScrolling();
        
        // 라운드 시작 시에도 자동 정지 (스크롤 완료 이벤트 발생)
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
        }
        autoStopCoroutine = StartCoroutine(StopScrollingAfterDelay(scrollDuration));
    }

    /// <summary>일정 시간 후 스크롤링 정지</summary>
    private IEnumerator StopScrollingAfterDelay(float delay)
    {
        Debug.Log($"[{gameObject.name}] 지연 정지 타이머 시작: {delay}초");
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[{gameObject.name}] {delay}초 후 배경 스크롤링 자동 정지");
        StopScrolling();
        
        // 코루틴 참조 정리
        autoStopCoroutine = null;
        // 라운드 진행 중에는 배경 위치를 유지 (ResetPositions 제거)
        // 스테이지 전환 시에만 수동으로 리셋 호출
    }
    #endregion

    #region ▶ 디버그 ◀
    /// <summary>테스트용 스크롤링 토글</summary>
    [ContextMenu("Toggle Scrolling")]
    public void ToggleScrolling()
    {
        if (isScrolling)
            StopScrolling();
        else
            StartScrolling();
    }

    /// <summary>테스트용 위치 리셋</summary>
    [ContextMenu("Reset Positions")]
    public void TestResetPositions()
    {
        ResetPositions();
    }

    /// <summary>무한 스크롤 재초기화</summary>
    [ContextMenu("Reinitialize Infinite Scroll")]
    public void ReinitializeInfiniteScroll()
    {
        InitializeAdvancedInfiniteScroll();
        Debug.Log($"[{gameObject.name}] 무한 스크롤 재초기화 완료");
    }

    /// <summary>복제본 간격 조정 테스트</summary>
    [ContextMenu("Test Clone Spacing")]
    public void TestCloneSpacing()
    {
        cloneSpacing = cloneSpacing == 1.5f ? 2.0f : 1.5f;
        InitializeAdvancedInfiniteScroll();
        Debug.Log($"[{gameObject.name}] 복제본 간격: {cloneSpacing}");
    }

    /// <summary>레이어 동기화 토글</summary>
    [ContextMenu("Toggle Layer Synchronization")]
    public void ToggleLayerSynchronization()
    {
        synchronizeLayerResets = !synchronizeLayerResets;
        InitializeAdvancedInfiniteScroll();
        Debug.Log($"[{gameObject.name}] 레이어 동기화: {synchronizeLayerResets}");
    }
    #endregion
}
