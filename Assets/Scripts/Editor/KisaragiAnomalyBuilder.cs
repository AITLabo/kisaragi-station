using UnityEngine;
using UnityEditor;
using TMPro;

// Kisaragi > Build Anomaly Objects
// きさらぎ駅の「10個の違和感」インタラクトオブジェクトを自動配置する
// SubwayStationDemo シーンで実行すること
// GameManager の correctSequence と一致するアクションIDを持つ
public class KisaragiAnomalyBuilder
{
    // SubwayStationDemoシーンのトンネル内座標（SM_Tunnel_10m付近）
    // X:-48, Y:-4.5, Z:-40 付近を中心にオブジェクトを配置
    static readonly Vector3 BASE_POS = new Vector3(-48f, -4.5f, -60f);

    [MenuItem("Kisaragi/Build Anomaly Objects (違和感10個を配置)")]
    public static void BuildAnomalyObjects()
    {
        // 既存の違和感オブジェクトを削除
        GameObject existingRoot = GameObject.Find("KisaragiAnomalies");
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
            Debug.Log("[AnomalyBuilder] 既存の KisaragiAnomalies を削除しました");
        }

        // 親オブジェクト
        GameObject root = new GameObject("KisaragiAnomalies");

        // 10個の違和感を順番に配置
        // GameManager.correctSequence と同じ順序
        CreateStationSign(root);       // 1. station_sign - 駅名が「きさらぎ」ではない
        CreateClock(root);             // 2. clock         - 時計が逆回り
        CreateTimetable(root);         // 3. timetable     - 時刻表が白紙
        CreateAnnouncement(root);      // 4. announcement  - 放送が日本語ではない
        CreateBench(root);             // 5. bench         - ベンチに荷物だけある
        CreateGate(root);              // 6. gate          - 改札が無人で開いている
        CreatePoster(root);            // 7. poster        - ポスターの人物が消えている
        CreatePhone(root);             // 8. phone         - 公衆電話が鳴っている
        CreateTrack(root);             // 9. track         - 線路が片側だけない
        CreateLight(root);             // 10. light        - 蛍光灯が1本だけ消えている

        // レイヤー設定
        SetInteractableLayer(root);

