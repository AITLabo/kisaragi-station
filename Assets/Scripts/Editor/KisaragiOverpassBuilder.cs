using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Unity メニュー「Kisaragi > Build Overpass（陸橋＋Bホーム構築）」
// StationPrototype シーンに陸橋構造とBホームを追加する
// ─────────────────────────────────────────────────────────
// レイアウト概要（修正版）:
//   Platform A : X = -2.25 ~ +2.25  幅4.5m（PrototypeBuilderと一致）
//   軌道帯    : X = -2.25 ~ -6.25   幅4.0m
//   Platform B : X = -6.25 ~ -10.75 幅4.5m
//   陸橋      : 軌道帯上空 (Y=3.5m) を横断 Z=18~28
//   階段      : A側(X≈+3.35)・B側(X≈-9.60) 各10段 幅2.2m
// ─────────────────────────────────────────────────────────
public class KisaragiOverpassBuilder
{
    // PrototypeBuilder と一致させる（不一致が全バグの原因だった）
    const float PLATFORM_H   = 0.25f;
    const float PLATFORM_W   = 9.0f;   // PrototypeBuilderと統一（旧7.0→9.0）
    const float PLATFORM_L   = 48f;
    const float CEILING_H    = 4.5f;   // 階段・陸橋クリアランス確保

    // 陸橋パラメータ
    const float BRIDGE_FLOOR_Y   = 3.5f;
    const float BRIDGE_THICKNESS = 0.15f;
    const float BRIDGE_WIDTH     = 3.0f;    // 渡り廊下の幅
    const float BRIDGE_Z_START   = 18f;
    const float BRIDGE_Z_END     = 28f;
    const float RAILING_H        = 1.0f;
    const float RAILING_T        = 0.05f;

    // Platform B
    // PLATFORM_W=9.0, TRACK_ZONE_W=5.0
    // PlatformA: X=-4.5~+4.5  軌道帯: X=-4.5~-9.5  PlatformB: X=-9.5~-18.5
    const float TRACK_ZONE_W    = 5.0f;    // 旧4.0→5.0（線路幅拡張）
    const float PLATFORM_B_X    = -(PLATFORM_W * 0.5f + TRACK_ZONE_W + PLATFORM_W * 0.5f);
    //                           = -(4.5 + 5.0 + 4.5) = -14.0
    const float PLATFORM_B_EDGE = -(PLATFORM_W * 0.5f + TRACK_ZONE_W);  // = -9.5

    // 陸橋のX範囲
    const float BRIDGE_X_A      = PLATFORM_W * 0.5f;                    // = +4.5（Aホーム縁）
    const float BRIDGE_X_B      = -(PLATFORM_W * 0.5f + TRACK_ZONE_W);  // = -9.5（Bホーム縁）
    const float BRIDGE_X_CENTER = (BRIDGE_X_A + BRIDGE_X_B) * 0.5f;    // = -2.5
    const float BRIDGE_X_SPAN   = BRIDGE_X_A - BRIDGE_X_B;             // = 14.0m

    // 階段
    const float STAIR_HEIGHT    = BRIDGE_FLOOR_Y - PLATFORM_H; // 3.25m
    const int   STAIR_STEPS     = 10;
    const float STAIR_STEP_H    = STAIR_HEIGHT / STAIR_STEPS;  // 0.325m/段
    const float STAIR_STEP_D    = 0.32f;
    const float STAIR_WIDTH     = 3.0f;   // 旧2.2→3.0（広い階段）

