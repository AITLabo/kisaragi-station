using UnityEngine;
using UnityEditor;
using TMPro;

// Kisaragi > Build Prototype Anomalies
// StationPrototype シーン用座標で違和感10個を再配置する
// Platform: Z -22〜+22, X -1.6〜+1.6, 天井 Y=2.8
public class KisaragiPrototypeAnomalyBuilder
{
    [MenuItem("Kisaragi/Build Prototype Anomalies \u2013 \u30d7\u30ed\u30c8\u30bf\u30a4\u30d7\u7528\u9055\u548c\u611f\u914d\u7f6e")]
    public static void BuildPrototypeAnomalies()
    {
        // 既存の KisaragiAnomalies を削除
        var existing = GameObject.Find("KisaragiAnomalies");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[PrototypeAnomalyBuilder] 既存の KisaragiAnomalies を削除しました");
        }

        var root = new GameObject("KisaragiAnomalies");
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer == -1) layer = 0;

        // ────────────────────────────────────────────────
        // 10個の違和感オブジェクト（StationPrototype座標系）
        // Platform: Z -22〜+22, X -1.6〜+1.6, Y 0.25=床面
        // ────────────────────────────────────────────────

        // 1. 駅名標 - station_sign  (線路と逆の右壁に設置・ホーム(-X)向き)
        var sign = CreateBase("駅名標_station_sign", "station_sign",
            new Vector3(4.3f, 2.0f, -10f), new Vector3(0.05f, 0.6f, 1.8f),
            new Color(0.1f, 0.1f, 0.35f), root, layer);
        AddTMP(sign, "━━━━━━", 0.2f, Color.white, new Vector3(-0.04f, 0, 0));
        // TMP を -X（ホーム側）向きに修正（AddTMP は +X 固定のため上書き）
        var signLabel = sign.transform.Find("Label");
        if (signLabel != null) signLabel.localRotation = Quaternion.Euler(0f, -90f, 0f);
        AddHint(sign, "この駅の名前が読めない...");

        // 2. 時計 - clock  (右壁・高め)
        var clock = CreateBase("時計_clock", "clock",
            new Vector3(1.25f, 2.72f, -5f), new Vector3(0.05f, 0.7f, 0.7f),
            new Color(0.88f, 0.86f, 0.80f), root, layer);
        AddTMP(clock, "◉", 0.12f, new Color(0.2f, 0.2f, 0.2f), new Vector3(-0.04f, 0, 0));
        clock.AddComponent<ClockAnomalyEffect>();
        AddHint(clock, "時計の針が逆に動いている...");

        // 3. 時刻表 - timetable  (左壁)
        var tt = CreateBase("時刻表_timetable", "timetable",
            new Vector3(-1.25f, 1.6f, -15f), new Vector3(0.05f, 1.4f, 1.1f),
            new Color(0.93f, 0.91f, 0.86f), root, layer);
        // ラベルはボード（非均等スケール）の子にせず root 直下にワールド座標で配置する。
        // 非均等スケールの子に TMP を置くと sizeDelta が親スケールで歪むため。
        {
            var labelGO = new GameObject("時刻表_Label");
            labelGO.transform.SetParent(root.transform);
            labelGO.transform.position = new Vector3(-1.22f, 2.2f, -15f); // ボード前面・上部
            labelGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // +X向き（ホーム側）
            labelGO.transform.localScale = new Vector3(-1f, 1f, 1f);      // X反転でテキスト正順
            var tmp = labelGO.AddComponent<TMPro.TextMeshPro>();
            tmp.text      = "時刻表";
            tmp.fontSize  = 0.8f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color     = Color.black;
            tmp.rectTransform.sizeDelta = new Vector2(1.0f, 0.4f);
            tmp.enableWordWrapping = false;
        }
        AddHint(tt, "時刻表の欄が全て空白だ...");

        // 4. 放送スピーカー - announcement  (右壁・高め)
        var spk = CreateBase("放送スピーカー_announcement", "announcement",
            new Vector3(1.25f, 2.65f, 0f), new Vector3(0.05f, 0.28f, 0.38f),
            new Color(0.28f, 0.28f, 0.32f), root, layer);
        for (int i = -1; i <= 1; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "SpeakerLine_" + i;
            line.transform.SetParent(spk.transform);
            line.transform.localPosition = new Vector3(-0.04f, i * 0.22f, 0);
            line.transform.localScale = new Vector3(0.2f, 0.05f, 0.8f);
            ApplyMat(line, new Color(0.18f, 0.18f, 0.18f));
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }
        var spkAudio = spk.AddComponent<AudioSource>();
        spkAudio.loop = true; spkAudio.spatialBlend = 1f; spkAudio.maxDistance = 12f;
        AddHint(spk, "放送が聞こえる...でも言葉が理解できない");

        // 5. ベンチ - bench  (①Bench_TrackSide の座面上に鞄を配置)
        // PrototypeBuilder が置いた Bench_TrackSide (X=-0.6, Z=0) に異変を乗せる。
        // ベンチ本体のキューブは生成せず、不可視コライダー + 鞄オブジェクトのみ。
        {
            const float BENCH_TOP_Y = 0.25f + 0.42f; // PLATFORM_H + BENCH_SEAT_H = 0.67
            var bench = new GameObject("ベンチ_bench");
            bench.transform.SetParent(root.transform);
            bench.transform.position = new Vector3(-1.3f, BENCH_TOP_Y, 0f);
            bench.layer = layer;
            SetCorrectObject(bench, "bench", layer);
            var benchCol = bench.AddComponent<BoxCollider>();
            benchCol.size = new Vector3(0.6f, 0.5f, 1.8f); // インタラクション範囲
            CreateChild(bench, "Bag",       new Vector3(0, 0.16f, -0.3f), new Vector3(0.28f, 0.32f, 0.24f), new Color(0.18f, 0.18f, 0.55f));
            CreateChild(bench, "BagHandle", new Vector3(0, 0.33f, -0.3f), new Vector3(0.15f, 0.05f, 0.04f), new Color(0.14f, 0.14f, 0.45f));
            AddHint(bench, "誰かの荷物が残されている...持ち主はどこに？");
        }

        // 6. 改札 - gate  → GateBuilder の KaisatsuBoard にイベントを付与
        {
            // GateRoot 配下の KaisatsuBoard を検索（GateBuilder で命名済み）
            var gateRootGo = GameObject.Find("GateRoot");
            GameObject eventGate = null;
            if (gateRootGo != null)
            {
                foreach (Transform child in gateRootGo.transform)
                {
                    if (child.name == "KaisatsuBoard")
                    {
                        eventGate = child.gameObject;
                        break; // 最初の1枚（Z=-6 付近）にイベントを付与
                    }
                }
            }
            if (eventGate == null)
            {
                // フォールバック: 改札エリア内に standalone Cube
                Debug.LogWarning("[AnomalyBuilder] KaisatsuBoard が見つかりません。Cube で代替します。");
                eventGate = CreateBase("改札_gate", "gate",
                    new Vector3(4.9f, 0.85f, -6f), new Vector3(0.8f, 1.2f, 0.12f),
                    new Color(0.68f, 0.70f, 0.73f), root, layer);
                CreateChild(eventGate, "GateBar", new Vector3(0.45f, 0, -0.08f), new Vector3(0.9f, 0.04f, 0.04f), new Color(0.5f, 0.52f, 0.55f));
            }
            SetCorrectObject(eventGate, "gate", layer);
            AddHint(eventGate, "改札が誰もいないのに全部開いている...");
        }

        // 7. ポスター - poster  (階段内壁・電話ボックス横 Z=17)
        var poster = CreateBase("ポスター_poster", "poster",
            new Vector3(4.4f, 1.6f, 19.5f), new Vector3(0.05f, 1.1f, 0.78f),
            new Color(0.88f, 0.83f, 0.73f), root, layer);
        AddTMP(poster, "観光案内", 0.08f, new Color(0.2f, 0.2f, 0.4f), new Vector3(0.04f, 0.25f, 0));
        CreateChild(poster, "FaceBlank", new Vector3(0.03f, -0.08f, 0), new Vector3(0.02f, 0.48f, 0.38f), new Color(0.88f, 0.83f, 0.73f));
        AddHint(poster, "ポスターの人物の顔が...消えている");

        // 8. 公衆電話 - phone  (右壁) ← phone.fbx に入れ替え
        {
            const string PHONE_FBX = "Assets/Models/phone.fbx";
            var phoneAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PHONE_FBX);
            GameObject phone;
            if (phoneAsset != null)
            {
                phone = Object.Instantiate(phoneAsset);
                phone.name = "公衆電話_phone";
                phone.transform.SetParent(root.transform);
                phone.transform.position = new Vector3(3.9f, 0.25f, 21.5f); // ホーム側・階段内壁(X=4.6)の手前
                phone.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // +Z向き
                phone.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f); // 少し大きく
                phone.layer = layer;
                // FBX 不要コンポーネント除去（Camera・Light）
                foreach (var c in phone.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c);
                foreach (var l in phone.GetComponentsInChildren<Light>(true))  Object.DestroyImmediate(l);
                // インタラクション用 BoxCollider をルートに追加
                var phoneCol = phone.AddComponent<BoxCollider>();
                phoneCol.size   = new Vector3(0.48f, 0.85f, 0.38f); // 元のCubeサイズに合わせる
                phoneCol.center = new Vector3(0f, 0.425f, 0f);
            }
            else
            {
                Debug.LogWarning("[AnomalyBuilder] " + PHONE_FBX + " が見つかりません。Cube で代替します。");
                phone = CreateBase("公衆電話_phone", "phone",
                    new Vector3(3.9f, 0.675f, 21f), new Vector3(0.48f, 0.85f, 0.38f),
                    new Color(0.08f, 0.38f, 0.13f), root, layer);
                CreateChild(phone, "Receiver", new Vector3(-0.05f, 0.28f, -0.22f), new Vector3(0.28f, 0.07f, 0.07f), new Color(0.06f, 0.28f, 0.09f));
            }
            SetCorrectObject(phone, "phone", layer);
            var phoneAudio = phone.AddComponent<AudioSource>();
            phoneAudio.loop = true; phoneAudio.spatialBlend = 1f; phoneAudio.maxDistance = 8f;
            AddHint(phone, "電話が鳴っている...出るべきか？");
        }

        // 9. 線路（欠損）- track  (ホームより外側・線路側)
        var track = new GameObject("線路_track");
        track.transform.SetParent(root.transform);
        track.transform.position = new Vector3(-2.5f, 0.1f, -18f);
        SetCorrectObject(track, "track", layer);
        var trackCol = track.AddComponent<BoxCollider>();
        trackCol.size = new Vector3(1.8f, 0.4f, 18f);
        CreateChild(track, "RailLeft",       new Vector3(-0.5f, 0, 0),   new Vector3(0.07f, 0.12f, 18f),  new Color(0.53f, 0.48f, 0.43f));
        CreateChild(track, "RailRight_Short",new Vector3(0.5f, 0, 4f),   new Vector3(0.07f, 0.12f, 5f),   new Color(0.53f, 0.48f, 0.43f));
        CreateChild(track, "RailRight_End",  new Vector3(0.5f, -0.15f, -2f), new Vector3(0.07f, 0.04f, 0.4f), new Color(0.38f, 0.33f, 0.28f));
        for (int i = -4; i <= 4; i++)
            CreateChild(track, "Sleeper_" + i, new Vector3(0, -0.04f, i * 1.4f), new Vector3(1.6f, 0.07f, 0.18f), new Color(0.33f, 0.26f, 0.20f));
        AddHint(track, "線路が...片側だけ消えている");

        // 10. 消えた蛍光灯 - light  (天井・ホーム端)
        var lightRoot = new GameObject("蛍光灯_light");
        lightRoot.transform.SetParent(root.transform);
        lightRoot.transform.position = new Vector3(1.3f, 2.58f, 20f);
        for (int i = -1; i <= 1; i++)
        {
            if (i == 0) continue;
            var fl = new GameObject("Fluorescent_" + i);
            fl.transform.SetParent(lightRoot.transform);
            fl.transform.localPosition = new Vector3(0, 0, i * 2f);
            var lt = fl.AddComponent<Light>();
            lt.type = LightType.Point; lt.intensity = 1.2f; lt.range = 5f;
            lt.color = new Color(0.93f, 0.93f, 1f);
            CreateChild(fl, "Tube", Vector3.zero, new Vector3(0.05f, 0.05f, 1.4f), new Color(0.93f, 0.93f, 0.88f));
        }
        var deadLight = new GameObject("DeadLight_Center");
        deadLight.transform.SetParent(lightRoot.transform);
        deadLight.transform.localPosition = Vector3.zero;
        CreateChild(deadLight, "Tube_Dead", Vector3.zero, new Vector3(0.05f, 0.05f, 1.4f), new Color(0.28f, 0.28f, 0.28f));
        SetCorrectObject(deadLight, "light", layer);
        var dlCol = deadLight.AddComponent<BoxCollider>();
        dlCol.size = new Vector3(1.5f, 0.8f, 1.5f);
        AddHint(deadLight, "この蛍光灯だけが...消えている");

        // InteractionSystem のレイヤーマスクを更新
        FixInteractionLayer(layer);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[PrototypeAnomalyBuilder] 違和感10個を StationPrototype 座標で配置しました");
        EditorUtility.DisplayDialog("完了",
            "StationPrototype 用の違和感10個を配置しました！\n\n" +
            "配置場所（プレイヤー座標系）:\n" +
            "  1. 駅名標  Z=-10 右壁\n" +
            "  2. 時計    Z=-5  右壁\n" +
            "  3. 時刻表  Z=-15 左壁\n" +
            "  4. 放送    Z=0   右壁上\n" +
            "  5. ベンチ  Z=0   中央①(Bench_TrackSide)\n" +
            "  6. 改札    Z=19  奥\n" +
            "  7. ポスター Z=8  左壁\n" +
            "  8. 電話    Z=12  右壁\n" +
            "  9. 線路    Z=-18 線路側\n" +
            " 10. 蛍光灯  Z=20  天井\n\n" +
            "Ctrl+S で保存 → Play でテスト！",
            "OK");
    }

    // ──────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────

    static GameObject CreateBase(string name, string actionID, Vector3 pos, Vector3 scale, Color color, GameObject parent, int layer)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.transform.SetParent(parent.transform);
        obj.layer = layer;
        ApplyMat(obj, color);
        SetCorrectObject(obj, actionID, layer);
        return obj;
    }

    static void SetCorrectObject(GameObject obj, string actionID, int layer)
    {
        obj.layer = layer;
        var co = obj.GetComponent<CorrectObject>() ?? obj.AddComponent<CorrectObject>();
        var so = new SerializedObject(co);
        so.FindProperty("actionID").stringValue = actionID;
        so.ApplyModifiedProperties();
    }

    static void CreateChild(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        ApplyMat(obj, color);
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }

    static void AddTMP(GameObject parent, string text, float fontSize, Color color, Vector3 localPos)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;

        // rotation(0,90,0) のとき TMP の幅方向=親のZ寸法、高さ方向=親のY寸法
        // sizeDelta を明示設定しないと横に間延びするため必須
        Vector3 ps = parent.transform.localScale;
        tmp.rectTransform.sizeDelta = new Vector2(ps.z * 0.9f, ps.y * 0.9f);
        tmp.enableWordWrapping = text.Contains("\n");

        // ※ Dynamic フォントを tmp.font に直接設定しない
        // Play 時に Dynamic フォントのマテリアルが再生成されて MissingReferenceException になるため
        // 日本語フォントは「Kisaragi > Fix Japanese Font」で TMP Settings Fallback に登録して使用する
    }

    static void AddHint(GameObject obj, string hint)
    {
        // CorrectObject の hintText フィールドにセット（あれば）
        var co = obj.GetComponent<CorrectObject>();
        if (co == null) return;
        var so = new SerializedObject(co);
        var prop = so.FindProperty("hintText");
        if (prop != null) { prop.stringValue = hint; so.ApplyModifiedProperties(); }
    }

    static void ApplyMat(GameObject obj, Color color)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        r.sharedMaterial = mat;
    }

    static void FixInteractionLayer(int layer)
    {
        if (layer == 0) return;
        var iSys = Object.FindObjectOfType<InteractionSystem>();
        if (iSys == null) return;
        var so = new SerializedObject(iSys);
        var prop = so.FindProperty("interactableLayer");
        if (prop != null)
        {
            prop.intValue = 1 << layer;
            so.ApplyModifiedProperties();
            Debug.Log($"[PrototypeAnomalyBuilder] InteractionSystem.interactableLayer を Layer {layer} に設定しました");
        }
    }
}
