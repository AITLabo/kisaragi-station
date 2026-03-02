using UnityEngine;

// きさらぎ駅 – 駅専用ループ別データ（Station は1個。変化は ScriptableObject 駆動）
[CreateAssetMenu(menuName = "Loop/Variant Data", fileName = "LoopVariantData")]
public class LoopVariantData : ScriptableObject
{
    [Header("Fog")]
    [Range(0f, 0.1f)] public float fogDensity = 0.012f;

    [Header("Station")]
    [Tooltip("このループで使う床マテリアル（空なら変更しない）")]
    public Material floorMaterial;
    [Tooltip("駅名表示テキスト")]
    public string stationName = "きさらぎ駅";
    [Tooltip("電光掲示板用")]
    public string signDisplayText;

    [Header("Behaviour")]
    public bool enableWatcher;
    [Tooltip("蛍光灯点滅を有効にする")]
    public bool flickerLights;
}
