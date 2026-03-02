using UnityEngine;

// きさらぎ駅ループ – シーケンス検証補助クラス
// GameManagerの判定ロジックを単体テスト可能な形に分離している
// （将来Brain自動生成時のテンプレ分離用）
public static class SequenceValidator
{
    /// <summary>
    /// 与えられたアクションが正解シーケンスの現在位置と一致するか判定する
    /// </summary>
    public static bool IsCorrect(string actionID, string[] sequence, int currentIndex)
    {
        if (sequence == null || sequence.Length == 0) return false;
        if (currentIndex < 0 || currentIndex >= sequence.Length) return false;

        return string.Equals(actionID, sequence[currentIndex], System.StringComparison.Ordinal);
    }

    /// <summary>
    /// シーケンスが完了しているか確認する
    /// </summary>
    public static bool IsComplete(int currentIndex, string[] sequence)
    {
        if (sequence == null) return false;
        return currentIndex >= sequence.Length;
    }
}
