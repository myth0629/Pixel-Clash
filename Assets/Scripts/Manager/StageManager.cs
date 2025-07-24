using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 스테이지와 라운드를 관리하는 매니저
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 설정")]
    [SerializeField] private int currentStage = 1;
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int roundsPerStage = 5;
    [SerializeField] private int enemiesPerRound = 2;
    [SerializeField] private bool autoStartOnLoad = false; // 로드 시 자동 시작 여부

    [Header("스테이지 스케일링")]
    [SerializeField] private float stageHpMultiplier = 1.2f;
    [SerializeField] private float stageAtkMultiplier = 1.15f;
    [SerializeField] private int roundEnemyIncrease = 1; // 라운드당 적 증가수

    // 이벤트
    public static event Action<int, int> OnStageStart; // stage, round
    public static event Action<int, int> OnRoundStart; // stage, round
    public static event Action<int> OnStageComplete;
    public static event Action OnGameComplete;

    // 프로퍼티
    public int CurrentStage => currentStage;
    public int CurrentRound => currentRound;
    public int RoundsPerStage => roundsPerStage;
    public bool IsLastRoundInStage => currentRound >= roundsPerStage;

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
        // 자동 시작이 활성화된 경우에만 첫 번째 스테이지 시작
        if (autoStartOnLoad)
        {
            StartGame();
        }
    }

    #region ▶ 스테이지 관리 ◀
    /// <summary>스테이지 시작</summary>
    public void StartStage(int stageNumber)
    {
        currentStage = stageNumber;
        currentRound = 1;

        Debug.Log($"Stage {currentStage}-{currentRound} 시작!");
        OnStageStart?.Invoke(currentStage, currentRound);

        // 첫 번째 라운드 시작
        StartCoroutine(StartRoundWithDelay(0.5f));
    }

    /// <summary>라운드 시작 (딜레이 포함)</summary>
    public void StartRound()
    {
        StartCoroutine(StartRoundWithDelay(0f));
    }

    private IEnumerator StartRoundWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"Stage {currentStage}-{currentRound} 시작!");
        OnRoundStart?.Invoke(currentStage, currentRound);

        // 전투 시작
        if (BattleManager.Instance != null)
        {
            int enemyCount = GetEnemyCountForCurrentRound();
            // GameUIManager에서 설정한 파티 사용
            BattleManager.Instance.StartBattleWithUIParty(enemyCount);
        }
    }

    /// <summary>라운드 완료</summary>
    public void CompleteRound()
    {
        Debug.Log($"Stage {currentStage}-{currentRound} 완료!");

        if (IsLastRoundInStage)
        {
            CompleteStage();
        }
        else
        {
            // 다음 라운드로
            currentRound++;
            StartCoroutine(StartRoundWithDelay(2f)); // 2초 후 다음 라운드
        }
    }

    /// <summary>스테이지 완료</summary>
    public void CompleteStage()
    {
        Debug.Log($"Stage {currentStage} 완료!");
        OnStageComplete?.Invoke(currentStage);

        // 보상 지급
        GiveStageReward();

        // 다음 스테이지로 이동
        StartCoroutine(MoveToNextStage());
    }

    private IEnumerator MoveToNextStage()
    {
        yield return new WaitForSeconds(3f); // 3초 대기

        currentStage++;
        
        // 최대 스테이지 체크 (예: 10스테이지까지)
        if (currentStage > 10)
        {
            Debug.Log("게임 완료!");
            OnGameComplete?.Invoke();
            
            // GameUIManager에게 알림
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnGameComplete();
            }
        }
        else
        {
            StartStage(currentStage);
        }
    }
    #endregion

    #region ▶ 게임 로직 ◀
    /// <summary>현재 라운드의 적 수 계산</summary>
    public int GetEnemyCountForCurrentRound()
    {
        return enemiesPerRound + (currentRound - 1) * roundEnemyIncrease;
    }

    /// <summary>스테이지에 따른 스탯 배율 계산</summary>
    public (float hpMultiplier, float atkMultiplier) GetStageMultipliers()
    {
        float hp = Mathf.Pow(stageHpMultiplier, currentStage - 1);
        float atk = Mathf.Pow(stageAtkMultiplier, currentStage - 1);
        return (hp, atk);
    }

    /// <summary>스테이지 보상 지급</summary>
    private void GiveStageReward()
    {
        if (GameDataManager.Instance != null)
        {
            int goldReward = 100 * currentStage;
            int expReward = 50 * currentStage;

            GameDataManager.Instance.AddGold(goldReward);
            GameDataManager.Instance.AddExp(expReward);

            Debug.Log($"스테이지 보상: {goldReward} 골드, {expReward} 경험치");
        }
    }

    /// <summary>테스트용 파티 정보 반환</summary>
    private System.Collections.Generic.List<(CharacterData, int)> GetTestParty()
    {
        if (BattleManager.Instance != null)
        {
            return BattleManager.Instance.GetTestPartyInfo();
        }
        
        return new System.Collections.Generic.List<(CharacterData, int)>();
    }
    #endregion

    #region ▶ 공용 메서드 ◀
    /// <summary>게임 시작 (타이틀에서 호출)</summary>
    public void StartGame()
    {
        StartStage(currentStage);
    }

    /// <summary>수동으로 다음 라운드 시작 (디버그용)</summary>
    public void ForceNextRound()
    {
        CompleteRound();
    }

    /// <summary>스테이지 리셋 (디버그용)</summary>
    public void ResetToStage(int stageNumber)
    {
        StartStage(stageNumber);
    }
    #endregion
}
