using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField, Min(1)] private int _contactDamage = 5;

    [SerializeField, Tooltip("Activa logs en consola para depurar daño por contacto.")]
    private bool _logDebug;

    public void TryApplyContactDamage(PlayerHealth player)
    {
        if (player == null || !player.IsAlive)
            return;

        if (player.IsInvulnerable)
            return;

        player.TakeDamage(_contactDamage);

        if (_logDebug)
            Debug.Log($"[EnemyContactDamage] Daño={_contactDamage} HP jugador={player.CurrentHealth}/{player.MaxHealth}", this);
    }
}
