using UnityEngine;
using TMPro;
using System.Text;

// きさらぎ駅ループ – 構内張り紙（縦書きヒントボード）
// ゲーム開始時に GameManager のランダム化済みシーケンスを読み取り、
// 縦書き（右→左、上→下）でヒント一覧をリアルタイム表示する。
// 正解が進むと完了済み項目はグレーアウト、現在のターゲットは黄色ハイライト。
public class HintBoard : MonoBehaviour
{
    [SerializeField] private TextMeshPro boardText;

    private void Start()
    {
        UpdateBoard();
        if (GameManager.Instance != null)
            GameManager.Instance.OnProgressAdvanced += OnProgress;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnProgressAdvanced -= OnProgress;
    }

    private void OnProgress(string _, int __) => UpdateBoard();

    private void UpdateBoard()
    {
        if (boardText == null || GameManager.Instance == null) return;

        var gm  = GameManager.Instance;
        var seq = gm.correctSequence;

        // 各列 = 丸数字 + ヒント文字列
        var items = new string[seq.Length];
        for (int i = 0; i < seq.Length; i++)
            items[i] = CircledNum(i + 1) + gm.GetHintForID(seq[i]);

        boardText.text = BuildTategaki(items, gm.currentProgress);
    }

    static string CircledNum(int n)
    {
        // ①=U+2460 ～ ⑩=U+2469
        return (n >= 1 && n <= 20) ? ((char)(0x2460 + n - 1)).ToString() : n + ".";
    }

    // 縦書きグリッド生成（右→左列、上→下行）
    static string BuildTategaki(string[] items, int progress)
    {
        int maxRows = 0;
        foreach (var s in items)
            if (s.Length > maxRows) maxRows = s.Length;

        var sb = new StringBuilder();
        for (int row = 0; row < maxRows; row++)
        {
            for (int col = items.Length - 1; col >= 0; col--) // 右→左
            {
                char c = row < items[col].Length ? items[col][row] : '\u3000'; // 全角スペースで揃える
                if      (col < progress)  sb.Append($"<color=#3a3a3a>{c}</color>");
                else if (col == progress) sb.Append($"<color=#d4b84a>{c}</color>"); // 現在の目標
                else                      sb.Append(c);

                if (col > 0) sb.Append('\u3000'); // 列間スペース
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
