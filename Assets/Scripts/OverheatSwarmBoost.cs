/// <summary>
/// Multiplicadores globales durante la fase intensa del Overheat (último tramo del temporizador).
/// Solo afecta a enemigos del <see cref="SwarmEnemyPool"/> (no al boss).
/// </summary>
public static class OverheatSwarmBoost
{
    public static float SpeedMultiplier { get; private set; } = 1f;
    public static int SpawnWaveMultiplier { get; private set; } = 1;

    public static bool IsIntensityActive => SpeedMultiplier > 1.01f;

    public static void SetIntensity(bool active)
    {
        if (active)
        {
            SpeedMultiplier = 2f;
            SpawnWaveMultiplier = 2;
        }
        else
        {
            SpeedMultiplier = 1f;
            SpawnWaveMultiplier = 1;
        }
    }
}
