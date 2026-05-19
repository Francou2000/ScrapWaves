using UnityEngine;

public interface IWeaponTargeting
{
    bool TryGetTarget(WeaponInstance weapon, Transform owner, float range, out Transform target);
}

public sealed class ClosestEnemyTargeting : IWeaponTargeting
{
    // Finds nearest enemy in range using shared registry.
    public bool TryGetTarget(WeaponInstance weapon, Transform owner, float range, out Transform target)
    {
        return EnemyRegistry.TryGetClosestOnPlane(owner.position, range, out target);
    }
}
