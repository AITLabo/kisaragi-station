using UnityEngine;
using UnityEditor;

// Unity メニュー「Kisaragi > Build Corridor」で駅通路を自動生成する
public class KisaragiCorridorBuilder
{
    [MenuItem("Kisaragi/Build Corridor (通路自動生成)")]
    public static void BuildCorridor()
    {
        // 親オブジェクト
        GameObject env = GetOrCreate("Environment");

        // ──────────────────────────────────────
        // 通路サイズ定義
        // 幅4m / 高さ3m / 長さ40m（ループ前提）
        // ──────────────────────────────────────
        float width  = 4f;
        float height = 3f;
        float length = 40f;

        // 床
        CreatePanel("Floor", env,
            new Vector3(0, 0, length / 2f),
            new Vector3(width, 0.1f, length),
            new Color(0.15f, 0.15f, 0.15f)); // 暗いコンクリート色

        // 天井
        CreatePanel("Ceiling", env,
            new Vector3(0, height, length / 2f),
            new Vector3(width, 0.1f, length),
            new Color(0.12f, 0.12f, 0.12f));

        // 左壁
        CreatePanel("WallLeft", env,
            new Vector3(-width / 2f, height / 2f, length / 2f),
            new Vector3(0.1f, height, length),
            new Color(0.18f, 0.18f, 0.18f));

        // 右壁
        CreatePanel("WallRight", env,
            new Vector3(width / 2f, height / 2f, length / 2f),
            new Vector3(0.1f, height, length),
            new Color(0.18f, 0.18f, 0.18f));

        // 奥壁（LoopTriggerの手前）
        CreatePanel("WallEnd", env,
            new Vector3(0, height / 2f, length),
            new Vector3(width, height, 0.1f),
            new Color(0.1f, 0.1f, 0.1f));

        // ──────────────────────────────────────
        // 照明（蛍光灯風スポット × 5本）
        // ──────────────────────────────────────
        int lightCount = 5;
        for (int i = 0; i < lightCount; i++)
        {
            float z = (length / (lightCount + 1)) * (i + 1);
            GameObject lightGO = new GameObject($"CeilingLight_{i}");
            lightGO.transform.SetParent(env.transform);
            lightGO.transform.position = new Vector3(0, height - 0.1f, z);

            Light lt = lightGO.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.intensity = 1.2f;
            lt.range     = 8f;
            lt.color     = new Color(0.9f, 0.95f, 1.0f); // 冷たい白色
        }

        // ──────────────────────────────────────
        // 駅名看板（Station Sign）
        // ──────────────────────────────────────
        GameObject signPost = new GameObject("StationSign");
        signPost.transform.SetParent(env.transform);
        signPost.transform.position = new Vector3(0, height - 0.4f, 8f);

        GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signBoard.name = "SignBoard";
        signBoard.transform.SetParent(signPost.transform);
        signBoard.transform.localPosition = Vector3.zero;
        signBoard.transform.localScale = new Vector3(2f, 0.4f, 0.05f);
        SetColor(signBoard, new Color(0.05f, 0.05f, 0.3f)); // 濃紺

        // 3Dテキストの代わりにプレースホルダーCube
        GameObject signText = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signText.name = "StationNamePlaceholder";
        signText.transform.SetParent(signPost.transform);
        signText.transform.localPosition = new Vector3(0, 0, -0.04f);
        signText.transform.localScale = new Vector3(1.8f, 0.25f, 0.01f);
        SetColor(signText, Color.white);
        // ※ TextMeshPro 3Dテキストに後で差し替える

        // ──────────────────────────────────────
        // 時計
        // ──────────────────────────────────────
        GameObject clock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        clock.name = "Clock";
        clock.transform.SetParent(env.transform);
        clock.transform.position = new Vector3(1.5f, height - 0.5f, 20f);
        clock.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
        clock.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        SetColor(clock, new Color(0.9f, 0.9f, 0.85f));

        // ──────────────────────────────────────
        // ポスター（壁に貼られた掲示物）
        // ──────────────────────────────────────
        for (int i = 0; i < 3; i++)
        {
            float z = 5f + i * 10f;
            GameObject poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            poster.name = $"Poster_{i}";
            poster.transform.SetParent(env.transform);
            poster.transform.position = new Vector3(-width / 2f + 0.06f, 1.5f, z);
            poster.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            poster.transform.localScale = new Vector3(0.8f, 1.1f, 1f);
            SetColor(poster, new Color(0.85f, 0.82f, 0.75f));
        }

        // ──────────────────────────────────────
        // LoopTrigger の位置を通路終端に合わせる
        // ──────────────────────────────────────
        GameObject loopTrigger = GameObject.Find("LoopTrigger");
        if (loopTrigger != null)
            loopTrigger.transform.position = new Vector3(0, 1.5f, length - 1f);

        // ──────────────────────────────────────
        // Player / ResetPoint の位置調整
        // ──────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(0, 1f, 1f);

        GameObject resetPoint = GameObject.Find("ResetPoint");
        if (resetPoint != null)
            resetPoint.transform.position = new Vector3(0, 1f, 1f);

        // ──────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("=== Kisaragi Corridor Build 完了 ===");
        EditorUtility.DisplayDialog(
            "Kisaragi Corridor Builder",
            "通路の自動生成が完了しました！\n\nCtrl+S でシーンを保存してください。",
            "OK"
        );
    }

    // ──────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────
    static GameObject GetOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    static void CreatePanel(string name, GameObject parent,
        Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = position;
        go.transform.localScale = scale;
        SetColor(go, color);
    }

    static void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        r.material = mat;
    }
}
