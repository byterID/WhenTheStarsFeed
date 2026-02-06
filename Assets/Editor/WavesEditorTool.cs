#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class WavesAutoDistributor
{
    [MenuItem("Tools/Distribute Enemies Automatically")]
    public static void DistributeEnemies()
    {
        var enemiesDB = AssetDatabase.LoadAssetAtPath<EnemiesDatabaseSO>("Assets/!WhenTheStartFeed/ScriptableObjects/EnemiesDatabaseSO.asset");
        var wavesDB = AssetDatabase.LoadAssetAtPath<WavesDatabaseSO>("Assets/!WhenTheStartFeed/ScriptableObjects/WavesDatabaseSO.asset");

        if (enemiesDB == null || wavesDB == null)
        {
            Debug.LogWarning("SO не найдены!");
            return;
        }
        foreach (var wave in wavesDB.waveDatabase)
        {
            wave.enemies.Clear();
            wave.enemies = new List<EnemiesData>();

            foreach (var enemy in enemiesDB.enemiesData)
            {
                if (wave.waveIndex >= enemy.minWave && wave.waveIndex <= enemy.maxWave)
                {
                    wave.enemies.Add(enemy);
                }
            }
        }

        EditorUtility.SetDirty(wavesDB);
        AssetDatabase.SaveAssets();

        Debug.Log("Автоматическое распределение врагов завершено!");
    }
}
#endif