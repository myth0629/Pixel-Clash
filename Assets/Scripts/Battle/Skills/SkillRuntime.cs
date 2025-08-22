using PixelClash.Data;
using UnityEngine;

/// <summary>
/// 런타임 스킬 상태: 쿨타임 타이머 관리
/// </summary>
public class SkillRuntime
{
    public readonly SkillData data;
    private float _cooldownLeft;

    public SkillRuntime(SkillData data)
    {
        this.data = data;
        //_cooldownLeft = 0f; // 스폰 후 첫 시전은 즉시 가능하게 시작
    }

    public void Tick(float dt)
    {
        if (_cooldownLeft > 0f)
            _cooldownLeft -= dt;
    }

    public bool IsReady => _cooldownLeft <= 0f;

    public void ResetCooldown()
    {
        _cooldownLeft = Mathf.Max(0f, data.cooldown);
    }
}
