using UnityEngine;

public static class WeaponDamageResolver
{
    // Calculates damage from weapon base, level/path, stats, and crit.
    public static float CalculateDamage(PlayerStats stats, WeaponInstance instance, bool eliteOrBoss, bool canCrit, float critMultiplierOverride = 1f)
    {
        float damage = Mathf.Max(0f, instance.Data.BaseDamage);
        damage *= GetLevelDamageMultiplier(instance);
        damage *= GetPathDamageMultiplier(instance);
        damage *= Mathf.Max(0f, stats.GetStat(StatType.DamageMultiplier));

        if (eliteOrBoss)
            damage *= Mathf.Max(0f, stats.GetStat(StatType.EliteDamageMultiplier));

        if (canCrit && RollCrit(stats))
            damage *= Mathf.Max(1f, stats.GetStat(StatType.CriticalDamage) * critMultiplierOverride);

        return damage;
    }

    // Returns configured level damage multiplier for weapon instance.
    private static float GetLevelDamageMultiplier(WeaponInstance instance)
    {
        WeaponLevelData levelData = WeaponMath.GetLevelData(instance);
        return levelData != null ? Mathf.Max(0.01f, levelData.DamageMultiplier) : 1f;
    }

    // Returns selected path damage multiplier if advanced path exists.
    private static float GetPathDamageMultiplier(WeaponInstance instance)
    {
        WeaponUpgradePathData pathData = WeaponMath.GetPathData(instance);
        return pathData != null ? Mathf.Max(0.01f, pathData.DamageMultiplier) : 1f;
    }

    // Rolls crit chance from stat system with clamping.
    private static bool RollCrit(PlayerStats stats)
    {
        float critChance = Mathf.Clamp01(stats.GetStat(StatType.CriticalChance));
        return UnityEngine.Random.value <= critChance;
    }
}