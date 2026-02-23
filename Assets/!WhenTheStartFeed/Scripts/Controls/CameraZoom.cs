using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    private float _currentZoom;

    private void Start()
    {
        // стартуем с текущей позиции
        _currentZoom = transform.localPosition.z;
    }

    private void Update()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll == 0f) return;

        _currentZoom -= scroll * zoomSpeed;
        _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

        Vector3 pos = transform.localPosition;
        pos.y = _currentZoom;
        transform.localPosition = pos;
    }
}
