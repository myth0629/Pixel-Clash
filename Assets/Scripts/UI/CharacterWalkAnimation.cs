using UnityEngine;
using System.Collections;

/// <summary>
/// 스테이지 전환 시 캐릭터들의 걷기 애니메이션을 관리
/// Animator Controller를 통한 스프라이트 애니메이션 + 물리적 움직임 조합
/// </summary>
public class CharacterWalkAnimation : MonoBehaviour
{
    [Header("걷기 애니메이션 설정")]
    [SerializeField] private float walkBobSpeed = 2f;     // 상하 움직임 속도
    [SerializeField] private float walkBobAmount = 0.1f;  // 상하 움직임 크기 (X 위치는 전투 위치 고정)

    [Header("재생 시간 설정")]
    [Tooltip("true면 지정된 시간(walkDuration) 후 자동으로 걷기 애니메이션을 정지합니다.")]
    [SerializeField] private bool autoStopOnDuration = true;
    [Tooltip("걷기 애니메이션을 재생할 시간(초)")]
    [SerializeField] private float walkDuration = 1.5f;

    [Header("애니메이터 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkStateName = "IsWalk"; // 걷기 Bool 파라미터명

    private Vector3 originalPosition; // 원래 위치 저장
    private bool isWalking = false;
    private Coroutine walkCoroutine;
    private Coroutine autoStopCoroutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        // StageManager 이벤트 구독
        Debug.Log($"[{gameObject.name}] StageManager 이벤트 구독");
        StageManager.OnStageTransitionStart += OnStageTransitionStart;
        StageManager.OnStageTransitionComplete += OnStageTransitionComplete;
        StageManager.OnRoundStart += OnRoundStart; // 라운드 시작 시에도 동기화

        // BackgroundScroller 스크롤 완료 이벤트 구독 (정확한 정지 타이밍 동기화)
        BackgroundScroller.OnScrollComplete += OnBackgroundScrollComplete;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        Debug.Log($"[{gameObject.name}] StageManager 이벤트 구독 해제");
        StageManager.OnStageTransitionStart -= OnStageTransitionStart;
        StageManager.OnStageTransitionComplete -= OnStageTransitionComplete;
        StageManager.OnRoundStart -= OnRoundStart;
        BackgroundScroller.OnScrollComplete -= OnBackgroundScrollComplete;
    }

    #region ▶ 이벤트 핸들러 ◀
    /// <summary>스테이지 전환 시작 시 호출</summary>
    private void OnStageTransitionStart(int nextStage)
    {
        Debug.Log($"[{gameObject.name}] 스테이지 {nextStage}로 전환 - 캐릭터 걷기 애니메이션 시작");
        StartWalking();
    }

    /// <summary>스테이지 전환 완료 시 호출</summary>
    private void OnStageTransitionComplete(int nextStage)
    {
        Debug.Log($"[{gameObject.name}] 스테이지 {nextStage} 전환 완료 - 캐릭터 걷기 애니메이션 정지");
        StopWalking();
    }

    /// <summary>라운드 시작 시 호출</summary>
    private void OnRoundStart(int stage, int round)
    {
        Debug.Log($"[{gameObject.name}] Stage {stage}-{round} 라운드 시작 - 캐릭터 걷기 애니메이션 시작");
        StartWalking();
    }

    /// <summary>배경 스크롤 완료 시 호출</summary>
    private void OnBackgroundScrollComplete()
    {
        Debug.Log($"[{gameObject.name}] 배경 스크롤 완료 - 캐릭터 걷기 애니메이션 정지");
        StopWalking();
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>걷기 애니메이션 시작</summary>
    public void StartWalking()
    {
        Debug.Log($"[{gameObject.name}] StartWalking 호출 - isWalking: {isWalking}");
        
        if (isWalking) return;

        isWalking = true;
        
        // 애니메이터 트리거 실행
        if (animator != null)
        {
            Debug.Log($"[{gameObject.name}] 애니메이터 트리거 실행: {walkStateName}");
            animator.SetBool(walkStateName, true);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Animator가 null입니다!");
        }
        
        walkCoroutine = StartCoroutine(WalkAnimationCoroutine());
        Debug.Log($"[{gameObject.name}] 걷기 코루틴 시작");

        // 지정 시간 후 자동 정지 (옵션)
        if (autoStopOnDuration && walkDuration > 0f)
        {
            if (autoStopCoroutine != null)
            {
                StopCoroutine(autoStopCoroutine);
            }
            autoStopCoroutine = StartCoroutine(AutoStopAfter(walkDuration));
        }
    }

    /// <summary>걷기 애니메이션 정지</summary>
    public void StopWalking()
    {
        Debug.Log($"[{gameObject.name}] StopWalking 호출 - isWalking: {isWalking}");
        
        if (!isWalking) return;

        isWalking = false;
        
        // 애니메이터 트리거 실행
        if (animator != null)
        {
            Debug.Log($"[{gameObject.name}] 애니메이터 정지: {walkStateName} = false");
            animator.SetBool(walkStateName, false);
        }
        
        if (walkCoroutine != null)
        {
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
            Debug.Log($"[{gameObject.name}] 걷기 코루틴 정지");
        }
        // Transform 위치는 더 이상 변경하지 않음 (XY 이동 로직 제거)

        // 자동 정지 타이머 정리
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
        }
    }

    /// <summary>걷기 상태 확인</summary>
    public bool IsWalking => isWalking;
    #endregion

    #region ▶ 애니메이션 코루틴 ◀
    /// <summary>걷기 애니메이션 메인 코루틴 (위치 변경 없음)</summary>
    private IEnumerator WalkAnimationCoroutine()
    {
        while (isWalking)
        {
            yield return null;
        }
    }

    /// <summary>지정된 시간 후 자동 정지</summary>
    private IEnumerator AutoStopAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isWalking)
        {
            Debug.Log($"[{gameObject.name}] 자동 정지: {duration}초 경과");
            StopWalking();
        }
    }
    #endregion

    #region ▶ 유틸리티 ◀
    /// <summary>애니메이터 설정</summary>
    public void SetAnimator(Animator characterAnimator)
    {
        animator = characterAnimator;
    }

    /// <summary>애니메이션 설정 변경</summary>
    public void SetAnimationSettings(float bobSpeed, float bobAmount)
    {
        walkBobSpeed = bobSpeed;
        walkBobAmount = bobAmount;
        // 좌우 움직임 설정은 제거됨 - X 위치 고정
    }
    #endregion
}
