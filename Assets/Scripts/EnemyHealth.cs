using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int _maxHealth = 12;

    private int _prefabMaxHealth;
    private int _currentHealth;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    /// <summary>Fija vida máxima y rellena al máximo (p. ej. boss spawneado por <see cref="BossManager"/>).</summary>
    public void ApplyConfiguredMaxHealth(int maxHealth)
    {
        _prefabMaxHealth = Mathf.Max(1, maxHealth);
        _maxHealth = _prefabMaxHealth;
        _currentHealth = _maxHealth;
    }

    /// <summary>Se invoca una vez al pasar a 0 HP, antes del despawn / desactivar el objeto.</summary>
    public event System.Action OnDied;

    private void Awake()
    {
        _prefabMaxHealth = _maxHealth;
    }

    private void OnEnable()
    {
        _maxHealth = _prefabMaxHealth;
        _currentHealth = _maxHealth;
    }

    /// <summary>Tras salir del pool; <see cref="DifficultyManager"/> ajusta vida según la partida.</summary>
    public void ConfigureDifficultyForSpawn(float healthMultiplier)
    {
        int newMax = Mathf.Max(1, Mathf.RoundToInt(_prefabMaxHealth * healthMultiplier));
        _maxHealth = newMax;
        _currentHealth = newMax;
    }

    public bool ApplyDamage(int amount)
    {
        if (amount <= 0 || _currentHealth <= 0)
            return false;

        _currentHealth -= amount;
        if (_currentHealth > 0)
        {
            AudioManager.TryPlayEnemyHit();
            return true;
        }

        _currentHealth = 0;

        AudioManager.TryPlayEnemyDeath();
        OnDied?.Invoke();
        RunCombatStats.RegisterEnemyEliminated();

        if (TryGetComponent(out SwarmPooledEnemy pooled))
            pooled.Despawn();
        else
            gameObject.SetActive(false);

        return true;
    }
}
