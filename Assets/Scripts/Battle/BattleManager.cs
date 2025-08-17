using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// 전투 씬 매니저 (간략 버전):
///  - 파티·적 리스트 관리
///  - 타깃 쿼리 제공
///  - 웨이브 클리어 판정
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private Transform playerSpawnRoot;
    [SerializeField] private Transform enemySpawnRoot;
    [Tooltip("파티 캐릭터가 2명 이상일 때 좌우로 배치할 간격")]
    [SerializeField] private float playerCharacterSpacing = 2f;
    [Tooltip("적 몬스터가 2명 이상일 때 좌우로 배치할 간격")]
    [SerializeField] private float enemyCharacterSpacing = 2f;
    [SerializeField] private GameObject enemyPrefab; // 호환성을 위해 유지
    [SerializeField] private MonsterData[] monsterDataList; // 새로운 몬스터 데이터 배열

    [Header("전투 시작 설정")]
    [SerializeField] private float battleStartDelay = 3f;  // 모든 캐릭터가 동시에 사용할 전투 시작 딜레이

    [SerializeField] private HealthBarUI healthBarPrefab; // 인스펙터에 프리팹 연결
    [SerializeField] private Transform uiRoot;            // 월드 스페이스 Canvas 루트
    [SerializeField] private GameObject GameOverPanel;

    [Header("VFX / Heal")]
    [Tooltip("힐 사용 시 재생할 VFX 프리팹 (Particle/Animator 모두 가능)")]
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private Vector3 healEffectOffset = new Vector3(0f, 1f, 0f);
    [Tooltip("이펙트에 파티클이 없을 때 기본 파괴 시간(초)")]
    [SerializeField] private float healEffectAutoDestroyTime = 2f;

    // ---------------- Test Mode ----------------
    [Header("Test Mode (Play‑Mode Quick Test)")]
    [Tooltip("Play 버튼을 누르면 즉시 StartBattle()을 호출합니다.")]
    [SerializeField] private bool autoStartOnPlay = true;

    [Tooltip("테스트용 파티 캐릭터들 (최대 3명 추천)")]
    [SerializeField] private List<CharacterData> testPartyCharacters = new();

    [Tooltip("각 캐릭터의 레벨. 개수가 부족하면 1레벨로 처리합니다.")]
    [SerializeField] private List<int> testPartyLevels = new();

    [Tooltip("스폰될 적 몬스터 수")]
    [SerializeField] private int testWaveEnemyCount = 3;

    public bool IsBattleRunning { get; private set; }

    private readonly List<PlayerCharacter> _players = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<HealthBarUI> _healthBars = new(); // 체력바 추적용 리스트

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // StageManager가 있으면 StageManager가 전투를 관리하도록 함
        if (StageManager.Instance != null) return;
        
        // StageManager가 없을 때만 기존 로직 실행
        if (!autoStartOnPlay) return;

        var partyInfo = new List<(CharacterData, int)>();
        for (int i = 0; i < testPartyCharacters.Count; i++)
        {
            var cd = testPartyCharacters[i];
            if (cd == null) continue;

            int level = (i < testPartyLevels.Count) ? testPartyLevels[i] : 1;
            level = Mathf.Max(1, level); // 최소 1레벨 보장

            partyInfo.Add((cd, level));
        }

        if (partyInfo.Count == 0)
        {
            Debug.LogWarning("BattleManager: autoStartOnPlay가 켜졌지만 testPartyCharacters가 비어 있습니다.");
            return;
        }

        StartBattle(partyInfo, testWaveEnemyCount);
    }

    #region ▶ 전투 시작 ◀
    public void StartBattle(List<(CharacterData, int)> partyInfo, int waveEnemyCount)
    {
        IsBattleRunning = true;

        // 기존 전투 정리
        ClearBattle();

        SpawnPlayers(partyInfo);
        SpawnWave(waveEnemyCount);
        
        // 모든 캐릭터들이 동시에 전투를 시작하도록 딜레이 적용
        StartCoroutine(StartBattleAfterDelay());
    }

    /// <summary>GameUIManager에서 설정한 파티로 전투 시작</summary>
    public void StartBattleWithUIParty(int waveEnemyCount = 3)
    {
        List<(CharacterData, int)> partyInfo;
        
        // GameUIManager에서 파티 정보 가져오기
        if (GameUIManager.Instance != null)
        {
            partyInfo = GameUIManager.Instance.GetCurrentPartyInfo();
            Debug.Log($"GameUIManager 파티 정보: {partyInfo.Count}명");
        }
        else
        {
            partyInfo = new List<(CharacterData, int)>();
            Debug.LogWarning("GameUIManager.Instance가 null입니다!");
        }
        
        // 파티가 비어있으면 전투 시작하지 않음 (GameUIManager에서 이미 검증해야 함)
        if (partyInfo.Count == 0)
        {
            Debug.LogError("파티가 비어있습니다! 전투를 시작할 수 없습니다. GameUIManager에서 파티 구성을 확인하세요.");
            return;
        }
        
        Debug.Log($"최종 파티 정보: {partyInfo.Count}명으로 전투 시작");
        
        // 기존 플레이어들이 있다면 새로운 라운드로 간주
        bool isNewRound = _players.Count > 0;
        if (isNewRound)
        {
            Debug.Log("새로운 라운드 시작 - 죽은 캐릭터 부활 및 체력 회복");
            
            // 기존 살아있는 플레이어들 체력 회복
            foreach (var player in _players)
            {
                if (player != null)
                {
                    player.StartNewRound();
                }
            }
            
            // 죽은 캐릭터들 부활 (파티 정보와 현재 플레이어 비교)
            ReviveDeadCharacters(partyInfo);
            
            // 적들만 제거하고 새로운 웨이브 생성
            ClearEnemiesOnly();
            SpawnWave(waveEnemyCount);
            IsBattleRunning = true;  // 새로운 라운드에서 전투 상태 활성화
            Debug.Log("새로운 라운드 - 전투 상태 활성화");
            
            // 새로운 라운드에서도 통합 딜레이 적용
            StartCoroutine(StartBattleAfterDelay());
        }
        else
        {
            // 첫 번째 라운드면 모든 것을 새로 시작
            StartBattle(partyInfo, waveEnemyCount);
        }
    }

    /// <summary>통합 전투 시작 딜레이 코루틴</summary>
    private IEnumerator StartBattleAfterDelay()
    {
        Debug.Log($"모든 캐릭터들이 {battleStartDelay}초 후 동시에 전투 시작");
        
        yield return new WaitForSeconds(battleStartDelay);
        
        // 모든 플레이어들의 전투 시작
        foreach (var player in _players)
        {
            if (player != null)
            {
                player.StartCombat();
            }
        }
        
        // 모든 적들의 전투 시작  
        foreach (var enemy in _enemies)
        {
            if (enemy != null)
            {
                enemy.StartCombat();
            }
        }
        
        Debug.Log("모든 캐릭터들의 전투 시작!");
    }

    /// <summary>기존 전투 정리</summary>
    private void ClearBattle()
    {
        // 기존 체력바들 제거
        foreach (var healthBar in _healthBars)
        {
            if (healthBar != null)
                Destroy(healthBar.gameObject);
        }
        _healthBars.Clear();

        // 기존 플레이어들 제거
        foreach (var player in _players)
        {
            if (player != null)
                Destroy(player.gameObject);
        }
        _players.Clear();

        // 기존 적들 제거
        foreach (var enemy in _enemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        _enemies.Clear();

        // 게임오버 패널 비활성화
        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);
    }

    /// <summary>적들만 제거 (새로운 라운드용)</summary>
    private void ClearEnemiesOnly()
    {
        // 기존 적들 제거 (적의 체력바는 Enemy가 사라질 때 자동으로 정리됨)
        foreach (var enemy in _enemies)
        {
            if (enemy != null)
            {
                // Enemy에 연결된 체력바도 함께 제거하기 위해 
                // 체력바 리스트에서 해당 Enemy와 연결된 것들을 찾아서 제거
                for (int i = _healthBars.Count - 1; i >= 0; i--)
                {
                    if (_healthBars[i] != null && _healthBars[i].gameObject.name.Contains("Enemy"))
                    {
                        Destroy(_healthBars[i].gameObject);
                        _healthBars.RemoveAt(i);
                    }
                }
                
                Destroy(enemy.gameObject);
            }
        }
        _enemies.Clear();
    }

    /// <summary>죽은 캐릭터들을 부활시킴</summary>
    private void ReviveDeadCharacters(List<(CharacterData, int)> partyInfo)
    {
        // 현재 살아있는 플레이어들의 CharacterData 확인
        var aliveCharacterDatas = new HashSet<CharacterData>();
        foreach (var player in _players)
        {
            if (player != null && player.data != null)
            {
                aliveCharacterDatas.Add(player.data);
            }
        }
        
        // 파티 정보에 있지만 현재 없는 캐릭터들을 부활시킴
        for (int slotIndex = 0; slotIndex < partyInfo.Count; slotIndex++)
        {
            var (characterData, level) = partyInfo[slotIndex];
            
            if (characterData != null && !aliveCharacterDatas.Contains(characterData))
            {
                Debug.Log($"캐릭터 부활: {characterData.displayName} (레벨 {level})");
                
                // 캐릭터 부활 (새로 생성) - SpawnPlayers와 동일한 방식
                var go = Instantiate(characterData.prefab, playerSpawnRoot);
                
                // 전방/후방 배치 로직
                bool isFrontRow = slotIndex == 0;
                float xOffset = isFrontRow ? 0f : -playerCharacterSpacing;
                go.transform.localPosition = new Vector3(xOffset, 0, 0);
                
                var pc = go.AddComponent<PlayerCharacter>();
                pc.Setup(characterData, level);
                
                // 위치 설정 (애니메이션 상태 적용)
                PositionType position = isFrontRow ? PositionType.Front : PositionType.Back;
                pc.SetPosition(position);
                
                // 체력바 생성
                var bar = Instantiate(healthBarPrefab, uiRoot);
                bar.Init(pc);
                _healthBars.Add(bar);
                
                pc.OnDeath += OnPlayerDead;
                _players.Add(pc);
                
                Debug.Log($"[{pc.gameObject.name}] 부활 완료 - 포지션: {(isFrontRow ? "전방" : "후방")}");
            }
        }
    }

    private void SpawnPlayers(List<(CharacterData, int)> party)
    {
        int partyCount = Mathf.Min(party.Count, 2); // 최대 2명까지만

        for (int i = 0; i < partyCount; i++)
        {
            var (cd, level) = party[i];
            var go = Instantiate(cd.prefab, playerSpawnRoot);

            // 전방/후방 배치 로직 (전방1, 후방1)
            bool isFrontRow = i == 0; // 첫 번째는 전방, 두 번째는 후방
            float xOffset = isFrontRow ? 0f : -playerCharacterSpacing; // 후방은 우측으로 spacing만큼 이동
            
            go.transform.localPosition = new Vector3(xOffset, 0, 0);

            var pc = go.AddComponent<PlayerCharacter>();
            pc.Setup(cd, level);

            // 위치 설정 (애니메이션 상태 적용)
            PositionType position = isFrontRow ? PositionType.Front : PositionType.Back;
            pc.SetPosition(position);

            // ▼ HP 바 생성 & 초기화
            var bar = Instantiate(healthBarPrefab, uiRoot);
            bar.Init(pc);
            _healthBars.Add(bar); // 체력바를 리스트에 추가

            pc.OnDeath += OnPlayerDead;
            _players.Add(pc);
        }
    }

    private void SpawnWave(int count)
    {
        int enemyCount = Mathf.Min(count, 2); // 최대 2명까지만

        // 스테이지/라운드별 몬스터 풀 조회 (없으면 로컬 monsterDataList 사용)
        MonsterData[] monsterPool = null;
        if (StageManager.Instance != null)
        {
            monsterPool = StageManager.Instance.GetMonsterPoolForCurrentRound();
        }
        if ((monsterPool == null || monsterPool.Length == 0) && monsterDataList != null && monsterDataList.Length > 0)
        {
            monsterPool = monsterDataList;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            // MonsterData가 있으면 사용, 없으면 기본 enemyPrefab 사용
            GameObject prefabToSpawn = enemyPrefab;
            MonsterData dataToUse = null;
            
            if (monsterPool != null && monsterPool.Length > 0)
            {
                // 랜덤하게 몬스터 데이터 선택
                dataToUse = monsterPool[UnityEngine.Random.Range(0, monsterPool.Length)];
                if (dataToUse.prefab != null)
                    prefabToSpawn = dataToUse.prefab;
            }

            var go = Instantiate(prefabToSpawn, enemySpawnRoot);

            // 전방/후방 배치 로직 (전방1, 후방1)
            bool isFrontRow = i == 0; // 첫 번째는 전방, 두 번째는 후방
            float xOffset = isFrontRow ? 0f : enemyCharacterSpacing; // 후방은 좌측으로 spacing만큼 이동
            
            Vector3 finalPosition = new Vector3(xOffset, 0, 0);

            var enemy = go.GetComponent<Enemy>();

            // MonsterData가 있으면 사용, 없으면 기존 방식
            if (dataToUse != null)
            {
                // 스테이지 배율 적용
                if (StageManager.Instance != null)
                {
                    var (stageHpMult, stageAtkMult) = StageManager.Instance.GetStageMultipliers();
                    
                    // 웨이브 스케일링 먼저 적용 (현재 라운드 사용)
                    int currentWave = StageManager.Instance.CurrentRound;
                    (int baseHp, int baseAtk) = dataToUse.GetScaledStats(currentWave);
                    
                    // 스테이지 배율 추가 적용
                    int finalHp = Mathf.RoundToInt(baseHp * stageHpMult);
                    int finalAtk = Mathf.RoundToInt(baseAtk * stageAtkMult);
                    
                    // MonsterData와 함께 Setup 호출 (보상 지급을 위해)
                    enemy.Setup(dataToUse, currentWave);
                    
                    // 스케일링된 스탯 재설정
                    enemy.InitStats(finalHp, finalAtk, dataToUse.attackInterval);
                    
                    Debug.Log($"Stage {StageManager.Instance.CurrentStage}-{currentWave} Enemy: " +
                              $"Base({baseHp}/{baseAtk}) → Final({finalHp}/{finalAtk}) " +
                              $"(Wave: {currentWave}, Stage Multipliers: {stageHpMult:F2}x/{stageAtkMult:F2}x)");
                }
                else
                {
                    // StageManager가 없으면 기본 웨이브 스케일링만 (웨이브 1로 시작)
                    enemy.Setup(dataToUse, 1);
                    Debug.Log($"Wave 1 Enemy (No StageManager): MonsterData scaling only");
                }
            }
            else
            {
                // 기존 방식 (호환성)에도 스테이지 배율 적용
                int baseHp = 30 + 10 * count;
                int baseAtk = 5 + 3 * count;
                
                if (StageManager.Instance != null)
                {
                    var (stageHpMult, stageAtkMult) = StageManager.Instance.GetStageMultipliers();
                    baseHp = Mathf.RoundToInt(baseHp * stageHpMult);
                    baseAtk = Mathf.RoundToInt(baseAtk * stageAtkMult);
                }
                
                enemy.Setup(baseHp, baseAtk);
            }

            // 몬스터 등장 애니메이션 시작 (화면 밖에서 걸어들어옴)
            enemy.StartWalkInAnimation(finalPosition);

            // ▼ HP 바 생성 & 초기화
            var bar = Instantiate(healthBarPrefab, uiRoot);
            bar.Init(enemy);
            _healthBars.Add(bar); // 체력바를 리스트에 추가

            enemy.OnDeath += OnEnemyDead;
            _enemies.Add(enemy);
        }
    }
    #endregion

    #region ▶ 타깃 헬퍼 ◀
    public Enemy GetNearestEnemy(Vector3 pos)
    {
        Enemy closest = null;
        float minSqr = float.MaxValue;
        bool foundFrontRow = false;

        // 1차: 전방 적들만 체크
        foreach (var e in _enemies)
        {
            if (e == null) continue;
            
            // 전방 판정 (x좌표가 0에 가까운 적들)
            bool isFrontRowEnemy = Mathf.Abs(e.transform.localPosition.x) < 0.1f;
            
            if (isFrontRowEnemy)
            {
                foundFrontRow = true;
                float d = (e.transform.position - pos).sqrMagnitude;
                if (d < minSqr) { minSqr = d; closest = e; }
            }
        }

        // 전방에 적이 없으면 후방 적들 중에서 선택
        if (!foundFrontRow)
        {
            foreach (var e in _enemies)
            {
                if (e == null) continue;
                float d = (e.transform.position - pos).sqrMagnitude;
                if (d < minSqr) { minSqr = d; closest = e; }
            }
        }

        return closest;
    }

    public PlayerCharacter GetRandomAlivePlayer()
    {
        var alive = _players.FindAll(p => p != null);
        if (alive.Count == 0) return null;
        
        // 전방 플레이어 우선 선택
        var frontRowPlayers = alive.FindAll(p => Mathf.Abs(p.transform.localPosition.x) < 0.1f);
        if (frontRowPlayers.Count > 0)
        {
            return frontRowPlayers[Random.Range(0, frontRowPlayers.Count)];
        }
        
        // 전방에 없으면 후방에서 선택
        return alive[Random.Range(0, alive.Count)];
    }

    #endregion

    #region ▶ 힐/이펙트 ◀
    /// <summary>전방 우선 생존자 1명 힐</summary>
    public void HealFrontPriorityTarget()
    {
        var target = GetRandomAlivePlayer();
        if (target == null)
        {
            Debug.Log("힐 대상 없음");
            return;
        }

        int amount = 50; // 기본값
        if (GameDataManager.Instance != null)
        {
            amount = GameDataManager.Instance.GetCurrentHealAmount();
        }

        target.Heal(amount);
        PlayHealEffect(target.transform);
        Debug.Log($"힐 적용: {target.name} +{amount}");
    }

    /// <summary>대상 위치에 힐 이펙트 생성</summary>
    private void PlayHealEffect(Transform target)
    {
        if (healEffectPrefab == null || target == null) return;

        var instance = Instantiate(healEffectPrefab, target);
        instance.transform.localPosition = healEffectOffset;

        // 파티클 기반이면 파티클 재생 시간 후 자동 파괴
        var ps = instance.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            // startLifetime이 MinMaxCurve이므로 최대값 사용 시도
            float life = 0f;
            var sl = main.startLifetime;
            switch (sl.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    life = Mathf.Max(sl.constantMin, sl.constantMax); break;
                case ParticleSystemCurveMode.TwoCurves:
                case ParticleSystemCurveMode.Curve:
                    life = sl.constantMax; break; // 근사치
                default:
                    life = sl.constant; break;
            }
            float destroyAfter = main.duration + life + 0.1f;
            Destroy(instance, destroyAfter);
            return;
        }

        // Animator만 있는 경우 기본 파괴 시간 사용
        Destroy(instance, healEffectAutoDestroyTime);
    }
    #endregion

    #region ▶ 사망 콜백 ◀
    private void OnEnemyDead(CharacterBase enemy)
    {
        _enemies.Remove(enemy as Enemy);
        if (_enemies.Count == 0)
        {
            // 웨이브 종료 → 전투 상태 종료 및 플레이어 공격 중지
            IsBattleRunning = false;
            
            // 모든 플레이어들의 공격 중지 (라운드 전환 준비)
            foreach (var player in _players)
            {
                if (player != null && player is PlayerCharacter pc)
                {
                    pc.StopCombat();
                }
            }
            
            Debug.Log("Wave Clear! 보상 지급 & 다음 스테이지 로딩");
            
            // StageManager에 라운드 완료 알림
            if (StageManager.Instance != null)
            {
                StageManager.Instance.CompleteRound();
            }
        }
    }

    private void OnPlayerDead(CharacterBase pc)
    {
        _players.Remove(pc as PlayerCharacter);
        if (_players.Count == 0)
        {
            IsBattleRunning = false;
            GameOverPanel.SetActive(true);
            Debug.Log("패배! 파티 전멸");
            
            // GameUIManager에게 게임 오버 알림
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnGameOver();
            }
        }
    }
    #endregion

    public void Restart()
    {
        SceneManager.LoadScene("Main");
    }

    /// <summary>
    /// 전투 강제 중지 및 정리 (UI에서 준비 화면으로 돌아갈 때 사용)
    /// </summary>
    public void AbortBattle()
    {
        // 플레이어 전투 중지 시그널
        foreach (var player in _players)
        {
            if (player != null)
            {
                player.StopCombat();
            }
        }

        // 상태 플래그 해제 및 전체 정리
        IsBattleRunning = false;
        ClearBattle();
        Debug.Log("BattleManager: 전투 강제 중지 및 정리 완료");
    }

    /// <summary>테스트 파티 정보 반환 (StageManager용)</summary>
    public System.Collections.Generic.List<(CharacterData, int)> GetTestPartyInfo()
    {
        var partyInfo = new System.Collections.Generic.List<(CharacterData, int)>();
        
        for (int i = 0; i < testPartyCharacters.Count; i++)
        {
            var cd = testPartyCharacters[i];
            if (cd == null) continue;

            int level = (i < testPartyLevels.Count) ? testPartyLevels[i] : 1;
            level = Mathf.Max(1, level); // 최소 1레벨 보장

            partyInfo.Add((cd, level));
        }
        
        return partyInfo;
    }
    
    /// <summary>기본 파티 생성 (최후의 백업)</summary>
    private System.Collections.Generic.List<(CharacterData, int)> CreateDefaultParty()
    {
        var partyInfo = new System.Collections.Generic.List<(CharacterData, int)>();
        
        // testPartyCharacters에서 사용 가능한 캐릭터가 있으면 사용
        if (testPartyCharacters.Count > 0)
        {
            for (int i = 0; i < Mathf.Min(testPartyCharacters.Count, 2); i++)
            {
                var cd = testPartyCharacters[i];
                if (cd != null)
                {
                    partyInfo.Add((cd, 1));
                }
            }
        }
        
        Debug.Log($"기본 파티 생성: {partyInfo.Count}명");
        return partyInfo;
    }
    
    /// <summary>현재 스폰된 모든 플레이어 캐릭터 반환</summary>
    public System.Collections.Generic.List<PlayerCharacter> GetAllPlayers()
    {
        return _players;
    }
    
    /// <summary>현재 스폰된 모든 적 캐릭터 반환</summary>
    public System.Collections.Generic.List<Enemy> GetAllEnemies()
    {
        return _enemies;
    }
}