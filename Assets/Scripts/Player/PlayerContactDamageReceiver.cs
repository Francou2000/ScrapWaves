using UnityEngine;

/// <summary>
/// Receptor de daño por contacto contra enemigos.
/// - Si hay <see cref="CharacterController"/>, usa <see cref="OnControllerColliderHit"/> (comportamiento anterior).
/// - Si el jugador usa físicas (Rigidbody), usa colisiones (OnCollisionEnter/Stay).
/// </summary>
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

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyFromCollider(collision.collider, "CollisionEnter");
    }

    private void OnCollisionStay(Collision collision)
    {
        TryApplyFromCollider(collision.collider, "CollisionStay");
    }

    private void TryApplyFromCollider(Collider other, string phase)
    {
        if (_playerHealth == null || !_playerHealth.IsAlive || other == null)
            return;

        if (_logContactDebug)
            Debug.Log($"[PlayerContactDamageReceiver] {phase}: {other.name} (root={other.transform.root.name})", other);

        EnemyContactDamage contact = other.GetComponentInParent<EnemyContactDamage>();
        if (contact != null)
            contact.TryApplyContactDamage(_playerHealth);
    }
}
