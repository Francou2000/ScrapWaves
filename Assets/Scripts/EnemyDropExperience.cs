using UnityEngine;

/// <summary>
/// Añade a un enemigo: al morir spawnea una bolita de XP usando el pool. Requiere <see cref="EnemyHealth"/> en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyDropExperience : MonoBehaviour
{
    [SerializeField, Tooltip("Opcional: fuerza un pool concreto. Si está vacío, se usa XPPool.Instance / búsqueda en la escena.")]
    private XPPool _xpPoolOverride;

    [SerializeField, Min(0), Tooltip("Experiencia que suelta este enemigo al morir.")]
    private int _experienceAmount = 8;

    private EnemyHealth _health;
    private static bool _warnedMissingPool;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        _health.OnDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        _health.OnDied -= OnEnemyDied;
    }

    private void OnEnemyDied()
    {
        if (_experienceAmount <= 0)
            return;

        XPPool pool = _xpPoolOverride != null ? _xpPoolOverride : XPPool.GetInstance();
        if (pool == null)
        {
            if (!_warnedMissingPool)
            {
                _warnedMissingPool = true;
                Debug.LogWarning(
                    "EnemyDropExperience: no hay XPPool en la escena. Añade un GameObject con XPPool o asigna _xpPoolOverride.",
                    this);
            }

            return;
        }

        pool.TrySpawn(transform.position, _experienceAmount);
    }
}
