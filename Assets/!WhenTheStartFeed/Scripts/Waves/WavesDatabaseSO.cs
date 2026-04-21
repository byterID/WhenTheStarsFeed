using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WavesDatabaseSO", menuName = "Scriptable Objects/WavesDatabaseSO")]
public class WavesDatabaseSO : ScriptableObject
{
    [Header("Волны")]
    public List<WaveData> waveDatabase;
}

// ── Данные одной волны ────────────────────────────────────────────────
[Serializable]
public class WaveData
{
    [Header("Индекс волны")]
    [field: SerializeField] public int waveIndex;

    [Header("Время подготовки до начала волны (сек)")]
    [field: SerializeField] public float preparationTime = 5f;

    [Header("Время до следующей волны после начала этой (сек)")]
    [field: SerializeField] public float timeToNextWave = 30f;

    [Header("Отряды юнитов в волне (по порядку)")]
    [SerializeField] public List<EnemySquad> squads = new();
}

// ── Отряд — группа юнитов одного типа с кучностью ────────────────────
[Serializable]
public class EnemySquad
{
    [Header("Тип врага")]
    public EnemiesData enemyData;

    [Header("Количество юнитов в отряде")]
    public int count = 5;

    [Header("Кучность (0=рассеянно, 1=плотно)")]
    [Range(0f, 1f)]
    public float density = 0.5f;

    // Вычисляемое свойство: задержка между спавном юнитов
    // density=0 → 1 сек, density=1 → 0.25 сек
    public float SpawnInterval => Mathf.Lerp(1f, 0.25f, density);
}
