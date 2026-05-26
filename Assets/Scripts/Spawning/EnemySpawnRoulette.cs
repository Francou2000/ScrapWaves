using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct EnemySpawnWeightSnapshot
{
    public EnemySpawnKind Kind { get; }
    public int EffectiveWeight { get; }
    public int CumulativeMin { get; }
    public int CumulativeMax { get; }
    public float Percent { get; }

    public EnemySpawnWeightSnapshot(
        EnemySpawnKind kind,
        int effectiveWeight,
        int cumulativeMin,
        int cumulativeMax,
        float percent)
    {
        Kind = kind;
        EffectiveWeight = effectiveWeight;
        CumulativeMin = cumulativeMin;
        CumulativeMax = cumulativeMax;
        Percent = percent;
    }
}

public readonly struct EnemySpawnRollResult
{
    public EnemySpawnKind SelectedKind { get; }
    public int BatchSize { get; }
    public GameObject Prefab { get; }
    public int TotalWeight { get; }
    public int RollIndex { get; }
    public int VariantWeightBonus { get; }
    public IReadOnlyList<EnemySpawnWeightSnapshot> Snapshots { get; }

    public EnemySpawnRollResult(
        EnemySpawnKind selectedKind,
        int batchSize,
        GameObject prefab,
        int totalWeight,
        int rollIndex,
        int variantWeightBonus,
        IReadOnlyList<EnemySpawnWeightSnapshot> snapshots)
    {
        SelectedKind = selectedKind;
        BatchSize = batchSize;
        Prefab = prefab;
        TotalWeight = totalWeight;
        RollIndex = rollIndex;
        VariantWeightBonus = variantWeightBonus;
        Snapshots = snapshots;
    }
}

public class EnemySpawnRoulette
{
    private readonly EnemySpawnRouletteConfig _config;
    private readonly List<EnemySpawnWeightSnapshot> _snapshotBuffer = new(8);

    public EnemySpawnRoulette(EnemySpawnRouletteConfig config)
    {
        _config = config;
    }

    public int GetVariantWeightBonus(float runTimeSeconds)
    {
        if (_config == null) return 0;

        float interval = Mathf.Max(1f, _config.VariantWeightBonusEverySeconds);
        int steps = Mathf.FloorToInt(runTimeSeconds / interval);
        return steps * Mathf.Max(0, _config.VariantWeightBonusPerStep);
    }

    public Dictionary<EnemySpawnKind, int> GetEffectiveWeights(float runTimeSeconds)
    {
        var weights = new Dictionary<EnemySpawnKind, int>();
        if (_config?.Entries == null) return weights;

        int variantBonus = GetVariantWeightBonus(runTimeSeconds);
        foreach (EnemySpawnRouletteConfig.Entry entry in _config.Entries)
        {
            if (entry == null || entry.BaseWeight <= 0) continue;

            int weight = entry.BaseWeight;
            if (entry.IsVariant)
                weight += variantBonus;

            weights[entry.Kind] = weight;
        }

        return weights;
    }

    public EnemySpawnRollResult Roll(float runTimeSeconds)
    {
        _snapshotBuffer.Clear();
        if (_config?.Entries == null || _config.Entries.Length == 0)
        {
            return new EnemySpawnRollResult(
                default,
                0,
                null,
                0,
                0,
                0,
                _snapshotBuffer);
        }

        int variantBonus = GetVariantWeightBonus(runTimeSeconds);
        int totalWeight = 0;

        foreach (EnemySpawnRouletteConfig.Entry entry in _config.Entries)
        {
            if (entry == null || entry.BaseWeight <= 0) continue;

            int effective = entry.BaseWeight;
            if (entry.IsVariant)
                effective += variantBonus;

            int cumulativeMin = totalWeight;
            totalWeight += effective;
            int cumulativeMax = totalWeight - 1;
            float percent = 0f;
            _snapshotBuffer.Add(new EnemySpawnWeightSnapshot(
                entry.Kind,
                effective,
                cumulativeMin,
                cumulativeMax,
                percent));
        }

        if (totalWeight <= 0)
        {
            return new EnemySpawnRollResult(
                default,
                0,
                null,
                0,
                0,
                variantBonus,
                _snapshotBuffer);
        }

        for (int i = 0; i < _snapshotBuffer.Count; i++)
        {
            EnemySpawnWeightSnapshot snap = _snapshotBuffer[i];
            _snapshotBuffer[i] = new EnemySpawnWeightSnapshot(
                snap.Kind,
                snap.EffectiveWeight,
                snap.CumulativeMin,
                snap.CumulativeMax,
                snap.EffectiveWeight / (float)totalWeight * 100f);
        }

        int rollIndex = UnityEngine.Random.Range(0, totalWeight);
        EnemySpawnKind selected = _snapshotBuffer[_snapshotBuffer.Count - 1].Kind;
        EnemySpawnRouletteConfig.Entry selectedEntry = null;

        foreach (EnemySpawnWeightSnapshot snap in _snapshotBuffer)
        {
            if (rollIndex >= snap.CumulativeMin && rollIndex <= snap.CumulativeMax)
            {
                selected = snap.Kind;
                break;
            }
        }

        selectedEntry = _config.GetEntry(selected);
        int batchSize = selectedEntry != null ? Mathf.Max(1, selectedEntry.BatchSize) : 1;
        GameObject prefab = selectedEntry?.Prefab;

        return new EnemySpawnRollResult(
            selected,
            batchSize,
            prefab,
            totalWeight,
            rollIndex,
            variantBonus,
            new List<EnemySpawnWeightSnapshot>(_snapshotBuffer));
    }
}
