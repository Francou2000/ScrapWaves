using System;

/// <summary>
/// Estadísticas de la partida actual. Se incrementa al eliminar un enemigo (vía <see cref="EnemyHealth"/>).
/// </summary>
public static class RunCombatStats
{
    static int _enemiesEliminated;

    public static int EnemiesEliminated => _enemiesEliminated;

    public static event Action OnEnemiesEliminatedChanged;

    public static void RegisterEnemyEliminated()
    {
        _enemiesEliminated++;
        OnEnemiesEliminatedChanged?.Invoke();
    }

    public static void Reset()
    {
        _enemiesEliminated = 0;
        OnEnemiesEliminatedChanged?.Invoke();
    }
}