        // Sceneを保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AnomalyBuilder] 違和感オブジェクト10個を配置しました");
        EditorUtility.DisplayDialog("完了",
            "きさらぎ駅の違和感オブジェクト10個を配置しました！\n\n" +
            "Hierarchy の「KisaragiAnomalies」内に配置されています。\n\n" +
            "次のステップ:\n" +
            "1. Kisaragi > Fix Player Position で位置確認\n" +
            "2. Play して各オブジェクトをクリックしてテスト",
            "OK");
    }

    // ──────────────────────────────────────
    // 1. 駅名標 - station_sign
    // 違和感: 駅名が「きさらぎ」ではなく「━━━━━」になっている
    // ──────────────────────────────────────
    static void CreateStationSign(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("駅名標_station_sign", "station_sign",
            BASE_POS + new Vector3(0, 2f, 5f),
            new Vector3(2f, 0.8f, 0.1f),
            new Color(0.1f, 0.1f, 0.4f),
            parent);

        // TextMeshPro 3Dテキストで駅名を表示
        GameObject textObj = new GameObject("StationNameText");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.06f);
        textObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "━━━━━━";  // 違和感：駅名が読めない
        tmp.fontSize = 0.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(1.8f, 0.4f);
        tmp.enableWordWrapping = false;

        // 正常な駅名の副看板（日本語）
        GameObject subTextObj = new GameObject("SubStationText");
        subTextObj.transform.SetParent(obj.transform);
        subTextObj.transform.localPosition = new Vector3(0, -0.25f, -0.06f);
        subTextObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var subTmp = subTextObj.AddComponent<TextMeshPro>();
        subTmp.text = "Kisaragi";
        subTmp.fontSize = 0.1f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.9f, 0.9f, 0.7f);
        subTmp.rectTransform.sizeDelta = new Vector2(1.8f, 0.2f);
        subTmp.enableWordWrapping = false;

        AddHintText(obj, "この駅の名前が読めない...");
    }

    // ──────────────────────────────────────
    // 2. 時計 - clock
    // 違和感: 針が逆方向に動いている
    // ──────────────────────────────────────
    static void CreateClock(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("時計_clock", "clock",
            BASE_POS + new Vector3(3f, 1.5f, -5f),
            new Vector3(0.8f, 0.8f, 0.1f),
            new Color(0.9f, 0.88f, 0.82f),
            parent);

        // 時計の文字盤
        GameObject faceObj = new GameObject("ClockFace");
        faceObj.transform.SetParent(obj.transform);
        faceObj.transform.localPosition = new Vector3(0, 0, -0.06f);
        faceObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = faceObj.AddComponent<TextMeshPro>();
        tmp.text = "◉"; // 時計のシンボル
        tmp.fontSize = 0.12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.2f, 0.2f);
        tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.7f);
        tmp.enableWordWrapping = false;

        // 逆回転コンポーネントを追加
        obj.AddComponent<ClockAnomalyEffect>();

        AddHintText(obj, "時計の針が逆に動いている...");
    }

    // ──────────────────────────────────────
    // 3. 時刻表 - timetable
    // 違和感: 全ての時刻が空白になっている
    // ──────────────────────────────────────
    static void CreateTimetable(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("時刻表_timetable", "timetable",
            BASE_POS + new Vector3(-3f, 1.5f, -5f),
            new Vector3(1.2f, 1.6f, 0.05f),
            new Color(0.95f, 0.93f, 0.88f),
            parent);

        GameObject textObj = new GameObject("TimetableText");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.04f);
        textObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text =
            "<size=0.06><b>きさらぎ駅　時刻表</b></size>\n" +
            "──────────────\n" +
            "　　　　　　　　　\n" +
            "　　　　　　　　　\n" +
            "　　　　　　　　　\n" +
            "　　　　　　　　　\n" +
            "──────────────\n" +
            "<size=0.045>最終電車の時刻は不明です</size>";
        tmp.fontSize = 0.04f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.1f, 0.1f, 0.1f);
        tmp.rectTransform.sizeDelta = new Vector2(1.1f, 1.4f);
        tmp.enableWordWrapping = true;

        AddHintText(obj, "時刻表の欄が全て空白だ...");
    }

    // ──────────────────────────────────────
    // 4. 放送スピーカー - announcement
    // 違和感: 日本語ではない放送が流れている
    // ──────────────────────────────────────
    static void CreateAnnouncement(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("放送スピーカー_announcement", "announcement",
            BASE_POS + new Vector3(0, 2.5f, -10f),
            new Vector3(0.4f, 0.3f, 0.2f),
            new Color(0.3f, 0.3f, 0.35f),
            parent);

        // スピーカーのグリル（縦線）
        for (int i = -2; i <= 2; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"SpeakerLine_{i}";
            line.transform.SetParent(obj.transform);
            line.transform.localPosition = new Vector3(i * 0.06f, 0, -0.11f);
            line.transform.localScale = new Vector3(0.02f, 0.8f, 0.1f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.2f, 0.2f);
            line.GetComponent<Renderer>().material = mat;
        }

        // 音源
        var audioSource = obj.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 15f;

        AddHintText(obj, "放送が聞こえる...でも言葉が理解できない");
    }

    // ──────────────────────────────────────
    // 5. ベンチ - bench
    // 違和感: ベンチに荷物だけが残されている（人がいない）
    // ──────────────────────────────────────
    static void CreateBench(GameObject parent)
    {
        // ベンチ本体
        GameObject bench = CreateInteractableBase("ベンチ_bench", "bench",
            BASE_POS + new Vector3(2f, -3.5f, 0f),
            new Vector3(2f, 0.1f, 0.6f),
            new Color(0.55f, 0.4f, 0.25f),
            parent);

        // ベンチの脚
        CreateChildBox(bench, "BenchLeg_L", new Vector3(-0.7f, -0.3f, 0), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.4f, 0.3f, 0.2f));
        CreateChildBox(bench, "BenchLeg_R", new Vector3(0.7f, -0.3f, 0), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.4f, 0.3f, 0.2f));

        // 放置された荷物（鞄）
        CreateChildBox(bench, "Bag", new Vector3(-0.3f, 0.2f, 0), new Vector3(0.3f, 0.35f, 0.25f), new Color(0.2f, 0.2f, 0.6f));
        // 荷物の取っ手
        CreateChildBox(bench, "BagHandle", new Vector3(-0.3f, 0.4f, 0), new Vector3(0.15f, 0.05f, 0.04f), new Color(0.15f, 0.15f, 0.5f));

        AddHintText(bench, "誰かの荷物が残されている...持ち主はどこに？");
    }

    // ──────────────────────────────────────
    // 6. 改札 - gate
    // 違和感: 改札が無人で全開きになっている
    // ──────────────────────────────────────
    static void CreateGate(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("改札_gate", "gate",
            BASE_POS + new Vector3(0, -3f, 15f),
            new Vector3(0.8f, 1.2f, 0.15f),
            new Color(0.7f, 0.72f, 0.75f),
            parent);

        // 改札バー（全開き）
        CreateChildBox(obj, "GateBar", new Vector3(0.5f, 0, -0.1f), new Vector3(1f, 0.05f, 0.05f), new Color(0.5f, 0.52f, 0.55f));

        // 表示パネル
        GameObject panel = new GameObject("GatePanel");
        panel.transform.SetParent(obj.transform);
        panel.transform.localPosition = new Vector3(0, 0.4f, -0.1f);
        panel.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = panel.AddComponent<TextMeshPro>();
        tmp.text = "<color=green>▶</color>";
        tmp.fontSize = 0.15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.4f);
        tmp.enableWordWrapping = false;

        AddHintText(obj, "改札が誰もいないのに全部開いている...");
    }

    // ──────────────────────────────────────
    // 7. ポスター - poster
    // 違和感: ポスターの人物の顔が消えている
    // ──────────────────────────────────────
    static void CreatePoster(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("ポスター_poster", "poster",
            BASE_POS + new Vector3(-3f, 0f, 10f),
            new Vector3(0.8f, 1.1f, 0.02f),
            new Color(0.9f, 0.85f, 0.75f),
            parent);

        // ポスターのテキスト
        GameObject textObj = new GameObject("PosterText");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = new Vector3(0, 0.3f, -0.02f);
        textObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "観光案内";
        tmp.fontSize = 0.08f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.2f, 0.4f);
        tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.25f);
        tmp.enableWordWrapping = false;

        // 人物の顔が消えている（空白エリア）
        CreateChildBox(obj, "FaceBlank",
            new Vector3(0, -0.1f, -0.02f),
            new Vector3(0.4f, 0.5f, 0.01f),
            new Color(0.9f, 0.85f, 0.75f));

        // 人物のシルエット（輪郭だけ）
        GameObject silhouetteObj = new GameObject("Silhouette");
        silhouetteObj.transform.SetParent(obj.transform);
        silhouetteObj.transform.localPosition = new Vector3(0, -0.1f, -0.03f);
        silhouetteObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var silTmp = silhouetteObj.AddComponent<TextMeshPro>();
        silTmp.text = "？";
        silTmp.fontSize = 0.2f;
        silTmp.alignment = TextAlignmentOptions.Center;
        silTmp.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        silTmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.6f);
        silTmp.enableWordWrapping = false;

        AddHintText(obj, "ポスターの人物の顔が...消えている");
    }

    // ──────────────────────────────────────
    // 8. 公衆電話 - phone
    // 違和感: 誰も触っていないのに鳴り続けている
    // ──────────────────────────────────────
    static void CreatePhone(GameObject parent)
    {
        GameObject obj = CreateInteractableBase("公衆電話_phone", "phone",
            BASE_POS + new Vector3(3f, -3f, 10f),
            new Vector3(0.5f, 0.9f, 0.4f),
            new Color(0.1f, 0.4f, 0.15f),
            parent);

        // 受話器
        CreateChildBox(obj, "Receiver", new Vector3(0, 0.3f, -0.25f), new Vector3(0.3f, 0.08f, 0.08f), new Color(0.08f, 0.3f, 0.1f));

        // ダイヤルパネル
        GameObject dialObj = new GameObject("DialText");
        dialObj.transform.SetParent(obj.transform);
        dialObj.transform.localPosition = new Vector3(0, 0, -0.22f);
        dialObj.transform.localEulerAngles = new Vector3(0, 180f, 0);
        var tmp = dialObj.AddComponent<TextMeshPro>();
        tmp.text = "📞";
        tmp.fontSize = 0.12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.4f);
        tmp.enableWordWrapping = false;

        // 鳴り続ける音源
        var audioSource = obj.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 10f;

        AddHintText(obj, "電話が鳴っている...出るべきか？");
    }

    // ──────────────────────────────────────
    // 9. 線路 - track
    // 違和感: 片側のレールだけがない
    // ──────────────────────────────────────
    static void CreateTrack(GameObject parent)
    {
        GameObject obj = new GameObject("線路_track");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = BASE_POS + new Vector3(0, -5f, -20f);

        // 左レール（正常）
        CreateChildBox(obj, "RailLeft", new Vector3(-0.7f, 0, 0), new Vector3(0.1f, 0.1f, 20f), new Color(0.55f, 0.5f, 0.45f));

        // 右レール（欠損 - 短い）
        CreateChildBox(obj, "RailRight_Short", new Vector3(0.7f, 0, 5f), new Vector3(0.1f, 0.1f, 5f), new Color(0.55f, 0.5f, 0.45f));
        // 右レールの残骸
        CreateChildBox(obj, "RailRight_End", new Vector3(0.7f, -0.2f, -5f), new Vector3(0.1f, 0.05f, 0.5f), new Color(0.4f, 0.35f, 0.3f));

        // 枕木（いくつか）
        for (int i = -4; i <= 4; i++)
        {
            CreateChildBox(obj, $"Sleeper_{i}", new Vector3(0, -0.05f, i * 1.5f), new Vector3(1.8f, 0.08f, 0.2f), new Color(0.35f, 0.28f, 0.22f));
        }

        // CorrectObject を追加
        var correctObj = obj.AddComponent<CorrectObject>();
        SerializedObject so = new SerializedObject(correctObj);
        so.FindProperty("actionID").stringValue = "track";
        so.ApplyModifiedProperties();

        // Collider を追加
        var col = obj.AddComponent<BoxCollider>();
        col.size = new Vector3(2f, 0.5f, 20f);

        AddHintText(obj, "線路が...片側だけ消えている");
    }

    // ──────────────────────────────────────
    // 10. 蛍光灯 - light
    // 違和感: 1本だけが消えていて、そこだけ暗い
    // ──────────────────────────────────────
    static void CreateLight(GameObject parent)
    {
        GameObject obj = new GameObject("蛍光灯_light");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = BASE_POS + new Vector3(0, 1.5f, -30f);

        // 通常の蛍光灯（複数）
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue; // 中央だけスキップ（消えている）

            GameObject lightObj = new GameObject($"Light_{i}");
            lightObj.transform.SetParent(obj.transform);
            lightObj.transform.localPosition = new Vector3(i * 2f, 0, 0);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 1.2f;
            light.range = 5f;
            light.color = new Color(0.95f, 0.95f, 1.0f);

            CreateChildBox(lightObj, "Tube", Vector3.zero, new Vector3(1.5f, 0.05f, 0.1f), new Color(0.95f, 0.95f, 0.9f));
        }

        // 消えている蛍光灯（中央）
        GameObject deadLight = new GameObject("DeadLight_Center");
        deadLight.transform.SetParent(obj.transform);
        deadLight.transform.localPosition = Vector3.zero;
        CreateChildBox(deadLight, "Tube_Dead", Vector3.zero, new Vector3(1.5f, 0.05f, 0.1f), new Color(0.3f, 0.3f, 0.3f));

        // CorrectObject を追加（消えている蛍光灯に近づくと反応）
        var correctObj = deadLight.AddComponent<CorrectObject>();
        SerializedObject so = new SerializedObject(correctObj);
        so.FindProperty("actionID").stringValue = "light";
        so.ApplyModifiedProperties();

        var col = deadLight.AddComponent<BoxCollider>();
        col.size = new Vector3(2f, 1f, 1f);

        AddHintText(deadLight, "この蛍光灯だけが...消えている");
    }

    // ──────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────

    static GameObject CreateInteractableBase(string name, string actionID, Vector3 pos, Vector3 scale, Color color, GameObject parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.transform.SetParent(parent.transform);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        obj.GetComponent<Renderer>().material = mat;

        var correctObj = obj.AddComponent<CorrectObject>();
        SerializedObject so = new SerializedObject(correctObj);
        so.FindProperty("actionID").stringValue = actionID;
        so.ApplyModifiedProperties();

        return obj;
    }

    static void CreateChildBox(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        obj.GetComponent<Renderer>().material = mat;
        // 子オブジェクトのコライダーを無効化（親のみで判定）
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }

    static void AddHintText(GameObject parent, string hintMessage)
    {
        // ヒントテキスト（インタラクト時に GameManager → InteractionSystem が表示する）
        // CorrectObject の名前をヒントとして使う（将来の拡張用）
        parent.name = $"{parent.name} [{hintMessage}]";
        parent.name = parent.name.Split('[')[0].Trim(); // 名前は元に戻す
    }

    static void SetInteractableLayer(GameObject root)
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer == -1)
        {
            Debug.LogWarning("[AnomalyBuilder] 'Interactable' レイヤーが存在しません。先に Kisaragi > Build Interactables を実行してください。");
            return;
        }

        SetLayerRecursive(root, layer);
        Debug.Log($"[AnomalyBuilder] Interactable レイヤーを設定しました（Layer: {layer}）");
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        // CorrectObject が付いているもののみレイヤーを設定
        if (obj.GetComponent<CorrectObject>() != null)
            obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}

// 時計の逆回転エフェクト（ランタイム用）
public class ClockAnomalyEffect : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = -30f; // 逆方向
    private Transform needle;

    private void Start()
    {
        // 針を作成
        GameObject needleObj = new GameObject("ClockNeedle");
        needleObj.transform.SetParent(transform);
        needleObj.transform.localPosition = new Vector3(0, 0.15f, -0.08f);
        needleObj.transform.localEulerAngles = Vector3.zero;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(needleObj.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.04f, 0.3f, 0.02f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.black;
        cube.GetComponent<Renderer>().material = mat;
        Destroy(cube.GetComponent<Collider>());

        needle = needleObj.transform;
    }

    private void Update()
    {
        if (needle != null)
            needle.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
