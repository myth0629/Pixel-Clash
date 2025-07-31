using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// 스테이지 전환 시 배경 스크롤링 효과를 관리
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("스크롤링 설정")]
    [SerializeField] private RectTransform[] backgroundLayers; // 배경 레이어들
    [SerializeField] private float[] scrollSpeeds = { 20f, 40f, 80f }; // 각 레이어별 속도 (패럴랙스)
    [SerializeField] private Vector2 scrollDirection = Vector2.left; // 스크롤 방향

    [Header("스크롤링 동작 설정")]
    [SerializeField] private float scrollDuration = 3f; // 스크롤링 지속 시간
    [SerializeField] private bool scrollOnRoundStart = true; // 라운드 시작 시 스크롤 여부

    [Header("부드러운 움직임 설정")]
    [SerializeField] private bool useSmoothMovement = true; // 부드러운 움직임 사용
    [SerializeField] private float smoothingFactor = 8f; // 부드러움 정도 (높을수록 부드러움)

    [Header("무한 스크롤 설정")]
    [SerializeField] private bool enableInfiniteScroll = true; // 무한 스크롤 활성화
    [SerializeField] private bool useScreenWidthForReset = true; // 화면 너비 기준 리셋
    [SerializeField] private float minResetDistance = 1920f; // 최소 리셋 거리

    // 스크롤 완료 이벤트
    public static event Action OnScrollComplete;

    private bool isScrolling = false;
    private bool isStageTransition = false; // 스테이지 전환 여부
    private Vector2[] originalPositions; // 각 레이어의 원래 위치
    private Vector2[] targetPositions; // 각 레이어의 목표 위치 (부드러운 움직임용)
    private Coroutine scrollCoroutine;

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
        }
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
        
        // 지정된 시간 후 자동으로 정지
        if (scrollDuration > 0)
        {
            StartCoroutine(AutoStopAfterDuration());
        }
        
        Debug.Log($"[{gameObject.name}] 배경 스크롤링 시작 - 레이어 수: {backgroundLayers.Length}, 지속시간: {scrollDuration}초");
    }

    /// <summary>지정된 시간 후 자동 정지</summary>
    private IEnumerator AutoStopAfterDuration()
    {
        yield return new WaitForSeconds(scrollDuration);
        
        if (isScrolling)
        {
            StopScrolling();
            Debug.Log($"[{gameObject.name}] {scrollDuration}초 후 자동 정지");
        }
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

        // 스크롤 완료 이벤트 발생
        Debug.Log($"[{gameObject.name}] 스크롤 완료 이벤트 발생!");
        OnScrollComplete?.Invoke();

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
            }
        }

        Debug.Log($"[{gameObject.name}] 배경 위치 리셋 완료");
    }

    /// <summary>스크롤링 상태 확인</summary>
    public bool IsScrolling => isScrolling;
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
        if (backgroundLayers == null) return;

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
            
            // 목표 위치 계산
            Vector2 movement = scrollDirection.normalized * speed * Time.deltaTime;
            targetPositions[i] += movement;

            // 부드러운 움직임 적용
            if (useSmoothMovement)
            {
                layer.anchoredPosition = Vector2.Lerp(
                    layer.anchoredPosition, 
                    targetPositions[i], 
                    smoothingFactor * Time.deltaTime
                );
            }
            else
            {
                layer.anchoredPosition = targetPositions[i];
            }

            // 디버그 로그 (덜 빈번하게)
            if (Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[{gameObject.name}] Layer[{i}] 위치: {layer.anchoredPosition} (목표: {targetPositions[i]}, 속도: {speed})");
            }

            // 무한 스크롤 처리
            if (enableInfiniteScroll)
            {
                HandleInfiniteScroll(layer, i);
            }
        }
    }

    /// <summary>무한 스크롤 처리</summary>
    private void HandleInfiniteScroll(RectTransform layer, int layerIndex)
    {
        float resetDistance;
        
        if (useScreenWidthForReset)
        {
            // 화면 너비를 기준으로 리셋 지점 계산
            float screenWidth = GetScreenWidth();
            resetDistance = Mathf.Max(screenWidth, minResetDistance);
        }
        else
        {
            // 레이어 너비 기준
            resetDistance = layer.rect.width;
        }
        
        // 왼쪽으로 스크롤하는 경우
        if (scrollDirection.x < 0)
        {
            // 리셋 거리만큼 왼쪽으로 이동했을 때 리셋
            if (layer.anchoredPosition.x <= -resetDistance)
            {
                // 실제 위치와 목표 위치 모두 리셋
                Vector2 resetOffset = new Vector2(resetDistance * 2f, 0);
                layer.anchoredPosition += resetOffset;
                targetPositions[layerIndex] += resetOffset;
                
                Debug.Log($"[{gameObject.name}] Layer[{layerIndex}] 무한스크롤 리셋: {layer.anchoredPosition.x} (ResetDistance: {resetDistance})");
            }
        }
        // 오른쪽으로 스크롤하는 경우
        else if (scrollDirection.x > 0)
        {
            if (layer.anchoredPosition.x >= resetDistance)
            {
                Vector2 resetOffset = new Vector2(-resetDistance * 2f, 0);
                layer.anchoredPosition += resetOffset;
                targetPositions[layerIndex] += resetOffset;
                
                Debug.Log($"[{gameObject.name}] Layer[{layerIndex}] 무한스크롤 리셋: {layer.anchoredPosition.x} (ResetDistance: {resetDistance})");
            }
        }
    }

    /// <summary>화면 너비 가져오기</summary>
    private float GetScreenWidth()
    {
        // Canvas의 RectTransform 너비 사용
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            return canvasRect.rect.width;
        }
        
        // 기본값 반환
        return Screen.width;
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
            targetPositions = new Vector2[backgroundLayers.Length];
            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                if (backgroundLayers[i] != null)
                {
                    originalPositions[i] = backgroundLayers[i].anchoredPosition;
                    targetPositions[i] = backgroundLayers[i].anchoredPosition;
                }
            }
        }
        
        Debug.Log($"[{gameObject.name}] 배경 레이어 설정 완료");
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
    }

    /// <summary>라운드 시작 시 호출</summary>
    private void OnRoundStart(int stage, int round)
    {
        if (!scrollOnRoundStart) return;
        
        Debug.Log($"[{gameObject.name}] Stage {stage}-{round} 시작 - 배경 스크롤링 시작");
        
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
        StartCoroutine(StopScrollingAfterDelay(scrollDuration));
    }

    /// <summary>일정 시간 후 스크롤링 정지</summary>
    private IEnumerator StopScrollingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[{gameObject.name}] {delay}초 후 배경 스크롤링 자동 정지");
        StopScrolling();
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
    #endregion
}
