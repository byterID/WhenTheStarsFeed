using UnityEngine;

public class TowerClickHandler : MonoBehaviour
{
    [HideInInspector] public int towerID;
    [HideInInspector] public Vector3Int gridCell;

    private static TowerClickHandler _currentSelected;

    public void Select()
    {
        if (_currentSelected == this) return; // уже выбрана

        if (_currentSelected != null)
            _currentSelected.Deselect();

        _currentSelected = this;

        if (TowerMoveUI.Instance != null)
            TowerMoveUI.Instance.Show(this);
    }

    public void Deselect()
    {
        if (_currentSelected == this)
            _currentSelected = null;

        if (TowerMoveUI.Instance != null)
            TowerMoveUI.Instance.Hide();
    }

    public static void DeselectAll()
    {
        if (_currentSelected != null)
        {
            _currentSelected.Deselect();
            _currentSelected = null;
        }
    }
}
