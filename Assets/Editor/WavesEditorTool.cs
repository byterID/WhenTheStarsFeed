#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class WavesAutoDistributor
{
    [MenuItem("Tools/Distribute Enemies Automatically")]
    public static void DistributeEnemies()
    {
        var enemiesDB = AssetDatabase.LoadAssetAtPath<EnemiesDatabaseSO>(
            "Assets/!WhenTheStartFeed/ScriptableObjects/EnemiesDatabaseSO.asset");
        var wavesDB = AssetDatabase.LoadAssetAtPath<WavesDatabaseSO>(
            "Assets/!WhenTheStartFeed/ScriptableObjects/WavesDatabaseSO.asset");

        if (enemiesDB == null || wavesDB == null)
        {
            Debug.LogWarning("SO не найдены!");
            return;
        }

        foreach (var wave in wavesDB.waveDatabase)
        {
            // ќчищаем старые отр€ды
            wave.squads.Clear();

            // ƒл€ каждого врага чь€ волна попадает в диапазон Ч создаЄм отр€д
            foreach (var enemy in enemiesDB.enemiesData)
            {
                if (wave.waveIndex >= enemy.minWave && wave.waveIndex <= enemy.maxWave)
                {
                    EnemySquad squad = new EnemySquad
                    {
                        enemyData = enemy,
                        count = 5,      // дефолтное количество Ч мен€й вручную
                        density = 0.5f    // средн€€ кучность Ч мен€й вручную
                    };
                    wave.squads.Add(squad);
                }
            }
        }

        EditorUtility.SetDirty(wavesDB);
        AssetDatabase.SaveAssets();

        Debug.Log("јвтоматическое распределение врагов завершено!");
    }
}
#endif
