using UnityEngine;

/// <summary>
/// Debe estar en el mismo GameObject que <see cref="CharacterController"/>.
/// El empuje y el contacto real del CC no siempre disparan triggers en hijos; este callback es la vía fiable.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerContactDamageReceiver : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;

    [SerializeField, Tooltip("Logs al recibir hits del CharacterController (spam si hay muchos contactos).")]
    private bool _logContactDebug;

    private void Awake()
    {
        if (_playerHealth == null)
            _playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_playerHealth == null || !_playerHealth.IsAlive)
            return;

        if (_logContactDebug)
            Debug.Log($"[PlayerContactDamageReceiver] CC hit: {hit.collider.name} (root={hit.collider.transform.root.name})", hit.collider);

        EnemyContactDamage contact = hit.collider.GetComponentInParent<EnemyContactDamage>();
        if (contact == null)
            return;

        contact.TryApplyContactDamage(_playerHealth);
    }
}
