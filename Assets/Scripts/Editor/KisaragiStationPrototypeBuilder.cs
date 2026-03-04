using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

// 駅プロトタイプ – 実寸48m・StationRoot構造・スクショ映え（Phase1）
public class KisaragiStationPrototypeBuilder
{
    const string SCENE_NAME = "StationPrototype";
    const string SCENES_PATH = "Assets/Scenes";

    // 実寸（station_modeling_full_procedure）
    const float PLATFORM_L = 48f;
    const float PLATFORM_W = 9.0f;   // 旧7.0→9.0（さらに広いホーム）
    const float PLATFORM_H = 0.25f;
    const float CEILING_H = 4.5f;     // 階段・陸橋クリアランス確保（旧3.2→4.5）
    const float WHITE_LINE_W = 0.15f;
    const float WHITE_LINE_H = 0.005f;
    const float RAIL_GAUGE = 1.067f;
    const float RAIL_W = 0.065f;
    const float RAIL_H = 0.15f;
    const float COLUMN_SIZE = 0.25f;
    const float COLUMN_H = 4.25f;     // CEILING_H - PLATFORM_H
    const float COLUMN_INTERVAL = 4.8f;
    const float ROOF_W = 9.0f;
    const float ROOF_T = 0.15f;
    const float BENCH_L = 1.6f;
    const float BENCH_D = 0.45f;
    const float BENCH_SEAT_H = 0.42f;
    const float SIGN_W = 1.8f;
    const float SIGN_H = 0.6f;
    const float ELECTRIC_W = 1.4f;
    const float ELECTRIC_H = 0.35f;
    const float ELECTRIC_D = 0.1f;

    const float FOG_DENSITY = 0.025f;
    // URP はリニアカラースペース。0.06 のような値はほぼ真っ黒になる
    // ガンマ 0.4 相当 = リニア 約 0.13 を基準に設定
    static readonly Color AmbientColor    = new Color(0.22f, 0.22f, 0.28f);    // 夜間ホーム（少し明るめ）
    static readonly Color FogColor        = new Color(0.04f, 0.045f, 0.06f);
    static readonly Color SkyColor        = new Color(0.02f, 0.02f, 0.04f);    // カメラ背景（夜空）
    static readonly Color MainLightColor  = new Color(0.749f, 0.839f, 1f);
    static readonly Color FluorescentColor = new Color(0.91f, 1f, 0.957f);