    [MenuItem("Kisaragi/Build Overpass（陸橋＋Bホーム構築）")]
    public static void BuildOverpass()
    {
        if (GameObject.Find("OverpassRoot") != null)
        {
            if (!EditorUtility.DisplayDialog("確認",
                "OverpassRoot が既に存在します。再構築しますか？",
                "再構築", "キャンセル"))
                return;
            Object.DestroyImmediate(GameObject.Find("OverpassRoot"));
        }

        var root = new GameObject("OverpassRoot");
        root.isStatic = true;

        // ── 0. Platform A 拡張（階段アクセス用） ────────
        BuildPlatformA_Extension(root);

        // ── 1. Platform B ──────────────────────────────
        BuildPlatformB(root);

        // ── 1b. Platform B 蛍光灯 5 本（うち1本チカチカ）──
        var lightGroupB = new GameObject("LightingGroup_B");
        lightGroupB.transform.SetParent(root.transform);
        int brokenIndexB = 3; // 0始まりで4本目をチカチカ
        // Z 範囲を屋根内（Z=-21.6 ～ +12）に収める（+16 カット済み屋根の内側）
        for (int i = 0; i < 5; i++)
        {
            float z = Mathf.Lerp(-PLATFORM_L * 0.45f, 12f, i / 4f);  // -21.6 ～ +12
            var pos = new Vector3(PLATFORM_B_X, CEILING_H - 0.12f, z);
            KisaragiStationPrototypeBuilder.AddFluorescentTube(lightGroupB.transform, pos, broken: i == brokenIndexB);
        }

        // ── 2. 陸橋本体（スラブ） ─────────────────────
        BuildBridgeSlab(root);

        // ── 3. 手すり（両側） ─────────────────────────
        BuildRailings(root);

        // ── 4. 柱（橋脚） ─────────────────────────────
        BuildBridgePiers(root);

        // ── 5. 階段 A側 ───────────────────────────────
        BuildStairs(root, sideA: true);

        // ── 6. 階段 B側 ───────────────────────────────
        BuildStairs(root, sideA: false);

        // ── 7. 陸橋ライティング ────────────────────────
        BuildBridgeLights(root);

        // ── 8. 見下ろしホラー演出用の暗がりライト ─────
        BuildTrackShadowLight(root);

        // ── 9. PlayerBounds を陸橋対応に更新 ────────
        var playerBounds = Object.FindObjectOfType<PlayerBounds>();
        if (playerBounds != null)
        {
            var pbSO = new SerializedObject(playerBounds);
            // PLATFORM_W=9.0, TRACK_ZONE_W=5.0 対応
            // PlatformB X: -14.0±4.5 = -18.5~-9.5  StairB X: ~-12.6
            // StairA X: ~+8.0  →  maxX=9.0 で余裕
            pbSO.FindProperty("minX").floatValue = -20.0f;
            pbSO.FindProperty("maxX").floatValue =   9.0f;
            pbSO.FindProperty("minZ").floatValue =  -24f;
            pbSO.FindProperty("maxZ").floatValue =   24f;
            pbSO.FindProperty("minY").floatValue =   -1f;
            pbSO.FindProperty("maxY").floatValue =    6.0f;
            pbSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(playerBounds);
            Debug.Log("[OverpassBuilder] PlayerBounds を陸橋対応に更新 (X:-20~+9, Y:-1~+6)");
        }
        else
        {
            Debug.LogWarning("[OverpassBuilder] PlayerBounds が見つかりません。Inspector で手動設定してください。");
        }

        // ── CharacterController の stepOffset を階段の段差に合わせて更新 ──
        // STAIR_STEP_H = 0.325m なので stepOffset >= 0.35f あれば登れる
        var cc = Object.FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            cc.stepOffset = 0.4f;
            EditorUtility.SetDirty(cc);
            Debug.Log("[OverpassBuilder] CharacterController.stepOffset を 0.4f に設定しました（階段登降可能）");
        }
        else
        {
            Debug.LogWarning("[OverpassBuilder] CharacterController が見つかりません。Player の stepOffset を 0.4f 以上に手動設定してください。");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            "陸橋と Platform B を構築しました！\n\n" +
            "構造:\n" +
            "  ・Platform A 拡張 (X=1.6~4.0 壁側・階段接続)\n" +
            "  ・Platform B (Aと対称・48m)\n" +
            "  ・陸橋スラブ  Y=3.5m, Z=18-28\n" +
            "  ・手すり (両側)\n" +
            "  ・橋脚 (軌道帯を挟む2本)\n" +
            "  ・階段 A側・B側 各10段\n" +
            "  ・陸橋照明 + 軌道帯暗がりライト\n\n" +
            "ホームAから右（壁側）に歩くと階段エリアに入れます。\n\n" +
            "Ctrl+S でシーンを保存してください。",
            "OK");

