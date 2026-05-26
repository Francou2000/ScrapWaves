using UnityEngine;

public sealed class RocketLauncherWeapon : BasicProjectileWeapon
{
    public RocketLauncherWeapon(IWeaponTargeting targeting, ProjectilePool pool, Transform spawn)
        : base(targeting, pool, spawn)
    {
    }

    // Fires automatic rocket bursts with heat-scaled extra rockets.
    public override void TickAutomatic(float deltaTime)
    {
        if (Runtime.State != WeaponState.Automatic)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        FireTimer = GetFireInterval();
        if (!Targeting.TryGetTarget(Runtime, Owner, Runtime.Data.BaseRange, out Transform target))
            return;

        int extra = GetThresholdRocketBonus();
        FireBurstAt(target.position, 2 + extra, 1f, 0.03f, 1.8f, 0.45f);
    }

    // Fires one fast manual rocket and consumes one ammo unit.
    public override void TickManual(float deltaTime, Vector3 aimDirection, bool isFiring)
    {
        if (Runtime.State != WeaponState.Manual || !isFiring)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        FireTimer = GetManualFireInterval();
        if (!TrySpendManualAmmo(1f, requireFullAmount: false))
            return;

        FireBurstAt(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, 1, 1.15f, 0f, 2.4f, 0.35f);
    }

    // Fires overloaded multi-target volley scaled by current heat amount.
    public override void UseActiveAbility(Vector3 aimDirection)
    {
        if (Runtime.State != WeaponState.Manual)
            return;

        if (!TrySpendManualAmmo(Runtime.Data.ActiveAbilityAmmoCost, requireFullAmount: true))
            return;

        int heatBonus = Heat != null ? Mathf.FloorToInt(Heat.NormalizedHeat * 10f) : 0;
        int rocketCount = Mathf.Clamp(10 + heatBonus, 1, 20);
        FireBurstAt(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, rocketCount, 2f, 0.08f, 2.9f, 0.5f);
    }

    // Returns manual fire interval boosted by heat percentage.
    private float GetManualFireInterval()
    {
        float baseInterval = GetFireInterval();
        float heatFactor = Heat != null ? 1f + Heat.NormalizedHeat : 1f;
        return baseInterval / Mathf.Max(0.2f, heatFactor);
    }

    // Converts 25/50/75 heat thresholds into bonus automatic rockets.
    private int GetThresholdRocketBonus()
    {
        if (Heat == null)
            return 0;

        float percent = Heat.NormalizedHeat * 100f;
        int bonus = 0;
        if (percent >= 25f) bonus++;
        if (percent >= 50f) bonus++;
        if (percent >= 75f) bonus++;
        return bonus;
    }

    // Spawns explosive rocket volley with optional spread and blast profile.
    private void FireBurstAt(Vector3 targetPosition, int count, float damageScale, float spreadRadius, float explosionRadius, float falloff)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spread = spreadRadius > 0f
                ? new Vector3(UnityEngine.Random.Range(-spreadRadius, spreadRadius), 0f, UnityEngine.Random.Range(-spreadRadius, spreadRadius))
                : Vector3.zero;

            FireExplosiveAt(targetPosition + spread, damageScale, false, explosionRadius, falloff);
        }
    }
}
