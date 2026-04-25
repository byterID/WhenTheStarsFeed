using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BlockedAreaMarker : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int size;

    private void Awake()
    {
        Vector3 s = transform.lossyScale;
        size = new Vector2Int(
            Mathf.RoundToInt(s.x),
            Mathf.RoundToInt(s.z)
        );
    }
}
