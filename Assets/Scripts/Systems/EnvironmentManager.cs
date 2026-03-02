using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// きさらぎ駅 – ループごとの環境変化（LoopData 駆動・コード修正なしで調整可能）
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("Optional References")]
    [SerializeField] private Light mainLight;
    [SerializeField] private Transform platformScaler;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private TMPro.TMP_Text stationNameText;
    [SerializeField] private TMPro.TMP_Text signDisplayText;
    [SerializeField] private PlayerController playerController;

    private ColorAdjustments _colorAdj;
    private ChromaticAberration _chromatic;
    private Vignette _vignette;
    private FilmGrain _filmGrain;

    private static readonly Color FogColorDesign = new Color(0.435f, 0.486f, 0.561f); // #6F7C8F
    private static readonly Color AmbientColorDesign = new Color(0.106f, 0.141f, 0.188f); // #1B2430

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _colorAdj);
            globalVolume.profile.TryGet(out _chromatic);
            globalVolume.profile.TryGet(out _vignette);
            globalVolume.profile.TryGet(out _filmGrain);
        }
    }

    /// <summary>ScriptableObject のループ設定を適用（プロダクション用・スクショ映え値）</summary>
    public void Apply(LoopData data)
    {
        if (data == null) return;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = data.fogDensity;
        RenderSettings.fogColor = FogColorDesign;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColorDesign;
        RenderSettings.ambientIntensity = 0.7f;
        RenderSettings.reflectionIntensity = 0.5f;

        if (_colorAdj != null)
        {
            _colorAdj.saturation.Override(data.saturation);
            _colorAdj.contrast.Override(data.contrast);
            _colorAdj.postExposure.Override(data.postExposure);
        }
        if (_chromatic != null) _chromatic.intensity.Override(data.chromaticAberration);
        if (_vignette != null)
        {
            _vignette.intensity.Override(data.vignette);
            _vignette.smoothness.Override(data.vignetteSmoothness);
        }
        if (_filmGrain != null)
        {
            _filmGrain.intensity.Override(data.filmGrain);
            _filmGrain.response.Override(data.filmGrainResponse);
        }

        if (mainLight != null && data.fogDensity > 0.03f)
            mainLight.intensity = Mathf.Max(0.28f, 1f - (data.fogDensity - 0.03f) * 10f);

        if (!string.IsNullOrEmpty(data.stationNameOverride) && stationNameText != null)
            stationNameText.text = data.stationNameOverride;
        if (signDisplayText != null)
            signDisplayText.text = string.IsNullOrEmpty(data.signDisplayText) ? signDisplayText.text : data.signDisplayText;

        if (playerController != null)
            playerController.SetFootstepDelay(data.footstepDelay);
    }

    /// <summary>ループ番号のみで適用（スクショ映え霧テーブル）</summary>
    public void UpdateEnvironment(int loop)
    {
        float density = loop switch { 1 => 0.012f, 2 => 0.018f, 3 => 0.025f, 4 => 0.035f, _ => 0.04f };
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = density;
        RenderSettings.fogColor = FogColorDesign;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColorDesign;
        RenderSettings.ambientIntensity = 0.7f;
        RenderSettings.reflectionIntensity = 0.5f;

        if (platformScaler != null && loop >= 2)
        {
            float scaleX = 1f + (loop - 1) * 0.025f;
            platformScaler.localScale = new Vector3(scaleX, platformScaler.localScale.y, platformScaler.localScale.z);
        }
        if (mainLight != null && loop >= 3)
            mainLight.intensity = Mathf.Max(0.4f, mainLight.intensity - 0.1f * (loop - 2));
    }
}
