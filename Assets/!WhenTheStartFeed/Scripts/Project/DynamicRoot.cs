using UnityEngine;

public class DynamicRoot : MonoBehaviour
{
    public static Transform Root { get; private set; }

    private void Awake()
    {
        if (Root != null && Root != transform)
        {
            Debug.LogWarning("[DynamicRoot] Потерян DynamicRoot! ");
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
