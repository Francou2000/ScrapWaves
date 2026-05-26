using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnRoulette", menuName = "ScrapWaves/Spawning/Enemy Spawn Roulette", order = 0)]
public class EnemySpawnRouletteConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EnemySpawnKind Kind;
        public GameObject Prefab;
        [Min(0)] public int BaseWeight = 1;
        [Min(1)] public int BatchSize = 1;
        public bool IsVariant;
    }

    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

    [SerializeField, Min(1f)] private float _variantWeightBonusEverySeconds = 120f;
    [SerializeField, Min(0)] private int _variantWeightBonusPerStep = 3;

    public Entry[] Entries => _entries;
    public float VariantWeightBonusEverySeconds => _variantWeightBonusEverySeconds;
    public int VariantWeightBonusPerStep => _variantWeightBonusPerStep;

    public Entry GetEntry(EnemySpawnKind kind)
    {
        if (_entries == null) return null;
        foreach (Entry entry in _entries)
        {
            if (entry != null && entry.Kind == kind)
                return entry;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_entries == null || _entries.Length == 0)
            SeedDefaultEntries();
    }

    public void InitializeDefaultEntries(GameObject defaultPrefab = null)
    {
        SeedDefaultEntries();
        if (defaultPrefab == null || _entries == null) return;
        foreach (Entry entry in _entries)
        {
            if (entry != null && entry.Prefab == null)
                entry.Prefab = defaultPrefab;
        }
    }

    [ContextMenu("Seed Default Entries")]
    private void SeedDefaultEntries()
    {
        _entries = new[]
        {
            new Entry { Kind = EnemySpawnKind.JunkSlime, BaseWeight = 50, BatchSize = 10, IsVariant = false },
            new Entry { Kind = EnemySpawnKind.VigilanceDrone, BaseWeight = 30, BatchSize = 3, IsVariant = false },
            new Entry { Kind = EnemySpawnKind.ChaserBot, BaseWeight = 20, BatchSize = 5, IsVariant = false },
            new Entry { Kind = EnemySpawnKind.HellfireSlime, BaseWeight = 5, BatchSize = 10, IsVariant = true },
            new Entry { Kind = EnemySpawnKind.BomberDrone, BaseWeight = 5, BatchSize = 3, IsVariant = true },
            new Entry { Kind = EnemySpawnKind.ShockerBot, BaseWeight = 5, BatchSize = 5, IsVariant = true }
        };
    }
#endif
}
