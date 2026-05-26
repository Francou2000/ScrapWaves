#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnemySpawnRouletteAssetUtility
{
    private const string AssetPath = "Assets/ScriptableObjects/Spawning/DefaultEnemySpawnRoulette.asset";

    [MenuItem("ScrapWaves/Spawning/Create Default Enemy Spawn Roulette")]
    public static void CreateDefaultAsset()
    {
        EnsureDefaultAssetExists();
    }

    public static EnemySpawnRouletteConfig EnsureDefaultAssetExists()
    {
        var existing = AssetDatabase.LoadAssetAtPath<EnemySpawnRouletteConfig>(AssetPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Spawning"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Spawning");

        var config = ScriptableObject.CreateInstance<EnemySpawnRouletteConfig>();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
        config.InitializeDefaultEntries(enemyPrefab);
        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;
        return config;
    }
}
#endif
