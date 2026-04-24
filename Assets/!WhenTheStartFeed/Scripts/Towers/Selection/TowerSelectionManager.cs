using UnityEngine;
using UnityEngine.EventSystems;

public class TowerSelectionManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (PlacementSystem.Instance != null && PlacementSystem.Instance.IsPlacing)
            return;

        HandleClick();
    }
    private void HandleClick()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.AllLayers,
                                                QueryTriggerInteraction.Ignore);

        TowerClickHandler found = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            TowerClickHandler handler =
                hit.collider.GetComponentInParent<TowerClickHandler>(false);

            if (handler != null && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                found = handler;
            }
        }

        if (found != null)
            found.Select();
        else
            TowerClickHandler.DeselectAll();
    }
}
