using System.Collections.Generic;
using UnityEngine;

public class AnnihilatorFireZone : MonoBehaviour
{
    private readonly HashSet<GameObject> _enemies = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
            _enemies.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
            _enemies.Remove(other.gameObject);
    }

    public List<IDamageable> GetEnemiesInZone()
    {
        _enemies.RemoveWhere(e => e == null);

        var result = new List<IDamageable>();
        foreach (var enemy in _enemies)
        {
            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg != null) result.Add(dmg);
        }
        return result;
    }

    public bool HasEnemies => _enemies.Count > 0;
}
