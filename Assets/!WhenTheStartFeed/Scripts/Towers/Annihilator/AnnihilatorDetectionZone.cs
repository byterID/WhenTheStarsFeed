using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Зона обнаружения врагов Аннигилятора.
/// Хранит список GameObject врагов в зоне.
/// </summary>
public class AnnihilatorDetectionZone : MonoBehaviour
{
    private readonly HashSet<GameObject> _enemies = new();

    public event Action OnEnemyEntered;  // первый враг вошёл
    public event Action OnZoneEmpty;     // последний враг вышел или умер

    public bool HasEnemies => _enemies.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        bool wasEmpty = _enemies.Count == 0;
        _enemies.Add(other.gameObject);

        if (wasEmpty)
            OnEnemyEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        _enemies.Remove(other.gameObject);

        if (_enemies.Count == 0)
            OnZoneEmpty?.Invoke();
    }

    // Чистим мёртвых врагов каждый кадр.
    // ВАЖНО: если убрали последнего → стреляем OnZoneEmpty,
    // чтобы AnnihilatorTower мог остановить цикл.
    private void Update()
    {
        if (_enemies.Count == 0) return;

        bool hadEnemies = _enemies.Count > 0;
        _enemies.RemoveWhere(e => e == null);
        bool nowEmpty = _enemies.Count == 0;

        // Была хотя бы 1 цель, стала 0 — уведомляем башню
        if (hadEnemies && nowEmpty)
            OnZoneEmpty?.Invoke();
    }
}
