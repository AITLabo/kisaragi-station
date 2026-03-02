using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

// きさらぎ駅ループ – 歪み演出の統合コントローラ
// DistortionLevel に応じてポストプロセス・ライト・テキストを変化させる
public class AnomalyController : MonoBehaviour
{
    // ──────────────────────────────────────
    // ポストプロセス
    // ──────────────────────────────────────
    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;

    private Vignette             vignette;
    private ChromaticAberration  chromatic;
    private FilmGrain            filmGrain;
    private ColorAdjustments     colorAdj;

    // ──────────────────────────────────────
    // シーン参照
    // ──────────────────────────────────────
    [Header("Scene References")]
    [SerializeField] private Light         mainDirectionalLight;
    [SerializeField] private TMPro.TMP_Text stationNameText;   // 駅名看板テキスト
    [SerializeField] private TMPro.TMP_Text clockText;         // 時計テキスト

    [Header("Station Name Settings")]
    [SerializeField] private string normalStationName  = "きさらぎ駅";
    [SerializeField] private string[] corruptedNames   = { "きさぎら駅", "き◻◻ぎ駅", "━━━━━", "????? 駅" };

    // ──────────────────────────────────────
    // パラメータ（Inspectorで調整可能）
    // ──────────────────────────────────────
    [Header("Distortion Params – Low")]
    [SerializeField, Range(0f, 1f)] private float lowVignette    = 0.15f;
    [SerializeField, Range(0f, 1f)] private float lowChromatic   = 0.0f;
    [SerializeField, Range(0f, 1f)] private float lowFilmGrain   = 0.1f;

    [Header("Distortion Params – Medium")]
    [SerializeField, Range(0f, 1f)] private float medVignette    = 0.30f;
    [SerializeField, Range(0f, 1f)] private float medChromatic   = 0.25f;
    [SerializeField, Range(0f, 1f)] private float medFilmGrain   = 0.25f;

    [Header("Distortion Params – High")]
    [SerializeField, Range(0f, 1f)] private float highVignette   = 0.50f;
    [SerializeField, Range(0f, 1f)] private float highChromatic  = 0.50f;
    [SerializeField, Range(0f, 1f)] private float highFilmGrain  = 0.45f;

    [Header("Distortion Params – Collapse")]
    [SerializeField, Range(0f, 1f)] private float collapseVignette  = 0.75f;
    [SerializeField, Range(0f, 1f)] private float collapseChromatic = 1.0f;
    [SerializeField, Range(0f, 1f)] private float collapseFilmGrain = 0.8f;
    [SerializeField] private Color collapseAmbientColor = new Color(0.4f, 0.0f, 0.0f);

    // ──────────────────────────────────────

    private void Awake()
    {
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();

        if (globalVolume == null)
        {
            Debug.LogError("[AnomalyController] globalVolume が未設定です");
            return;
        }

        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out chromatic);
        globalVolume.profile.TryGet(out filmGrain);
        globalVolume.profile.TryGet(out colorAdj);
    }

    // ──────────────────────────────────────
    // 主エントリポイント
    // ──────────────────────────────────────
    public void ApplyDistortion(DistortionLevel level)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToLevel(level));
    }

    private IEnumerator TransitionToLevel(DistortionLevel level)
    {
        float duration = 1.5f;
        float elapsed  = 0f;

        float targetVig = 0f, targetChr = 0f, targetGrain = 0f;

        switch (level)
        {
            case DistortionLevel.None:
                targetVig = 0f; targetChr = 0f; targetGrain = 0f;
                RestoreStationName();
                RestoreLightColor();
                break;
            case DistortionLevel.Low:
                targetVig = lowVignette; targetChr = lowChromatic; targetGrain = lowFilmGrain;
                break;
            case DistortionLevel.Medium:
                targetVig = medVignette; targetChr = medChromatic; targetGrain = medFilmGrain;
                StartCoroutine(CorruptStationName());
                break;
            case DistortionLevel.High:
                targetVig = highVignette; targetChr = highChromatic; targetGrain = highFilmGrain;
                StartCoroutine(CorruptStationName());
                StartCoroutine(FlickerLight());
                break;
            case DistortionLevel.Collapse:
                targetVig = collapseVignette; targetChr = collapseChromatic; targetGrain = collapseFilmGrain;
                SetLightColor(collapseAmbientColor);
                if (stationNameText != null) stationNameText.text = "あなたはここにいる";
                break;
        }

        float startVig   = vignette   != null ? vignette.intensity.value   : 0f;
        float startChr   = chromatic  != null ? chromatic.intensity.value  : 0f;
        float startGrain = filmGrain  != null ? filmGrain.intensity.value  : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (vignette  != null) vignette.intensity.value  = Mathf.Lerp(startVig,   targetVig,   t);
            if (chromatic != null) chromatic.intensity.value = Mathf.Lerp(startChr,   targetChr,   t);
            if (filmGrain != null) filmGrain.intensity.value = Mathf.Lerp(startGrain, targetGrain, t);

            yield return null;
        }
    }

    // ──────────────────────────────────────
    // 駅名崩壊演出
    // ──────────────────────────────────────
    private IEnumerator CorruptStationName()
    {
        if (stationNameText == null) yield break;

        yield return new WaitForSeconds(Random.Range(2f, 5f));
        string corrupted = corruptedNames[Random.Range(0, corruptedNames.Length)];
        stationNameText.text = corrupted;
        yield return new WaitForSeconds(0.3f);
        stationNameText.text = normalStationName;
    }

    private void RestoreStationName()
    {
        if (stationNameText != null) stationNameText.text = normalStationName;
    }

    // ──────────────────────────────────────
    // 照明演出
    // ──────────────────────────────────────
    private IEnumerator FlickerLight()
    {
        if (mainDirectionalLight == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            mainDirectionalLight.enabled = false;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            mainDirectionalLight.enabled = true;
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }

    private void SetLightColor(Color color)
    {
        if (mainDirectionalLight != null) mainDirectionalLight.color = color;
    }

    private void RestoreLightColor()
    {
        if (mainDirectionalLight != null) mainDirectionalLight.color = Color.white;
    }
}
