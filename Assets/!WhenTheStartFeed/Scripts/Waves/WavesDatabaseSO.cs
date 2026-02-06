using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WavesDatabaseSO", menuName = "Scriptable Objects/WavesDatabaseSO")]
public class WavesDatabaseSO : ScriptableObject
{
    [Header("Волна")]
    public List<WaveData> waveDatabase;
}

[Serializable]
public class WaveData
{
    [Header("Индекс волны")]
    [field: SerializeField] public int waveIndex { get; private set; } 

    [Header("Длительность волны")]
    [field: SerializeField] public float waveDuration { get; private set; }

    [Header("Время для подготовки")]
    [field: SerializeField] public float preparationTime;

    [Header("Кулдаун спавна врагов")]
    [field: SerializeField] public float spawnCooldown;

    [Header("Враги для волны")]
    [field: SerializeField] public List<EnemiesData> enemies;

    [Header("Кол-во врагов на волну")]
    [field: SerializeField] public int enemyCount;
}