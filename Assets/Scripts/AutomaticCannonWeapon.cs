using UnityEngine;

public sealed class AutomaticCannonWeapon : BasicProjectileWeapon
{
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

        FireTimer = GetFireInterval();
        if (!Targeting.TryGetTarget(Runtime, Owner, Runtime.Data.BaseRange, out Transform target))
            return;

        FireBurst(target.position, 3, 1f);
    }

    // Fires five-round burst in manual mode.
    public override void TickManual(float deltaTime, Vector3 aimDirection, bool isFiring)
    {
        if (Runtime.State != WeaponState.Manual || !isFiring)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        FireTimer = GetFireInterval();
        Runtime.CurrentAmmo -= 5f;
        FireBurst(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, 5, 1f);
    }

    // Fires spread burst active ability, scaled by heat.
    public override void UseActiveAbility(Vector3 aimDirection)
    {
        if (Runtime.State != WeaponState.Manual)
            return;

        Runtime.CurrentAmmo -= Runtime.Data.ActiveAbilityAmmoCost;
        int extra = Heat != null ? Mathf.FloorToInt((Heat.NormalizedHeat * 100f) / 5f) : 0;
        FireBurst(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, 20 + extra, 0.85f);
    }

    // Automatic cannon uses default crit rules.
    public override bool CanCrit() => true;

    // Spawns configurable burst with small horizontal spread.
    private void FireBurst(Vector3 targetPosition, int count, float damageScale)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spread = new(UnityEngine.Random.Range(-0.07f, 0.07f), 0f, UnityEngine.Random.Range(-0.07f, 0.07f));
            FireAt(targetPosition + spread, damageScale, false);
        }
    }
}
