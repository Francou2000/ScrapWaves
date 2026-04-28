using UnityEngine;

/// <summary>
/// Definición data-driven de una mejora. Crea activos con: Create → Scrap Waves → Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade", menuName = "Scrap Waves/Upgrade", order = 0)]
public class Upgrade : ScriptableObject
{
    [SerializeField, Tooltip("Texto en la UI de elección.")]
    private string _displayName;

    [TextArea(2, 4)]
    [SerializeField, Tooltip("Opcional, para tooltips o UI futura.")]
    private string _description;

    [SerializeField]
    private PlayerStatType _targetStat;

    [SerializeField, Tooltip("Daño / vida máx.: suma entera (se redondea). Fire rate: reduce segundos del intervalo (ej. 0.03). Move speed: suma unidades/s.")]
    private float _value;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public string Description => _description;
    public PlayerStatType TargetStat => _targetStat;
    public float Value => _value;
}
