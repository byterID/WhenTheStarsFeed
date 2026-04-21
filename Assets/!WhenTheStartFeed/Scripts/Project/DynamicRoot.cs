using UnityEngine;

/// <summary>
/// Единственный компонент на объекте «_Dynamic».
/// Предоставляет статический доступ к его Transform,
/// чтобы любой скрипт мог написать DynamicRoot.Root
/// вместо GameObject.Find("_Dynamic").transform.
///
/// Убедитесь, что Script Execution Order для DynamicRoot
/// установлен раньше скриптов-потребителей (например, -90).
/// </summary>
public class DynamicRoot : MonoBehaviour
{
    public static Transform Root { get; private set; }

    private void Awake()
    {
        if (Root != null && Root != transform)
        {
            Debug.LogWarning("[DynamicRoot] На сцене больше одного DynamicRoot! " +
                             "Оставьте только один.");
            return;
        }
        Root = transform;
    }

    private void OnDestroy()
    {
        if (Root == transform)
            Root = null;
    }
}
