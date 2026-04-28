using UnityEngine;

public class XPPoolMember : MonoBehaviour
{
    private XPPool _pool;

    public void Bind(XPPool pool)
    {
        _pool = pool;
    }

    public bool BelongsTo(XPPool pool)
    {
        return _pool == pool;
    }

    public void Despawn()
    {
        if (_pool != null)
            _pool.Release(gameObject);
    }
}
