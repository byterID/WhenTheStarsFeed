using UnityEngine;

public enum DeviceType
{
    Desktop,
    Mobile
}
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
