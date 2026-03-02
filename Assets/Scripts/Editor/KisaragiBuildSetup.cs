using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// Kisaragi > Add Scenes to Build Settings
// ゲームフロー順にシーンを Build Settings へ自動追加する
// TrainScene → SubwayStationDemo → EscapeScene
public class KisaragiBuildSetup
{
    // ゲームフロー順のシーンパス
    static readonly string[] SCENE_ORDER = new string[]
    {
        "Assets/Scenes/TrainScene.unity",           // Index 0: 電車内
        "Assets/SubwayModelSet/Scenes/SubwayStationDemo.unity", // Index 1: きさらぎ駅
        "Assets/Scenes/EscapeScene.unity",          // Index 2: 帰還エンディング
    };

    [MenuItem("Kisaragi/Add Scenes to Build Settings (ビルド設定にシーンを追加)")]
    public static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        List<string> missing = new List<string>();
        List<string> added = new List<string>();

        foreach (string scenePath in SCENE_ORDER)
        {
            if (File.Exists(scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                added.Add(scenePath);
                Debug.Log($"[BuildSetup] ✓ 追加: {scenePath}");
            }
            else
            {
                missing.Add(scenePath);
                Debug.LogWarning($"[BuildSetup] ✗ ファイルが見つかりません: {scenePath}");
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        string message = $"Build Settings を更新しました。\n\n";
        message += $"追加されたシーン ({added.Count}):\n";
        for (int i = 0; i < added.Count; i++)
            message += $"  [{i}] {System.IO.Path.GetFileNameWithoutExtension(added[i])}\n";

        if (missing.Count > 0)
        {
            message += $"\n未作成のシーン ({missing.Count}):\n";
            foreach (var m in missing)
                message += $"  • {System.IO.Path.GetFileNameWithoutExtension(m)}\n";
            message += "\n未作成のシーンは対応するビルダーを実行してください:\n";
            message += "  • TrainScene    → Kisaragi > Build Train Scene\n";
            message += "  • EscapeScene   → Kisaragi > Build Escape Scene\n";
        }

        EditorUtility.DisplayDialog("Build Settings 更新完了", message, "OK");
    }

    [MenuItem("Kisaragi/Full Setup - Build All Scenes (全シーンを一括構築)")]
    public static void FullSetup()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "全シーン一括構築",
            "以下の順序で全シーンを構築します:\n\n" +
            "1. TrainScene（電車内）\n" +
            "2. EscapeScene（帰還エンディング）\n" +
            "3. Build Settings に全シーンを追加\n\n" +
            "※ SubwayStationDemo（きさらぎ駅）は既存シーンを使用します。\n" +
            "※ 現在開いているシーンへの変更は保存されます。\n\n" +
            "実行しますか？",
            "実行", "キャンセル");

        if (!confirm) return;

        // TrainScene を構築
        Debug.Log("[FullSetup] TrainScene を構築中...");
        KisaragiTrainBuilder.BuildTrainScene();

        // EscapeScene を構築
        Debug.Log("[FullSetup] EscapeScene を構築中...");
        KisaragiEsceneBuilder.BuildEscapeScene();

        // Build Settings に追加
        Debug.Log("[FullSetup] Build Settings を更新中...");
        AddScenesToBuildSettings();

        // SampleScene に戻る（念のため）
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (File.Exists(sampleScenePath))
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sampleScenePath);

        Debug.Log("[FullSetup] 完了！");
        EditorUtility.DisplayDialog(
            "完了",
            "全シーンの構築が完了しました！\n\n" +
            "次のステップ:\n" +
            "1. SubwayStationDemo シーンを開く\n" +
            "2. Kisaragi > Build Anomaly Objects を実行（違和感10個を配置）\n" +
            "3. Hierarchy に SceneFlowManager を追加\n" +
            "4. Play して電車内からスタートするか確認\n\n" +
            "File > Build Settings > Build で WebGL ビルドできます。",
            "OK");
    }

    [MenuItem("Kisaragi/Show Build Settings Status (ビルド設定確認)")]
    public static void ShowBuildStatus()
    {
        var scenes = EditorBuildSettings.scenes;
        string status = $"現在の Build Settings ({scenes.Length} シーン):\n\n";

        for (int i = 0; i < scenes.Length; i++)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(scenes[i].path);
            string enabled = scenes[i].enabled ? "✓" : "✗";
            status += $"  [{i}] {enabled} {name}\n      {scenes[i].path}\n";
        }

        if (scenes.Length == 0)
            status += "  （シーンが登録されていません）\n\n" +
                      "Kisaragi > Add Scenes to Build Settings を実行してください。";

        EditorUtility.DisplayDialog("Build Settings 確認", status, "OK");
        Debug.Log(status);
    }
}
