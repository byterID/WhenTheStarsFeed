using UnityEngine;

public class DynamicRoot : MonoBehaviour
{
    public static Transform Root { get; private set; }

    private void Awake()
    {
        Root = transform;
    }
}
