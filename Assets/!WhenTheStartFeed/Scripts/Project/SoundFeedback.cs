using UnityEngine;

/// <summary>
/// Центральная система звука игры.
/// Регистрируется в ServiceLocator — все скрипты получают её через TryGet.
///
/// ═══════════════════════════════════════════════════════════════
/// АРХИТЕКТУРА
/// ═══════════════════════════════════════════════════════════════
///
/// SFX (одиночные звуки)  — AudioSource _sfxSource   (Play One Shot)
/// Музыка                 — AudioSource _musicSource  (loop)
/// Ambient / окружение    — AudioSource _ambientSource (loop)
/// Огнемёт аннигилятора   — AudioSource _flamethrowerSource (loop, fade)
///
/// Все четыре AudioSource должны быть дочерними объектами
/// и назначены в Inspector.
///
/// ═══════════════════════════════════════════════════════════════
/// НАСТРОЙКА В INSPECTOR
/// ═══════════════════════════════════════════════════════════════
///
/// 1. Создай пустой GameObject «SoundManager» на сцене.
/// 2. Добавь компонент SoundFeedback.
/// 3. Создай 4 дочерних GameObject:
///      «SFX_Source»         — AudioSource, Play On Awake = OFF
///      «Music_Source»       — AudioSource, Play On Awake = OFF, Loop = ON
///      «Ambient_Source»     — AudioSource, Play On Awake = OFF, Loop = ON
///      «Flamethrower_Source»— AudioSource, Play On Awake = OFF, Loop = ON
/// 4. Назначь все 4 объекта в поля ниже.
/// 5. Заполни поля AudioClip (см. секции).
/// 6. Для музыки и амбиента настрой Volume на 0.3–0.5.
/// </summary>
public class SoundFeedback : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    // ИСТОЧНИКИ ЗВУКА
    // ═══════════════════════════════════════════════════════════════

    [Header("─── Audio Sources ───────────────────────────────────")]
    [Tooltip("Для коротких одиночных звуков (стрельба, клики, попадания)")]
    [SerializeField] private AudioSource _sfxSource;

    [Tooltip("Для фоновой музыки (loop). Переключается между треками.")]
    [SerializeField] private AudioSource _musicSource;

    [Tooltip("Для звуков окружения (птицы, ветер и т.д., loop)")]
    [SerializeField] private AudioSource _ambientSource;

    [Tooltip("Для огнемёта аннигилятора (loop с fade-in/out)")]
    [SerializeField] private AudioSource _flamethrowerSource;

    // ═══════════════════════════════════════════════════════════════
    // SFX КЛИПЫ
    // ═══════════════════════════════════════════════════════════════

    [Header("─── SFX: Башни ─────────────────────────────────────")]
    [Tooltip("Звук выстрела обычной башни")]
    [SerializeField] private AudioClip _shootClip;

    [Tooltip("Звук попадания пули во врага")]
    [SerializeField] private AudioClip _bulletHitClip;

    [Header("─── SFX: Аннигилятор ──────────────────────────────")]
    [Tooltip("Звук зарядки аннигилятора (однократный)")]
    [SerializeField] private AudioClip _annihilatorChargeClip;

    [Tooltip("Звук огнемёта аннигилятора (looped — назначается в _flamethrowerSource)")]
    [SerializeField] private AudioClip _annihilatorFireClip;

    [Tooltip("Громкость огнемёта при полном огне (0–1)")]
    [SerializeField] [Range(0f, 1f)] private float _flamethrowerMaxVolume = 0.8f;

    [Tooltip("Время нарастания/затухания звука огнемёта (сек)")]
    [SerializeField] private float _flamethrowerFadeTime = 0.4f;

    [Header("─── SFX: UI / Строительство ────────────────────────")]
    [Tooltip("Клик по кнопке меню/UI")]
    [SerializeField] private AudioClip _clickClip;

    [Tooltip("Звук успешной установки башни")]
    [SerializeField] private AudioClip _placeClip;

    [Tooltip("Звук удаления башни")]
    [SerializeField] private AudioClip _removeClip;

    [Tooltip("Звук неверного места установки")]
    [SerializeField] private AudioClip _wrongPlacementClip;

    // ═══════════════════════════════════════════════════════════════
    // МУЗЫКА
    // ═══════════════════════════════════════════════════════════════

    [Header("─── Музыка ─────────────────────────────────────────")]
    [Tooltip("Музыка главного меню")]
    [SerializeField] private AudioClip _menuMusicClip;

    [Tooltip("Музыка основной игровой сцены")]
    [SerializeField] private AudioClip _gameMusicClip;

    [Tooltip("Громкость музыки (0–1)")]
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.4f;

    // ═══════════════════════════════════════════════════════════════
    // AMBIENT
    // ═══════════════════════════════════════════════════════════════

    [Header("─── Окружение (Ambient) ───────────────────────────")]
    [Tooltip("Звук окружения: птицы, ветер и т.д. (loop)")]
    [SerializeField] private AudioClip _ambientClip;

    [Tooltip("Громкость окружения (0–1)")]
    [SerializeField] [Range(0f, 1f)] private float _ambientVolume = 0.3f;

    [Tooltip("Запускать ambient автоматически при старте сцены")]
    [SerializeField] private bool _playAmbientOnStart = true;

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<SoundFeedback>(this);
    }

    private void Start()
    {
        if (_playAmbientOnStart)
            PlayAmbient();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SoundFeedback>();
    }

    // ═══════════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЙ API — SFX
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Универсальный метод для обратной совместимости</summary>
    public void PlaySound(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.Click:          PlayClip(_clickClip);          break;
            case SoundType.Place:          PlayClip(_placeClip);          break;
            case SoundType.Remove:         PlayClip(_removeClip);         break;
            case SoundType.wrongPlacement: PlayClip(_wrongPlacementClip); break;
            case SoundType.shoot:          PlayClip(_shootClip);          break;
        }
    }

    /// <summary>Звук выстрела обычной башни</summary>
    public void PlayShoot()         => PlayClip(_shootClip);

    /// <summary>Звук попадания пули во врага</summary>
    public void PlayBulletHit()     => PlayClip(_bulletHitClip);

    /// <summary>Однократный звук начала зарядки аннигилятора</summary>
    public void PlayAnnihilatorCharge() => PlayClip(_annihilatorChargeClip);

    /// <summary>Запуск зацикленного звука огнемёта с fade-in</summary>
    public void StartFlamethrower()
    {
        if (_flamethrowerSource == null || _annihilatorFireClip == null) return;
        if (_flamethrowerSource.isPlaying) return;

        _flamethrowerSource.clip   = _annihilatorFireClip;
        _flamethrowerSource.loop   = true;
        _flamethrowerSource.volume = 0f;
        _flamethrowerSource.Play();

        StopCoroutine_Safe(_fadeFlamethrowerCoroutine);
        _fadeFlamethrowerCoroutine = StartCoroutine(
            FadeAudioSource(_flamethrowerSource, 0f, _flamethrowerMaxVolume, _flamethrowerFadeTime));
    }

    /// <summary>Остановка огнемёта с fade-out</summary>
    public void StopFlamethrower()
    {
        if (_flamethrowerSource == null) return;
        if (!_flamethrowerSource.isPlaying) return;

        StopCoroutine_Safe(_fadeFlamethrowerCoroutine);
        _fadeFlamethrowerCoroutine = StartCoroutine(
            FadeAudioSource(_flamethrowerSource, _flamethrowerSource.volume, 0f, _flamethrowerFadeTime,
                            stopOnEnd: true));
    }

    /// <summary>Клик кнопки меню/UI</summary>
    public void PlayClick()         => PlayClip(_clickClip);

    /// <summary>Установка башни</summary>
    public void PlayPlace()         => PlayClip(_placeClip);

    /// <summary>Удаление башни</summary>
    public void PlayRemove()        => PlayClip(_removeClip);

    /// <summary>Неверное место</summary>
    public void PlayWrongPlacement() => PlayClip(_wrongPlacementClip);

    // ═══════════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЙ API — МУЗЫКА
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Запустить музыку главного меню</summary>
    public void PlayMenuMusic()  => PlayMusic(_menuMusicClip);

    /// <summary>Запустить музыку игровой сцены</summary>
    public void PlayGameMusic()  => PlayMusic(_gameMusicClip);

    /// <summary>Остановить музыку</summary>
    public void StopMusic()
    {
        if (_musicSource != null) _musicSource.Stop();
    }

    // ═══════════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЙ API — AMBIENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Запустить звук окружения</summary>
    public void PlayAmbient()
    {
        if (_ambientSource == null || _ambientClip == null) return;
        if (_ambientSource.isPlaying) return;

        _ambientSource.clip   = _ambientClip;
        _ambientSource.loop   = true;
        _ambientSource.volume = _ambientVolume;
        _ambientSource.Play();
    }

    /// <summary>Остановить звук окружения</summary>
    public void StopAmbient()
    {
        if (_ambientSource != null) _ambientSource.Stop();
    }

    // ═══════════════════════════════════════════════════════════════
    // ВНУТРЕННИЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════════════

    private void PlayClip(AudioClip clip)
    {
        if (_sfxSource == null || clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (_musicSource == null || clip == null) return;
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip   = clip;
        _musicSource.loop   = true;
        _musicSource.volume = _musicVolume;
        _musicSource.Play();
    }

    // ── Coroutine fade ────────────────────────────────────────────────

    private Coroutine _fadeFlamethrowerCoroutine;

    private System.Collections.IEnumerator FadeAudioSource(
        AudioSource source, float fromVol, float toVol, float duration, bool stopOnEnd = false)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(fromVol, toVol, elapsed / duration);
            yield return null;
        }
        source.volume = toVol;
        if (stopOnEnd) source.Stop();
    }

    private void StopCoroutine_Safe(Coroutine c)
    {
        if (c != null) StopCoroutine(c);
    }
}

// ═══════════════════════════════════════════════════════════════
// ENUM — обратная совместимость со старым кодом
// ═══════════════════════════════════════════════════════════════

public enum SoundType
{
    Click,
    Place,
    Remove,
    wrongPlacement,
    shoot
}
