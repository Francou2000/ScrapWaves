using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Min(1)] private int _damage = 2;

    [SerializeField, Tooltip("Unidades por segundo (movimiento en FixedUpdate con Rigidbody kinematic).")]
    private float _speed = 18f;

    [SerializeField, Tooltip("Segundos de vida por defecto (el pool puede sobrescribir en ConfigurePooled).")]
    private float _maxLifetime = 4f;

    private float _activeMaxLifetime;
    private Vector3 _direction = Vector3.forward;
    private float _elapsed;
    private bool _consumed;
    private Rigidbody _rigidbody;
    private bool _useExplosion;
    private float _explosionRadius;
    private float _explosionFalloff;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        _activeMaxLifetime = _maxLifetime;
    }

    public void ConfigurePooled(float maxLifetimeSeconds)
    {
        _activeMaxLifetime = Mathf.Max(0.05f, maxLifetimeSeconds);
    }

    public void ConfigurePooled(float maxLifetimeSeconds, int damageForThisShot)
    {
        ConfigurePooled(maxLifetimeSeconds);
        _damage = Mathf.Max(1, damageForThisShot);
    }

    // Launches projectile and resets runtime damage mode state.
    public void Launch(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude > 0.0001f)
            _direction = worldDirection.normalized;

        _elapsed = 0f;
        _consumed = false;

        _rigidbody.position = transform.position;
        _rigidbody.rotation = transform.rotation; _useExplosion = false;
        _explosionRadius = 0f;
        _explosionFalloff = 0f;
    }


    // Configures radial explosion damage behavior for this shot.
    public void ConfigureExplosion(float radius, float falloff)
    {
        _useExplosion = radius > 0f;
        _explosionRadius = Mathf.Max(0f, radius);
        _explosionFalloff = Mathf.Clamp01(falloff);
    }

    private void FixedUpdate()
    {
        if (_consumed)
            return;

        Vector3 delta = _direction * (_speed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(_rigidbody.position + delta);
    }

    private void Update()
    {
        if (_consumed)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= _activeMaxLifetime)
            DespawnOrDestroy();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_consumed)
            return;

        // Si choca contra el entorno (Terrain), se destruye sin aplicar daño.
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        if (terrainLayer >= 0 && other.gameObject.layer == terrainLayer)
        {
            DespawnOrDestroy();
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (_useExplosion)
            ApplyExplosionDamage();
        else if (damageable != null)

            DespawnOrDestroy();
    }

    // Applies area damage around impact point with distance-based falloff.
    private void ApplyExplosionDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            float distance = Vector3.Distance(transform.position, hits[i].transform.position);
            float t = _explosionRadius <= 0f ? 1f : Mathf.Clamp01(distance / _explosionRadius);
            float falloffScale = Mathf.Lerp(1f, 1f - _explosionFalloff, t);
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(_damage * falloffScale));
            damageable.ApplyDamage(finalDamage);
        }
    }

    // Returns projectile to pool or destroys when pooling unavailable.
    private void DespawnOrDestroy()
    {
        if (_consumed)
            return;

        _consumed = true;

        if (TryGetComponent(out ProjectilePoolMember poolMember))
            poolMember.Despawn();
        else
            Destroy(gameObject);
    }
}
