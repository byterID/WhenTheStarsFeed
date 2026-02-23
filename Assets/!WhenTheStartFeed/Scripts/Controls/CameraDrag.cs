using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDrag : MonoBehaviour
{
    private bool _isDragging;
    private Vector2 _lastMousePos;

    public float speed = 0.02f;

    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            _isDragging = true;
            _lastMousePos = Mouse.current.position.ReadValue();
        }

        if (ctx.canceled)
            _isDragging = false;
    }

    private void LateUpdate()
    {
        if (!_isDragging) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 delta = mousePos - _lastMousePos;

        Vector3 right = transform.right;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        Vector3 move =
            -right * delta.x * speed
            - forward * delta.y * speed;

        move.y = 0f;

        transform.position += move;
        _lastMousePos = mousePos;
    }

}
