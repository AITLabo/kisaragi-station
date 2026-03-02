using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ─────────────────────────────────────────────────────────────
// Kisaragi > Build Gate Building（改札ビル構築）
//
// プレイヤーが居るホームA の線路と逆側（右 +X）に改札ビルを追加する。
// プレイヤーは改札エリアに自由に立ち入れる。
//
// 構造:
//   ① 改札ホール（床・右壁・前後壁・天井）
//   ② 改札ボード（kaisatsu.blend.fbx / 視覚的な改札機）
//   ③ 「改札口」看板 TMP
//   ④ ホール内照明（薄暗いホラー演出）
//   ⑤ 非常口サイン（×印）
// ─────────────────────────────────────────────────────────────
public class KisaragiGateBuilder
{
    // ── 寸法 ──────────────────────────────────────────────────
    const float PLATFORM_W   = 9.0f;
    const float PLATFORM_H   = 0.25f;

    // 改札ビルの X 範囲（ホームA 右端 +4.5 から右方向へ）
    const float BLDG_DEPTH   = 7.0f;
    const float GATE_X_START = PLATFORM_W * 0.5f;               // +4.5
    const float GATE_X_END   = GATE_X_START + BLDG_DEPTH;       // +11.5
    const float GATE_X_CTR   = (GATE_X_START + GATE_X_END) * 0.5f; // +8.0

    // 改札ビルの Z 範囲
    const float GATE_Z_START = -8.0f;
    const float GATE_Z_END   = +14.0f;
    const float GATE_Z_LEN   = GATE_Z_END - GATE_Z_START;       // 22m
    const float GATE_Z_CTR   = (GATE_Z_START + GATE_Z_END) * 0.5f; // +3.0

    // 高さ
    const float BLDG_H    = 4.0f;
    const float FLOOR_Y   = PLATFORM_H;      // 0.25
    const float CEIL_Y    = FLOOR_Y + BLDG_H; // 4.25
    const float WALL_T    = 0.15f;

    // kaisatsu ボードの配置 X
    const float BOARD_OFFSET_X = 0.8f;

    // 閉鎖ドアのサイズ
    const float DOOR_W = 1.6f;   // Z方向幅
    const float DOOR_H = 2.2f;   // 高さ

