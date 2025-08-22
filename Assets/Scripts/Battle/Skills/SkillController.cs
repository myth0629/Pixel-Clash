using System.Collections.Generic;
using PixelClash.Data;
using UnityEngine;

/// <summary>
/// 캐릭터(플레이어/적)마다 부착되는 스킬 컨트롤러.
/// - CharacterData/MonsterData에서 스킬 목록을 받아 런타임 스킬로 운용
/// - 자동 공격과 병행하여, 쿨타임이 끝나면 자동 발동
/// </summary>
public class SkillController : MonoBehaviour
{
    private CharacterBase _owner;
    private readonly List<SkillRuntime> _skills = new();
    private Animator _anim;
    [SerializeField] private bool debugSkillLog = false;

    // 애니메이션 이벤트 시점에 적용할 보류 스냅샷
    private PendingCast _pending;
    private float _pendingExpireAt;
    private const float PendingTimeoutSeconds = 4f; // 종료 이벤트 누락 시 자동 정리

    private class PendingCast
    {
        public SkillRuntime sr;
        public System.Collections.Generic.List<CharacterBase> targets;
        public bool isHeal;
    }

    private bool _active = false; // 전투 시작 시 활성화

    private void Awake()
    {
        // _owner 초기화는 Start에서 수행 (다른 컴포넌트들이 추가될 때까지 대기)
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // CharacterBase 컴포넌트를 찾아서 _owner 설정 (SetOwner로 이미 설정되었을 수도 있음)
        if (_owner == null)
        {
            _owner = GetComponentInParent<CharacterBase>();
            if (_owner == null) _owner = GetComponent<CharacterBase>();
        }
        
        if (_owner == null)
        {
            Debug.LogError($"[SkillController] {gameObject.name}에서 CharacterBase를 찾을 수 없습니다!");
        }
        else 
        {
            if (debugSkillLog)
            {
                Debug.Log($"[SkillController] Owner found: {_owner.name}, Attack: {_owner.Attack}");
            }
            
            // Attack이 0이면 경고 (InitStats가 호출되지 않았을 가능성)
            if (_owner.Attack <= 0)
            {
                Debug.LogWarning($"[SkillController] {_owner.name}의 Attack이 {_owner.Attack}입니다. PlayerCharacter.Setup()이 아직 호출되지 않았거나 baseAtk가 0일 수 있습니다.");
            }
        }
    }

    public void BindSkills(IEnumerable<SkillData> skills)
    {
        _skills.Clear();
        if (skills == null) return;
        foreach (var s in skills)
        {
            if (s == null) continue;
            _skills.Add(new SkillRuntime(s));
        }
    }

    /// <summary>
    /// _owner를 수동으로 설정 (타이밍 문제 해결용)
    /// </summary>
    public void SetOwner(CharacterBase owner)
    {
        _owner = owner;
        if (debugSkillLog && _owner != null)
        {
            Debug.Log($"[SkillController] SetOwner: {_owner.name}, Attack: {_owner.Attack}");
        }
    }

    private void Update()
    {
        if (_owner == null || !_active || !BattleManager.Instance.IsBattleRunning) return;

        float dt = Time.deltaTime;

        // 종료 이벤트 누락 대비 타임아웃 정리
        if (_pending != null && Time.time >= _pendingExpireAt)
        {
            _pending = null;
        }
        
        foreach (var sr in _skills)
        {
            sr.Tick(dt);
            if (sr.IsReady && CanCast(sr))
            {
                if (TryCast(sr))
                {
                    sr.ResetCooldown();
                }
            }
        }
    }

    public void StartCombat()
    {
        _active = true;
    }

    public void StopCombat()
    {
        _active = false;
    }

    private bool CanCast(SkillRuntime sr)
    {
        // 추가 조건(기절/침묵 등) 생기면 여기에서 필터링
        return true;
    }

