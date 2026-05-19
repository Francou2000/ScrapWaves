using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScrapWaves/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string WeaponId;
    public string DisplayName;
    public WeaponType WeaponType = WeaponType.AutomaticCannon;
    public WeaponTargetingMode AutoTargetingMode = WeaponTargetingMode.ClosestInRange;
    public WeaponManualMode ManualMode = WeaponManualMode.AimAtReticle;

    public float BaseDamage = 10f;
    public float BaseAttackRate = 1f;
    public float BaseRange = 12f;
    public float BaseKnockback = 1f;
    public float BaseManualAmmo = 100f;
    public float ActiveAbilityAmmoCost = 20f;

    public List<WeaponLevelData> LevelData = new();
    public WeaponUpgradePathData PathA;
    public WeaponUpgradePathData PathB;
}