using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private List<StatDefinition> _statDefinitions = new();

    private readonly Dictionary<StatType, RuntimeStat> _stats = new();

    public event Action<StatType, float> OnStatChanged;

    private void Awake()
    {
        InitializeStats();
        SyncBaseMaxHealthFromHealthComponent();
    }

    private void SyncBaseMaxHealthFromHealthComponent()
    {
        if (!TryGetComponent(out PlayerHealth health)) return;
        if (_stats.TryGetValue(StatType.MaxHealth, out RuntimeStat maxHealthStat))
        {
            maxHealthStat.SetBaseValue(health.MaxHealth);
            OnStatChanged?.Invoke(StatType.MaxHealth, maxHealthStat.CurrentValue);
        }
    }

    private void InitializeStats()
    {
        _stats.Clear();

        foreach (StatDefinition definition in _statDefinitions)
        {
            if (definition == null) continue;
            if (_stats.ContainsKey(definition.StatType))
            {
                Debug.LogWarning($"PlayerStats: definición duplicada para {definition.StatType}.", this);
                continue;
            }

            _stats.Add(definition.StatType, new RuntimeStat(definition));
        }
    }

    public float GetStat(StatType statType)
    {
        if (_stats.TryGetValue(statType, out RuntimeStat stat))
            return stat.CurrentValue;

        Debug.LogWarning($"PlayerStats: falta el stat {statType} en _statDefinitions.", this);
        return 0f;
    }

    public int GetStatInt(StatType statType) => Mathf.FloorToInt(GetStat(statType));

    public StatDefinition GetDefinition(StatType statType)
    {
        return _stats.TryGetValue(statType, out RuntimeStat stat) ? stat.Definition : null;
    }

    public IReadOnlyList<StatDefinition> GetAllDefinitions() => _statDefinitions;

    public void AddModifier(StatModifier modifier)
    {
        if (!_stats.TryGetValue(modifier.StatType, out RuntimeStat stat))
        {
            Debug.LogWarning($"PlayerStats: no se puede aplicar modificador, falta {modifier.StatType}.", this);
            return;
        }

        stat.AddModifier(modifier);
        OnStatChanged?.Invoke(modifier.StatType, stat.CurrentValue);
    }

    public void RemoveModifiersFromSource(object sourceReference)
    {
        foreach (RuntimeStat stat in _stats.Values)
            stat.RemoveModifiersFromSource(sourceReference);

        RecalculateAllStats();
    }

    public void ClearModifiersFromSourceType(StatUpgradeSource source)
    {
        foreach (RuntimeStat stat in _stats.Values)
            stat.ClearModifiersFromSourceType(source);

        RecalculateAllStats();
    }

    private void RecalculateAllStats()
    {
        foreach (KeyValuePair<StatType, RuntimeStat> pair in _stats)
            OnStatChanged?.Invoke(pair.Key, pair.Value.CurrentValue);
    }

    // Compatibilidad temporal con sistemas existentes.
    public int GetDamage() => Mathf.Max(1, Mathf.RoundToInt(GetStat(StatType.DamageFlat)));
    public float GetFireInterval()
    {
        float rateMult = Mathf.Max(0.01f, GetStat(StatType.AttackSpeedMultiplier));
        float baseInterval = Mathf.Max(0.05f, GetStat(StatType.BaseFireInterval));
        return baseInterval / rateMult;
    }

    public float GetMoveSpeed() => Mathf.Max(0.1f, GetStat(StatType.MovementSpeed));
    public int GetMaxHealthTotal() => Mathf.Max(1, Mathf.RoundToInt(GetStat(StatType.MaxHealth)));

    public void SetRuntimeFireRateMultiplier(float multiplier)
    {
        float delta = Mathf.Max(0.01f, multiplier) - 1f;
        ClearModifiersFromSourceType(StatUpgradeSource.TemporaryEffect);
        AddModifier(new StatModifier(StatType.AttackSpeedMultiplier, delta, StatUpgradeSource.TemporaryEffect, this));
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        if (upgrade == null) return;

        StatType mapped = upgrade.TargetStat switch
        {
            PlayerStatType.Damage => StatType.DamageFlat,
            PlayerStatType.FireRate => StatType.AttackSpeedMultiplier,
            PlayerStatType.MoveSpeed => StatType.MovementSpeed,
            PlayerStatType.MaxHealth => StatType.MaxHealth,
            _ => StatType.MaxHealth
        };

        AddModifier(new StatModifier(mapped, upgrade.Value, StatUpgradeSource.LevelUp, upgrade));

        if (mapped == StatType.MaxHealth && TryGetComponent(out PlayerHealth health))
            health.ApplyMaxHealthIncrease(Mathf.RoundToInt(upgrade.Value));
    }
}