    private bool TryCast(SkillRuntime sr)
    {
        // _owner 상태 검증
        if (_owner == null || _owner.Attack <= 0)
        {
            if (debugSkillLog)
            {
                Debug.LogWarning($"[Skill] TryCast 취소: _owner={_owner?.name ?? "null"}, Attack={_owner?.Attack ?? 0}");
            }
            return false;
        }

        // 1) 대상 스냅샷 + 수치 계산
        var targets = BuildTargets(sr);
        if (targets == null || targets.Count == 0)
            return false;

        bool isHeal = IsHealSkill(sr);

        _pending = new PendingCast
        {
            sr = sr,
            targets = targets,
            isHeal = isHeal
        };

        // 보류 만료 시간 설정 (애니메이션 종료 이벤트 누락 대비)
        _pendingExpireAt = Time.time + PendingTimeoutSeconds;

        if (debugSkillLog)
        {
            Debug.Log($"[Skill] Cast Ready: owner={name}, atk={_owner.Attack}, skill={sr.data.displayName} ({sr.data.id})");
            Debug.Log($"[Skill] Cast Data: mult={sr.data.powerMultiplier}, flatDmg={sr.data.flatBonusDamage}, flatHeal={sr.data.flatBonusHeal}, targets={targets.Count}");
        }

        // 2) 시전 애니메이션 트리거 (패시브 스킬이 아닌 경우만 실행)
        if (_anim != null && sr.data.type != PixelClash.Data.SkillType.Passive)
        {
            var trig = string.IsNullOrEmpty(sr.data.animatorTrigger) ? "Skill" : sr.data.animatorTrigger;
            _anim.SetTrigger(trig);
        }
        
        // 패시브 스킬인 경우 애니메이션 없이 즉시 효과 적용
        if (sr.data.type == PixelClash.Data.SkillType.Passive)
        {
            // 즉시 스킬 효과 적용
            OnSkillImpact();
            // 패시브 스킬은 즉시 완료 처리
            OnSkillCastEnd();
        }

        return true;
    }    // --- 애니메이션 이벤트에서 호출할 공개 메서드 ---
    // Animator 이벤트에 "OnSkillImpact"를 추가해 호출하세요.
    public void OnSkillImpact()
    {
        if (_pending == null) return;

        // 타격 시점에 다시 계산하여 DealDamage와 동일한 타이밍으로 atk 반영
        int amount = _pending.isHeal ? CalcHealAmount(_pending.sr) : CalcDamage(_pending.sr);

        // 스냅샷된 타깃이 비었거나 모두 사라졌다면 한 번 더 재빌드 시도
        if (_pending.targets == null || _pending.targets.Count == 0)
        {
            _pending.targets = BuildTargets(_pending.sr);
        }

        int applied = 0;
        foreach (var t in _pending.targets)
        {
            if (t == null || t.IsDead) continue;
            if (_pending.isHeal) t.Heal(amount);
            else t.TakeDamage(amount);
            applied++;
        }
        // 멀티 히트 지원: 여기서는 클리어하지 않음. 애니메이션 끝에서 정리.

        if (debugSkillLog)
        {
            Debug.Log($"[Skill] Impact: owner={name}, atk={_owner.Attack}, amount={amount}, applied={applied}, heal={_pending.isHeal}");
        }
    }

    // 애니메이션 마지막 프레임에서 호출하여 보류 상태 정리
    public void OnSkillCastEnd()
    {
        _pending = null;
    }

