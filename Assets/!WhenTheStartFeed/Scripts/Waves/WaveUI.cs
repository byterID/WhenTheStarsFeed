using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI countdownText;

    private void Start()
    {
        Waves.Instance.OnWaveStarted += OnWaveStarted;
        Waves.Instance.OnCountdownTick += OnCountdownTick;
    }

    private void OnDestroy()
    {
        if (Waves.Instance == null) return;
        Waves.Instance.OnWaveStarted -= OnWaveStarted;
        Waves.Instance.OnCountdownTick -= OnCountdownTick;
    }

    private void OnWaveStarted(int waveNumber)
    {
        waveText.text = $"Волна {waveNumber}";
        countdownText.text = "";
    }

    private void OnCountdownTick(float seconds)
    {
        if (seconds > 0f)
            countdownText.text = $"До волны: {seconds:F0}с";
        else
            countdownText.text = "Волна началась!";
    }
}
