using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;
    public Transform target;
    
    private void ChaseTarget(Transform target)
    {
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);
    }

    private void Update()
    {
        if(target != null)
            ChaseTarget(target);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            collision.gameObject.GetComponent<Enemy>().CallHit();
            Destroy(gameObject);
        }
    }
}
