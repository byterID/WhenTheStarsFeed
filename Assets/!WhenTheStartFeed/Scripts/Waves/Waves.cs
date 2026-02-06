using System.Collections;
using UnityEngine;

public class Waves : MonoBehaviour
{
    public static Waves Instance;

    [SerializeField] private WavesDatabaseSO _wave;
    [SerializeField] private EnemySpawner _spawner;

    private int currentWave = 0;

    public void StartNextWave()
    {
        if (currentWave >= _wave.waveDatabase.Count) 
            return;

        WaveData wave = _wave.waveDatabase[currentWave];

        StartCoroutine(RunWave(wave));
        currentWave++;
    }

    IEnumerator RunWave(WaveData wave)
    {
        // Время подготовки перед стартом волны
        yield return new WaitForSeconds(wave.preparationTime);

        GameObject[] prefabArray = new GameObject[wave.enemies.Count];
        for (int i = 0; i < wave.enemies.Count; i++)
            prefabArray[i] = wave.enemies[i].prefab;

        _spawner.SpawnEnemySinglePoint(prefabArray, wave.spawnCooldown);
        //_spawner.SpawnEnemiesMultiplePoints(prefabArray);

        // Волна закончилась, ждём waveDuration перед следующей
        yield return new WaitForSeconds(wave.waveDuration);

        // Запускаем следующую волну
        StartNextWave();
    }
}
