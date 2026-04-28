using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField, Tooltip("Origen visual del disparo (hijo del jugador, delante del modelo).")]
    private Transform _firePoint;

    [SerializeField, Tooltip("Pool de proyectiles (sustituye Instantiate).")]
    private ProjectilePool _projectilePool;

    [SerializeField, Min(0.1f), Tooltip("Radio en XZ desde la posición del jugador para candidatos a objetivo.")]
    private float _detectionRange = 12f;

    private float _fireCooldown;
    private PlayerStats _stats;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        float interval = _stats.GetFireInterval();

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown > 0f)
            return;

        _fireCooldown = interval;

        if (_projectilePool == null || _firePoint == null)
            return;

        if (!EnemyRegistry.TryGetClosestOnPlane(transform.position, _detectionRange, out Transform target))
            return;

        // Dirección 3D hacia el enemigo (incluye altura); el registro sigue eligiendo el más cercano en XZ.
        Vector3 aim = target.position - _firePoint.position;
        if (aim.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = aim.normalized;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dir);

        if (_projectilePool.TrySpawnProjectile(_firePoint.position, rot, dir, _stats.GetDamage()))
            AudioManager.TryPlayShoot();
    }
}
