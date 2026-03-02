using UnityEngine;

// きさらぎ駅 – ループごとの演出設定（データで管理・コード修正なしで調整可能）
[CreateAssetMenu(menuName = "Game/Loop Data", fileName = "LoopData")]
public class LoopData : ScriptableObject
{
    [Header("Visual (スクショ映え: ループ1=0.012 → 4=0.035)")]
    [Range(0f, 0.1f)] public float fogDensity = 0.012f;
    [Range(-100f, 100f)] public float saturation = -15f;
    [Range(0f, 1f)] public float chromaticAberration = 0.2f;
    [Range(0f, 1f)] public float vignette = 0.35f;
    [Range(0f, 1f)] public float vignetteSmoothness = 0.5f;
    [Range(0f, 1f)] public float filmGrain = 0.25f;
    [Range(0f, 1f)] public float filmGrainResponse = 0.8f;
    [Range(-2f, 2f)] public float postExposure = -0.3f;
    [Range(-100f, 100f)] public float contrast = 10f;

    [Header("Audio")]
    [Range(0f, 1f)] public float lowFrequencyVolume = 0f;
    [Range(0f, 1f)] public float windVolume = 0.2f;
    [Range(0f, 1f)] public float footstepDelay = 0f;

    [Header("Environment")]
    public bool enableDummy;
    public bool reverseClock;
    [Tooltip("空なら駅名変更なし。例: きさらぎ駅 → きさら木駅 → 読めない")]
    public string stationNameOverride;
    [Tooltip("電光掲示板用。空なら変更なし")]
    public string signDisplayText;

    [Header("Steam向け破壊（後半ループ）")]
    [Tooltip("ONでメニューが歪む／開きにくい")]
    public bool menuBreak;
    [Tooltip("ONで設定画面が開けない or 別画面")]
    public bool settingsBreak;
    [Tooltip("ONで左右定位反転（右から音→実は左）。ループ4以降")]
    public bool stereoFlip;
}
