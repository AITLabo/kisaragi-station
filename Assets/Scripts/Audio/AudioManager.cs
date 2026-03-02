using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

// きさらぎ駅ループ – 音響歪み管理
// 歪みレベルに応じてノイズ音量・LowPassフィルタ・ランダムノイズを制御する
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;   // 環境音（電車音・踏切など）
    [SerializeField] private AudioSource noiseSource;     // 低周波ノイズ
    [SerializeField] private AudioSource announcementSource; // 駅構内放送

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("Noise Volume by Level")]
    [SerializeField, Range(0f, 1f)] private float noiseNone     = 0.0f;
    [SerializeField, Range(0f, 1f)] private float noiseLow      = 0.15f;
    [SerializeField, Range(0f, 1f)] private float noiseMedium   = 0.35f;
    [SerializeField, Range(0f, 1f)] private float noiseHigh     = 0.60f;
    [SerializeField, Range(0f, 1f)] private float noiseCollapse = 0.90f;

    [Header("LowPass Cutoff by Level (Hz)")]
    [SerializeField] private float cutoffNone     = 22000f;
    [SerializeField] private float cutoffLow      = 18000f;
    [SerializeField] private float cutoffMedium   = 12000f;
    [SerializeField] private float cutoffHigh     = 6000f;
    [SerializeField] private float cutoffCollapse = 2500f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1.2f;

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (ambientSource == null) Debug.LogError("[AudioManager] ambientSource が未設定です");
        if (noiseSource   == null) Debug.LogError("[AudioManager] noiseSource が未設定です");
    }

    // ──────────────────────────────────────
    // GameManagerから呼ばれる
    // ──────────────────────────────────────
    public void ApplyDistortion(DistortionLevel level)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionAudio(level));
    }

    private IEnumerator TransitionAudio(DistortionLevel level)
    {
        float targetNoise   = GetNoiseTarget(level);
        float targetCutoff  = GetCutoffTarget(level);
        float elapsed       = 0f;

        float startNoise  = noiseSource != null ? noiseSource.volume : 0f;
        float startCutoff = 22000f;

        if (masterMixer != null)
            masterMixer.GetFloat("AmbientLowPassCutoff", out startCutoff);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (noiseSource != null)
                noiseSource.volume = Mathf.Lerp(startNoise, targetNoise, t);

            if (masterMixer != null)
                masterMixer.SetFloat("AmbientLowPassCutoff", Mathf.Lerp(startCutoff, targetCutoff, t));

            yield return null;
        }

        // Collapse時は放送を歪ませる
        if (level == DistortionLevel.Collapse)
            StartCoroutine(DistortAnnouncement());
    }

    // 放送歪み演出（Collapseレベルで発動）
    private IEnumerator DistortAnnouncement()
    {
        if (announcementSource == null) yield break;

        float original = announcementSource.pitch;
        for (int i = 0; i < 5; i++)
        {
            announcementSource.pitch = Random.Range(0.5f, 1.5f);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
        }
        announcementSource.pitch = original;
    }

    // ──────────────────────────────────────
    private float GetNoiseTarget(DistortionLevel level) => level switch
    {
        DistortionLevel.None     => noiseNone,
        DistortionLevel.Low      => noiseLow,
        DistortionLevel.Medium   => noiseMedium,
        DistortionLevel.High     => noiseHigh,
        DistortionLevel.Collapse => noiseCollapse,
        _                        => 0f
    };

    private float GetCutoffTarget(DistortionLevel level) => level switch
    {
        DistortionLevel.None     => cutoffNone,
        DistortionLevel.Low      => cutoffLow,
        DistortionLevel.Medium   => cutoffMedium,
        DistortionLevel.High     => cutoffHigh,
        DistortionLevel.Collapse => cutoffCollapse,
        _                        => 22000f
    };
}
