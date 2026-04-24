using UnityEngine;

public enum DeviceType
{
    Desktop,
    Mobile
}

/// <summary>
/// Определяет тип устройства один раз при старте.
///
/// Изменения:
///   - Регистрируется в ServiceLocator.
///   - Статическое поле CurrentDevice сохранено для удобства
///     (InputBootstrap и CameraController читают его напрямую).
///   - Script Execution Order: -100 (самый первый!).
///     InputBootstrap зависит от DeviceDetector → должен быть позже.
/// </summary>
public class DeviceDetector : MonoBehaviour
{
    public static DeviceType CurrentDevice { get; private set; }

    private void Awake()
    {
        // Определяем устройство
#if UNITY_EDITOR
        // В редакторе можно симулировать мобильное устройство через вкладку Game
        // Если нужен принудительный мобильный режим — раскомментируй строку ниже:
        // CurrentDevice = DeviceType.Mobile;
        CurrentDevice = DeviceType.Desktop;
#else
        CurrentDevice = Application.isMobilePlatform
            ? DeviceType.Mobile
            : DeviceType.Desktop;
#endif

        ServiceLocator.Register<DeviceDetector>(this);
        Debug.Log($"[DeviceDetector] Устройство: {CurrentDevice}");
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<DeviceDetector>();
    }
}