    [MenuItem("Kisaragi/Build Gate Building（改札ビル構築）")]
    public static void BuildGateBuilding()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モード中は実行できません。", "OK");
            return;
        }

        // 既存 GateRoot は常に破棄して再構築（AllBuilder から呼ばれるため確認不要）
        var existing = GameObject.Find("GateRoot");
        if (existing != null) Object.DestroyImmediate(existing);

        var stationRoot = GameObject.Find("StationRoot");
        var gateRoot = new GameObject("GateRoot");
        gateRoot.isStatic = true;
        if (stationRoot != null)
            gateRoot.transform.SetParent(stationRoot.transform);

        var concMat  = GetOrCreateMat("Mat_Gate_Concrete", new Color(0.30f, 0.28f, 0.27f));
        var floorMat = GetOrCreateMat("Mat_Gate_Floor",    new Color(0.25f, 0.24f, 0.23f));
        var signMat  = GetOrCreateMat("Mat_Gate_Sign",     new Color(0.12f, 0.11f, 0.10f));

        // ─────────────────────────────────────────────
        // 1. 床
        // ─────────────────────────────────────────────
        Cube("Gate_Floor", gateRoot,
            new Vector3(GATE_X_CTR, FLOOR_Y * 0.5f, GATE_Z_CTR),
            new Vector3(BLDG_DEPTH, FLOOR_Y, GATE_Z_LEN), floorMat);

        // ─────────────────────────────────────────────
        // 2. 右壁
        // ─────────────────────────────────────────────
        Cube("Gate_WallR", gateRoot,
            new Vector3(GATE_X_END - WALL_T * 0.5f,
                        FLOOR_Y + BLDG_H * 0.5f, GATE_Z_CTR),
            new Vector3(WALL_T, BLDG_H, GATE_Z_LEN + WALL_T * 2), concMat);

        // ─────────────────────────────────────────────
        // 3. 南壁（Z 手前）
        // ─────────────────────────────────────────────
        Cube("Gate_WallS", gateRoot,
            new Vector3(GATE_X_CTR,
                        FLOOR_Y + BLDG_H * 0.5f, GATE_Z_START - WALL_T * 0.5f),
            new Vector3(BLDG_DEPTH, BLDG_H, WALL_T), concMat);

        // ─────────────────────────────────────────────
        // 4. 北壁（Z 奥）
        // ─────────────────────────────────────────────
        Cube("Gate_WallN", gateRoot,
            new Vector3(GATE_X_CTR,
                        FLOOR_Y + BLDG_H * 0.5f, GATE_Z_END + WALL_T * 0.5f),
            new Vector3(BLDG_DEPTH, BLDG_H, WALL_T), concMat);

        // ─────────────────────────────────────────────
        // 5. 天井
        // ─────────────────────────────────────────────
        Cube("Gate_Ceiling", gateRoot,
            new Vector3(GATE_X_CTR, CEIL_Y + WALL_T * 0.5f, GATE_Z_CTR),
            new Vector3(BLDG_DEPTH + WALL_T * 2, WALL_T, GATE_Z_LEN + WALL_T * 2), concMat);

        // ─────────────────────────────────────────────
        // 6. 改札口側の柱（入口両端）
        // ─────────────────────────────────────────────
        float pillarH = BLDG_H;
        float pillarS = 0.3f;
        Cube("Gate_PillarS", gateRoot,
            new Vector3(GATE_X_START + pillarS * 0.5f,
                        FLOOR_Y + pillarH * 0.5f, GATE_Z_START - pillarS * 0.5f),
            new Vector3(pillarS, pillarH, pillarS), concMat);
        Cube("Gate_PillarN", gateRoot,
            new Vector3(GATE_X_START + pillarS * 0.5f,
                        FLOOR_Y + pillarH * 0.5f, GATE_Z_END + pillarS * 0.5f),
            new Vector3(pillarS, pillarH, pillarS), concMat);

        // 上部まぐさ
        Cube("Gate_Lintel", gateRoot,
            new Vector3(GATE_X_START + WALL_T * 0.5f,
                        FLOOR_Y + BLDG_H - WALL_T * 0.5f, GATE_Z_CTR),
            new Vector3(WALL_T * 2, WALL_T, GATE_Z_LEN), concMat);

        // ─────────────────────────────────────────────
        // 6b. 入口絞り壁 + 南北仕切り壁 + 職員待機所
        //     南絞り壁: Z=-8〜-2 (6m, 南ニッチ Z=-2〜-1 確保)
        //     北絞り壁: Z=+7〜+14 (7m)
        //     開口: Z=-1〜+7 (8m)
        //     職員待機所: 南角ニッチ Z=-1.5 / ホーム境界側 X=4.65
        // ─────────────────────────────────────────────
        const float ENTRY_WALL_S = 6.0f;   // 南: 1m短縮で角ニッチ
        const float ENTRY_WALL_N = 7.0f;   // 北
        float entryWallX = GATE_X_START + WALL_T;           // X=4.65
        // 南絞り壁 (Z=-8〜-2)
        Cube("Gate_EntryWallS", gateRoot,
            new Vector3(entryWallX, FLOOR_Y + BLDG_H * 0.5f,
                        GATE_Z_START + ENTRY_WALL_S * 0.5f),            // Z=-5.0
            new Vector3(WALL_T * 2, BLDG_H, ENTRY_WALL_S), concMat);
        // 北絞り壁 (Z=+7〜+14)
        Cube("Gate_EntryWallN", gateRoot,
            new Vector3(entryWallX, FLOOR_Y + BLDG_H * 0.5f,
                        GATE_Z_END - ENTRY_WALL_N * 0.5f),              // Z=10.5
            new Vector3(WALL_T * 2, BLDG_H, ENTRY_WALL_N), concMat);

        // 北仕切り壁（Z=+7、出口壁まで横断）
        float dividerNZ = GATE_Z_END - ENTRY_WALL_N;                    // +7.0
        Cube("Gate_NorthDivider", gateRoot,
            new Vector3(GATE_X_CTR, FLOOR_Y + BLDG_H * 0.5f,
                        dividerNZ + WALL_T * 0.5f),
            new Vector3(BLDG_DEPTH, BLDG_H, WALL_T), concMat);

        // 南仕切り壁（Z=-1、出口壁まで横断）
        float dividerSZ = GATE_Z_START + ENTRY_WALL_S + 1.0f;          // -8+6+1 = -1
        Cube("Gate_SouthDivider", gateRoot,
            new Vector3(GATE_X_CTR, FLOOR_Y + BLDG_H * 0.5f,
                        dividerSZ - WALL_T * 0.5f),
            new Vector3(BLDG_DEPTH, BLDG_H, WALL_T), concMat);

        // 職員待機所 → StationBooth（section 9）に統合済み

        // ─────────────────────────────────────────────
        // 7. 改札ボード（kaisatsu.blend.fbx）
        //    入口付近に等間隔で配置（通り抜け可能・視覚的な改札機）
        // ─────────────────────────────────────────────
        {
            float boardX = GATE_X_START + BOARD_OFFSET_X;
            var kaisatsuAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/kaisatsu.blend.fbx");
            float[] boardZs = { -6f, -2.4f, 1.2f, 4.8f, 8.4f, 12f };
            foreach (float bz in boardZs)
            {
                if (kaisatsuAsset != null)
                {
                    var kb = Object.Instantiate(kaisatsuAsset);
                    kb.name = "KaisatsuBoard";
                    kb.transform.SetParent(gateRoot.transform);
                    kb.transform.position = new Vector3(boardX, FLOOR_Y, bz);
                    kb.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    kb.isStatic = true;
                    PurgeFbxExtras(kb);
                }
                else
                {
                    Debug.LogWarning("[GateBuilder] Assets/Models/kaisatsu.blend.fbx が見つかりません。スキップします。");
                    break;
                }
            }
        }

        // ─────────────────────────────────────────────
        // 8. 「改札口」看板
        // ─────────────────────────────────────────────
        Cube("GateSign_Frame", gateRoot,
            new Vector3(GATE_X_START + 0.1f, FLOOR_Y + 3.1f, GATE_Z_CTR),
            new Vector3(0.05f, 0.55f, 3.2f), signMat);

        var signGO = new GameObject("GateSign_Text");
        signGO.transform.SetParent(gateRoot.transform);
        signGO.transform.position = new Vector3(GATE_X_START + 0.13f, FLOOR_Y + 3.1f, GATE_Z_CTR);
        signGO.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        var tmp = signGO.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "改　札　口";
        tmp.fontSize = 1.0f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.82f, 0.70f);
        tmp.rectTransform.sizeDelta = new Vector2(3.0f, 0.5f);

        // ─────────────────────────────────────────────
        // 9. 駅員室（ホーム境界側・南ニッチ Z=-1.5）
        //    ホームから見える位置（X=GATE_X_START 付近）に配置
        // ─────────────────────────────────────────────
        {
            float boothZ = dividerSZ - 0.5f;                            // -1.5（南ニッチ中央）
            float boothX = GATE_X_START + 1.0f;                        // X=5.5（ホーム境界寄り）
            var boothAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/booth.fbx");
            if (boothAsset != null)
            {
                var booth = Object.Instantiate(boothAsset);
                booth.name = "StationBooth";
                booth.transform.SetParent(gateRoot.transform);
                booth.transform.position = new Vector3(boothX, FLOOR_Y, boothZ);
                booth.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                booth.isStatic = true;
                PurgeFbxExtras(booth);
            }
            else
            {
                Cube("StationBooth", gateRoot,
                    new Vector3(boothX, FLOOR_Y + 1.2f, boothZ),
                    new Vector3(2.5f, 2.4f, 2.0f),
                    GetOrCreateMat("Mat_Booth", new Color(0.28f, 0.27f, 0.26f)));
            }
        }

        // ─────────────────────────────────────────────
        // 10. 閉鎖両扉ドア（右壁中央・外に出られない）
        //     Gate_WallR の BoxCollider が通過を物理的に防ぐ。
        //     両扉デザインで「出口はあるが封鎖されている」演出。
        // ─────────────────────────────────────────────
        {
            float doorZ     = GATE_Z_CTR;               // 右壁中央 Z=3.0
            float doorFaceX = GATE_X_END - WALL_T - 0.03f; // 右壁内面（X≈11.32）
            float halfW     = DOOR_W * 0.5f;            // 片扉幅 = 0.6m

            var doorMat   = GetOrCreateMat("Mat_Door",      new Color(0.18f, 0.16f, 0.14f));
            var frameMat  = GetOrCreateMat("Mat_DoorFrame", new Color(0.28f, 0.26f, 0.24f));
            var handleMat = GetOrCreateMat("Mat_Handle",    new Color(0.40f, 0.33f, 0.18f));

            // ── ドア枠（上梁・左右柱） ──
            Cube("Door_FrameTop", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H + 0.10f, doorZ),
                new Vector3(0.07f, 0.20f, DOOR_W + 0.40f), frameMat);
            Cube("Door_FrameL", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H * 0.5f, doorZ - halfW - 0.10f),
                new Vector3(0.07f, DOOR_H, 0.20f), frameMat);
            Cube("Door_FrameR", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H * 0.5f, doorZ + halfW + 0.10f),
                new Vector3(0.07f, DOOR_H, 0.20f), frameMat);
            // 中央縦桟（両扉の合わせ目）
            Cube("Door_CenterPost", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H * 0.5f, doorZ),
                new Vector3(0.07f, DOOR_H, 0.06f), frameMat);

            // ── 両扉パネル（左右） ──
            Cube("Door_PanelL", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H * 0.5f, doorZ - halfW * 0.5f),
                new Vector3(0.04f, DOOR_H, halfW - 0.03f), doorMat);
            Cube("Door_PanelR", gateRoot,
                new Vector3(doorFaceX, FLOOR_Y + DOOR_H * 0.5f, doorZ + halfW * 0.5f),
                new Vector3(0.04f, DOOR_H, halfW - 0.03f), doorMat);

            // ── 両扉ノブ（中央付近・対称） ──
            Cube("Door_HandleL", gateRoot,
                new Vector3(doorFaceX - 0.055f, FLOOR_Y + 1.0f, doorZ - 0.12f),
                new Vector3(0.07f, 0.04f, 0.15f), handleMat);
            Cube("Door_HandleR", gateRoot,
                new Vector3(doorFaceX - 0.055f, FLOOR_Y + 1.0f, doorZ + 0.12f),
                new Vector3(0.07f, 0.04f, 0.15f), handleMat);

            // ── 「出口」小サイン（ドア上枠） ──
            var outSignGO = new GameObject("Door_OutSign");
            outSignGO.transform.SetParent(gateRoot.transform);
            outSignGO.transform.position = new Vector3(doorFaceX - 0.06f, FLOOR_Y + DOOR_H + 0.35f, doorZ);
            outSignGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var outTMP = outSignGO.AddComponent<TMPro.TextMeshPro>();
            outTMP.text = "出　口";
            outTMP.fontSize = 0.35f;
            outTMP.alignment = TMPro.TextAlignmentOptions.Center;
            outTMP.color = new Color(0.75f, 0.72f, 0.60f);
            outTMP.rectTransform.sizeDelta = new Vector2(1.4f, 0.3f);

            // ── 「×閉鎖中」サイン（ドアパネル中央） ──
            var closedSignGO = new GameObject("Door_ClosedSign");
            closedSignGO.transform.SetParent(gateRoot.transform);
            closedSignGO.transform.position = new Vector3(doorFaceX - 0.06f, FLOOR_Y + 1.4f, doorZ);
            closedSignGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var closedTMP = closedSignGO.AddComponent<TMPro.TextMeshPro>();
            closedTMP.text = "×\n<size=55%>閉　鎖　中</size>";
            closedTMP.fontSize = 0.55f;
            closedTMP.alignment = TMPro.TextAlignmentOptions.Center;
            closedTMP.color = new Color(0.80f, 0.07f, 0.05f);
            closedTMP.rectTransform.sizeDelta = new Vector2(1.2f, 0.8f);
        }

        // ─────────────────────────────────────────────
        // 12. ホール内照明
        // ─────────────────────────────────────────────
        AddHallLight(gateRoot, GATE_Z_CTR,       CEIL_Y - 0.15f, 1.5f, 10f,
                     new Color(0.78f, 0.82f, 0.65f));
        AddHallLight(gateRoot, GATE_Z_CTR + 5f,  CEIL_Y - 0.15f, 0.5f,  6f,
                     new Color(0.75f, 0.60f, 0.40f));

        // ─────────────────────────────────────────────
        // 13. FlickerLight
        // ─────────────────────────────────────────────
        var flickerLampGO = new GameObject("Gate_FlickerLight");
        flickerLampGO.transform.SetParent(gateRoot.transform);
        flickerLampGO.transform.position = new Vector3(GATE_X_CTR, CEIL_Y - 0.15f, GATE_Z_CTR - 4.0f);
        var lt2 = flickerLampGO.AddComponent<Light>();
        lt2.type      = LightType.Point;
        lt2.intensity = 0.8f;
        lt2.range     = 7f;
        lt2.color     = new Color(0.88f, 0.94f, 0.78f);
        lt2.shadows   = LightShadows.None;
        if (flickerLampGO.GetComponent<FlickerLight>() == null)
            flickerLampGO.AddComponent<FlickerLight>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[GateBuilder] 改札ビル構築完了");
        EditorUtility.DisplayDialog("改札ビル構築完了",
            "改札ビルを追加しました。\n\n" +
            "■ 壁: コンクリート色（外壁テクスチャなし）\n" +
            "■ プレイヤーは改札エリアに自由に立ち入れます\n\n" +
            "Ctrl+S で保存してください。", "OK");
    }

    static void AddHallLight(GameObject parent, float z, float y,
                              float intensity, float range, Color color)
    {
        var go = new GameObject("Gate_HallLight");
        go.transform.SetParent(parent.transform);
        go.transform.position = new Vector3(GATE_X_CTR, y, z);
        var lt = go.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.intensity = intensity;
        lt.range     = range;
        lt.color     = color;
        lt.shadows   = LightShadows.Soft;
    }

    static void Cube(string name, GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { name = name, color = color };
        return m;
    }

    static void PurgeFbxExtras(GameObject go)
    {
        foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(cam);
        foreach (var lt in go.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(lt);
    }
}
