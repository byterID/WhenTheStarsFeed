using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float hideDelay = 2f;
    [SerializeField] private bool alwaysVisible = false;

    private Camera mainCamera;
    private float lastDamageTime;
    private CanvasGroup canvasGroup;
    private int maxHealth; // Сохраняем максимальное здоровье

    private void Awake()
    {
        mainCamera = Camera.main;
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (!alwaysVisible)
            canvasGroup.alpha = 0f;

        // ИНИЦИАЛИЗИРУЕМ СЛАЙДЕР
        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = 1; // Всегда держим maxValue = 1 для процентного отображения
            slider.value = 1; // Начинаем с полного здоровья
            slider.wholeNumbers = false; // Разрешаем дробные значения
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }

        if (!alwaysVisible && Time.time - lastDamageTime > hideDelay)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * 5f);
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        this.maxHealth = maxHealth; // Сохраняем для возможного использования

        if (slider != null)
        {
            // Вычисляем процент здоровья и устанавливаем значение слайдера
            float healthPercent = (float)currentHealth / maxHealth;
            slider.value = healthPercent;
        }

        if (!alwaysVisible)
        {
            lastDamageTime = Time.time;
            canvasGroup.alpha = 1f;
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        if (slider != null)
        {
            // Не меняем maxValue слайдера, так как он всегда должен быть 1
            // Просто устанавливаем текущее значение на 1 (100%)
            slider.value = 1;
            this.maxHealth = maxHealth;
        }
    }
}