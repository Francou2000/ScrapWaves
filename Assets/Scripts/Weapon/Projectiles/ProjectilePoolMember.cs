using UnityEngine;

public class ProjectilePoolMember : MonoBehaviour
{
    private ProjectilePool _pool;

    public void Bind(ProjectilePool pool)
    {
        _pool = pool;
    }

    public bool BelongsTo(ProjectilePool pool)
    {
        return _pool == pool;
    }

    public void Despawn()
    {
        if (_pool != null)
            _pool.Release(gameObject);
    }
}
