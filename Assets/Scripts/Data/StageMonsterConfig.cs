using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지/라운드별로 출현 가능한 몬스터 풀을 지정하는 설정
/// </summary>
[CreateAssetMenu(fileName = "StageMonsterConfig", menuName = "Game Data/Stage Monster Config")]
public class StageMonsterConfig : ScriptableObject
{
    [Header("전역 기본 몬스터 풀 (스테이지 미지정 시 사용)")]
    public MonsterData[] globalDefaultMonsters;

    [Header("스테이지별 설정")]
    public List<StageEntry> stages = new();

    [Serializable]
    public class StageEntry
    {
        [Tooltip("스테이지 번호 (예: 1이면 1-1~1-5에 해당)")]
        public int stageNumber = 1;

        [Tooltip("해당 스테이지의 기본 몬스터 풀 (라운드 미지정 시 사용)")]
        public MonsterData[] defaultMonsters;

        [Tooltip("라운드별 개별 몬스터 풀 지정")]
        public List<RoundEntry> rounds = new();
    }

    [Serializable]
    public class RoundEntry
    {
        [Tooltip("라운드 번호 (예: 1~N)")]
        public int roundNumber = 1;

        [Tooltip("이 라운드에서 등장 가능한 몬스터 풀")]
        public MonsterData[] monsters;
    }

    /// <summary>
    /// 스테이지/라운드에 해당하는 몬스터 풀을 반환합니다.
    /// 우선순위: 라운드별 → 스테이지 기본 → 전역 기본 → null
    /// </summary>
    public MonsterData[] GetMonsterPool(int stageNumber, int roundNumber)
    {
        var stage = stages.Find(s => s.stageNumber == stageNumber);
        if (stage != null)
        {
            var round = stage.rounds.Find(r => r.roundNumber == roundNumber);
            if (round != null && round.monsters != null && round.monsters.Length > 0)
                return round.monsters;

            if (stage.defaultMonsters != null && stage.defaultMonsters.Length > 0)
                return stage.defaultMonsters;
        }

        if (globalDefaultMonsters != null && globalDefaultMonsters.Length > 0)
            return globalDefaultMonsters;

        return null;
    }
}
