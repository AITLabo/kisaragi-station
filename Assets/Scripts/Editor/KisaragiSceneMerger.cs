using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Unity メニュー「Kisaragi > Merge to Subway Scene」
// SampleScene のゲームオブジェクトを SubwayStationDemo シーンに自動移植する
public class KisaragiSceneMerger
{
    static readonly string[] COPY_OBJECTS = new string[]
    {
        "GameManager",
        "AnomalyController",
        "AudioManager",
        "LogManager",
        "LoopController",
        "LoopTrigger",
        "ResetPoint",
        "Player",
        "UI Canvas",
        "Interactables",
        "PsychologicalEffects",
        "Global Volume"
    };

    [MenuItem("Kisaragi/Merge to Subway Scene (地下鉄シーンに移植)")]
    public static void MergeToSubwayScene()
    {
        // ──────────────────────────────────────
        // SampleScene が開いているか確認
        // ──────────────────────────────────────
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.name.Contains("Sample"))
        {
            EditorUtility.DisplayDialog("確認",
                "SampleScene を開いた状態で実行してください。\n\n現在: " + currentScene.name, "OK");
            return;
        }

        // ──────────────────────────────────────
        // SubwayStationDemo シーンのパスを探す
        // ──────────────────────────────────────
        string subwayScenePath = "";
        string[] guids = AssetDatabase.FindAssets("SubwayStationDemo t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("SubwayModelSet"))
            {
                subwayScenePath = path;
                break;
            }
        }

        if (string.IsNullOrEmpty(subwayScenePath))
        {
            EditorUtility.DisplayDialog("エラー",
                "SubwayStationDemo シーンが見つかりません。\nSubwayModelSet がインポートされているか確認してください。", "OK");
            return;
        }

        // ──────────────────────────────────────
        // コピー対象オブジェクトを収集
        // ──────────────────────────────────────
        List<GameObject> objectsToCopy = new List<GameObject>();
        foreach (string name in COPY_OBJECTS)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                objectsToCopy.Add(go);
                Debug.Log($"[Merger] コピー対象: {name}");
            }
            else
            {
                Debug.LogWarning($"[Merger] 見つかりません: {name}（スキップ）");
            }
        }

        if (objectsToCopy.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "コピー対象が見つかりません。", "OK");
            return;
        }

        // ──────────────────────────────────────
        // SampleScene を保存
        // ──────────────────────────────────────
        EditorSceneManager.SaveScene(currentScene);

        // ──────────────────────────────────────
        // SubwayStationDemo を開く
        // ──────────────────────────────────────
        Scene subwayScene = EditorSceneManager.OpenScene(subwayScenePath, OpenSceneMode.Single);

        // ──────────────────────────────────────
        // オブジェクトをSubwaySceneに移動
        // ──────────────────────────────────────
        foreach (GameObject go in objectsToCopy)
        {
            SceneManager.MoveGameObjectToScene(go, subwayScene);
            Debug.Log($"[Merger] 移植完了: {go.name}");
        }

        // ──────────────────────────────────────
        // Player の位置をSubwayシーンに合わせる
        // ──────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0, 1f, 0);
            Debug.Log("[Merger] Player 位置をリセットしました");
        }

        // ──────────────────────────────────────
        // Global Volume が重複していれば古い方を削除
        // ──────────────────────────────────────
        var volumes = Object.FindObjectsOfType<UnityEngine.Rendering.Volume>();
        if (volumes.Length > 1)
        {
            // Kisaragi用以外を削除
            foreach (var v in volumes)
            {
                if (v.gameObject.name != "Global Volume")
                {
                    Object.DestroyImmediate(v.gameObject);
                    Debug.Log("[Merger] 重複 Volume を削除しました");
                }
            }
        }

        // ──────────────────────────────────────
        // Fix References を自動実行
        // ──────────────────────────────────────
        KisaragiRefFixer.FixReferences();

        // ──────────────────────────────────────
        // シーン保存
        // ──────────────────────────────────────
        EditorSceneManager.SaveScene(subwayScene);

        Debug.Log("=== Subway Scene への移植完了 ===");
        EditorUtility.DisplayDialog("完了",
            "SubwayStationDemo シーンへの移植が完了しました！\n\n" +
            "▶ Play で動作確認してください。\n\n" +
            "Playerが地面に埋まっている場合は\n" +
            "Player の Y 座標を調整してください。", "OK");
    }
}
