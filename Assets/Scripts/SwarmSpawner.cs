using UnityEngine;

public class SwarmSpawner : MonoBehaviour
{
    [SerializeField] private SwarmEnemyPool _pool;

    [SerializeField, Tooltip("Vacío = FindAnyObjectByType. Escala intervalo, tamaño de oleada y stats al spawn.")]
    private DifficultyManager _difficultyManager;

    [SerializeField, Min(0.05f)] private float _spawnInterval = 1.5f;

    [SerializeField, Min(1)] private int _spawnPerWave = 5;

    [SerializeField, Min(0f)] private float _minSpawnRadius = 8f;

    [SerializeField, Min(0f)] private float _maxSpawnRadius = 18f;

    [SerializeField, Min(1)] private int _maxActiveEnemies = 200;

    [SerializeField] private bool _spawnOnStart = true;

    [SerializeField, Tooltip("Desplazamiento Y del spawn respecto al jugador (solo XZ se randomiza con el anillo).")]
    private float _spawnHeightOffset;

    private Transform _player;
    private float _nextSpawnTime;

    private void Awake()
    {
        if (_difficultyManager == null)
            _difficultyManager = FindAnyObjectByType<DifficultyManager>();
    }

    private void Start()
    {
        float interval = EffectiveSpawnInterval();
        _nextSpawnTime = _spawnOnStart ? 0f : Time.time + interval;
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
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        if (_pool == null)
            return;

        if (_player == null)
            _player = PlayerMovement.PlayerTransform;

        if (_player == null)
            return;

        if (Time.time < _nextSpawnTime)
            return;

        _nextSpawnTime = Time.time + EffectiveSpawnInterval();
        SpawnWave();
    }

    private float EffectiveSpawnInterval()
    {
        float scale = _difficultyManager != null ? _difficultyManager.GetSpawnIntervalScale() : 1f;
        return Mathf.Max(0.05f, _spawnInterval * scale);
    }

    private void SpawnWave()
    {
        float diffCount = _difficultyManager != null ? _difficultyManager.GetSpawnCountMultiplier() : 1f;
        int count = Mathf.Max(1, Mathf.RoundToInt(_spawnPerWave * diffCount * OverheatSwarmBoost.SpawnWaveMultiplier));
        for (int i = 0; i < count; i++)
        {
            if (_pool.ActiveLeasedCount >= _maxActiveEnemies)
                break;

            GameObject enemy = _pool.TryGet();
            if (enemy == null)
                break;

            _difficultyManager?.ApplySpawnModifiers(enemy);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(_minSpawnRadius, _maxSpawnRadius);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                _spawnHeightOffset,
                Mathf.Sin(angle) * radius);

            Vector3 position = _player.position + offset;
            enemy.transform.SetPositionAndRotation(position, Quaternion.identity);

            Vector3 toPlayer = _player.position - position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.0001f)
                enemy.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }
    }
}
