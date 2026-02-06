using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint; //одна точка спавна
    [SerializeField] private Transform[] _multipleSpawnPoints; //много точек спавка
    private int _randomIndex;

    private void RandomizeEnemySpawnPoint()
    {
        _randomIndex = Random.Range(0, _multipleSpawnPoints.Length);
    }

    public void SpawnEnemySinglePoint(GameObject[] enemyPrefab, float cooldown)//спавн в одном месте
    {
        for (int i = 0; i < enemyPrefab.Length; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab[i], _spawnPoint);
            enemy.transform.SetParent(DynamicRoot.Root);
        }
    }

    public void SpawnEnemiesMultiplePoints(GameObject[] enemyPrefab, float cooldown)//спавн в разных местах
    {
        for (int i = 0; i < enemyPrefab.Length; i++)
        {
            RandomizeEnemySpawnPoint();
            GameObject enemy = Instantiate(enemyPrefab[i], _multipleSpawnPoints[_randomIndex]);
            enemy.transform.SetParent(DynamicRoot.Root);
        }
    }
}


