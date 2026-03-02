using UnityEngine;

// エリアID: 0=ホームA / 1=待合室 / 2=跨線橋 / 3=ホームB
// BoxCollider(isTrigger=true) と同じオブジェクトに配置
public class AreaVolume : MonoBehaviour
{
    [Header("エリア識別")]
    [Tooltip("0=ホームA, 1=待合室, 2=跨線橋, 3=ホームB")]
    public int areaID;

    public static int CurrentAreaID { get; private set; } = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CurrentAreaID = areaID;
        Debug.Log($"[AreaVolume] エリア {areaID} に入りました");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"[AreaVolume] エリア {areaID} から出ました");
    }
}
