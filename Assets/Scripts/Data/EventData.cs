using UnityEngine;

// きさらぎ駅 – 異常の質（Steam向け）。L1=物理 / L2=認知 / L3=存在（バズ用）
public enum AnomalyLayer
{
    L1_Physical,   // ベンチ消える・時計逆回転・ポスター変化
    L2_Cognitive,  // 看板文字微妙に違う・白線幅・影の方向逆
    L3_Existence   // Watcher毎回違う・見た人で位置違う・スクショで形が違う
}

// きさらぎ駅 – 違和感イベントのデータ定義（ScriptableObject で演出追加可能）
public enum EventTriggerType
{
    OnLook,
    OnInteract,
    OnTime,
    OnEnterZone
}

[CreateAssetMenu(menuName = "Game/Event Data", fileName = "EventData")]
public class EventData : ScriptableObject
{
    public string eventID;
    [Tooltip("異常のレイヤー（L1=物理 / L2=認知 / L3=存在）。Steam・実況映え用")]
    public AnomalyLayer anomalyLayer = AnomalyLayer.L1_Physical;
    [Tooltip("このループ以上で有効")]
    public int minLoop = 1;
    [Tooltip("このループ以下で有効（0=制限なし）")]
    public int maxLoop = 0;

    public EventTriggerType triggerType;

    [Tooltip("発火時に有効/無効にするオブジェクト（任意）")]
    public GameObject targetObject;
    public AudioClip sound;
    [Tooltip("一度発火したら二度と発火しない")]
    public bool disableAfterTrigger = true;
}
