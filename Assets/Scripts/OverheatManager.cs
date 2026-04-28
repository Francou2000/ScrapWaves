using System;
using UnityEngine;

/// <summary>
/// Motivo por el que termina la fase de Overheat (buff + ventana de boss).
/// </summary>
public enum OverheatEndReason
{
    TimeExpired,
    BossDefeated,
    Interrupted
}

/// <summary>
/// Al llenar el Heat (<see cref="HeatManager.OnOverheat"/>), entra en Overheat: aplica un multiplicador de cadencia al <see cref="PlayerStats"/>
/// durante <see cref="_overheatDuration"/> y luego resetea el Heat a <see cref="_heatAfterOverheat"/>.
/// <see cref="BossManager"/> puede escuchar <see cref="OnOverheatStarted"/> / <see cref="OnOverheatFinished"/> y llamar <see cref="NotifyBossDefeatedEarly"/>.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-32)]
public class OverheatManager : MonoBehaviour
{
    [SerializeField, Tooltip("Si está vacío, se usa HeatManager.GetInstance().")]
    private HeatManager _heatManager;

    [SerializeField, Tooltip("Stats del jugador (mismo GameObject o referencia explícita).")]
    private PlayerStats _playerStats;

    [SerializeField, Min(0.1f), Tooltip("Segundos que dura el estado Overheat.")]
    private float _overheatDuration = 5f;

    [SerializeField, Min(0.01f), Tooltip("Multiplicador de cadencia de disparo durante Overheat (2 = el doble de rápido, intervalo ~mitad).")]
    private float _fireRateMultiplier = 1.5f;

    [SerializeField, Min(0f), Tooltip("Heat tras terminar Overheat (0 = vacío; sube de nuevo con kills).")]
    private float _heatAfterOverheat = 0f;

    [SerializeField, Tooltip("Loguear inicio y fin de Overheat.")]
    private bool _logState;

    [SerializeField, Tooltip("Pool de enemigos comunes; vacío = FindAnyObjectByType al terminar Overheat.")]
    private SwarmEnemyPool _swarmEnemyPool;

    private float _remaining;
    private bool _isOverheating;

    public bool IsOverheating => _isOverheating;

    /// <summary>Tiempo restante de Overheat en segundos.</summary>
    public float OverheatTimeRemaining => _isOverheating ? Mathf.Max(0f, _remaining) : 0f;

    /// <summary>Progreso 0–1 del Overheat (1 = recién empezado, 0 = a punto de acabar).</summary>
    public float NormalizedOverheatTimeRemaining =>
        _isOverheating && _overheatDuration > 0f ? Mathf.Clamp01(_remaining / _overheatDuration) : 0f;

    /// <summary>Duración configurada de la fase (misma que usa el temporizador).</summary>
    public float ConfiguredOverheatDuration => _overheatDuration;

    /// <summary>Al entrar en Overheat (buff activo y temporizador iniciado).</summary>
    public event Action OnOverheatStarted;

    /// <summary>Al salir de Overheat; incluye éxito por boss, tiempo agotado o interrupción.</summary>
    public event Action<OverheatEndReason> OnOverheatFinished;

    private void Awake()
    {
        if (_heatManager == null)
            _heatManager = HeatManager.GetInstance();
        if (_playerStats == null)
            _playerStats = FindAnyObjectByType<PlayerStats>();
        if (_swarmEnemyPool == null)
            _swarmEnemyPool = FindAnyObjectByType<SwarmEnemyPool>();
    }

    private void OnEnable()
    {
        if (_heatManager != null)
            _heatManager.OnOverheat += OnMaxHeatReached;
    }

    private void OnDisable()
    {
        if (_heatManager != null)
            _heatManager.OnOverheat -= OnMaxHeatReached;

        if (_isOverheating)
            EndOverheat(OverheatEndReason.Interrupted);
    }

    private void Update()
    {
        if (!_isOverheating)
            return;

        _remaining -= Time.deltaTime;

        if (_remaining <= 0f)
            EndOverheat(OverheatEndReason.TimeExpired);
    }

    /// <summary>Si el boss muere a tiempo, termina Overheat como éxito (sin esperar al temporizador).</summary>
    public void NotifyBossDefeatedEarly()
    {
        if (!_isOverheating)
            return;

        EndOverheat(OverheatEndReason.BossDefeated);
    }

    private void OnMaxHeatReached()
    {
        if (_isOverheating)
            return;

        _isOverheating = true;
        _remaining = _overheatDuration;
        OverheatSwarmBoost.SetIntensity(false);

        if (_playerStats != null)
            _playerStats.SetRuntimeFireRateMultiplier(_fireRateMultiplier);
        else if (_logState)
            Debug.LogWarning("OverheatManager: no hay PlayerStats; no se aplica buff de cadencia.", this);

        if (_logState)
            Debug.Log($"Overheat iniciado ({_overheatDuration:0.#} s, x{_fireRateMultiplier:0.##} fire rate)", this);

        OnOverheatStarted?.Invoke();
    }

    private void EndOverheat(OverheatEndReason reason)
    {
        _isOverheating = false;
        _remaining = 0f;
        OverheatSwarmBoost.SetIntensity(false);

        if (_swarmEnemyPool == null)
            _swarmEnemyPool = FindAnyObjectByType<SwarmEnemyPool>();
        _swarmEnemyPool?.ReleaseAllActive();

        if (_playerStats != null)
            _playerStats.SetRuntimeFireRateMultiplier(1f);

        if (_heatManager != null)
        {
            _heatManager.ApplyEscalationAfterOverheat();
            float cap = _heatManager.MaxHeat;
            _heatManager.SetHeat(Mathf.Clamp(_heatAfterOverheat, 0f, cap));
        }

        if (_logState)
            Debug.Log($"Overheat terminado ({reason}); escalado de heat aplicado; enemigos del pool devueltos.", this);

        OnOverheatFinished?.Invoke(reason);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_overheatDuration < 0.1f)
            _overheatDuration = 0.1f;
        if (_fireRateMultiplier < 0.01f)
            _fireRateMultiplier = 0.01f;
        if (_heatAfterOverheat < 0f)
            _heatAfterOverheat = 0f;
    }
#endif
}
