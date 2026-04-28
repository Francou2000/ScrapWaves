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

    public void Launch(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude > 0.0001f)
            _direction = worldDirection.normalized;

        _elapsed = 0f;
        _consumed = false;

        _rigidbody.position = transform.position;
        _rigidbody.rotation = transform.rotation;
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
        if (damageable != null)
            damageable.ApplyDamage(_damage);

        DespawnOrDestroy();
    }

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
