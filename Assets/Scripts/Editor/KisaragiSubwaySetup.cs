using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

// Unity メニュー「Kisaragi > Setup Subway Scene」
// 1. マテリアルをURPに変換
// 2. SampleSceneからゲームオブジェクトをコピー
// 3. Player位置をSubway内に配置
// 4. 全参照を修正
public class KisaragiSubwaySetup
{
    [MenuItem("Kisaragi/Setup Subway Scene (地下鉄シーン完全セットアップ)")]
    public static void SetupSubwayScene()
    {
        Scene active = EditorSceneManager.GetActiveScene();

        // ──────────────────────────────────────
        // STEP 1: マテリアルをURPに変換
        // ──────────────────────────────────────
        ConvertMaterials();

        // ──────────────────────────────────────
        // STEP 2: SampleSceneからオブジェクトをコピー
        // ──────────────────────────────────────
        string sampleScenePath = "";
        string[] guids = AssetDatabase.FindAssets("SampleScene t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Assets/Scenes"))
            {
                sampleScenePath = path;
                break;
            }
        }

        if (!string.IsNullOrEmpty(sampleScenePath))
        {
            // SampleSceneをAdditiveで開く
            Scene sampleScene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Additive);

            string[] copyNames = {
                "GameManager", "AnomalyController", "AudioManager", "LogManager",
                "LoopController", "LoopTrigger", "ResetPoint", "Player",
                "UI Canvas", "Interactables", "PsychologicalEffects"
            };

            foreach (string name in copyNames)
            {
                // SampleScene内から探す
                GameObject found = null;
                foreach (GameObject go in sampleScene.GetRootGameObjects())
                {
                    if (go.name == name) { found = go; break; }
                    // 子も検索
                    Transform child = go.transform.Find(name);
                    if (child != null) { found = child.gameObject; break; }
                }

                if (found != null)
                {
                    SceneManager.MoveGameObjectToScene(found, active);
                    Debug.Log($"[Setup] 移植: {name}");
                }
                else
                {
                    Debug.LogWarning($"[Setup] 見つからず: {name}");
                }
            }

            // SampleSceneを閉じる
            EditorSceneManager.CloseScene(sampleScene, true);
        }
        else
        {
            Debug.LogWarning("[Setup] SampleScene が見つかりません。オブジェクトのコピーをスキップします。");
        }

        // ──────────────────────────────────────
        // STEP 3: Player位置をSubway内に配置
        // ──────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            // SubwayStationDemo 座標系: 床 Y≈-9.85、島ホーム中央 X≈-34.85、南側 Z≈-88
            player.transform.position = new Vector3(-34.85f, -8.85f, -88f);
            Debug.Log("[Setup] Player位置を設定しました（Subway座標系）");
        }

        GameObject resetPoint = GameObject.Find("ResetPoint");
        if (resetPoint != null)
            resetPoint.transform.position = new Vector3(-34.85f, -8.85f, -88f);

        // ──────────────────────────────────────
        // STEP 4: Global Volumeの重複を削除
        // ──────────────────────────────────────
        var volumes = Object.FindObjectsOfType<Volume>();
        Volume kisaragiVolume = null;
        foreach (var v in volumes)
            if (v.gameObject.name == "Global Volume") { kisaragiVolume = v; break; }

        if (kisaragiVolume != null)
        {
            foreach (var v in volumes)
            {
                if (v != kisaragiVolume)
                    Object.DestroyImmediate(v.gameObject);
            }
        }

        // ──────────────────────────────────────
        // STEP 5: LoopTriggerをSubway奥に移動
        // ──────────────────────────────────────
        // SubwayStationDemo: 橋の北壁付近に配置（Z≈-120、橋床 Y≈-6.35）
        GameObject loopTrigger = GameObject.Find("LoopTrigger");
        if (loopTrigger != null)
            loopTrigger.transform.position = new Vector3(-34.85f, -5.8f, -120f);

        // ──────────────────────────────────────
        // STEP 6: 全参照を再設定
        // ──────────────────────────────────────
        KisaragiRefFixer.FixReferences();

        // ──────────────────────────────────────
        // シーン保存
        // ──────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        EditorUtility.DisplayDialog("完了",
            "地下鉄シーンのセットアップが完了しました！\n\n" +
            "▶ Play で動作確認してください。\n\n" +
            "Playerが地面に埋まる場合は\n" +
            "Player の Y 座標を少し上げてください。", "OK");
    }

    // ──────────────────────────────────────
    // マテリアルURPへ変換
    // ──────────────────────────────────────
    static void ConvertMaterials()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SubwayModelSet/Materials" });
        List<Material> materials = new List<Material>();

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) materials.Add(mat);
        }

        if (materials.Count == 0)
        {
            Debug.LogWarning("[Setup] SubwayModelSet/Materials にマテリアルが見つかりません");
            return;
        }

        // URPシェーダーに差し替え
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[Setup] URP/Lit シェーダーが見つかりません");
            return;
        }

        int converted = 0;
        foreach (Material mat in materials)
        {
            if (mat.shader.name != "Universal Render Pipeline/Lit" &&
                mat.shader.name != "Universal Render Pipeline/Simple Lit")
            {
                // テクスチャを保持して差し替え
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color     = mat.HasProperty("_Color")   ? mat.GetColor("_Color")     : Color.white;

                mat.shader = urpLit;

                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                mat.SetColor("_BaseColor", color);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Setup] {converted} 個のマテリアルをURPに変換しました");
    }
}
