using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SpawnRouletteDebugHarness : MonoBehaviour
{
    [SerializeField] private EnemySpawnRouletteConfig _config;

    [SerializeField, Tooltip("Vacío = PlayerMovement.PlayerTransform")]
    private Transform _player;

    [SerializeField, Min(0f)] private float _minSpawnRadius = 8f;

    [SerializeField, Min(0f)] private float _maxSpawnRadius = 18f;

    [SerializeField] private float _spawnHeightOffset;

    [Header("Spawn en suelo")]
    [SerializeField] private LayerMask _groundRaycastMask;

    [SerializeField] private LayerMask _fallbackGroundRaycastMask;

    [SerializeField] private LayerMask _overlapSolidMask;

    [SerializeField, Min(1f)] private float _raycastStartHeight = 48f;

    [SerializeField, Min(1f)] private float _raycastMaxDistance = 220f;

    [SerializeField, Min(0f)] private float _maxAbsSpawnSurfaceDeltaY = 3.5f;

    [SerializeField, Min(0f)] private float _surfaceSeparation = 0.02f;

    [SerializeField, Min(0)] private int _maxProjectionIterations = 14;

    [SerializeField, Min(0f)] private float _resolveStepUp = 0.08f;

    [SerializeField, Min(0f)] private float _resolveStepOut = 0.06f;

    private EnemySpawnRoulette _roulette;
    private int _totalRolls;
    private readonly Dictionary<EnemySpawnKind, int> _spawnCountByKind = new();
    private readonly Dictionary<EnemySpawnKind, int> _rollWinsByKind = new();
    private float _runStartTime;

    private void Awake()
    {
        _runStartTime = Time.timeSinceLevelLoad;

        if (_groundRaycastMask.value == 0)
            _groundRaycastMask = LayerMask.GetMask("Terrain");
        if (_fallbackGroundRaycastMask.value == 0)
            _fallbackGroundRaycastMask = LayerMask.GetMask("Terrain", "Default");
        if (_overlapSolidMask.value == 0)
            _overlapSolidMask = LayerMask.GetMask("Terrain", "Default");

        if (_config != null)
            _roulette = new EnemySpawnRoulette(_config);

        foreach (EnemySpawnKind kind in System.Enum.GetValues(typeof(EnemySpawnKind)))
        {
            _spawnCountByKind[kind] = 0;
            _rollWinsByKind[kind] = 0;
        }
    }

    private void OnValidate()
    {
        if (_maxSpawnRadius < _minSpawnRadius)
        {
            float t = _minSpawnRadius;
            _minSpawnRadius = _maxSpawnRadius;
            _maxSpawnRadius = t;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.numpad1Key.wasPressedThisFrame)
            ExecuteRollAndSpawn();

        if (Keyboard.current.numpad4Key.wasPressedThisFrame)
            LogAverageStats();
    }

    private Transform ResolvePlayer()
    {
        if (_player != null) return _player;
        return PlayerMovement.PlayerTransform;
    }

    private float RunTimeSeconds => Time.timeSinceLevelLoad - _runStartTime;

    private void ExecuteRollAndSpawn()
    {
        if (_roulette == null || _config == null)
        {
            Debug.LogWarning("[SpawnRoulette] Asigna EnemySpawnRouletteConfig en SpawnRouletteDebugHarness.");
            return;
        }

        Transform player = ResolvePlayer();
        if (player == null)
        {
            Debug.LogWarning("[SpawnRoulette] No hay jugador (asigna _player o añade PlayerMovement).");
            return;
        }

        float runTime = RunTimeSeconds;
        EnemySpawnRollResult result = _roulette.Roll(runTime);
        _totalRolls++;

        if (!_rollWinsByKind.ContainsKey(result.SelectedKind))
            _rollWinsByKind[result.SelectedKind] = 0;
        _rollWinsByKind[result.SelectedKind]++;

        var log = new StringBuilder(512);
        log.AppendLine($"[SpawnRoulette] t={FormatTime(runTime)} | roll={result.RollIndex}/{result.TotalWeight} | variantBonus=+{result.VariantWeightBonus}");

        foreach (EnemySpawnWeightSnapshot snap in result.Snapshots)
        {
            log.AppendLine(
                $"  {snap.Kind,-16} w={snap.EffectiveWeight,3} ({snap.Percent,5:F1}%)  [{snap.CumulativeMin}..{snap.CumulativeMax}]");
        }

        if (result.Prefab == null)
        {
            log.AppendLine($"  => {result.SelectedKind} | batch={result.BatchSize} | ERROR: prefab no asignado");
            Debug.LogWarning(log.ToString());
            return;
        }

        log.AppendLine($"  => {result.SelectedKind} | batch={result.BatchSize} | spawning...");

        int spawned = 0;
        for (int i = 0; i < result.BatchSize; i++)
        {
            int dir = OrbitalSpawnPlacement.PickRandomDirectionIndex();
            if (OrbitalSpawnPlacement.TrySpawnAtOrbitalPoint(
                    player,
                    result.Prefab,
                    dir,
                    _minSpawnRadius,
                    _maxSpawnRadius,
                    _spawnHeightOffset,
                    _groundRaycastMask,
                    _fallbackGroundRaycastMask,
                    _overlapSolidMask,
                    _raycastStartHeight,
                    _raycastMaxDistance,
                    _maxAbsSpawnSurfaceDeltaY,
                    _surfaceSeparation,
                    _maxProjectionIterations,
                    _resolveStepUp,
                    _resolveStepOut,
                    out _,
                    out _,
                    out string placementLog))
            {
                spawned++;
                log.AppendLine($"  #{i + 1} {placementLog}");
            }
            else
            {
                log.AppendLine($"  #{i + 1} FAILED ({placementLog})");
            }
        }

        _spawnCountByKind[result.SelectedKind] += spawned;
        log.Append(BuildTotalsLine());
        Debug.Log(log.ToString());
    }

    private void LogAverageStats()
    {
        var log = new StringBuilder(384);
        log.AppendLine($"[SpawnRoulette Stats] rolls={_totalRolls} | runTime={FormatTime(RunTimeSeconds)}");

        if (_totalRolls == 0)
        {
            log.AppendLine("  (sin tiradas — pulsa Numpad 1)");
            Debug.Log(log.ToString());
            return;
        }

        int totalUnits = 0;
        foreach (KeyValuePair<EnemySpawnKind, int> pair in _spawnCountByKind)
            totalUnits += pair.Value;

        foreach (EnemySpawnKind kind in System.Enum.GetValues(typeof(EnemySpawnKind)))
        {
            int units = _spawnCountByKind.TryGetValue(kind, out int u) ? u : 0;
            int wins = _rollWinsByKind.TryGetValue(kind, out int w) ? w : 0;
            float unitPercent = totalUnits > 0 ? units / (float)totalUnits * 100f : 0f;
            float winPercent = _totalRolls > 0 ? wins / (float)_totalRolls * 100f : 0f;
            float avgBatchWhenWon = wins > 0 ? units / (float)wins : 0f;

            log.AppendLine(
                $"  {kind,-16} units={units,4} ({unitPercent,5:F1}% of spawns) | wins={wins,3} ({winPercent,5:F1}% of rolls) | avgUnitsPerWin={avgBatchWhenWon:F1}");
        }

        Debug.Log(log.ToString());
    }

    private string BuildTotalsLine()
    {
        var sb = new StringBuilder("[SpawnRoulette] totals: rolls=");
        sb.Append(_totalRolls);
        foreach (EnemySpawnKind kind in System.Enum.GetValues(typeof(EnemySpawnKind)))
        {
            int count = _spawnCountByKind.TryGetValue(kind, out int c) ? c : 0;
            if (count > 0)
                sb.Append($" | {kind}={count}");
        }

        return sb.ToString();
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
