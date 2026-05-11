using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MovementSpeed, JumpHeight, AirJumps, DashCharges, DashSpeed,
    DamageMultiplier, DamageFlat, EliteDamageMultiplier, AttackSpeedMultiplier, ProjectileAreaSize, CriticalChance, CriticalDamage, Knockback,
    MaxHealth, HealthRegeneration, Lifesteal, DamageResistance,
    PickupRange, ExtraEliteChance, Scavenging, DoubleDrop,
    BaseFireInterval
}

public enum StatCategory { Mobility, Offensive, Defensive, Miscellaneous }
public enum StatUpgradeSource { Base, LevelUp, PassiveItem, Weapon, TemporaryEffect }

[CreateAssetMenu(menuName = "ScrapWaves/Stats/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [field: SerializeField] public StatType StatType { get; private set; }
    [field: SerializeField] public StatCategory Category { get; private set; }
    [field: SerializeField] public float BaseValue { get; private set; }
    [field: SerializeField] public bool UpgradeableByLevel { get; private set; }
    [field: SerializeField] public bool UpgradeableByItems { get; private set; }
    [field: SerializeField] public float LevelUpgradeBaseAmount { get; private set; }
    [field: SerializeField] public bool IsPercentage { get; private set; }
    [field: SerializeField] public bool IsInteger { get; private set; }
}

[Serializable]
public class StatModifier
{
    public StatType StatType;
    public float Value;
    public StatUpgradeSource Source;
    public object SourceReference;

    public StatModifier(StatType statType, float value, StatUpgradeSource source, object sourceReference = null)
    {
        StatType = statType;
        Value = value;
        Source = source;
        SourceReference = sourceReference;
    }
}

[Serializable]
public class RuntimeStat
{
    [SerializeField] private StatDefinition _definition;
    [SerializeField] private float _baseOverride;
    [SerializeField] private bool _useBaseOverride;

    private readonly List<StatModifier> _modifiers = new();

    public StatDefinition Definition => _definition;
    public float BaseValue => _useBaseOverride ? _baseOverride : _definition.BaseValue;

    public float CurrentValue
    {
        get
        {
            float value = BaseValue;
            foreach (StatModifier modifier in _modifiers) value += modifier.Value;
            if (_definition.IsInteger) value = Mathf.Floor(value);
            return value;
        }
    }

    public RuntimeStat(StatDefinition definition) => _definition = definition;
    public void SetBaseValue(float value) { _baseOverride = value; _useBaseOverride = true; }
    public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);
    public void RemoveModifiersFromSource(object sourceReference) => _modifiers.RemoveAll(m => m.SourceReference == sourceReference);
    public void ClearModifiersFromSourceType(StatUpgradeSource source) => _modifiers.RemoveAll(m => m.Source == source);
}
