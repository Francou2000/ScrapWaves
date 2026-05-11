using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStatsLevelUpHandler : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private int _levelCap = 36;

    private readonly Dictionary<StatType, int> _rouletteWeights = new();

    // Resolves dependencies and initializes roulette weights at startup.
    private void Awake()
    {
        if (_playerStats == null) _playerStats = GetComponent<PlayerStats>();
        InitializeRouletteWeights();
    }

    // Seeds roulette weights for stats allowed to level up.
    private void InitializeRouletteWeights()
    {
        _rouletteWeights.Clear();
        if (_playerStats == null) return;

        foreach (StatDefinition definition in _playerStats.GetAllDefinitions())
        {
            if (definition != null && definition.UpgradeableByLevel)
                _rouletteWeights[definition.StatType] = 5;
        }
    }

    // Applies multiple weighted stat upgrades when a new level is reached.
    public void ApplyLevelUpStats(int newLevel)
    {
        if (_rouletteWeights.Count == 0) return;

        int upgradeCount = GetUpgradeCountForLevel(newLevel);
        HashSet<StatType> selectedThisLevel = new();

        for (int i = 0; i < upgradeCount; i++)
        {
            StatType selectedStat = RollStat();
            selectedThisLevel.Add(selectedStat);
            ApplyUpgradeToStat(selectedStat, newLevel);
            _rouletteWeights[selectedStat] = Mathf.Max(1, _rouletteWeights[selectedStat] - 1);
        }

        IncreaseWeightsForUnselectedStats(selectedThisLevel);
    }

    // Selects one stat using weighted random roulette selection.
    private StatType RollStat()
    {
        int totalWeight = 0;
        foreach (int weight in _rouletteWeights.Values) totalWeight += weight;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (KeyValuePair<StatType, int> pair in _rouletteWeights)
        {
            current += pair.Value;
            if (roll < current) return pair.Key;
        }

        foreach (StatType type in _rouletteWeights.Keys) return type;
        return StatType.MaxHealth;
    }

    // Computes and applies one level-up modifier to selected stat.
    private void ApplyUpgradeToStat(StatType statType, int newLevel)
    {
        StatDefinition definition = _playerStats.GetDefinition(statType);
        if (definition == null) return;

        float amount = StatMath.CalculateStatUpgradeAmount(definition.LevelUpgradeBaseAmount, newLevel, _levelCap);
        _playerStats.AddModifier(new StatModifier(statType, amount, StatUpgradeSource.LevelUp));
    }

    // Increases weights for stats not selected this level-up cycle.
    private void IncreaseWeightsForUnselectedStats(HashSet<StatType> selectedThisLevel)
    {
        List<StatType> allStats = new(_rouletteWeights.Keys);
        foreach (StatType statType in allStats)
            if (!selectedThisLevel.Contains(statType)) _rouletteWeights[statType] += 1;
    }

    // Returns how many stat upgrades to grant for a level.
    private static int GetUpgradeCountForLevel(int level)
    {
        if (level >= 36) return 20;
        if (level >= 30) return 11;
        if (level >= 26) return 10;
        if (level >= 21) return 9;
        if (level >= 16) return 8;
        if (level >= 11) return 7;
        if (level >= 6) return 6;
        return 5;
    }
}

public static class StatMath
{
    // Calculates upgrade scaling factor based on current level and cap.
    public static float CalculateLevelScale(int currentLevel, int levelCap) => 1f + (currentLevel / (levelCap / 2f));

    // Calculates final upgrade amount including scaling and random variation.
    public static float CalculateStatUpgradeAmount(float baseUpgradeAmount, int currentLevel, int levelCap)
    {
        float levelScale = CalculateLevelScale(currentLevel, levelCap);
        float randomFactor = Random.Range(0.9f, 1.1f);
        return baseUpgradeAmount * levelScale * randomFactor;
    }
}
