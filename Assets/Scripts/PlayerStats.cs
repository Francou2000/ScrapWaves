using UnityEngine;

/// <summary>
/// Bonificadores de stats del jugador. Los componentes (<see cref="PlayerAutoAttack"/>, etc.) leen valores efectivos aquí.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Bases (deben alinearse con el prefab / otros componentes antes de mejoras)")]
    [SerializeField, Min(1)] private int _baseDamage = 2;

    [SerializeField, Min(0.05f)] private float _baseFireInterval = 0.35f;

    [SerializeField, Min(0.1f)] private float _baseMoveSpeed = 7f;

    [SerializeField, Min(1)] private int _baseMaxHealth = 100;

    [Header("Bonificadores acumulados (solo lectura en runtime)")]
    [SerializeField] private int _bonusDamage;
    [SerializeField] private float _fireIntervalReduction;
    [SerializeField] private float _bonusMoveSpeed;
    [SerializeField] private int _bonusMaxHealth;

    /// <summary>Multiplicador de cadencia de disparo en runtime (1 = normal). Lo ajusta <see cref="OverheatManager"/> u otros buffs temporales.</summary>
    private float _runtimeFireRateMultiplier = 1f;

    private void Awake()
    {
        if (TryGetComponent(out PlayerHealth health))
            _baseMaxHealth = health.MaxHealth;
    }

    public int GetDamage() => Mathf.Max(1, _baseDamage + _bonusDamage);

    public float GetFireInterval()
    {
        float interval = _baseFireInterval - _fireIntervalReduction;
        interval /= Mathf.Max(0.01f, _runtimeFireRateMultiplier);
        return Mathf.Max(0.05f, interval);
    }

    /// <summary>Fija el multiplicador de cadencia temporal (p. ej. 1.5 = 50% más rápido). Volver a 1 al terminar el buff.</summary>
    public void SetRuntimeFireRateMultiplier(float multiplier)
    {
        _runtimeFireRateMultiplier = Mathf.Max(0.01f, multiplier);
    }

    public float GetMoveSpeed() => Mathf.Max(0.1f, _baseMoveSpeed + _bonusMoveSpeed);

    public int GetMaxHealthTotal() => Mathf.Max(1, _baseMaxHealth + _bonusMaxHealth);

    /// <summary>Aplica una mejora definida en un <see cref="Upgrade"/>.</summary>
    public void ApplyUpgrade(Upgrade upgrade)
    {
        if (upgrade == null)
            return;

        switch (upgrade.TargetStat)
        {
            case PlayerStatType.Damage:
                _bonusDamage += Mathf.RoundToInt(upgrade.Value);
                break;
            case PlayerStatType.FireRate:
                _fireIntervalReduction += upgrade.Value;
                break;
            case PlayerStatType.MoveSpeed:
                _bonusMoveSpeed += upgrade.Value;
                break;
            case PlayerStatType.MaxHealth:
                int add = Mathf.RoundToInt(upgrade.Value);
                _bonusMaxHealth += add;
                if (TryGetComponent(out PlayerHealth health))
                    health.ApplyMaxHealthIncrease(add);
                break;
        }
    }
}
