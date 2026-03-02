using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;

// Unity メニュー「Kisaragi > Cleanup Scene」
// 1. 重複オブジェクトを削除
// 2. AudioListenerの重複を削除
// 3. マテリアルをURPに強制変換
public class KisaragiCleanup
{
    [MenuItem("Kisaragi/Cleanup Scene (重複削除・マテリアル修正)")]
    public static void CleanupScene()
    {
        // ──────────────────────────────────────
        // STEP 1: 重複オブジェクトを削除
        // 同名オブジェクトが複数ある場合、最初の1つだけ残す
        // ──────────────────────────────────────
        string[] uniqueNames = {
            "GameManager", "AnomalyController", "AudioManager", "LogManager",
            "LoopController", "LoopTrigger", "ResetPoint", "Player",
            "UI Canvas", "Interactables", "PsychologicalEffects",
            "Global Volume", "Main Camera"
        };

        int removedCount = 0;
        foreach (string name in uniqueNames)
        {
            GameObject[] all = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> matches = new List<GameObject>();
            foreach (GameObject go in all)
                if (go.name == name && go.transform.parent == null)
                    matches.Add(go);

            if (matches.Count > 1)
            {
                // 最初の1つ以外を削除
                for (int i = 1; i < matches.Count; i++)
                {
                    Debug.Log($"[Cleanup] 重複削除: {name} ({i+1}個目)");
                    Object.DestroyImmediate(matches[i]);
                    removedCount++;
                }
            }
        }
        Debug.Log($"[Cleanup] 重複オブジェクト {removedCount} 個を削除しました");

        // ──────────────────────────────────────
        // STEP 2: AudioListenerの重複を削除
        // ──────────────────────────────────────
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            // Main Camera の AudioListener だけ残す
            AudioListener keep = null;
            foreach (var l in listeners)
                if (l.gameObject.name == "Main Camera") { keep = l; break; }
            if (keep == null) keep = listeners[0];

            foreach (var l in listeners)
                if (l != keep)
                {
                    Object.DestroyImmediate(l);
                    Debug.Log("[Cleanup] 重複 AudioListener を削除しました");
                }
        }

        // ──────────────────────────────────────
        // STEP 3: マテリアルをURPに強制変換
        // ──────────────────────────────────────
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[Cleanup] URP/Lit シェーダーが見つかりません");
        }
        else
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SubwayModelSet" });
            int converted = 0;
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                if (mat.shader == urpLit) continue;

                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLit;
                if (mainTex  != null) mat.SetTexture("_BaseMap",   mainTex);
                if (normalMap != null) mat.SetTexture("_BumpMap",   normalMap);
                mat.SetColor("_BaseColor", color);

                EditorUtility.SetDirty(mat);
                converted++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cleanup] {converted} 個のマテリアルをURPに変換しました");
        }

        // ──────────────────────────────────────
        // STEP 4: 全参照を再設定
        // ──────────────────────────────────────
        KisaragiRefFixer.FixReferences();

        // ──────────────────────────────────────
        // シーン保存
        // ──────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("=== Cleanup 完了 ===");
        EditorUtility.DisplayDialog("完了",
            "クリーンアップが完了しました！\n\n" +
            "▶ Play で動作確認してください。", "OK");
    }
}
