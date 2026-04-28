/// <summary>Stats del jugador que pueden modificarse con <see cref="Upgrade"/>.</summary>
public enum PlayerStatType
{
    Damage,
    /// <summary>Mayor valor = dispara más a menudo (reduce el intervalo entre disparos).</summary>
    FireRate,
    MoveSpeed,
    MaxHealth
}
