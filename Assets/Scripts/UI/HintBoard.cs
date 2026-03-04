using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;

// きさらぎ駅ループ – 構内張り紙（縦書きヒントボード）
// GameManager が存在する場合はランダム化済みシーケンスを表示。
// GameManager がない場合（シーン単体テスト時）はフォールバックリストを表示。
public class HintBoard : MonoBehaviour
{
    [SerializeField] private TextMeshPro boardText;

    // GameManager なし時のフォールバックヒント
    private static readonly string[] Fallback = new string[]
    {
        "①駅名を確かめろ",   "②時を刻む音に耳を澄ませ", "③時刻表を調べろ",
        "④声が聞こえる方へ向かえ", "⑤誰かが残した物がある", "⑥改札を確認しろ",
        "⑦壁のポスターを見ろ", "⑧鳴り続ける電話がある",  "⑨線路を見渡せ",
        "⑩暗い場所に手がかりがある",
    };

    private void Start() => StartCoroutine(InitWhenReady());

    private IEnumerator InitWhenReady()
    {
        // GameManager.Awake が先に走ることを保証するため最大5フレーム待機
        int wait = 0;
        while (GameManager.Instance == null && wait++ < 5)
            yield return null;

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
        if (boardText == null) return;

        if (GameManager.Instance == null)
        {
            // フォールバック：全項目を黒・ハイライトなしで表示
            boardText.text = BuildTategaki(Fallback, -1);
            return;
        }

        var gm  = GameManager.Instance;
        var seq = gm.correctSequence;
        var items = new string[seq.Length];
        for (int i = 0; i < seq.Length; i++)
            items[i] = CircledNum(i + 1) + gm.GetHintForID(seq[i]);

        boardText.text = BuildTategaki(items, gm.currentProgress);
    }

    static string CircledNum(int n)
        => (n >= 1 && n <= 20) ? ((char)(0x2460 + n - 1)).ToString() : n + ".";

    // 縦書きグリッド（右→左列、上→下行）
    static string BuildTategaki(string[] items, int progress)
    {
        int maxRows = 0;
        foreach (var s in items)
            if (s.Length > maxRows) maxRows = s.Length;

        var sb = new StringBuilder();
        for (int row = 0; row < maxRows; row++)
        {
            for (int col = items.Length - 1; col >= 0; col--)
            {
                char c = row < items[col].Length ? items[col][row] : '\u3000';
                if      (col < progress)  sb.Append($"<color=#3a3a3a>{c}</color>");
                else if (col == progress) sb.Append($"<color=#d4b84a>{c}</color>");
                else                      sb.Append(c);

                if (col > 0) sb.Append('\u3000');
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
