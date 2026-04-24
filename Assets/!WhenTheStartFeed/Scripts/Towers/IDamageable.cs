/// <summary>
/// Интерфейс урона. Повесить реализацию на скрипт Enemy.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Получить урон.
    /// </summary>
    /// <param name="damage">Базовый урон</param>
    /// <param name="armorPenetration">Бронебойность — вычитается из брони врага</param>
    void TakeDamage(float damage, float armorPenetration);
}