    // --- 내부 유틸리티 ---
    private System.Collections.Generic.List<CharacterBase> BuildTargets(SkillRuntime sr)
    {
        var list = new System.Collections.Generic.List<CharacterBase>();
        switch (sr.data.target)
        {
            case TargetType.Self:
                list.Add(_owner);
                break;
            case TargetType.Ally:
            case TargetType.AllAllies:
                if (_owner is PlayerCharacter)
                {
                    var allies = BattleManager.Instance.GetAllPlayers();
                    if (allies != null)
                        foreach (var a in allies) if (a != null) list.Add(a);
                }
                else if (_owner is Enemy)
                {
                    // 적 아군 전부(확장 시 필요). 현재는 적-적 힐을 별도 요구가 없으면 미사용
                    // 필요 시 BattleManager에 GetAllEnemies()로 적측 아군 수집
                    var enemies = BattleManager.Instance.GetAllEnemies();
                    if (enemies != null)
                        foreach (var e in enemies) if (e != null) list.Add(e);
                }
                break;
            case TargetType.SingleEnemy:
                {
                    if (_owner is PlayerCharacter)
                    {
                        var e = BattleManager.Instance.GetNearestEnemy(transform.position);
                        if (e != null) list.Add(e);
                    }
                    else if (_owner is Enemy)
                    {
                        var players = BattleManager.Instance.GetAllPlayers();
                        if (players != null && players.Count > 0)
                        {
                            // 전방 우선 랜덤
                            PlayerCharacter target = BattleManager.Instance.GetRandomAlivePlayer();
                            if (target != null) list.Add(target);
                        }
                    }
                }
                break;
            case TargetType.AllEnemies:
                if (_owner is PlayerCharacter)
                {
                    var enemies = BattleManager.Instance.GetAllEnemies();
                    if (enemies != null)
                        foreach (var e in enemies) if (e != null) list.Add(e);
                }
                else if (_owner is Enemy)
                {
                    var allies = BattleManager.Instance.GetAllPlayers();
                    if (allies != null)
                        foreach (var a in allies) if (a != null) list.Add(a);
                }
                break;
        }
        return list;
    }

    private bool IsHealSkill(SkillRuntime sr)
    {
        switch (sr.data.target)
        {
            case TargetType.Self:
            case TargetType.Ally:
            case TargetType.AllAllies:
                return true;
            default:
                return false;
        }
    }

    private int CalcDamage(SkillRuntime sr)
    {
        // _owner와 Attack 값 실시간 검증
        if (_owner == null)
        {
            // 다시 한 번 찾기 시도
            _owner = GetComponentInParent<CharacterBase>();
            if (_owner == null) _owner = GetComponent<CharacterBase>();
            
            if (_owner == null)
            {
                Debug.LogError("[SkillController] _owner를 찾을 수 없습니다!");
                return 1;
            }
        }

        int ownerAtk = _owner.Attack;
        if (ownerAtk <= 0)
        {
            Debug.LogWarning($"[SkillController] {_owner.name}의 Attack이 {ownerAtk}입니다. CharacterBase.InitStats가 호출되었는지 확인하세요.");
            
            // 잠시 대기 후 다시 시도
            if (Time.frameCount > 10) // 게임 시작 후 충분한 프레임이 지났다면
            {
                Debug.LogError($"[SkillController] {_owner.name}: InitStats가 호출되지 않았거나 baseAtk가 0입니다. PlayerCharacter.Setup() 호출을 확인하세요.");
            }
        }

        float multiplier = Mathf.Max(0f, sr.data.powerMultiplier);
        int baseDmg = Mathf.CeilToInt(ownerAtk * multiplier);
        int dmg = baseDmg + sr.data.flatBonusDamage;
        
        // 스킬 데미지는 최소한 공격력의 50% 이상이어야 함 (임시 보정)
        int minSkillDmg = Mathf.Max(1, ownerAtk / 2);
        int finalDmg = Mathf.Max(minSkillDmg, dmg);
        
        if (debugSkillLog)
        {
            Debug.Log($"[Skill] CalcDamage: atk={ownerAtk}, mult={multiplier}, baseDmg={baseDmg}, flatBonus={sr.data.flatBonusDamage}, minSkillDmg={minSkillDmg}, finalDmg={finalDmg}");
        }
        
        return finalDmg;
    }

    private int CalcHealAmount(SkillRuntime sr)
    {
        float multiplier = Mathf.Max(0f, sr.data.powerMultiplier);
        int baseHeal = Mathf.CeilToInt(_owner.Attack * multiplier);
        int amount = baseHeal + sr.data.flatBonusHeal;
        int finalAmount = Mathf.Max(1, amount);
        
        if (debugSkillLog)
        {
            Debug.Log($"[Skill] CalcHeal: atk={_owner.Attack}, mult={multiplier}, baseHeal={baseHeal}, flatBonus={sr.data.flatBonusHeal}, finalAmount={finalAmount}");
        }
        
        return finalAmount;
    }
}
