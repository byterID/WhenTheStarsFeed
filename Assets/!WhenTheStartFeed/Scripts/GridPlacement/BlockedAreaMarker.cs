using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BlockedAreaMarker : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int size; // размер в клетках

    private void Awake()
    {
        Vector3 s = transform.lossyScale;
        size = new Vector2Int(
            Mathf.RoundToInt(s.x), // ширина по X
            Mathf.RoundToInt(s.z)  // длина по Z
        );
    }
}
