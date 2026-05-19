using UnityEngine;

public static class WeaponMath
{
    // Gets exact level tuning row for current weapon level.
    public static WeaponLevelData GetLevelData(WeaponInstance instance)
    {
        if (instance == null || instance.Data == null)
            return null;

        for (int i = 0; i < instance.Data.LevelData.Count; i++)
        {
            WeaponLevelData entry = instance.Data.LevelData[i];
            if (entry != null && entry.Level == instance.Level)
                return entry;
        }

        return null;
    }

    // Gets selected path tuning payload for current weapon.
    public static WeaponUpgradePathData GetPathData(WeaponInstance instance)
    {
        if (instance == null || instance.Data == null || !instance.HasAdvancedPath)
            return null;

        return instance.SelectedPath switch
        {
            WeaponUpgradePath.PathA => instance.Data.PathA,
            WeaponUpgradePath.PathB => instance.Data.PathB,
            _ => null
        };
    }

    // Calculates max manual ammo including level and path modifiers.
    public static float GetMaxManualAmmo(WeaponInstance instance, PlayerStats stats)
    {
        if (instance == null || instance.Data == null || stats == null)
            return 0f;

        float ammo = Mathf.Max(0f, instance.Data.BaseManualAmmo);
        WeaponLevelData levelData = GetLevelData(instance);
        WeaponUpgradePathData pathData = GetPathData(instance);

        if (levelData != null)
            ammo *= Mathf.Max(0.01f, levelData.ManualAmmoMultiplier);

        if (pathData != null && pathData.ManualAmmoOverride >= 0f)
            ammo = pathData.ManualAmmoOverride;

        return ammo;
    }

    // Calculates final attack-rate scalar from level and selected path.
    public static float GetAttackRateMultiplier(WeaponInstance instance)
    {
        float result = 1f;
        WeaponLevelData levelData = GetLevelData(instance);
        WeaponUpgradePathData pathData = GetPathData(instance);

        if (levelData != null)
            result *= Mathf.Max(0.01f, levelData.AttackRateMultiplier);
        if (pathData != null)
            result *= Mathf.Max(0.01f, pathData.AttackRateMultiplier);

        return result;
    }
}
