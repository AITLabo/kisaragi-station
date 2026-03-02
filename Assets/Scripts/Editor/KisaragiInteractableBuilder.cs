using UnityEngine;
using UnityEditor;

// Unity メニュー「Kisaragi > Build Interactables」で10個のインタラクション対象を自動生成
public class KisaragiInteractableBuilder
{
    [MenuItem("Kisaragi/Build Interactables (インタラクション対象自動生成)")]
    public static void BuildInteractables()
    {
        GameObject root = GetOrCreate("Interactables");

        // ──────────────────────────────────────
        // 通路を塞がない配置ルール
        // ・壁沿いに小さく置く（通路中央を塞がない）
        // ・床には埋めない（y位置を適切に）
        // ・サイズは小さめ（調べる対象として自然なサイズ）
        // ──────────────────────────────────────
        var items = new (string id, string label, Vector3 pos, Vector3 scale, Color color)[]
        {
            // 1. 改札 → 右壁沿いに小さく（通路をふさがない）
            ("gate",         "改札",     new Vector3( 1.5f, 0.8f,  4f),  new Vector3(0.3f, 1.6f, 1.0f), new Color(0.3f, 0.3f, 0.6f)),
            // 2. 階段 → 左壁沿い、床面
            ("stairs",       "階段",     new Vector3(-1.5f, 0.3f,  8f),  new Vector3(1.0f, 0.5f, 0.8f), new Color(0.5f, 0.4f, 0.3f)),
            // 3. ホーム標識 → 右壁に小さく
            ("platform",     "ホーム",   new Vector3( 1.7f, 1.2f, 12f),  new Vector3(0.1f, 0.6f, 0.6f), new Color(0.4f, 0.6f, 0.4f)),
            // 4. スピーカー → 天井近く右壁
            ("announcement", "放送",     new Vector3( 1.7f, 2.3f, 16f),  new Vector3(0.2f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 0.2f)),
            // 5. 電車の窓 → 左壁に埋め込み（覗き込む感じ）
            ("train",        "電車",     new Vector3(-1.7f, 1.2f, 20f),  new Vector3(0.1f, 0.8f, 1.2f), new Color(0.1f, 0.2f, 0.5f)),
            // 6. 駅名標 → 天井から吊り下げ（通路中央上部）
            ("station_sign", "駅名標",   new Vector3( 0f,   2.6f, 23f),  new Vector3(1.5f, 0.3f, 0.05f),new Color(0.05f,0.05f,0.35f)),
            // 7. 時計 → 右壁上部
            ("clock",        "時計",     new Vector3( 1.7f, 2.3f, 27f),  new Vector3(0.05f,0.4f, 0.4f), new Color(0.9f, 0.9f, 0.8f)),
            // 8. トンネル入口 → 奥壁に薄く（くぐれる）
            ("tunnel",       "トンネル", new Vector3( 0f,   2.9f, 31f),  new Vector3(2.0f, 0.2f, 0.1f), new Color(0.05f,0.05f,0.05f)),
            // 9. 暗闇 → 左壁沿い小さく
            ("darkness",     "暗闇",     new Vector3(-1.7f, 1.0f, 35f),  new Vector3(0.1f, 0.8f, 0.8f), new Color(0.02f,0.02f,0.02f)),
            // 10. 光 → 通路奥の天井（進行方向の目印）
            ("light",        "光",       new Vector3( 0f,   2.5f, 38f),  new Vector3(0.8f, 0.8f, 0.1f), new Color(1.0f, 0.98f,0.8f)),
        };

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer == -1)
        {
            Debug.LogWarning("'Interactable' レイヤーが未設定です。Default レイヤーで生成します。");
            interactableLayer = 0;
        }

        foreach (var item in items)
        {
            Transform existing = root.transform.Find(item.label);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = item.label;
            go.transform.SetParent(root.transform);
            go.transform.position = item.pos;
            go.transform.localScale = item.scale;
            go.layer = interactableLayer;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = item.color;
                if (item.id == "light")
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.7f) * 3f);
                }
                r.material = mat;
            }

            CorrectObject co = go.AddComponent<CorrectObject>();
            SerializedObject so = new SerializedObject(co);
            so.FindProperty("actionID").stringValue = item.id;
            so.ApplyModifiedProperties();
        }

        // InteractionSystem の layerMask 更新
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            InteractionSystem iSys = player.GetComponent<InteractionSystem>();
            if (iSys != null && interactableLayer != 0)
            {
                SerializedObject so = new SerializedObject(iSys);
                so.FindProperty("interactableLayer").intValue = 1 << interactableLayer;
                so.ApplyModifiedProperties();
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("=== Kisaragi Interactables Build 完了（通路ふさがない配置）===");
        EditorUtility.DisplayDialog("完了",
            "10個のインタラクション対象を再配置しました！\n\n通路の中央は空いています。\n\nCtrl+S で保存してください。", "OK");
    }

    static GameObject GetOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }
}
