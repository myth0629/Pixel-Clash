using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private GameObject enemyPrefab;

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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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

        SpawnPlayers(partyInfo);
        SpawnWave(waveEnemyCount);
    }

    private void SpawnPlayers(List<(CharacterData, int)> party)
    {
        foreach (var (cd, level) in party)
        {
            var go = Instantiate(cd.prefab, playerSpawnRoot);
            var pc = go.AddComponent<PlayerCharacter>();
            pc.Setup(cd, level);

            pc.OnDeath += OnPlayerDead;
            _players.Add(pc);
        }
    }

    private void SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(enemyPrefab, enemySpawnRoot);
            var enemy = go.GetComponent<Enemy>();

            // 간단 스케일링 예시
            int hp  = 30 + 10 * count;
            int atk = 5 +  3 * count;
            enemy.Setup(hp, atk);

            enemy.OnDeath += OnEnemyDead;
            _enemies.Add(enemy);
        }
    }
    #endregion

    #region ▶ 타깃 헬퍼 ◀
    public Enemy GetNearestEnemy(Vector3 pos)
    {
        Enemy closest = null;
        float minSqr  = float.MaxValue;

        foreach (var e in _enemies)
        {
            if (e == null) continue;
            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < minSqr) { minSqr = d; closest = e; }
        }
        return closest;
    }

    public PlayerCharacter GetRandomAlivePlayer()
    {
        var alive = _players.FindAll(p => p != null);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }
    #endregion

    #region ▶ 사망 콜백 ◀
    private void OnEnemyDead(CharacterBase enemy)
    {
        _enemies.Remove(enemy as Enemy);
        if (_enemies.Count == 0)
        {
            // 웨이브 종료 → 보상 지급
            IsBattleRunning = false;
            Debug.Log("Wave Clear! 보상 지급 & 다음 스테이지 로딩");
        }
    }

    private void OnPlayerDead(CharacterBase pc)
    {
        _players.Remove(pc as PlayerCharacter);
        if (_players.Count == 0)
        {
            IsBattleRunning = false;
            Debug.Log("패배! 파티 전멸");
        }
    }
    #endregion
}