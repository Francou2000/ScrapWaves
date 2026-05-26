using UnityEngine;

public sealed class AutomaticCannonWeapon : BasicProjectileWeapon
{
    private const int AutomaticBurstCount = 3;
    private const int ManualBurstCount = 5;
    private const int ActiveBaseBulletCount = 20;
    private const float ActiveSpreadDegrees = 22f;
    private const float LineProjectileSpacing = 0.45f;
    private const float HeatDamageBonusPerThreshold = 0.15f;
    private const float CriticalDamageOverride = 2f;

    public AutomaticCannonWeapon(IWeaponTargeting targeting, ProjectilePool pool, Transform spawn)
        : base(targeting, pool, spawn)
    {
    }

    // Fires three-round burst in automatic mode.
    public override void TickAutomatic(float deltaTime)
    {
        if (Runtime.State != WeaponState.Automatic)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        if (Spawn == null)
            return;

        if (!Targeting.TryGetTarget(Runtime, Owner, Runtime.Data.BaseRange, out Transform target))
            return;

        FireTimer = GetFireInterval();
        FireLineBurst(target.position - Spawn.position, AutomaticBurstCount, GetHeatDamageMultiplier());
    }

    // Fires five-round burst in manual mode.
    public override void TickManual(float deltaTime, Vector3 aimDirection, bool isFiring)
    {
        if (Runtime.State != WeaponState.Manual || !isFiring)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        if (aimDirection.sqrMagnitude <= 0.0001f)
            return;

        int bulletsToFire = Mathf.Clamp(Mathf.CeilToInt(Runtime.CurrentAmmo), 1, ManualBurstCount);
        if (!TrySpendManualAmmo(bulletsToFire, requireFullAmount: false))
            return;

        FireTimer = GetFireInterval();
        FireLineBurst(aimDirection, bulletsToFire, GetHeatDamageMultiplier());
    }

    // Fires spread burst active ability, scaled by heat.
    public override void UseActiveAbility(Vector3 aimDirection)
    {
        if (Runtime.State != WeaponState.Manual)
            return;

        if (aimDirection.sqrMagnitude <= 0.0001f)
            return;

        if (!TrySpendManualAmmo(Runtime.Data.ActiveAbilityAmmoCost, requireFullAmount: true))
            return;

        int extra = Heat != null ? Mathf.FloorToInt((Heat.NormalizedHeat * 100f) / 5f) : 0;
        FireScatterBurst(aimDirection, ActiveBaseBulletCount + extra, GetHeatDamageMultiplier(), ActiveSpreadDegrees);
    }

    // Automatic cannon can critically strike, with a custom multiplier override below.
    public override bool CanCrit() => true;

    // Automatic cannon gains damage at 25/50/75 heat, not fire-rate scaling.
    protected override float GetHeatFireRateMultiplier() => 1f;

    // Critical hits deal double the normal critical damage effect.
    protected override float GetCritMultiplierOverride() => CriticalDamageOverride;

    // Converts 25/50/75 heat thresholds into stacking damage bonuses.
    private float GetHeatDamageMultiplier()
    {
        if (Heat == null)
            return 1f;

        float percent = Heat.NormalizedHeat * 100f;
        int thresholds = 0;
        if (percent >= 25f) thresholds++;
        if (percent >= 50f) thresholds++;
        if (percent >= 75f) thresholds++;
        return 1f + thresholds * HeatDamageBonusPerThreshold;
    }

    // Spawns normal cannon bursts as a straight line of projectiles.
    private void FireLineBurst(Vector3 aimDirection, int count, float damageScale)
    {
        Vector3 baseDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Spawn.forward;

        for (int i = 0; i < count; i++)
        {
            Vector3 position = Spawn.position + baseDirection * (LineProjectileSpacing * i);
            FireFromPositionInDirection(position, baseDirection, damageScale, false);
        }
    }

    // Spawns active ability burst with horizontal angular spread.
    private void FireScatterBurst(Vector3 aimDirection, int count, float damageScale, float spreadDegrees)
    {
        Vector3 baseDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Spawn.forward;

        for (int i = 0; i < count; i++)
        {
            float yaw = spreadDegrees > 0f ? UnityEngine.Random.Range(-spreadDegrees, spreadDegrees) : 0f;
            Vector3 shotDirection = Quaternion.AngleAxis(yaw, Vector3.up) * baseDirection;
            FireInDirection(shotDirection, damageScale, false);
        }
    }
}
