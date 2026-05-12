using UnityEngine;

public class PlayerContactHurtbox : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;

    [SerializeField, Tooltip("Logs cuando el trigger del hurtbox detecta un collider.")]
    private bool _logTriggerDebug;

    private void Awake()
    {
        if (_playerHealth == null)
            _playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyFromCollider(other, "Enter");
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyFromCollider(other, "Stay");
    }

    private void TryApplyFromCollider(Collider other, string phase)
    {
        if (_logTriggerDebug)
            Debug.Log($"[PlayerContactHurtbox] {phase}: {other.name}", other);

        if (_playerHealth == null || !_playerHealth.IsAlive)
            return;

        EnemyContactDamage contact = other.GetComponentInParent<EnemyContactDamage>();
        if (contact != null)
            contact.TryApplyContactDamage(_playerHealth);
    }
}
