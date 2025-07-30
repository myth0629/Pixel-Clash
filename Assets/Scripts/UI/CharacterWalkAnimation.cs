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

    [Header("애니메이터 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkStateName = "IsWalk"; // 걷기 Bool 파라미터명

    private Vector3 originalPosition;
    private bool isWalking = false;
    private Coroutine walkCoroutine;

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
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        Debug.Log($"[{gameObject.name}] StageManager 이벤트 구독 해제");
        StageManager.OnStageTransitionStart -= OnStageTransitionStart;
        StageManager.OnStageTransitionComplete -= OnStageTransitionComplete;
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

        // 원래 위치로 복원
        transform.localPosition = originalPosition;
        Debug.Log($"[{gameObject.name}] 원래 위치로 복원: {originalPosition}");
    }

    /// <summary>걷기 상태 확인</summary>
    public bool IsWalking => isWalking;
    #endregion

    #region ▶ 애니메이션 코루틴 ◀
    /// <summary>걷기 애니메이션 메인 코루틴</summary>
    private IEnumerator WalkAnimationCoroutine()
    {
        float timeOffset = Random.Range(0f, Mathf.PI * 2f); // 캐릭터마다 다른 오프셋

        while (isWalking)
        {
            // 위치 애니메이션 (물리적 움직임)
            UpdateWalkPosition(timeOffset);
            
            yield return null;
        }
    }

    /// <summary>걷기 위치 업데이트</summary>
    private void UpdateWalkPosition(float timeOffset)
    {
        float time = Time.time + timeOffset;
        
        // 상하 움직임 (바운싱) - X 위치는 고정하고 Y축만 움직임
        float bobOffset = Mathf.Sin(time * walkBobSpeed) * walkBobAmount;
        
        // X축은 원래 위치 유지, Y축만 바운싱 효과 적용
        Vector3 newPosition = new Vector3(originalPosition.x, originalPosition.y + bobOffset, originalPosition.z);
        transform.localPosition = newPosition;
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

    #region ▶ 디버그 ◀
    /// <summary>테스트용 걻기 토글</summary>
    [ContextMenu("Toggle Walking")]
    public void ToggleWalking()
    {
        if (isWalking)
            StopWalking();
        else
            StartWalking();
    }
    #endregion
}
