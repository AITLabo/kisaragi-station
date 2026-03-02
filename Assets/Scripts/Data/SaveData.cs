using System;
using UnityEngine;

// きさらぎ駅 – セーブデータ構造（バージョン管理・JsonUtility 対応のため配列で保持）
[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public int loop;
    public int step;
    public float posX, posY, posZ;
    public string[] triggeredEvents;
}
