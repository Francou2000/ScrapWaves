using UnityEngine;

public class SwarmPooledEnemy : MonoBehaviour
{
    private SwarmEnemyPool _pool;

    public void Bind(SwarmEnemyPool pool)
    {
        _pool = pool;
    }

    public bool BelongsTo(SwarmEnemyPool pool)
    {
        return _pool == pool;
    }

    public void NotifySpawned()
    {
        GetComponent<EnemyFollow>()?.PrepareForSpawn();
        GetComponent<SimpleFollow>()?.PrepareForSpawn();
    }

    public void NotifyDespawned()
    {
        GetComponent<EnemyFollow>()?.OnDespawned();
        GetComponent<SimpleFollow>()?.OnDespawned();
    }

    public void Despawn()
    {
        if (_pool != null)
            _pool.Release(gameObject);
    }
}
