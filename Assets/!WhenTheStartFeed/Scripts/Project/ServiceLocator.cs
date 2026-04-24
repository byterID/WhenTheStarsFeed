using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Лёгкий ServiceLocator.
/// Пример: Вместо GameObject.Find("SoundFeedback") пишем:
///     ServiceLocator.Get<SoundFeedback>()
///
/// Регистрация происходит в Awake каждого сервиса:
///     ServiceLocator.Register<SoundFeedback>(this);
///
/// Порядок инициализации:
/// Script Execution Order должен гарантировать, что сервисы
/// регистрируются РАНЬШЕ потребителей (см. Project Settings → Script Execution Order).
/// Рекомендуемый порядок (чем меньше число — тем раньше):
///   -100  ServiceLocator        (сам контейнер)
///   -90   DynamicRoot
///   -80   SoundFeedback
///   -70   MoneyManager
///   -60   GameManager
///   -50   MainBase
///   -40   PlacementSystem, Waves, EnemySpawner  (потребители)
///     0   Tower, AnnihilatorTower               (стандартный порядок)
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    // ── Регистрация ───────────────────────────────────────────────────

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    // ── Получение ─────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает зарегистрированный сервис.
    /// Бросает Exception если сервис не найден — это намеренно,
    /// чтобы сразу видеть ошибку конфигурации.
    /// </summary>
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out object service))
            return service as T;

        throw new Exception($"[ServiceLocator] Сервис {typeof(T).Name} не зарегистрирован! " +
                            $"Убедитесь что компонент существует на сцене и вызывает " +
                            $"ServiceLocator.Register в Awake.");
    }

    /// <summary>
    /// Безопасное получение — возвращает null если сервис не найден.
    /// Используйте когда сервис опционален.
    /// </summary>
    public static T TryGet<T>() where T : class
    {
        _services.TryGetValue(typeof(T), out object service);
        return service as T;
    }

    public static bool Has<T>() where T : class => _services.ContainsKey(typeof(T));

    // ── Очистка (вызывать при смене сцены если нужно) ────────────────
    public static void Clear() => _services.Clear();
}
