using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

// きさらぎ駅 – Station 用ループ別見た目適用（LoopVariantData 駆動・依存最小化）
public class VariantController : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("ループ 0 が Loop1 用。4 種作成推奨")]
    public LoopVariantData[] variants;

    [Header("References")]
    [Tooltip("床の Renderer（マテリアル差し替え用）")]
    public Renderer floorRenderer;
    [Tooltip("駅名表示")]
    public TMP_Text stationNameText;
    [Tooltip("電光掲示板（任意）")]
    public TMP_Text signDisplayText;
    [Tooltip("Watcher の親など。enableWatcher で SetActive")]
    public GameObject watcherRoot;
    [Tooltip("蛍光灯の親。flickerLights で子の FlickerLight を一括 ON/OFF")]
    public GameObject flickerLightsRoot;

    public void ApplyVariant(int index)
    {
        if (variants == null || index < 0 || index >= variants.Length) return;
        var v = variants[index];

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = v.fogDensity;

        if (floorRenderer != null && v.floorMaterial != null)
            floorRenderer.sharedMaterial = v.floorMaterial;

        if (stationNameText != null)
            stationNameText.text = v.stationName;

        if (signDisplayText != null && !string.IsNullOrEmpty(v.signDisplayText))
            signDisplayText.text = v.signDisplayText;

        if (watcherRoot != null)
            watcherRoot.SetActive(v.enableWatcher);

        if (flickerLightsRoot != null)
        {
            var flickers = flickerLightsRoot.GetComponentsInChildren<FlickerLight>(true);
            foreach (var f in flickers)
                f.enabled = v.flickerLights;
        }
    }
}
