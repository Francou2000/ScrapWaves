using UnityEngine;

/// <summary>
/// En el prefab del enemigo: al morir notifica al <see cref="HeatManager"/> (mismo patrón que <see cref="EnemyDropExperience"/>).
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyHeatOnKill : MonoBehaviour
{
    [SerializeField, Tooltip("Opcional: fuerza un manager concreto. Si está vacío, se usa HeatManager.Instance / búsqueda en escena.")]
    private HeatManager _heatManagerOverride;

    private EnemyHealth _health;
    private static bool _warnedMissingManager;

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
        HeatManager manager = _heatManagerOverride != null ? _heatManagerOverride : HeatManager.GetInstance();
        if (manager == null)
        {
            if (!_warnedMissingManager)
            {
                _warnedMissingManager = true;
                Debug.LogWarning(
                    "EnemyHeatOnKill: no hay HeatManager en la escena. Añade un GameObject con HeatManager o asigna _heatManagerOverride.",
                    this);
            }

            return;
        }

        manager.RegisterKill();
    }
}
