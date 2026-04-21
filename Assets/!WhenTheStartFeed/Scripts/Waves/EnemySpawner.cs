using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Точки спавна")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform[] _multipleSpawnPoints;

    [Header("Маршрут")]
    [SerializeField] private Transform[] _path;

    // ── Тестовый спавн (оставляем для кнопки UI) ─────────────────────
    [Header("Тест")]
    [SerializeField] private GameObject _enemyPrefab;

    public void SpawnEnemy()
    {
        SpawnSingle(_enemyPrefab, _spawnPoint.position, 1f, 0f);
    }

    // ── Основной метод: спавн отряда с задержкой ─────────────────────

    /// <summary>
    /// Спавнит отряд юнитов по одному с интервалом из density.
    /// scaleMult — множитель статов (для повторяющихся циклов волн).
    /// </summary>
    public IEnumerator SpawnSquad(EnemySquad squad, float scaleMult)
    {
        for (int i = 0; i < squad.count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition();
            SpawnSingle(squad.enemyData.prefab, spawnPos,
                        squad.enemyData.health * scaleMult,
                        squad.enemyData.resistance);

            yield return new WaitForSeconds(squad.SpawnInterval);
        }
    }

    // ── Внутренний спавн одного врага ────────────────────────────────

    private void SpawnSingle(GameObject prefab, Vector3 position,
                              float health, float resistance)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        enemy.transform.SetParent(DynamicRoot.Root);

        // Назначаем маршрут
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
            movement.SetPath(_path);

        // Инициализируем статы
        EnemyActions actions = enemy.GetComponent<EnemyActions>();
        if (actions != null)
            actions.InitFromData(health, resistance);
    }

    // ── Позиция спавна ────────────────────────────────────────────────

    private Vector3 GetSpawnPosition()
    {
        if (_multipleSpawnPoints != null && _multipleSpawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _multipleSpawnPoints.Length);
            return _multipleSpawnPoints[idx].position;
        }
        return _spawnPoint.position;
    }
}
