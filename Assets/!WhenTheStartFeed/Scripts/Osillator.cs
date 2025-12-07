using UnityEngine;

public class Osillator : MonoBehaviour
{
    [SerializeField] private float timeCounter = 0;
    [SerializeField] private float speed;
    [SerializeField] private float width;
    [SerializeField] private float height;

    private void Update()
    {
        timeCounter += Time.deltaTime * speed;

        float x = Mathf.Cos(timeCounter) * width;
        float y = 0;
        float z = Mathf.Sin(timeCounter) * height;

        transform.position = new Vector3(x, y, z);
    }
}
