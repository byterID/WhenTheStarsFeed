using UnityEngine;

public class CameraDiagnostic : MonoBehaviour
{
    void LateUpdate()
    {
        // 1. Кто мы и где находимся
        Debug.Log($"[DIAG] GO='{gameObject.name}' pos={transform.position} parent={(transform.parent ? transform.parent.name : "NONE")}");

        // 2. Где находится основная камера, которую рендерит Unity
        if (Camera.main != null)
        {
            Debug.Log($"[DIAG] Camera.main GO='{Camera.main.gameObject.name}' worldPos={Camera.main.transform.position} parent={(Camera.main.transform.parent ? Camera.main.transform.parent.name : "NONE")}");
        }
        else
        {
            Debug.LogWarning("[DIAG] Camera.main is NULL — нет камеры с тегом MainCamera!");
        }

        // 3. Проверяем все камеры в сцене
        foreach (var cam in Camera.allCameras)
        {
            Debug.Log($"[DIAG] AllCameras: '{cam.gameObject.name}' pos={cam.transform.position} enabled={cam.enabled} tag={cam.tag}");
        }
    }
}
