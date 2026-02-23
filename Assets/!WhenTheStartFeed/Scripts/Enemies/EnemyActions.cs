using UnityEngine;
using System.Collections;

public class EnemyActions : MonoBehaviour
{
    [SerializeField] private int maxHp = 10;
    [SerializeField] private int damage;
    [SerializeField] private float _attackCooldown = 2f;

    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private bool showHealthBarAlways = false;

    private int currentHp;
    private Coroutine _attackRoutine;

    private void Start()
    {
        currentHp = maxHp;

        // Ищем HealthBar, если не назначен вручную
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<EnemyHealthBar>();
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHp);
            healthBar.UpdateHealthBar(currentHp, maxHp);
        }
    }

    public void CallHit()
    {
        Debug.Log("АЙ!!!");
        currentHp--;

        // Обновляем HealthBar
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Враг повержен!");
        // Здесь можно добавить анимацию смерти, звуки, эффекты
        Destroy(gameObject);
    }

    public void StartAttacking()
    {
        if (_attackRoutine == null)
        {
            _attackRoutine = StartCoroutine(AttackLoop());
        }
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            Debug.Log("НЫААА!"); // здесь логика урона

            yield return new WaitForSeconds(_attackCooldown);
        }
    }
}