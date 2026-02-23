using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed;
    public Transform[] moveTargets;

    private int _currentTargetIndex = 0;
    private EnemyActions _enemyAttack;

    private void Start()
    {
        _enemyAttack = GetComponent<EnemyActions>();
    }

    private void Update()
    {
        if (moveTargets == null || moveTargets.Length == 0) return;

        if (_currentTargetIndex >= moveTargets.Length) return;

        Transform target = moveTargets[_currentTargetIndex];

        float step = _speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            _currentTargetIndex++;

            if (_currentTargetIndex >= moveTargets.Length)
            {
                _enemyAttack?.StartAttacking();
            }
        }
    }
}