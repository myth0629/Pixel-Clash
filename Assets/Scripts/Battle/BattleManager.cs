using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

///  - 파티·적 리스트 관리
///  - 타깃 쿼리 제공
///  - 웨이브 클리어 판정
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    #region Constants
    private const int MAX_PARTY_SIZE = 2;
    private const int MAX_ENEMY_SIZE = 2;
    private const int MIN_LEVEL = 1;
    private const float FRONT_ROW_THRESHOLD = 0.1f;
    private const int DEFAULT_HEAL_AMOUNT = 50;
    #endregion

    #region Serialized Fields
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
    #endregion

    #region Properties
    public bool IsBattleRunning { get; private set; }
    #endregion

    #region Private Fields
    private readonly List<PlayerCharacter> _players = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<HealthBarUI> _healthBars = new(); // 체력바 추적용 리스트
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (ShouldStartTestMode())
        {
            StartTestMode();
        }
    }
    #endregion

    #region Test Mode
    /// <summary>테스트 모드 시작 조건 확인</summary>
    private bool ShouldStartTestMode()
    {
        return StageManager.Instance == null && autoStartOnPlay;
    }

    /// <summary>테스트 모드로 전투 시작</summary>
    private void StartTestMode()
    {
        var partyInfo = CreateTestPartyInfo();
        if (partyInfo.Count == 0)
        {
            Debug.LogWarning("BattleManager: autoStartOnPlay가 켜졌지만 testPartyCharacters가 비어 있습니다.");
            return;
        }

        StartBattle(partyInfo, testWaveEnemyCount);
    }

    /// <summary>테스트용 파티 정보 생성</summary>
    private List<(CharacterData, int)> CreateTestPartyInfo()
    {
        var partyInfo = new List<(CharacterData, int)>();
        
        for (int i = 0; i < testPartyCharacters.Count; i++)
        {
            var cd = testPartyCharacters[i];
            if (cd == null) continue;

            int level = GetLevelForIndex(i);
            partyInfo.Add((cd, level));
        }

        return partyInfo;
    }

    /// <summary>인덱스에 해당하는 레벨 반환 (최소 1레벨 보장)</summary>
    private int GetLevelForIndex(int index)
    {
        int level = (index < testPartyLevels.Count) ? testPartyLevels[index] : MIN_LEVEL;
        return Mathf.Max(MIN_LEVEL, level);
    }

    /// <summary>테스트 파티 정보 반환 (StageManager용)</summary>
    public List<(CharacterData, int)> GetTestPartyInfo()
    {
        return CreateTestPartyInfo();
    }
    #endregion

    #region Battle Management
    /// <summary>메인 전투 시작 메서드</summary>
    public void StartBattle(List<(CharacterData, int)> partyInfo, int waveEnemyCount)
    {
        InitializeBattle();
        SpawnAllCharacters(partyInfo, waveEnemyCount);
        StartBattleSequence();
    }

    /// <summary>전투 초기화</summary>
    private void InitializeBattle()
    {
        IsBattleRunning = true;
        ClearBattle();
    }

    /// <summary>모든 캐릭터 스폰</summary>
    private void SpawnAllCharacters(List<(CharacterData, int)> partyInfo, int waveEnemyCount)
    {
        SpawnPlayers(partyInfo);
        SpawnWave(waveEnemyCount);
    }

    /// <summary>전투 시퀀스 시작</summary>
    private void StartBattleSequence()
    {
        StartCoroutine(StartBattleAfterDelay());
    }

    /// <summary>GameUIManager에서 설정한 파티로 전투 시작</summary>
    public void StartBattleWithUIParty(int waveEnemyCount = 3)
    {
        var partyInfo = GetUIPartyInfo();
        
        if (!ValidatePartyInfo(partyInfo))
        {
            Debug.LogError("파티가 비어있습니다! 전투를 시작할 수 없습니다.");
            return;
        }
        
        Debug.Log($"최종 파티 정보: {partyInfo.Count}명으로 전투 시작");
        
        if (IsNewRound())
        {
            StartNewRound(partyInfo, waveEnemyCount);
        }
        else
        {
            StartBattle(partyInfo, waveEnemyCount);
        }
    }

    /// <summary>UI에서 파티 정보 가져오기</summary>
    private List<(CharacterData, int)> GetUIPartyInfo()
    {
        if (GameUIManager.Instance != null)
        {
            var partyInfo = GameUIManager.Instance.GetCurrentPartyInfo();
            Debug.Log($"GameUIManager 파티 정보: {partyInfo.Count}명");
            return partyInfo;
        }
        
        Debug.LogWarning("GameUIManager.Instance가 null입니다!");
        return new List<(CharacterData, int)>();
    }

    /// <summary>파티 정보 유효성 검증</summary>
    private bool ValidatePartyInfo(List<(CharacterData, int)> partyInfo)
    {
        return partyInfo != null && partyInfo.Count > 0;
    }

    /// <summary>새로운 라운드인지 확인</summary>
    private bool IsNewRound()
    {
        return _players.Count > 0;
    }

    /// <summary>새로운 라운드 시작</summary>
    private void StartNewRound(List<(CharacterData, int)> partyInfo, int waveEnemyCount)
    {
        Debug.Log("새로운 라운드 시작 - 죽은 캐릭터 부활 및 체력 회복");
        
        PrepareExistingPlayersForNewRound();
        ReviveDeadCharacters(partyInfo);
        
        ClearEnemiesOnly();
        SpawnWave(waveEnemyCount);
        
        IsBattleRunning = true;
        Debug.Log("새로운 라운드 - 전투 상태 활성화");
        
        StartBattleSequence();
    }

    /// <summary>기존 플레이어들을 새로운 라운드에 맞게 준비</summary>
    private void PrepareExistingPlayersForNewRound()
    {
        Debug.Log($"[PrepareExistingPlayersForNewRound] 기존 플레이어 {_players.Count}명 새 라운드 준비");
        
        foreach (var player in _players)
        {
            if (player != null)
            {
                Debug.Log($"[PrepareExistingPlayersForNewRound] {player.data?.displayName} StartNewRound 호출 전 - HP: {player.CurrentHp}/{player.MaxHp}");
                player.StartNewRound();
                Debug.Log($"[PrepareExistingPlayersForNewRound] {player.data?.displayName} StartNewRound 호출 후 - HP: {player.CurrentHp}/{player.MaxHp}");
            }
        }
    }

    /// <summary>통합 전투 시작 딜레이 코루틴</summary>
    private IEnumerator StartBattleAfterDelay()
    {
        Debug.Log($"모든 캐릭터들이 {battleStartDelay}초 후 동시에 전투 시작");
        
        yield return new WaitForSeconds(battleStartDelay);
        
        StartCombatForAllCharacters();
        
        Debug.Log("모든 캐릭터들의 전투 시작!");
    }

    /// <summary>모든 캐릭터들의 전투 시작</summary>
    private void StartCombatForAllCharacters()
    {
        StartCombatForPlayers();
        StartCombatForEnemies();
    }

    /// <summary>모든 플레이어들의 전투 시작</summary>
    private void StartCombatForPlayers()
    {
        foreach (var player in _players)
        {
            if (player != null)
            {
                player.StartCombat();
            }
        }
    }

    /// <summary>모든 적들의 전투 시작</summary>
    private void StartCombatForEnemies()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy != null)
            {
                enemy.StartCombat();
            }
        }
    }
    #endregion

    #region Spawn Management

    /// <summary>기존 전투 정리</summary>
    private void ClearBattle()
    {
        ClearHealthBars();
        ClearAllCharacters();
        HideGameOverPanel();
    }

    /// <summary>모든 체력바 제거</summary>
    private void ClearHealthBars()
    {
        foreach (var healthBar in _healthBars)
        {
            if (healthBar != null)
                Destroy(healthBar.gameObject);
        }
        _healthBars.Clear();
    }

    /// <summary>모든 캐릭터 제거</summary>
    private void ClearAllCharacters()
    {
        ClearPlayers();
        ClearEnemies();
    }

    /// <summary>플레이어들 제거</summary>
    private void ClearPlayers()
    {
        foreach (var player in _players)
        {
            if (player != null)
                Destroy(player.gameObject);
        }
        _players.Clear();
    }

    /// <summary>적들 제거</summary>
    private void ClearEnemies()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        _enemies.Clear();
    }

    /// <summary>게임오버 패널 숨기기</summary>
    private void HideGameOverPanel()
    {
        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);
    }

    /// <summary>적들만 제거 (새로운 라운드용)</summary>
    private void ClearEnemiesOnly()
    {
        ClearEnemyHealthBars();
        ClearEnemies();
    }

    /// <summary>적 체력바들만 제거</summary>
    private void ClearEnemyHealthBars()
    {
        for (int i = _healthBars.Count - 1; i >= 0; i--)
        {
            if (_healthBars[i] != null)
            {
                var target = _healthBars[i].GetTarget();
                if (target is Enemy)
                {
                    Destroy(_healthBars[i].gameObject);
                    _healthBars.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>죽은 캐릭터들을 부활시킴</summary>
    private void ReviveDeadCharacters(List<(CharacterData, int)> partyInfo)
    {
        Debug.Log($"[ReviveDeadCharacters] 파티 정보 확인: {partyInfo.Count}명");
        
        for (int slotIndex = 0; slotIndex < partyInfo.Count; slotIndex++)
        {
            var (characterData, level) = partyInfo[slotIndex];
            Debug.Log($"[ReviveDeadCharacters] 슬롯 {slotIndex}: {characterData?.displayName} (레벨 {level})");
            
            if (ShouldReviveCharacter(characterData, slotIndex))
            {
                Debug.Log($"[ReviveDeadCharacters] {characterData.displayName} 부활 필요");
                ReviveExistingCharacter(characterData, level, slotIndex);
            }
            else
            {
                Debug.Log($"[ReviveDeadCharacters] {characterData?.displayName} 부활 불필요 (이미 살아있거나 없음)");
            }
        }
    }

    /// <summary>캐릭터 부활 필요 여부 확인 (새로운 로직)</summary>
    private bool ShouldReviveCharacter(CharacterData characterData, int slotIndex)
    {
        if (characterData == null) 
        {
            Debug.Log($"[ShouldReviveCharacter] 슬롯 {slotIndex}: characterData가 null");
            return false;
        }
        
        // 현재 플레이어 리스트 상태 확인
        Debug.Log($"[ShouldReviveCharacter] 현재 플레이어 수: {_players.Count}");
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i] != null)
            {
                Debug.Log($"  플레이어 {i}: {_players[i].data?.displayName} (HP: {_players[i].CurrentHp}/{_players[i].MaxHp})");
            }
        }
        
        // 해당 슬롯에 이미 살아있는 캐릭터가 있는지 확인
        foreach (var player in _players)
        {
            if (player != null && player.data == characterData && player.CurrentHp > 0)
            {
                Debug.Log($"[ShouldReviveCharacter] {characterData.displayName} 이미 살아있음");
                return false; // 이미 살아있음
            }
        }
        
        // 사망한 캐릭터가 있는지 확인
        foreach (var player in _players)
        {
            if (player != null && player.data == characterData && player.CurrentHp <= 0)
            {
                Debug.Log($"[ShouldReviveCharacter] {characterData.displayName} 사망 상태 - 부활 필요");
                return true; // 사망한 캐릭터 발견 - 부활 필요
            }
        }
        
        // 아예 없는 캐릭터면 새로 생성
        Debug.Log($"[ShouldReviveCharacter] {characterData.displayName} 플레이어 리스트에 없음 - 새로 생성 필요");
        return true;
    }

    /// <summary>기존 캐릭터 부활 또는 새 캐릭터 생성</summary>
    private void ReviveExistingCharacter(CharacterData characterData, int level, int slotIndex)
    {
        // 사망한 기존 캐릭터 찾기
        PlayerCharacter deadCharacter = null;
        foreach (var player in _players)
        {
            if (player != null && player.data == characterData && player.CurrentHp <= 0)
            {
                deadCharacter = player;
                break;
            }
        }
        
        if (deadCharacter != null)
        {
            // 기존 캐릭터 부활
            Debug.Log($"기존 캐릭터 부활: {characterData.displayName} (레벨 {level})");
            deadCharacter.StartNewRound(); // 이미 체력 회복과 애니메이션 리셋 포함
            
            // 체력바가 없다면 다시 생성
            if (!HasHealthBarForCharacter(deadCharacter))
            {
                Debug.Log($"[ReviveExistingCharacter] {characterData.displayName} 체력바 없음 - 재생성");
                CreatePlayerHealthBar(deadCharacter);
            }
        }
        else
        {
            // 새 캐릭터 생성
            ReviveSingleCharacter(characterData, level, slotIndex);
        }
    }

    /// <summary>단일 캐릭터 부활</summary>
    private void ReviveSingleCharacter(CharacterData characterData, int level, int slotIndex)
    {
        Debug.Log($"캐릭터 부활: {characterData.displayName} (레벨 {level})");
        
        var revivedObject = CreatePlayerObject(characterData, slotIndex);
        var revivedCharacter = SetupPlayerCharacter(revivedObject, characterData, level, slotIndex);
        CreatePlayerHealthBar(revivedCharacter);
        RegisterPlayer(revivedCharacter);
        
        bool isFrontRow = slotIndex == 0;
        Debug.Log($"[{revivedCharacter.gameObject.name}] 부활 완료 - 포지션: {(isFrontRow ? "전방" : "후방")}");
    }

    /// <summary>플레이어 캐릭터들 스폰</summary>
    private void SpawnPlayers(List<(CharacterData, int)> party)
    {
        int partyCount = Mathf.Min(party.Count, MAX_PARTY_SIZE);

        for (int i = 0; i < partyCount; i++)
        {
            SpawnSinglePlayer(party[i], i);
        }
    }

    /// <summary>단일 플레이어 캐릭터 스폰</summary>
    private void SpawnSinglePlayer((CharacterData characterData, int level) playerInfo, int slotIndex)
    {
        var (cd, level) = playerInfo;
        var playerObject = CreatePlayerObject(cd, slotIndex);
        var playerCharacter = SetupPlayerCharacter(playerObject, cd, level, slotIndex);
        CreatePlayerHealthBar(playerCharacter);
        RegisterPlayer(playerCharacter);
    }

    /// <summary>플레이어 오브젝트 생성 및 배치</summary>
    private GameObject CreatePlayerObject(CharacterData characterData, int slotIndex)
    {
        var playerObject = Instantiate(characterData.prefab, playerSpawnRoot);
        SetPlayerPosition(playerObject, slotIndex);
        return playerObject;
    }

    /// <summary>플레이어 위치 설정</summary>
    private void SetPlayerPosition(GameObject playerObject, int slotIndex)
    {
        bool isFrontRow = slotIndex == 0;
        float xOffset = isFrontRow ? 0f : -playerCharacterSpacing;
        playerObject.transform.localPosition = new Vector3(xOffset, 0, 0);
    }

    /// <summary>플레이어 캐릭터 컴포넌트 설정</summary>
    private PlayerCharacter SetupPlayerCharacter(GameObject playerObject, CharacterData characterData, int level, int slotIndex)
    {
        var playerCharacter = playerObject.AddComponent<PlayerCharacter>();
        playerCharacter.Setup(characterData, level);

        // 위치 설정 (애니메이션 상태 적용)
        PositionType position = slotIndex == 0 ? PositionType.Front : PositionType.Back;
        playerCharacter.SetPosition(position);

        return playerCharacter;
    }

    /// <summary>플레이어 체력바 생성</summary>
    private void CreatePlayerHealthBar(PlayerCharacter playerCharacter)
    {
        var healthBar = Instantiate(healthBarPrefab, uiRoot);
        healthBar.Init(playerCharacter);
        _healthBars.Add(healthBar);
    }

    /// <summary>특정 캐릭터의 체력바가 존재하는지 확인</summary>
    private bool HasHealthBarForCharacter(PlayerCharacter character)
    {
        // null 체력바 정리
        for (int i = _healthBars.Count - 1; i >= 0; i--)
        {
            if (_healthBars[i] == null)
            {
                _healthBars.RemoveAt(i);
            }
        }
        
        // 해당 캐릭터의 체력바 존재 여부 확인
        foreach (var healthBar in _healthBars)
        {
            if (healthBar != null && healthBar.GetTarget() == character)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>플레이어 등록</summary>
    private void RegisterPlayer(PlayerCharacter playerCharacter)
    {
        playerCharacter.OnDeath += OnPlayerDead;
        _players.Add(playerCharacter);
    }

    /// <summary>웨이브 스폰</summary>
    private void SpawnWave(int count)
    {
        int enemyCount = Mathf.Min(count, MAX_ENEMY_SIZE);
        MonsterData[] monsterPool = GetMonsterPool();

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnSingleEnemy(i, monsterPool);
        }
    }

    /// <summary>몬스터 풀 가져오기</summary>
    private MonsterData[] GetMonsterPool()
    {
        MonsterData[] monsterPool = null;
        
        if (StageManager.Instance != null)
        {
            monsterPool = StageManager.Instance.GetMonsterPoolForCurrentRound();
        }
        
        if ((monsterPool == null || monsterPool.Length == 0) && 
            monsterDataList != null && monsterDataList.Length > 0)
        {
            monsterPool = monsterDataList;
        }
        
        return monsterPool;
    }

    /// <summary>단일 적 스폰</summary>
    private void SpawnSingleEnemy(int slotIndex, MonsterData[] monsterPool)
    {
        var (prefabToSpawn, dataToUse) = SelectEnemyPrefabAndData(monsterPool);
        var enemyObject = CreateEnemyObject(prefabToSpawn, slotIndex);
        var enemy = SetupEnemyComponent(enemyObject, dataToUse);
        CreateEnemyHealthBar(enemy);
        RegisterEnemy(enemy);
    }

    /// <summary>적 프리팹과 데이터 선택</summary>
    private (GameObject prefab, MonsterData data) SelectEnemyPrefabAndData(MonsterData[] monsterPool)
    {
        GameObject prefabToSpawn = enemyPrefab;
        MonsterData dataToUse = null;
        
        if (monsterPool != null && monsterPool.Length > 0)
        {
            dataToUse = monsterPool[UnityEngine.Random.Range(0, monsterPool.Length)];
            if (dataToUse.prefab != null)
                prefabToSpawn = dataToUse.prefab;
        }
        
        return (prefabToSpawn, dataToUse);
    }

    /// <summary>적 오브젝트 생성 및 배치</summary>
    private GameObject CreateEnemyObject(GameObject prefab, int slotIndex)
    {
        var enemyObject = Instantiate(prefab, enemySpawnRoot);
        Vector3 finalPosition = CalculateEnemyPosition(slotIndex);
        
        var enemy = enemyObject.GetComponent<Enemy>();
        enemy.StartWalkInAnimation(finalPosition);
        
        return enemyObject;
    }

    /// <summary>적 위치 계산</summary>
    private Vector3 CalculateEnemyPosition(int slotIndex)
    {
        bool isFrontRow = slotIndex == 0;
        float xOffset = isFrontRow ? 0f : enemyCharacterSpacing;
        return new Vector3(xOffset, 0, 0);
    }

    /// <summary>적 컴포넌트 설정</summary>
    private Enemy SetupEnemyComponent(GameObject enemyObject, MonsterData dataToUse)
    {
        var enemy = enemyObject.GetComponent<Enemy>();
        
        if (dataToUse != null)
        {
            SetupEnemyWithMonsterData(enemy, dataToUse);
        }
        else
        {
            SetupEnemyWithBasicStats(enemy);
        }
        
        return enemy;
    }

    /// <summary>몬스터 데이터로 적 설정</summary>
    private void SetupEnemyWithMonsterData(Enemy enemy, MonsterData dataToUse)
    {
        if (StageManager.Instance != null)
        {
            ApplyStageScaling(enemy, dataToUse);
        }
        else
        {
            enemy.Setup(dataToUse, 1);
            Debug.Log("Wave 1 Enemy (No StageManager): MonsterData scaling only");
        }
    }

    /// <summary>스테이지 스케일링 적용</summary>
    private void ApplyStageScaling(Enemy enemy, MonsterData dataToUse)
    {
        var (stageHpMult, stageAtkMult) = StageManager.Instance.GetStageMultipliers();
        int currentWave = StageManager.Instance.CurrentRound;
        
        (int baseHp, int baseAtk) = dataToUse.GetScaledStats(currentWave);
        
        int finalHp = Mathf.RoundToInt(baseHp * stageHpMult);
        int finalAtk = Mathf.RoundToInt(baseAtk * stageAtkMult);
        
        enemy.Setup(dataToUse, currentWave);
        enemy.InitStats(finalHp, finalAtk, dataToUse.attackInterval);
        
        Debug.Log($"Stage {StageManager.Instance.CurrentStage}-{currentWave} Enemy: " +
                  $"Base({baseHp}/{baseAtk}) → Final({finalHp}/{finalAtk}) " +
                  $"(Wave: {currentWave}, Stage Multipliers: {stageHpMult:F2}x/{stageAtkMult:F2}x)");
    }

    /// <summary>기본 스탯으로 적 설정</summary>
    private void SetupEnemyWithBasicStats(Enemy enemy)
    {
        int baseHp = 30 + 10 * testWaveEnemyCount;
        int baseAtk = 5 + 3 * testWaveEnemyCount;
        
        if (StageManager.Instance != null)
        {
            var (stageHpMult, stageAtkMult) = StageManager.Instance.GetStageMultipliers();
            baseHp = Mathf.RoundToInt(baseHp * stageHpMult);
            baseAtk = Mathf.RoundToInt(baseAtk * stageAtkMult);
        }
        
        enemy.Setup(baseHp, baseAtk);
    }

    /// <summary>적 체력바 생성</summary>
    private void CreateEnemyHealthBar(Enemy enemy)
    {
        var healthBar = Instantiate(healthBarPrefab, uiRoot);
        healthBar.Init(enemy);
        _healthBars.Add(healthBar);
    }

    /// <summary>적 등록</summary>
    private void RegisterEnemy(Enemy enemy)
    {
        enemy.OnDeath += OnEnemyDead;
        _enemies.Add(enemy);
    }
    #endregion

    #region Targeting System
    /// <summary>가장 가까운 적 반환 (전방 우선)</summary>
    public Enemy GetNearestEnemy(Vector3 pos)
    {
        Enemy frontRowTarget = FindFrontRowEnemy(pos);
        return frontRowTarget ?? FindAnyEnemy(pos);
    }

    /// <summary>전방 적 찾기</summary>
    private Enemy FindFrontRowEnemy(Vector3 pos)
    {
        Enemy closest = null;
        float minSqr = float.MaxValue;

        foreach (var enemy in _enemies)
        {
            if (enemy == null || !IsFrontRowEnemy(enemy)) continue;
            
            float distance = (enemy.transform.position - pos).sqrMagnitude;
            if (distance < minSqr)
            {
                minSqr = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    /// <summary>아무 적이나 찾기</summary>
    private Enemy FindAnyEnemy(Vector3 pos)
    {
        Enemy closest = null;
        float minSqr = float.MaxValue;

        foreach (var enemy in _enemies)
        {
            if (enemy == null) continue;
            
            float distance = (enemy.transform.position - pos).sqrMagnitude;
            if (distance < minSqr)
            {
                minSqr = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    /// <summary>전방 적인지 확인</summary>
    private bool IsFrontRowEnemy(Enemy enemy)
    {
        return Mathf.Abs(enemy.transform.localPosition.x) < FRONT_ROW_THRESHOLD;
    }

    /// <summary>랜덤 생존 플레이어 반환 (전방 우선)</summary>
    public PlayerCharacter GetRandomAlivePlayer()
    {
        var alivePlayers = GetAlivePlayers();
        if (alivePlayers.Count == 0) return null;
        
        var frontRowPlayers = GetFrontRowPlayers(alivePlayers);
        if (frontRowPlayers.Count > 0)
        {
            return frontRowPlayers[Random.Range(0, frontRowPlayers.Count)];
        }
        
        return alivePlayers[Random.Range(0, alivePlayers.Count)];
    }

    /// <summary>생존 플레이어 목록 반환</summary>
    private List<PlayerCharacter> GetAlivePlayers()
    {
        return _players.FindAll(p => p != null && p.CurrentHp > 0);
    }

    /// <summary>전방 플레이어들 필터링</summary>
    private List<PlayerCharacter> GetFrontRowPlayers(List<PlayerCharacter> players)
    {
        return players.FindAll(p => IsFrontRowPlayer(p));
    }

    /// <summary>전방 플레이어인지 확인</summary>
    private bool IsFrontRowPlayer(PlayerCharacter player)
    {
        return Mathf.Abs(player.transform.localPosition.x) < FRONT_ROW_THRESHOLD;
    }
    #endregion

    #region Heal System
    /// <summary>전방 우선 생존자 1명 힐</summary>
    public void HealFrontPriorityTarget()
    {
        var target = GetRandomAlivePlayer();
        if (target == null)
        {
            Debug.Log("힐 대상 없음");
            return;
        }

        int healAmount = GetCurrentHealAmount();
        ApplyHeal(target, healAmount);
    }

    /// <summary>현재 힐 량 가져오기</summary>
    private int GetCurrentHealAmount()
    {
        if (GameDataManager.Instance != null)
        {
            return GameDataManager.Instance.GetCurrentHealAmount();
        }
        return DEFAULT_HEAL_AMOUNT;
    }

    /// <summary>힐 적용</summary>
    private void ApplyHeal(PlayerCharacter target, int amount)
    {
        target.Heal(amount);
        PlayHealEffect(target.transform);
        Debug.Log($"힐 적용: {target.name} +{amount}");
    }

    /// <summary>대상 위치에 힐 이펙트 생성</summary>
    private void PlayHealEffect(Transform target)
    {
        if (healEffectPrefab == null || target == null) return;

        var effectInstance = CreateHealEffect(target);
        DestroyHealEffectAfterTime(effectInstance);
    }

    /// <summary>힐 이펙트 생성</summary>
    private GameObject CreateHealEffect(Transform target)
    {
        var instance = Instantiate(healEffectPrefab, target);
        instance.transform.localPosition = healEffectOffset;
        return instance;
    }

    /// <summary>힐 이펙트 시간 후 제거</summary>
    private void DestroyHealEffectAfterTime(GameObject effectInstance)
    {
        var particleSystem = effectInstance.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            float destroyTime = CalculateParticleDestroyTime(particleSystem);
            Destroy(effectInstance, destroyTime);
        }
        else
        {
            Destroy(effectInstance, healEffectAutoDestroyTime);
        }
    }

    /// <summary>파티클 시스템 파괴 시간 계산</summary>
    private float CalculateParticleDestroyTime(ParticleSystem particleSystem)
    {
        var main = particleSystem.main;
        float lifetime = GetParticleLifetime(main.startLifetime);
        return main.duration + lifetime + 0.1f;
    }

    /// <summary>파티클 라이프타임 계산</summary>
    private float GetParticleLifetime(ParticleSystem.MinMaxCurve startLifetime)
    {
        return startLifetime.mode switch
        {
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(startLifetime.constantMin, startLifetime.constantMax),
            ParticleSystemCurveMode.TwoCurves or ParticleSystemCurveMode.Curve => startLifetime.constantMax,
            _ => startLifetime.constant
        };
    }
    #endregion

    #region Death Callbacks
    /// <summary>적 사망 처리</summary>
    private void OnEnemyDead(CharacterBase enemy)
    {
        _enemies.Remove(enemy as Enemy);
        
        if (IsWaveCleared())
        {
            HandleWaveCleared();
        }
    }

    /// <summary>웨이브 클리어 여부 확인</summary>
    private bool IsWaveCleared()
    {
        return _enemies.Count == 0;
    }

    /// <summary>웨이브 클리어 처리</summary>
    private void HandleWaveCleared()
    {
        StopBattle();
        StopAllPlayerCombat();
        
        Debug.Log("Wave Clear! 보상 지급 & 다음 스테이지 로딩");
        
        NotifyStageManagerRoundComplete();
    }

    /// <summary>전투 중지</summary>
    private void StopBattle()
    {
        IsBattleRunning = false;
    }

    /// <summary>모든 플레이어 전투 중지</summary>
    private void StopAllPlayerCombat()
    {
        foreach (var player in _players)
        {
            if (player != null && player is PlayerCharacter pc)
            {
                pc.StopCombat();
            }
        }
    }

    /// <summary>스테이지 매니저에 라운드 완료 알림</summary>
    private void NotifyStageManagerRoundComplete()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CompleteRound();
        }
    }

    /// <summary>플레이어 사망 처리</summary>
    private void OnPlayerDead(CharacterBase pc)
    {
        // 플레이어는 사망해도 리스트에서 제거하지 않음 (Death 애니메이션 상태로 유지)
        Debug.Log($"[{pc.gameObject.name}] 플레이어 사망 - 리스트에서 제거하지 않고 유지");
        
        if (IsPartyDefeated())
        {
            HandlePartyDefeated();
        }
    }

    /// <summary>파티 전멸 여부 확인</summary>
    private bool IsPartyDefeated()
    {
        // 살아있는 플레이어가 있는지 확인 (HP > 0)
        foreach (var player in _players)
        {
            if (player != null && player.CurrentHp > 0)
            {
                return false; // 살아있는 플레이어가 하나라도 있으면 전멸 아님
            }
        }
        return true; // 모든 플레이어가 사망
    }

    /// <summary>파티 전멸 처리</summary>
    private void HandlePartyDefeated()
    {
        StopBattle();
        ShowGameOverPanel();
        NotifyGameUIManagerGameOver();
        
        Debug.Log("패배! 파티 전멸");
    }

    /// <summary>게임오버 패널 표시</summary>
    private void ShowGameOverPanel()
    {
        if (GameOverPanel != null)
            GameOverPanel.SetActive(true);
    }

    /// <summary>게임 UI 매니저에 게임오버 알림</summary>
    private void NotifyGameUIManagerGameOver()
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.OnGameOver();
        }
    }
    #endregion

    #region Public Utilities
    /// <summary>씬 재시작</summary>
    public void Restart()
    {
        SceneManager.LoadScene("Main");
    }

    /// <summary>전투 강제 중지 및 정리 (UI에서 준비 화면으로 돌아갈 때 사용)</summary>
    public void AbortBattle()
    {
        StopAllPlayerCombat();
        StopBattle();
        ClearBattle();
        
        Debug.Log("BattleManager: 전투 강제 중지 및 정리 완료");
    }
    #endregion

    #region Utility Methods
    /// <summary>기본 파티 생성 (최후의 백업)</summary>
    private List<(CharacterData, int)> CreateDefaultParty()
    {
        var partyInfo = new List<(CharacterData, int)>();
        
        if (testPartyCharacters.Count > 0)
        {
            for (int i = 0; i < Mathf.Min(testPartyCharacters.Count, MAX_PARTY_SIZE); i++)
            {
                var cd = testPartyCharacters[i];
                if (cd != null)
                {
                    partyInfo.Add((cd, MIN_LEVEL));
                }
            }
        }
        
        Debug.Log($"기본 파티 생성: {partyInfo.Count}명");
        return partyInfo;
    }
    
    /// <summary>현재 스폰된 모든 플레이어 캐릭터 반환</summary>
    public List<PlayerCharacter> GetAllPlayers()
    {
        return _players;
    }
    
    /// <summary>현재 스폰된 모든 적 캐릭터 반환</summary>
    public List<Enemy> GetAllEnemies()
    {
        return _enemies;
    }
    #endregion
}