    [MenuItem("Kisaragi/Station Prototype \u2013 \u99c5\u30b7\u30fc\u30f3\u69cb\u7bc9\uff08\u5b9f\u5bf848m\uff09")]
    public static void BuildStationPrototype()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モード中は実行できません。\nStop してから実行してください。", "OK");
            return;
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (!System.IO.Directory.Exists(SCENES_PATH))
            System.IO.Directory.CreateDirectory(SCENES_PATH);

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Camera.main != null)
            Object.DestroyImmediate(Camera.main.gameObject);

        // ── 霧・環境光 ──
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = FOG_DENSITY;
        RenderSettings.fogColor = FogColor;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 0.2f;
        RenderSettings.skybox = null;

        // ── 月光 ──
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            var light = lightGo.GetComponent<Light>();
            if (light != null)
            {
                light.intensity = 0.8f;    // 月明かり（少し明るめ）
                light.color = MainLightColor;
                light.transform.rotation = Quaternion.Euler(28f, -110f, 0f);
                light.shadowStrength = 0.75f;
            }
        }

        // ── StationRoot ──
        var stationRoot = new GameObject("StationRoot");
        stationRoot.isStatic = true;

        // Platform
        var platform = new GameObject("Platform");
        platform.transform.SetParent(stationRoot.transform);
        platform.isStatic = true;

        var floorMat = GetOrCreateMat("Mat_Platform", new Color(0.38f, 0.36f, 0.35f));
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(platform.transform);
        floor.transform.localPosition = new Vector3(0, PLATFORM_H * 0.5f, 0);
        floor.transform.localScale = new Vector3(PLATFORM_W, PLATFORM_H, PLATFORM_L);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        floor.isStatic = true;

        var whiteLineMat = GetOrCreateMat("Mat_WhiteLine", new Color(0.95f, 0.95f, 0.95f));
        var whiteLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        whiteLine.name = "WhiteLine";
        whiteLine.transform.SetParent(platform.transform);
        whiteLine.transform.localPosition = new Vector3(-PLATFORM_W * 0.5f + WHITE_LINE_W * 0.5f, PLATFORM_H + WHITE_LINE_H * 0.5f, 0);
        whiteLine.transform.localScale = new Vector3(WHITE_LINE_W, WHITE_LINE_H, PLATFORM_L);
        whiteLine.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
        whiteLine.isStatic = true;

        // ── 点字ブロック（黄色・線路側エッジ）──
        // 線路端から 1.0m 内側に配置（柱 X≈-4.2 と重ならないよう離す）
        const float TACTILE_W = 0.40f;
        const float TACTILE_H = 0.012f;
        float tactileX_A = -PLATFORM_W * 0.5f + 1.0f; // = -3.5（線路端 -4.5 から 1m 内側）
        var tactileMat_A = MakeTactileMat("Mat_TactileA");
        var tactile_A = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tactile_A.name = "TactileBlock_A";
        tactile_A.transform.SetParent(platform.transform);
        tactile_A.transform.localPosition = new Vector3(tactileX_A, PLATFORM_H + TACTILE_H * 0.5f, 0);
        tactile_A.transform.localScale = new Vector3(TACTILE_W, TACTILE_H, PLATFORM_L);
        tactile_A.GetComponent<Renderer>().sharedMaterial = tactileMat_A;
        tactile_A.isStatic = true;

        var railMat = GetOrCreateMat("Mat_Rail", new Color(0.25f, 0.24f, 0.22f));
        float railX1 = -PLATFORM_W * 0.5f - 0.5f;
        float railX2 = railX1 - RAIL_GAUGE;
        CreateRail(platform.transform, "Rail_L", railMat, railX1, PLATFORM_L * 3f);
        CreateRail(platform.transform, "Rail_R", railMat, railX2, PLATFORM_L * 3f);

        var gravel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gravel.name = "Gravel";
        gravel.transform.SetParent(platform.transform);
        gravel.transform.localPosition = new Vector3(railX2 - 1f, -0.05f, 0);
        gravel.transform.localScale = new Vector3(2f, 0.1f, PLATFORM_L * 3f);
        gravel.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Gravel", new Color(0.2f, 0.19f, 0.18f));
        gravel.isStatic = true;


        // Roof（Z=16 より先は陸橋・階段エリアのため屋根を除外）
        // OverpassBuilder の BRIDGE_Z_START(18) から余裕 2m 手前でカット
        const float ROOF_CUT_Z = 16f;
        float roofLen    = ROOF_CUT_Z - (-PLATFORM_L * 0.5f);     // = 40m
        float roofCenter = -PLATFORM_L * 0.5f + roofLen * 0.5f;   // = -4m
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(stationRoot.transform);
        roof.transform.localPosition = new Vector3(0.0f, CEILING_H - ROOF_T * 0.5f, roofCenter);
        roof.transform.localScale = new Vector3(ROOF_W, ROOF_T, roofLen);
        roof.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMat("Mat_Roof", new Color(0.32f, 0.30f, 0.28f));
        roof.isStatic = true;

        // Bench（Blender製FBXモデル / なければCubeフォールバック）
        var benchGroup = new GameObject("BenchGroup");
        benchGroup.transform.SetParent(stationRoot.transform);

        const string BENCH_FBX = "Assets/Models/bench.fbx";
        var benchAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BENCH_FBX);

        // ── ホーム中央 半向かい合わせベンチ（プレイヤースタート位置 Z=0 付近）──
        // ホームと平行（Z方向）に 2 脚を配置。背中が中央を向くように外向き。
        //   軌道側（X=-0.6）: 軌道(-X)方向向き  … Y=-90°
        //   壁側  （X=+0.6）: 壁  (+X)方向向き  … Y=+90°
        float[]  benchPairX    = { -1.4f,           +1.4f           };
        float[]  benchPairRotY = { -90f,             +90f            };
        string[] benchPairName = { "Bench_TrackSide", "Bench_WallSide" };
        var benchMat = GetOrCreateMat("Mat_Bench", new Color(0.3f, 0.28f, 0.26f));
        for (int i = 0; i < 2; i++)
        {
            float bx = benchPairX[i];
            if (benchAsset != null)
            {
                var b = Object.Instantiate(benchAsset);
                b.name = benchPairName[i];
                b.transform.SetParent(benchGroup.transform);
                b.transform.position = new Vector3(bx, PLATFORM_H, 0f);
                b.transform.rotation = Quaternion.Euler(0f, benchPairRotY[i], 0f);
                b.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);
                b.isStatic = true;
                PurgeFbxExtras(b);
            }
            else
            {
                Debug.LogWarning("[Builder] " + BENCH_FBX + " が見つかりません。Cube で代替します。");
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = benchPairName[i];
                b.transform.SetParent(benchGroup.transform);
                b.transform.localPosition = new Vector3(bx, PLATFORM_H + BENCH_SEAT_H * 0.5f, 0f);
                b.transform.localScale = new Vector3(BENCH_D, BENCH_SEAT_H, BENCH_L); // 長さ Z 方向
                b.GetComponent<Renderer>().sharedMaterial = benchMat;
                b.isStatic = true;
            }
        }

        // SignBoard（駅名標）
        var signBoard = GameObject.CreatePrimitive(PrimitiveType.Quad);
        signBoard.name = "SignBoard";
        signBoard.transform.SetParent(stationRoot.transform);
        signBoard.transform.localPosition = new Vector3(PLATFORM_W * 0.5f - 0.3f, 2f, -10f);
        signBoard.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signBoard.transform.localScale = new Vector3(SIGN_W, SIGN_H, 1f);
        signBoard.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Sign", new Color(0.15f, 0.15f, 0.15f));
        signBoard.isStatic = true;

        // ヒント張り紙（HintBoard）– 階段前の壁（X=6.1, Z=14.1）
        // 降りてくる時に正面。TMP は非均等スケール歪み回避のため独立配置。
        {
            // 紙面キューブ（和紙色・Z方向に薄い）
            var notice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            notice.name = "HintBoard_Notice";
            notice.transform.SetParent(stationRoot.transform);
            notice.transform.localPosition = new Vector3(6.1f, 2.6f, 14.1f);
            notice.transform.localScale    = new Vector3(3.2f, 2.8f, 0.04f);
            notice.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMat("Mat_Notice", new Color(0.92f, 0.89f, 0.80f));
            notice.isStatic = true;

            // TMP は独立オブジェクト（スケール歪みなし）、ボード北面（+Z面）の前に配置
            var labelGO = new GameObject("HintBoard_Text");
            labelGO.transform.SetParent(stationRoot.transform);
            labelGO.transform.position = new Vector3(6.1f, 2.6f, 14.13f);
            labelGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var noticeTmp = labelGO.AddComponent<TMPro.TextMeshPro>();
            noticeTmp.text               = "";
            noticeTmp.fontSize           = 0.18f;
            noticeTmp.alignment          = TMPro.TextAlignmentOptions.TopRight;
            noticeTmp.color              = Color.black;
            noticeTmp.enableWordWrapping = false;
            noticeTmp.rectTransform.sizeDelta = new Vector2(3.0f, 2.6f);

            var hintBoard   = notice.AddComponent<HintBoard>();
            var hintBoardSO = new SerializedObject(hintBoard);
            hintBoardSO.FindProperty("boardText").objectReferenceValue = noticeTmp;
            hintBoardSO.ApplyModifiedProperties();
        }

        // Clock（時計）- 右壁に掛かった駅時計
        {
            var clockAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/clock.fbx");
            if (clockAsset != null)
            {
                var clock = Object.Instantiate(clockAsset);
                clock.name = "Clock";
                clock.transform.SetParent(stationRoot.transform);
                clock.transform.position = new Vector3(PLATFORM_W * 0.5f - 0.05f, 2.72f, 0f);
                clock.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // 壁面向き
                clock.isStatic = true;
                PurgeFbxExtras(clock);
            }
            else
            {
                Debug.LogWarning("[Builder] Assets/Models/clock.fbx が見つかりません。Cylinder で代替します。");
                var clock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                clock.name = "Clock";
                clock.transform.SetParent(stationRoot.transform);
                clock.transform.localPosition = new Vector3(PLATFORM_W * 0.5f - 0.05f, 2.6f, 0f);
                clock.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                clock.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
                clock.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Clock", new Color(0.88f, 0.84f, 0.76f));
                clock.isStatic = true;
            }
        }

        // DustBox（ゴミ箱）- ホームA 柱の脇・数カ所
        {
            var dustAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/dustbox.fbx");
            float[] dustZs = { -12f, 14f }; // ホーム南側・北側（改札付近 Z=4 は削除）
            float dustX = PLATFORM_W * 0.5f - 0.35f; // 右壁沿い
            foreach (float dz in dustZs)
            {
                if (dustAsset != null)
                {
                    var db = Object.Instantiate(dustAsset);
                    db.name = "DustBox";
                    db.transform.SetParent(stationRoot.transform);
                    db.transform.position = new Vector3(dustX, PLATFORM_H, dz);
                    db.isStatic = true;
                    PurgeFbxExtras(db);
                }
                else
                {
                    Debug.LogWarning("[Builder] Assets/Models/dustbox.fbx が見つかりません。Cube で代替します。");
                    var db = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    db.name = "DustBox";
                    db.transform.SetParent(stationRoot.transform);
                    db.transform.localPosition = new Vector3(dustX, PLATFORM_H + 0.4f, dz);
                    db.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
                    db.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_DustBox", new Color(0.22f, 0.22f, 0.24f));
                    db.isStatic = true;
                }
            }
        }

        // FareBoard（運賃パネル）- 右壁（改札口側）に掲示
        {
            var fareBoardAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/boad1.fbx");
            // 右壁沿いに2枚（南・北）
            float[] fareZs = { -18f, 10f };
            foreach (float fz in fareZs)
            {
                if (fareBoardAsset != null)
                {
                    var fb = Object.Instantiate(fareBoardAsset);
                    fb.name = "FareBoard";
                    fb.transform.SetParent(stationRoot.transform);
                    fb.transform.position = new Vector3(PLATFORM_W * 0.5f - 0.05f, 2.2f, fz);
                    fb.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // 壁面向き
                    fb.isStatic = true;
                    PurgeFbxExtras(fb);
                }
                else
                {
                    Debug.LogWarning("[Builder] Assets/Models/boad1.fbx が見つかりません。Cube で代替します。");
                    var fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fb.name = "FareBoard";
                    fb.transform.SetParent(stationRoot.transform);
                    fb.transform.localPosition = new Vector3(PLATFORM_W * 0.5f - 0.05f, 2.2f, fz);
                    fb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    fb.transform.localScale = new Vector3(0.05f, 1.0f, 1.6f);
                    fb.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_FareBoard", new Color(0.85f, 0.82f, 0.72f));
                    fb.isStatic = true;
                }
            }
        }

        // Timetable（時刻表ボード）
        var timetable = GameObject.CreatePrimitive(PrimitiveType.Cube);
        timetable.name = "Timetable";
        timetable.transform.SetParent(stationRoot.transform);
        timetable.transform.localPosition = new Vector3(PLATFORM_W * 0.5f - 0.05f, 1.8f, 8f);
        timetable.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        timetable.transform.localScale = new Vector3(0.8f, 0.6f, 0.03f);
        timetable.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Timetable", new Color(0.92f, 0.92f, 0.88f));
        timetable.isStatic = true;

        // AnnouncementBoard（案内板・放送スピーカー込み）
        var announcementBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        announcementBoard.name = "AnnouncementBoard";
        announcementBoard.transform.SetParent(stationRoot.transform);
        announcementBoard.transform.localPosition = new Vector3(-PLATFORM_W * 0.5f + 0.1f, 2.4f, -12f);
        announcementBoard.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        announcementBoard.transform.localScale = new Vector3(1.2f, 0.4f, 0.08f);
        announcementBoard.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Announcement", new Color(0.15f, 0.15f, 0.18f));
        announcementBoard.isStatic = true;

        // ElectricBoard
        var electricBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        electricBoard.name = "ElectricBoard";
        electricBoard.transform.SetParent(stationRoot.transform);
        electricBoard.transform.localPosition = new Vector3(-PLATFORM_W * 0.5f + 0.5f, CEILING_H - 0.4f - ELECTRIC_H * 0.5f, -10f);
        electricBoard.transform.localScale = new Vector3(ELECTRIC_W, ELECTRIC_H, ELECTRIC_D);
        electricBoard.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("Mat_Electric", new Color(0.08f, 0.08f, 0.1f));
        electricBoard.isStatic = true;

        // LightingGroup – Platform A 蛍光灯 5 本（うち1本チカチカ）
        var lightingGroup = new GameObject("LightingGroup");
        lightingGroup.transform.SetParent(stationRoot.transform);
        int brokenIndexA = 1; // 0始まりで2本目をチカチカ
        // Z 範囲を屋根内（ROOF_CUT_Z=16 より手前）に収める
        for (int i = 0; i < 5; i++)
        {
            float z = Mathf.Lerp(-PLATFORM_L * 0.45f, 12f, i / 4f);  // -21.6 ～ +12
            var pos = new Vector3(0f, CEILING_H - 0.12f, z);
            AddFluorescentTube(lightingGroup.transform, pos, broken: i == brokenIndexA);
        }

        // LoopVariantContainer（空の骨組み）
        var loopContainer = new GameObject("LoopVariantContainer");
        loopContainer.transform.SetParent(stationRoot.transform);
        for (int i = 1; i <= 4; i++)
        {
            var v = new GameObject("Variant_Loop" + i);
            v.transform.SetParent(loopContainer.transform);
        }

        // ── 落下防止バリア（不可視コライダー）──
        // プレイヤーがホーム端から落ちないよう各端にコライダーを設置
        {
            var barriers = new GameObject("PlatformBarriers");
            barriers.transform.SetParent(stationRoot.transform);
            float barrierH = 2.5f;
            float barrierY = PLATFORM_H + barrierH * 0.5f;

            // 線路側 (X = -PLATFORM_W/2 = -4.5)
            var bTrack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bTrack.name = "Barrier_TrackSide";
            bTrack.transform.SetParent(barriers.transform);
            bTrack.transform.localPosition = new Vector3(-PLATFORM_W * 0.5f, barrierY, 0f);
            bTrack.transform.localScale = new Vector3(0.1f, barrierH, PLATFORM_L);
            bTrack.GetComponent<Renderer>().enabled = false;
            bTrack.isStatic = true;

            // 南端 (Z = -PLATFORM_L/2 = -24)
            var bSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bSouth.name = "Barrier_South";
            bSouth.transform.SetParent(barriers.transform);
            bSouth.transform.localPosition = new Vector3(0f, barrierY, -PLATFORM_L * 0.5f);
            bSouth.transform.localScale = new Vector3(PLATFORM_W, barrierH, 0.1f);
            bSouth.GetComponent<Renderer>().enabled = false;
            bSouth.isStatic = true;

            // 北端 (Z = +PLATFORM_L/2 = +24)
            var bNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bNorth.name = "Barrier_North";
            bNorth.transform.SetParent(barriers.transform);
            bNorth.transform.localPosition = new Vector3(0f, barrierY, PLATFORM_L * 0.5f);
            bNorth.transform.localScale = new Vector3(PLATFORM_W, barrierH, 0.1f);
            bNorth.GetComponent<Renderer>().enabled = false;
            bNorth.isStatic = true;
        }

        // ── ホーム右側壁（南ゾーン：ホーム南端〜改札南端）──
        // GateBuilder.GATE_Z_START(-1) からホーム南端(Z=-24)まで X=+4.5 に壁を設置
        {
            var wallMat = GetOrCreateMat("Mat_PlatformWall", new Color(0.40f, 0.38f, 0.36f));

            const float GATE_SOUTH_Z = -1f;            // GateBuilder.GATE_Z_START と一致
            float platSouthZ  = -PLATFORM_L * 0.5f;   // = -24
            float wallZCenter = (GATE_SOUTH_Z + platSouthZ) * 0.5f; // = -12.5
            float wallZLen    = GATE_SOUTH_Z - platSouthZ;           // = 23m
            var pwallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pwallS.name = "PlatformWall_South";
            pwallS.transform.SetParent(stationRoot.transform);
            pwallS.transform.localPosition = new Vector3(PLATFORM_W * 0.5f, CEILING_H * 0.5f, wallZCenter);
            pwallS.transform.localScale    = new Vector3(0.2f, CEILING_H, wallZLen);
            pwallS.GetComponent<Renderer>().sharedMaterial = wallMat;
            pwallS.isStatic = true;

            // ── ホーム右側壁（北ゾーン：改札北端〜拡張ホーム開始手前）──
            // GateBuilder.GATE_Z_END(+7) から OverpassBuilder の Platform A 拡張開始(Z=14) まで壁を設置
            // Z=14 以降は拡張ホーム床が X>4.5 に生成されるため、ここで壁を止めてプレイヤーが
            // 階段エリア(X>4.5)へ入れるよう開口を確保する
            const float GATE_NORTH_Z   = +7f;   // GateBuilder.GATE_Z_END と一致
            const float BRIDGE_STAIR_Z = 14f;   // OverpassBuilder.GATE_Z_END_VAL（拡張ホーム開始Z）と一致
            float wallNZCenter = (GATE_NORTH_Z + BRIDGE_STAIR_Z) * 0.5f; // = 10.5
            float wallNZLen    = BRIDGE_STAIR_Z - GATE_NORTH_Z;          // = 7m
            var pwallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pwallN.name = "PlatformWall_North";
            pwallN.transform.SetParent(stationRoot.transform);
            pwallN.transform.localPosition = new Vector3(PLATFORM_W * 0.5f, CEILING_H * 0.5f, wallNZCenter);
            pwallN.transform.localScale    = new Vector3(0.2f, CEILING_H, wallNZLen);
            pwallN.GetComponent<Renderer>().sharedMaterial = wallMat;
            pwallN.isStatic = true;
        }

        // VariantController
        var vc = stationRoot.AddComponent<VariantController>();
        vc.floorRenderer = floor.GetComponent<Renderer>();
        vc.flickerLightsRoot = lightingGroup;

        // Player
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 0, -10f); // ベンチ(Z=0)・階段(Z≈17)から離れたホーム南側中央
        player.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.28f;
        cc.center = new Vector3(0, 0.9f, 0);
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerBounds>();

        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        var camComp = cam.AddComponent<Camera>();
        camComp.clearFlags = CameraClearFlags.SolidColor;   // skybox ではなく単色で空を塗る
        camComp.backgroundColor = SkyColor;                  // ほぼ黒（夜空）
        cam.AddComponent<AudioListener>();
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 1.6f, 0.3f);
        cam.transform.localRotation = Quaternion.identity;

        // PlayerController に cameraTransform を設定
        var pcComp = player.GetComponent<PlayerController>();
        var pcSO   = new SerializedObject(pcComp);
        pcSO.FindProperty("cameraTransform").objectReferenceValue = cam.transform;

        // 足音 AudioSource（カツカツ演出）
        var footstepAS = player.AddComponent<AudioSource>();
        footstepAS.loop         = false;
        footstepAS.spatialBlend = 0f;   // プレイヤー自身の音なので 2D
        footstepAS.volume       = 0.75f;
        footstepAS.pitch        = 1.0f;
        // クリップが Assets/Audio/footstep.wav にあれば自動アサイン
        var footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/footstep.wav")
                        ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/footstep.ogg")
                        ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/footstep.mp3");
        if (footstepClip != null) footstepAS.clip = footstepClip;
        pcSO.FindProperty("footstepSource").objectReferenceValue = footstepAS;

        pcSO.ApplyModifiedProperties();

        // ── ホーム反響リバーブゾーン（コンクリートホールのカツカツ反響）──
        var reverbGO = new GameObject("Platform_ReverbZone");
        reverbGO.transform.SetParent(stationRoot.transform);
        reverbGO.transform.localPosition = new Vector3(0f, 0f, 0f);
        var rvz = reverbGO.AddComponent<AudioReverbZone>();
        rvz.reverbPreset = AudioReverbPreset.StoneCorridor; // コンクリート廊下
        rvz.minDistance  = 1f;
        rvz.maxDistance  = 60f; // ホーム全長をカバー

        var canvas = new GameObject("UI Canvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── AudioManager（ambientSource・noiseSource を自動作成して割り当て）──
        var audioManagerGo = new GameObject("AudioManager");
        var audioManager = audioManagerGo.AddComponent<AudioManager>();
        // ambientSource: 環境音（電車音・踏切）
        var ambientSrc = audioManagerGo.AddComponent<AudioSource>();
        ambientSrc.playOnAwake = false;
        ambientSrc.loop = true;
        ambientSrc.volume = 0.5f;
        ambientSrc.spatialBlend = 0f;   // 2D
        // noiseSource: 低周波ノイズ
        var noiseSrc = audioManagerGo.AddComponent<AudioSource>();
        noiseSrc.playOnAwake = false;
        noiseSrc.loop = true;
        noiseSrc.volume = 0f;
        noiseSrc.spatialBlend = 0f;
        // SerializedObject 経由で private SerializeField に代入
        var amSO = new SerializedObject(audioManager);
        amSO.FindProperty("ambientSource").objectReferenceValue = ambientSrc;
        amSO.FindProperty("noiseSource").objectReferenceValue   = noiseSrc;
        amSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(audioManager);
        Debug.Log("[StationPrototype] AudioManager + AudioSources を自動設定しました");

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SCENES_PATH + "/" + SCENE_NAME + ".unity");
        Debug.Log("[StationPrototype] 実寸48m・StationRoot で保存: " + SCENES_PATH + "/" + SCENE_NAME + ".unity");
    }

    static void CreateRail(Transform parent, string name, Material mat, float x, float length)
    {
        var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = name;
        rail.transform.SetParent(parent);
        rail.transform.localPosition = new Vector3(x, RAIL_H * 0.5f - 0.05f, 0);
        rail.transform.localScale = new Vector3(RAIL_W, RAIL_H, length);
        rail.GetComponent<Renderer>().sharedMaterial = mat;
        rail.isStatic = true;
    }

    // ─────────────────────────────────────────────────────────
    // 蛍光灯1本を生成（チューブメッシュ + スポットライト）
    //   broken=true  → チカチカ（蛍光灯切れ演出）
    //   broken=false → 常時点灯
    // ─────────────────────────────────────────────────────────
    public static void AddFluorescentTube(Transform parent, Vector3 worldPos, bool broken)
    {
        string goName = broken ? "Fluorescent_Broken" : "Fluorescent";
        var root = new GameObject(goName);
        root.transform.SetParent(parent);
        root.transform.position = worldPos;

        // ── 蛍光管メッシュ（Cylinder を横向き・自発光マテリアル）──
        var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.name = "Tube";
        tube.transform.SetParent(root.transform);
        tube.transform.localPosition = Vector3.zero;
        tube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        tube.transform.localScale    = new Vector3(0.12f, 0.70f, 0.12f); // 直径12cm・長さ1.4m（太め）

        // URP Lit + Emission で自発光させる（チューブが光って見える）
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var tubeMat = new Material(shader);
        tubeMat.name = broken ? "Mat_Tube_Dead" : "Mat_Tube";
        Color tubeColor = broken
            ? new Color(0.55f, 0.55f, 0.45f)          // 切れた管（くすんだ）
            : new Color(0.92f, 0.98f, 0.88f);          // 正常管（白緑）
        tubeMat.color = tubeColor;
        // エミッション有効化
        tubeMat.EnableKeyword("_EMISSION");
        Color emissionColor = broken
            ? new Color(0.2f, 0.2f, 0.15f)             // 弱い発光
            : new Color(0.85f, 0.95f, 0.80f);          // 明るい発光
        tubeMat.SetColor("_EmissionColor", emissionColor);
        tubeMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        tube.GetComponent<Renderer>().sharedMaterial = tubeMat;
        Object.DestroyImmediate(tube.GetComponent<Collider>());
        tube.isStatic = true;

        // ── スポットライト（下向き・広角・強め）──
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform);
        lightGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sl = lightGo.AddComponent<Light>();
        sl.type           = LightType.Spot;
        sl.intensity      = broken ? 5.5f : 18.0f;   // 明るめ（7m幅ホームをカバー）
        sl.range          = 12f;                      // 天井4.5mから床まで余裕で届く
        sl.spotAngle      = 110f;                     // 広角でホーム全幅をカバー
        sl.innerSpotAngle = 70f;
        sl.color          = broken
            ? new Color(0.90f, 0.85f, 0.65f)          // 切れた：黄色みがかった白
            : FluorescentColor;
        sl.shadows        = LightShadows.Soft;

        // ── チカチカ設定（broken のみ）──
        if (broken)
        {
            var fl = lightGo.AddComponent<FlickerLight>();
            fl.minInterval  = 0.08f;
            fl.maxInterval  = 0.35f;
            fl.firstDelay   = 0.8f;
            fl.offDuration  = new Vector2(0.04f, 0.2f);
            fl.intervalMultiplierFromLoop3 = 0.35f;
        }
    }

    // Blender FBX に含まれるカメラ・ライトを削除（画面乗っ取り防止）
    static void PurgeFbxExtras(GameObject go)
    {
        foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(cam);
        foreach (var lt in go.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(lt);
    }

    static Material GetOrCreateMat(string name, Color c)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = c;
        mat.name = name;
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
            m.SetTextureScale("_BaseMap", new Vector2(4f, 1f)); // 屋根：横長タイリング
        }
        else
            Debug.LogWarning("[Builder] Assets/Textures/outside.png が見つかりません。");
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

        var yellow = new Color(0.95f, 0.82f, 0.0f);
        var dotCol = new Color(0.50f, 0.42f, 0.0f); // 突起の陰影色（立体感）

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
        Debug.Log("[Builder] 点字ドットテクスチャを生成: " + SAVE_PATH);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SAVE_PATH);
    }
}