        Debug.Log("[OverpassBuilder] 陸橋＋Bホーム構築完了");
    }

    // ────────────────────────────────────────────────────────
    // Platform A 拡張  (ホームA 右端から階段エリアへの接続床)
    // BRIDGE_X_A = +4.5、STAIR_WIDTH=3.0 → EXT_W = STAIR_WIDTH + 余裕
    // ────────────────────────────────────────────────────────
    static void BuildPlatformA_Extension(GameObject parent)
    {
        // 拡張エリアのパラメータ（階段幅 + 余裕 を確保）
        const float EXT_W         = 4.0f;   // STAIR_WIDTH(3.0) + 両側 0.5m 余裕
        float       extCenterX    = BRIDGE_X_A + EXT_W * 0.5f;   // = 4.5 + 2.0 = 6.5

        // 階段Aが配置されるZ範囲を十分カバーする
        float stairZStart  = BRIDGE_Z_START + 1f;
        float stairZLen    = STAIR_STEPS * STAIR_STEP_D;           // = 3.2m
        float stairZEnd    = stairZStart + stairZLen;              // = 22.2
        const float GATE_Z_END_VAL = 14.0f;                        // GateBuilderのGATE_Z_ENDと一致（改札〜階段の隙間を解消）
        float floorZEnd    = stairZEnd + 2.0f;                     // 24.2（階段終端+余裕）
        float floorZLen    = floorZEnd - GATE_Z_END_VAL;           // 10.2m（改札北壁から連続）
        float floorZ       = GATE_Z_END_VAL + floorZLen * 0.5f;   // 19.1（中心）

        var ext = new GameObject("PlatformA_Extension");
        ext.transform.SetParent(parent.transform);
        ext.isStatic = true;

        var mat = Mat("Mat_PlatformA_Ext", new Color(0.38f, 0.36f, 0.35f));  // ホームAと同色

        // ── 床スラブ ──
        Cube("Floor_Ext_A", ext,
            new Vector3(extCenterX, PLATFORM_H * 0.5f, floorZ),
            new Vector3(EXT_W, PLATFORM_H, floorZLen),
            mat);

        // ── 屋根なし（階段エリアは開放・天井でプレイヤーがブロックされないよう）──

        // ── 外壁（階段の向こう側） ──
        Cube("OuterWall_Ext_A", ext,
            new Vector3(BRIDGE_X_A + EXT_W + 0.05f, PLATFORM_H + CEILING_H * 0.5f, floorZ),
            new Vector3(0.1f, CEILING_H, floorZLen),
            Mat("Mat_PlatformA_Ext_Wall", new Color(0.33f, 0.30f, 0.28f)));

        // ── 南壁（Z=14、ゴミ箱付近の外への穴を塞ぐ）──────────────────────
        // PlatformWall_North（Z=7〜14, X=4.5）の北端と OuterWall（X=8.55）を接続。
        // 拡張エリア南面が開口していると、プレイヤーが Z=14 縁から -Z 方向に歩いて
        // Z=7〜14 の床なし地帯（ゲート建物と拡張ホームの間）に落ちて外に出られる。
        var wallMat = Mat("Mat_PlatformA_Ext_Wall", new Color(0.33f, 0.30f, 0.28f));
        Cube("SouthWall_Ext_A", ext,
            new Vector3(extCenterX,
                        PLATFORM_H + CEILING_H * 0.5f,
                        GATE_Z_END_VAL - BRIDGE_THICKNESS * 0.5f),
            new Vector3(EXT_W, CEILING_H, BRIDGE_THICKNESS),
            wallMat);

        Debug.Log("[OverpassBuilder] Platform A 拡張完了 (X=1.6~4.0, Z=" + (floorZ - floorZLen * 0.5f).ToString("F1") + "~" + (floorZ + floorZLen * 0.5f).ToString("F1") + ")");
    }

    // ────────────────────────────────────────────────────────
    // Platform B  (Platform A の左側・鏡像)
    // ────────────────────────────────────────────────────────
    static void BuildPlatformB(GameObject parent)
    {
        var platformB = new GameObject("PlatformB");
        platformB.transform.SetParent(parent.transform);
        platformB.isStatic = true;

        var concreteMat = Mat("Mat_PlatformB", new Color(0.35f, 0.33f, 0.32f));

        // 床スラブ
        Cube("Floor_B", platformB,
            new Vector3(PLATFORM_B_X, PLATFORM_H * 0.5f, 0f),
            new Vector3(PLATFORM_W, PLATFORM_H, PLATFORM_L),
            concreteMat);

        // ホワイトライン (右端 = 軌道側)
        Cube("WhiteLine_B", platformB,
            new Vector3(PLATFORM_B_X + PLATFORM_W * 0.5f - 0.075f, PLATFORM_H + 0.0025f, 0f),
            new Vector3(0.15f, 0.005f, PLATFORM_L),
            Mat("Mat_WhiteLineB", new Color(0.95f, 0.95f, 0.95f)));

        // 点字ブロック（黄色・線路側エッジ）
        // 線路端から 1.0m 内側に配置（柱と重ならないよう離す）
        const float TACTILE_W = 0.40f;
        const float TACTILE_H = 0.012f;
        float tactileX_B = PLATFORM_B_X + PLATFORM_W * 0.5f - 1.0f; // = -10.5（線路端 -9.5 から 1m 内側）
        Cube("TactileBlock_B", platformB,
            new Vector3(tactileX_B, PLATFORM_H + TACTILE_H * 0.5f, 0f),
            new Vector3(TACTILE_W, TACTILE_H, PLATFORM_L),
            MakeTactileMat("Mat_TactileB"));

        // 屋根（階段エリア Z=16以降は開放 → プレイヤーがブロックされない）
        // 階段 Z = BRIDGE_Z_START+1 ～ BRIDGE_Z_START+1+STAIR_STEPS*STAIR_STEP_D = 19 ～ 22.2
        // 安全マージンとして Z = 16 で切る
        float roofCutZ   = BRIDGE_Z_START - 2f;          // = 16
        float roofHalfL  = (-PLATFORM_L * 0.5f + roofCutZ) * 0.5f - (-PLATFORM_L * 0.5f) * 0.5f;
        // Z = -24 ～ +16、長さ = 40m、中心 Z = -4
        float roof1Len = roofCutZ - (-PLATFORM_L * 0.5f); // = 16 - (-24) = 40
        float roof1Z   = -PLATFORM_L * 0.5f + roof1Len * 0.5f; // = -24 + 20 = -4
        Cube("Roof_B", platformB,
            new Vector3(PLATFORM_B_X, CEILING_H - 0.075f, roof1Z),
            new Vector3(PLATFORM_W, 0.15f, roof1Len),
            Mat("Mat_RoofB", new Color(0.32f, 0.30f, 0.28f)));

        // 柱 (4.8m間隔)
        int colCount = Mathf.FloorToInt(PLATFORM_L / 4.8f) + 1;
        float colX   = PLATFORM_B_X - PLATFORM_W * 0.5f + 0.2f;
        var colMat   = Mat("Mat_ColB", new Color(0.33f, 0.30f, 0.28f));
        for (int i = 0; i < colCount; i++)
        {
            float z = Mathf.Lerp(-PLATFORM_L * 0.5f + 2f, PLATFORM_L * 0.5f - 2f,
                                  (float)i / Mathf.Max(1, colCount - 1));
            z += (Random.value - 0.5f) * 0.2f;
            Cube("ColB_" + i, platformB,
                new Vector3(colX, PLATFORM_H + CEILING_H * 0.5f, z),
                new Vector3(0.25f, CEILING_H, 0.25f),
                colMat);
        }

        // ベンチ（Bホーム）Blender製FBXモデル / なければCubeフォールバック
        const string BENCH_FBX_B = "Assets/Models/bench.fbx";
        var benchAsset_B = AssetDatabase.LoadAssetAtPath<GameObject>(BENCH_FBX_B);
        float benchX_B = PLATFORM_B_X - PLATFORM_W * 0.5f + 2.5f; // 外縁から2.5m内側（柱との干渉を避ける）
        // Z 位置は柱間（4.8m間隔の中間）に配置：約-10, 0, +10
        float[] benchZs_B = { -10f, 0f, 10f };
        foreach (float bz in benchZs_B)
        {
            if (benchAsset_B != null)
            {
                var b = Object.Instantiate(benchAsset_B);
                b.name = "Bench_B";
                b.transform.SetParent(platformB.transform);
                // Bホームは左端壁沿い・+X（線路・ホーム）向き
                b.transform.position = new Vector3(benchX_B, PLATFORM_H, bz);
                b.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // 左壁側を背に・ホーム（+X）向き
                b.isStatic = true;
                PurgeFbxExtras(b);
            }
            else
            {
                Cube("Bench_B", platformB,
                    new Vector3(benchX_B, PLATFORM_H + 0.21f, bz),
                    new Vector3(1.6f, 0.42f, 0.45f),
                    Mat("Mat_BenchB", new Color(0.30f, 0.27f, 0.25f)));
            }
        }

        // ── 黒い人影（向かいホームBに立つシルエット）──────────────────
        // プレイヤーから見てホームBの軌道側エッジ付近に静止
        var shadowMat = MakeShadowMat();
        var shadow = new GameObject("ShadowFigure");
        shadow.transform.SetParent(platformB.transform);

        float figX = PLATFORM_B_X + PLATFORM_W * 0.5f - 0.4f;

        // 頭（Sphere）
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(shadow.transform);
        head.transform.localPosition = new Vector3(figX, PLATFORM_H + 1.65f, 5f);
        head.transform.localScale    = new Vector3(0.22f, 0.24f, 0.22f);
        head.GetComponent<Renderer>().sharedMaterial = shadowMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        // 胴体（Cube）
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(shadow.transform);
        body.transform.localPosition = new Vector3(figX, PLATFORM_H + 0.95f, 5f);
        body.transform.localScale    = new Vector3(0.30f, 0.70f, 0.20f);
        body.GetComponent<Renderer>().sharedMaterial = shadowMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // 下半身（Cube）
        var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legs.name = "Legs";
        legs.transform.SetParent(shadow.transform);
        legs.transform.localPosition = new Vector3(figX, PLATFORM_H + 0.42f, 5f);
        legs.transform.localScale    = new Vector3(0.26f, 0.80f, 0.18f);
        legs.GetComponent<Renderer>().sharedMaterial = shadowMat;
        Object.DestroyImmediate(legs.GetComponent<Collider>());

        // スモークパーティクル（胴体中心）
        AddSmokeParticles(shadow.transform, body.transform.position);

        // ローカル Bloom Volume（影の周囲半径5m）
        AddShadowBloomVolume(platformB.transform, body.transform.position);

        Debug.Log("[OverpassBuilder] Platform B 構築完了（人影含む）");
    }

    // ────────────────────────────────────────────────────────
    // 陸橋スラブ
    // ────────────────────────────────────────────────────────
    static void BuildBridgeSlab(GameObject parent)
    {
        var bridge = new GameObject("Bridge");
        bridge.transform.SetParent(parent.transform);
        bridge.isStatic = true;

        float slabY = BRIDGE_FLOOR_Y + BRIDGE_THICKNESS * 0.5f;
        float bridgeZ = (BRIDGE_Z_START + BRIDGE_Z_END) * 0.5f;
        float bridgeZLen = BRIDGE_Z_END - BRIDGE_Z_START;

        // 渡り廊下スラブ（軌道帯横断部分）
        Cube("Slab_Main", bridge,
            new Vector3(BRIDGE_X_CENTER, slabY, bridgeZ),
            new Vector3(BRIDGE_X_SPAN, BRIDGE_THICKNESS, bridgeZLen),
            Mat("Mat_Bridge", new Color(0.40f, 0.38f, 0.36f)));

        // A/B 側アプローチ（踊り場）
        // ★ 階段 Z = BRIDGE_Z_START+1 〜 BRIDGE_Z_START+1+STAIR_STEPS*STAIR_STEP_D = 19〜22.2
        //    Landing は階段終端より後ろ（Z=22.2〜28）のみに置く
        //    → これより手前に床板を置くと階段に乗ったプレイヤーの頭がぶつかりブロックされる
        float stairEndZ    = BRIDGE_Z_START + 1f + STAIR_STEPS * STAIR_STEP_D; // 22.2
        float landingLen   = BRIDGE_Z_END - stairEndZ;                          // 5.8m
        float landingZ     = stairEndZ + landingLen * 0.5f;                     // 25.1
        var   bridgeMat    = Mat("Mat_Bridge", new Color(0.40f, 0.38f, 0.36f));

        // A 側踊り場（階段と同じ幅に合わせる）
        // 階段A: centerX = BRIDGE_X_A + STAIR_WIDTH*0.5 + 0.1 = 4.5+1.5+0.1 = 6.1
        // 階段A: X = 4.6 〜 7.6 → Landing も 4.5〜7.7 でカバー（幅3.2m）
        float landingCenterX_A = BRIDGE_X_A + STAIR_WIDTH * 0.5f + 0.1f; // = 6.1（階段中心と揃える）
        Cube("Landing_A", bridge,
            new Vector3(landingCenterX_A, slabY, landingZ),
            new Vector3(STAIR_WIDTH + 0.2f, BRIDGE_THICKNESS, landingLen),
            bridgeMat);

        // B 側踊り場（階段と同じ幅に合わせる）
        // 階段B: centerX = BRIDGE_X_B - STAIR_WIDTH*0.5 - 0.1 = -9.5-1.5-0.1 = -11.1
        // 階段B: X = -12.6 〜 -9.6 → Landing も -12.7〜-9.5 でカバー
        float landingCenterX_B = BRIDGE_X_B - STAIR_WIDTH * 0.5f - 0.1f; // = -11.1
        Cube("Landing_B", bridge,
            new Vector3(landingCenterX_B, slabY, landingZ),
            new Vector3(STAIR_WIDTH + 0.2f, BRIDGE_THICKNESS, landingLen),
            bridgeMat);

        // ── 渡り廊下 天井・壁（橋上を閉じた廊下にする）──────────────
        // 廊下高さ: BRIDGE_FLOOR_Y + 2.4m = 5.9m（プレイヤー1.8m + 余裕0.6m = 確実に通れる）
        float corrCeilY = BRIDGE_FLOOR_Y + 2.4f;       // 5.9m
        // 踊り場まで含めた全幅をカバー
        // 踊り場A: X=4.5〜7.7  踊り場B: X=-12.7〜-9.5 → 全体: -12.7〜+7.7 = 20.4m
        float corrWidth = BRIDGE_X_SPAN + STAIR_WIDTH * 2 + 0.8f; // ≈20.8m（ランディング・階段をカバー）

        // 天井スラブ
        Cube("Ceiling_Bridge", bridge,
            new Vector3(BRIDGE_X_CENTER, corrCeilY + BRIDGE_THICKNESS * 0.5f, bridgeZ),
            new Vector3(corrWidth, BRIDGE_THICKNESS, bridgeZLen),
            bridgeMat);

        // 南壁 (Z=BRIDGE_Z_START) ── 廊下の一方の端面
        Cube("Wall_Bridge_S", bridge,
            new Vector3(BRIDGE_X_CENTER, BRIDGE_FLOOR_Y + 1.2f, BRIDGE_Z_START - BRIDGE_THICKNESS * 0.5f),
            new Vector3(corrWidth, 2.4f, BRIDGE_THICKNESS),
            bridgeMat);

        // 北壁 (Z=BRIDGE_Z_END) ── 廊下のもう一方の端面
        Cube("Wall_Bridge_N", bridge,
            new Vector3(BRIDGE_X_CENTER, BRIDGE_FLOOR_Y + 1.2f, BRIDGE_Z_END + BRIDGE_THICKNESS * 0.5f),
            new Vector3(corrWidth, 2.4f, BRIDGE_THICKNESS),
            bridgeMat);

        // ※ 廊下X側の封鎖壁は削除（橋入口をふさいでしまうため）
        // → 踊り場の外縁は BuildStairs の LandingWall_Outer で保護済み

        Debug.Log("[OverpassBuilder] 陸橋スラブ構築完了（天井・壁含む）");
    }

    // ────────────────────────────────────────────────────────
    // 手すり
    // ────────────────────────────────────────────────────────
    static void BuildRailings(GameObject parent)
    {
        var railings = new GameObject("Railings");
        railings.transform.SetParent(parent.transform);
        railings.isStatic = true;

        float railY  = BRIDGE_FLOOR_Y + RAILING_H * 0.5f;
        float bridgeZ = (BRIDGE_Z_START + BRIDGE_Z_END) * 0.5f;
        float bridgeZLen = BRIDGE_Z_END - BRIDGE_Z_START;
        var mat = Mat("Mat_Railing", new Color(0.25f, 0.24f, 0.25f));

        // 渡り廊下の両側
        // 上側（軌道帯の北側縁・Z+側）
        Cube("Rail_N", railings,
            new Vector3(BRIDGE_X_CENTER, railY, BRIDGE_Z_END - RAILING_T * 0.5f),
            new Vector3(BRIDGE_X_SPAN + 4f, RAILING_H, RAILING_T), mat);

        // 下側（Z-側）
        Cube("Rail_S", railings,
            new Vector3(BRIDGE_X_CENTER, railY, BRIDGE_Z_START + RAILING_T * 0.5f),
            new Vector3(BRIDGE_X_SPAN + 4f, RAILING_H, RAILING_T), mat);

        // 縦支柱（軌道上を望める隙間を作るため柱+横バーのみ）
        int postCount = 5;
        for (int i = 0; i <= postCount; i++)
        {
            float t = (float)i / postCount;
            float x = Mathf.Lerp(BRIDGE_X_B - 1f, BRIDGE_X_A + 1f, t);

            // 北側支柱
            Cube("Post_N_" + i, railings,
                new Vector3(x, railY, BRIDGE_Z_END - 0.3f),
                new Vector3(RAILING_T, RAILING_H, 0.3f), mat);

            // 南側支柱
            Cube("Post_S_" + i, railings,
                new Vector3(x, railY, BRIDGE_Z_START + 0.3f),
                new Vector3(RAILING_T, RAILING_H, 0.3f), mat);
        }

        Debug.Log("[OverpassBuilder] 手すり構築完了");
    }

    // ────────────────────────────────────────────────────────
    // 橋脚
    // ────────────────────────────────────────────────────────
    static void BuildBridgePiers(GameObject parent)
    {
        var piers = new GameObject("BridgePiers");
        piers.transform.SetParent(parent.transform);
        piers.isStatic = true;

        float pierH = BRIDGE_FLOOR_Y;                         // 地面～陸橋床まで
        float pierY = PLATFORM_H + pierH * 0.5f;
        var mat = Mat("Mat_Pier", new Color(0.38f, 0.36f, 0.34f));

        // A側橋脚（Platform A 縁の直外）
        Cube("Pier_A1", piers,
            new Vector3(BRIDGE_X_A + 0.3f, pierY, BRIDGE_Z_START + 1.5f),
            new Vector3(0.4f, pierH, 0.4f), mat);
        Cube("Pier_A2", piers,
            new Vector3(BRIDGE_X_A + 0.3f, pierY, BRIDGE_Z_END - 1.5f),
            new Vector3(0.4f, pierH, 0.4f), mat);

        // B側橋脚
        Cube("Pier_B1", piers,
            new Vector3(BRIDGE_X_B - 0.3f, pierY, BRIDGE_Z_START + 1.5f),
            new Vector3(0.4f, pierH, 0.4f), mat);
        Cube("Pier_B2", piers,
            new Vector3(BRIDGE_X_B - 0.3f, pierY, BRIDGE_Z_END - 1.5f),
            new Vector3(0.4f, pierH, 0.4f), mat);

        Debug.Log("[OverpassBuilder] 橋脚構築完了");
    }

    // ────────────────────────────────────────────────────────
    // 階段（A側 or B側）
    // ────────────────────────────────────────────────────────
    static void BuildStairs(GameObject parent, bool sideA)
    {
        string side   = sideA ? "A" : "B";
        float  centerX = sideA
            ? BRIDGE_X_A + STAIR_WIDTH * 0.5f + 0.1f       // A側
            : BRIDGE_X_B - STAIR_WIDTH * 0.5f - 0.1f;      // B側

        var stairRoot = new GameObject($"Stairs_{side}");
        stairRoot.transform.SetParent(parent.transform);
        stairRoot.isStatic = true;

        var mat = Mat("Mat_Stairs", new Color(0.36f, 0.34f, 0.32f));

        // 各段をキューブで積み重ね（solid block方式：床面から踏み面まで）
        // 各段は Z方向に STAIR_STEP_D ずつオフセット → 正しい階段形状になる
        for (int i = 0; i < STAIR_STEPS; i++)
        {
            float solidH   = STAIR_STEP_H * (i + 1);                            // 床から踏み面までの高さ
            float stepYctr = PLATFORM_H + solidH * 0.5f;                        // Y中心
            float stepZctr = BRIDGE_Z_START + 1f + STAIR_STEP_D * i + STAIR_STEP_D * 0.5f; // Z中心（段ごとにオフセット）

            Cube($"Step_{side}_{i}", stairRoot,
                new Vector3(centerX, stepYctr, stepZctr),
                new Vector3(STAIR_WIDTH, solidH, STAIR_STEP_D),
                mat);
        }

        // 側壁: ホーム対面の内側は入口をふさぐため省略し、外側のみ設置
        // sideA: 外壁 = 右（高X）側、内側（低X=X≈1.65）はホームエッジと重なりブロックになる
        // sideB: 外壁 = 左（低X）側、内側（高X=X≈-4.55）はPlatformB縁と重なりブロックになる
        float sideWallH = STAIR_HEIGHT;
        float sideWallZ = BRIDGE_Z_START + 1f + STAIR_STEPS * STAIR_STEP_D * 0.5f;
        float sideWallDepth = STAIR_STEPS * STAIR_STEP_D;
        var wallMat = Mat("Mat_StairWall", new Color(0.32f, 0.30f, 0.28f));

        // 外壁（階段横・外側）
        float outerX = sideA
            ? centerX + STAIR_WIDTH * 0.5f + 0.05f   // sideA 外壁: 高X側
            : centerX - STAIR_WIDTH * 0.5f - 0.05f;   // sideB 外壁: 低X側

        Cube($"SideWall_{side}_Outer", stairRoot,
            new Vector3(outerX, PLATFORM_H + sideWallH * 0.5f, sideWallZ),
            new Vector3(0.1f, sideWallH, sideWallDepth), wallMat);

        // 内壁（階段横・内側 = 線路・軌道帯に面する側）
        float innerX = sideA
            ? centerX - STAIR_WIDTH * 0.5f - 0.05f   // sideA 内壁: 低X側
            : centerX + STAIR_WIDTH * 0.5f + 0.05f;   // sideB 内壁: 高X側

        Cube($"SideWall_{side}_Inner", stairRoot,
            new Vector3(innerX, PLATFORM_H + sideWallH * 0.5f, sideWallZ),
            new Vector3(0.1f, sideWallH, sideWallDepth), wallMat);

        // 踊り場レベルの外壁延長（階段の上、Landing エリアの外縁）
        float stairEndZ  = BRIDGE_Z_START + 1f + STAIR_STEPS * STAIR_STEP_D; // = 22.2
        float landingLen = BRIDGE_Z_END - stairEndZ;                          // = 5.8m
        float landingZ   = stairEndZ + landingLen * 0.5f;                     // = 25.1

        Cube($"LandingWall_{side}_Outer", stairRoot,
            new Vector3(outerX, BRIDGE_FLOOR_Y + 1.2f, landingZ),
            new Vector3(0.1f, 2.4f, landingLen), wallMat);

        // ── 正面壁（階段奥端 Z=stairEndZ に面する壁）────────────────
        // 階段下から見て「向かい」に当たる壁。
        // 高さ上限は BRIDGE_FLOOR_Y - 0.15f（= 3.35m）にして、
        // 頂上段(step 9)の踏み面 Y=3.5m に立つプレイヤーが壁にぶつからないよう余裕を残す。
        float frontWallH = BRIDGE_FLOOR_Y - PLATFORM_H - 0.15f; // = 3.1m
        Cube($"FrontWall_{side}", stairRoot,
            new Vector3(centerX,
                        PLATFORM_H + frontWallH * 0.5f,
                        stairEndZ + BRIDGE_THICKNESS * 0.5f),
            new Vector3(STAIR_WIDTH, frontWallH, BRIDGE_THICKNESS),
            wallMat);

        Debug.Log($"[OverpassBuilder] 階段 {side} 側構築完了");
    }

    // ────────────────────────────────────────────────────────
    // 陸橋ライティング（薄暗い・不気味な蛍光灯）
    // ────────────────────────────────────────────────────────
    static void BuildBridgeLights(GameObject parent)
    {
        var lights = new GameObject("BridgeLights");
        lights.transform.SetParent(parent.transform);

        // 渡り廊下中央に 3 灯
        float bridgeZ = (BRIDGE_Z_START + BRIDGE_Z_END) * 0.5f;
        float[] lightZs = { bridgeZ - 3f, bridgeZ, bridgeZ + 3f };

        for (int i = 0; i < lightZs.Length; i++)
        {
            var go = new GameObject($"BridgeLight_{i}");
            go.transform.SetParent(lights.transform);
            go.transform.position = new Vector3(BRIDGE_X_CENTER, BRIDGE_FLOOR_Y + 2.4f - 0.2f, lightZs[i]); // 天井 (corrCeilY=5.9m) から 0.2m 下

            var lt = go.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.intensity = 0.6f;      // 意図的に暗い
            lt.range     = 5f;
            lt.color     = new Color(0.85f, 0.90f, 0.78f);  // 薄緑がかった蛍光色
            lt.shadows   = LightShadows.Soft;

            // 蛍光管の見た目
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tube.name = "Tube";
            tube.transform.SetParent(go.transform);
            tube.transform.localPosition = new Vector3(0, 0, 0);
            tube.transform.localScale    = new Vector3(0.8f, 0.05f, 0.05f);  // 廊下(X)方向に伸ばす（Z方向ではなくX方向）
            tube.GetComponent<Renderer>().sharedMaterial =
                Mat("Mat_BridgeTube", new Color(0.88f, 0.93f, 0.80f));
            Object.DestroyImmediate(tube.GetComponent<Collider>());
        }
    }

    // ────────────────────────────────────────────────────────
    // 軌道帯見下ろし用・底部暗がりライト
    // （陸橋から見下ろすと軌道帯が暗く見える → ホラー演出）
    // ────────────────────────────────────────────────────────
    static void BuildTrackShadowLight(GameObject parent)
    {
        var go = new GameObject("TrackShadow_Light");
        go.transform.SetParent(parent.transform);
        go.transform.position = new Vector3(BRIDGE_X_CENTER, 0.3f, (BRIDGE_Z_START + BRIDGE_Z_END) * 0.5f);

        var lt = go.AddComponent<Light>();
        lt.type      = LightType.Spot;
        lt.intensity = 0.05f;   // 極めて暗い（ほぼ見えないが完全な黒でもない）
        lt.range     = 12f;
        lt.spotAngle = 100f;
        lt.color     = new Color(0.4f, 0.35f, 0.5f);   // 薄紫・不気味な色
        lt.transform.rotation = Quaternion.Euler(90f, 0, 0);  // 上向き → 軌道帯を下から照らす
    }

    // ────────────────────────────────────────────────────────
    // ユーティリティ
    // ────────────────────────────────────────────────────────
    static void Cube(string name, GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position  = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
    }

    // Blender FBX に含まれるカメラ・ライトを削除（画面乗っ取り防止）
    static void PurgeFbxExtras(GameObject go)
    {
        foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(cam);
        foreach (var lt in go.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(lt);
    }

    static Material Mat(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.name  = name;
        mat.color = color;
        return mat;
    }

    static Material MakeExteriorMat(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { name = name, color = Color.white };
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/outside.png");
        if (tex != null)
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(4f, 1f));
        }
        else
            Debug.LogWarning("[OverpassBuilder] Assets/Textures/outside.png が見つかりません。");
        return m;
    }

    // ────────────────────────────────────────────────────────
    // 点字ブロック用マテリアル（ドット突起テクスチャ）
    // Assets/Textures/tactile.png があればそれを優先使用、
    // なければプログラムでドットグリッドテクスチャを生成
    // ────────────────────────────────────────────────────────
    static Material MakeTactileMat(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { name = name, color = new Color(0.95f, 0.82f, 0.0f) };

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/tenji.png")
                  ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/tactile.png")
                  ?? GenerateTactileDotTexture();
        if (tex != null)
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(6f, 6f));  // ホーム全長にドットを繰り返す
        }
        return m;
    }

    // 64×64pxの黄色地に暗色ドットを並べたテクスチャを生成・保存
    static Texture2D GenerateTactileDotTexture()
    {
        const string SAVE_PATH = "Assets/Textures/tactile_dots.png";

        // 既に生成済みなら再利用
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(SAVE_PATH);
        if (existing != null) return existing;

        const int SIZE    = 64;
        const int DOT_R   = 4;   // ドット半径 (px)
        const int SPACING = 16;  // ドット中心間隔 (px)

        var yellow  = new Color(0.95f, 0.82f, 0.0f);
        var dotCol  = new Color(0.50f, 0.42f, 0.0f); // 突起の陰影色（立体感）

        var t = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        var pixels = new Color[SIZE * SIZE];
        for (int py = 0; py < SIZE; py++)
        {
            for (int px = 0; px < SIZE; px++)
            {
                int   cx   = Mathf.RoundToInt((float)px / SPACING) * SPACING;
                int   cy   = Mathf.RoundToInt((float)py / SPACING) * SPACING;
                float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                pixels[py * SIZE + px] = dist <= DOT_R ? dotCol : yellow;
            }
        }
        t.SetPixels(pixels);
        t.Apply();
        t.wrapMode = TextureWrapMode.Repeat;

        // Textures フォルダへ保存
        var dir = Application.dataPath + "/Textures";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/tactile_dots.png", t.EncodeToPNG());
        AssetDatabase.ImportAsset(SAVE_PATH);
        Debug.Log("[OverpassBuilder] 点字ドットテクスチャを生成: " + SAVE_PATH);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SAVE_PATH);
    }

    // ── 半透明シルエット用マテリアル ─────────────────────────────────
    static Material MakeShadowMat()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = "Mat_Shadow" };
        mat.color = new Color(0.02f, 0.02f, 0.03f, 0.5f);

        // URP Transparent (Fade相当: アルファで完全透明まで抜ける)
        mat.SetFloat("_Surface", 1f);                                         // Transparent
        mat.SetFloat("_Blend",   0f);                                         // Alpha blend
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite",   0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        return mat;
    }

    // ── 体にまとわせるスモークパーティクル ────────────────────────────
    static void AddSmokeParticles(Transform parent, Vector3 worldPos)
    {
        var go = new GameObject("SmokeParticles");
        go.transform.SetParent(parent);
        go.transform.position = worldPos;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.maxParticles    = 60;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(2f, 3f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.32f);
        main.startColor      = new Color(0.04f, 0.04f, 0.06f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f; // ゆっくり上昇

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(0.32f, 1.5f, 0.22f);

        // 時間経過でアルファをフェードアウト
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(new Color(0.04f, 0.04f, 0.06f), 0f),
                                      new GradientColorKey(new Color(0.04f, 0.04f, 0.06f), 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.35f, 0f),
                                      new GradientAlphaKey(0f,    1f) }
        );
        col.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var pShader  = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit");
        if (pShader != null)
        {
            var pMat = new Material(pShader) { name = "Mat_Smoke" };
            pMat.color = new Color(0.04f, 0.04f, 0.06f, 0.35f);
            renderer.material = pMat;
        }
    }

    // ── ShadowFigure 用ローカル Bloom Volume ─────────────────────────
    static void AddShadowBloomVolume(Transform parent, Vector3 worldPos)
    {
        var go = new GameObject("ShadowBloom_Volume");
        go.transform.SetParent(parent);
        go.transform.position = worldPos;

        // Volume の有効範囲をトリガー球で定義
        var col    = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 5f;

        var volume          = go.AddComponent<Volume>();
        volume.isGlobal     = false;
        volume.blendDistance = 3f;
        volume.weight        = 1f;
        volume.priority      = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom   = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.8f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.5f);
        bloom.tint.Override(new Color(0.75f, 0.85f, 1f)); // 青白い輪郭にじみ
        volume.profile = profile;
    }
}
