using UnityEngine;

// きさらぎ駅 – 全ループの LoopData をまとめたアセット
[CreateAssetMenu(menuName = "Game/Loop Database", fileName = "LoopDatabase")]
public class LoopDatabase : ScriptableObject
{
    public LoopData[] loops;

    /// <summary>1始まりのループ番号で LoopData を取得（1→loops[0], 2→loops[1]）</summary>
    public LoopData GetLoop(int loopNumber)
    {
        if (loops == null || loops.Length == 0) return null;
        int index = Mathf.Clamp(loopNumber - 1, 0, loops.Length - 1);
        return loops[index];
    }

    public int LoopCount => loops != null ? loops.Length : 0;
}
