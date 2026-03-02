using UnityEngine;
using UnityEditor;

// ─────────────────────────────────────────────────────────
// Kisaragi > ★ Build Station Scene All（駅シーン一括構築）
//
// 駅プロトタイプシーンに必要な全ビルダーを順番に実行する。
// 毎回この1コマンドだけ実行すれば OK。
//
// 実行順:
//   ① KisaragiStationPrototypeBuilder  → ホームA・プレイヤー・カメラ・点字ブロック
//   ② KisaragiOverpassBuilder          → 陸橋・ホームB・蛍光灯・点字ブロック
//   ③ KisaragiGateBuilder              → 改札ビル（プレイヤーは入れない）
//   ④ KisaragiPrototypeAnomalyBuilder  → 違和感オブジェクト10個
//   ⑤ KisaragiFontFixer                → 日本語フォント自動設定
// ─────────────────────────────────────────────────────────
public class KisaragiStationAllBuilder
{
    [MenuItem("Kisaragi/★ Build Station Scene All（駅シーン一括構築）")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー",
                "Play モード中は実行できません。\nStop してから実行してください。", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "駅シーン一括構築",
            "以下の順序で全オブジェクトを構築します:\n\n" +
            "① Station Prototype  … ホームA・プレイヤー・カメラ・点字ブロック（ドット）\n" +
            "② Build Overpass     … 陸橋・ホームB・蛍光灯（天井・廊下向き）・点字（ドット）\n" +
            "③ Build Gate         … 改札ビル（プレイヤー通過可能・改札〜階段接続）\n" +
            "④ Prototype Anomalies… 違和感オブジェクト10個\n" +
            "⑤ Fix Japanese Font  … 日本語フォント自動設定\n\n" +
            "現在のシーンは保存を確認してから上書きされます。\n\n" +
            "実行しますか？",
            "実行", "キャンセル");

        if (!confirm) return;

        // ① ホームA 基本シーン（新規シーン生成）
        Debug.Log("[BuildAll] ① Station Prototype 構築中...");
        KisaragiStationPrototypeBuilder.BuildStationPrototype();

        // ② 陸橋・ホームB（OverpassRoot を追加）
        Debug.Log("[BuildAll] ② Build Overpass 構築中...");
        KisaragiOverpassBuilder.BuildOverpass();

        // ③ 改札ビル（GateRoot を追加・プレイヤーは入れない）
        Debug.Log("[BuildAll] ③ Build Gate Building 構築中...");
        KisaragiGateBuilder.BuildGateBuilding();

        // ④ 違和感オブジェクト10個（KisaragiAnomalies を追加）
        Debug.Log("[BuildAll] ④ Build Prototype Anomalies 構築中...");
        KisaragiPrototypeAnomalyBuilder.BuildPrototypeAnomalies();

        // ⑤ 日本語フォント（TMP_FontAsset 生成・全 TMP に割り当て）
        Debug.Log("[BuildAll] ⑤ Fix Japanese Font...");
        KisaragiFontFixer.FixJapaneseFont();

        // ⑥ レティクル・InteractionSystem（照準UI + インタラクト）
        Debug.Log("[BuildAll] ⑥ Build Reticle...");
        KisaragiReticleBuilder.BuildReticle();

        Debug.Log("[BuildAll] 駅シーン一括構築完了");
    }
}
