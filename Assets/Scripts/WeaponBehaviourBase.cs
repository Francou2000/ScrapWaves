using UnityEngine;

public interface IWeaponBehaviour
{
    WeaponInstance Runtime { get; }
    void Setup(WeaponInstance instance, Transform owner, PlayerStats stats, HeatManager heat);
    void TickAutomatic(float deltaTime);
    void TickManual(float deltaTime, Vector3 aimDirection, bool isFiring);
    void UseActiveAbility(Vector3 aimDirection);
    bool CanCrit();
}

public class BasicProjectileWeapon : IWeaponBehaviour
{
    protected readonly IWeaponTargeting Targeting;
    protected readonly ProjectilePool Pool;
    protected readonly Transform Spawn;

    protected Transform Owner;
    protected PlayerStats Stats;
    protected HeatManager Heat;
    protected float FireTimer;

    public WeaponInstance Runtime { get; protected set; }

    public BasicProjectileWeapon(IWeaponTargeting targeting, ProjectilePool pool, Transform spawn)
    {
        Targeting = targeting;
        Pool = pool;
        Spawn = spawn;
    }

    // Stores runtime dependencies required by weapon behavior.
    public void Setup(WeaponInstance instance, Transform owner, PlayerStats stats, HeatManager heat)
    {
        Runtime = instance;
        Owner = owner;
        Stats = stats;
        Heat = heat;
    }

    // Handles automatic fire attempts using selected targeting logic.
    public virtual void TickAutomatic(float deltaTime)
    {
        if (Runtime.State != WeaponState.Automatic)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        FireTimer = GetFireInterval();
        if (!Targeting.TryGetTarget(Runtime, Owner, Runtime.Data.BaseRange, out Transform target))
            return;

        FireAt(target.position, 1f, false);
    }

    // Fires manually toward aim direction and consumes one ammo.
    public virtual void TickManual(float deltaTime, Vector3 aimDirection, bool isFiring)
    {
        if (Runtime.State != WeaponState.Manual || !isFiring)
            return;

        FireTimer -= deltaTime;
        if (FireTimer > 0f)
            return;

        FireTimer = GetFireInterval();
        Runtime.CurrentAmmo -= 1f;
        FireAt(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, 1f, false);
    }

    // Executes baseline active ability projectile and ammo spending.
    public virtual void UseActiveAbility(Vector3 aimDirection)
    {
        if (Runtime.State != WeaponState.Manual)
            return;

        Runtime.CurrentAmmo -= Runtime.Data.ActiveAbilityAmmoCost;
        FireAt(Spawn.position + aimDirection.normalized * Runtime.Data.BaseRange, 1.75f, false);
    }

    // Enables critical hits by default for generic projectile weapons.
    public virtual bool CanCrit() => true;

    // Computes interval from base rate, stats, level/path, and heat.
    protected virtual float GetFireInterval()
    {
        float attackSpeed = Mathf.Max(0.01f, Stats.GetStat(StatType.AttackSpeedMultiplier));
        float weaponRate = Mathf.Max(0.01f, Runtime.Data.BaseAttackRate * WeaponMath.GetAttackRateMultiplier(Runtime));
        float heatBonus = Heat != null ? 1f + Heat.NormalizedHeat * 0.25f : 1f;
        return 1f / Mathf.Max(0.05f, weaponRate * attackSpeed * heatBonus);
    }

    // Spawns explosive projectile with configurable radius and falloff behavior.
    protected void FireExplosiveAt(Vector3 targetPosition, float damageScale, bool eliteOrBoss, float explosionRadius, float falloff)
    {
        if (Pool == null || Spawn == null)
            return;

        Vector3 direction = (targetPosition - Spawn.position).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
        float damage = WeaponDamageResolver.CalculateDamage(Stats, Runtime, eliteOrBoss, CanCrit()) * damageScale;
        Pool.TrySpawnExplosiveProjectile(Spawn.position, rotation, direction, Mathf.RoundToInt(damage), explosionRadius, falloff);
    }

    // Spawns one projectile toward position and resolves final scaled damage.
    protected void FireAt(Vector3 targetPosition, float damageScale, bool eliteOrBoss)
    {
        if (Pool == null || Spawn == null)
            return;

        Vector3 direction = (targetPosition - Spawn.position).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
        float damage = WeaponDamageResolver.CalculateDamage(Stats, Runtime, eliteOrBoss, CanCrit()) * damageScale;
        Pool.TrySpawnProjectile(Spawn.position, rotation, direction, Mathf.RoundToInt(damage));
    }
}
