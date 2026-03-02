using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ─────────────────────────────────────────────────────────────────────────
// Kisaragi > Add Overpass to Subway Scene（SubwayStationDemoに陸橋追加）
//
// SubwayStationDemo 座標系（床 Y≈-9.85、北方向 -Z）に合わせた陸橋ビルダー。
// SM_Column01 / SM_Bench01 / SM_SubwayLamp などから座標を自動検出して配置。
//
// 構造:
//   ① 島ホーム両端に階段（左端 X≈-41、右端 X≈-30）
//   ② 橋床スラブ（全幅 -50〜-20、Z=-108〜-118）
//   ③ 橋天井・北壁・左右壁（囲まれた廊下）
//   ④ 線路側は手すりのみ（見下ろし可能・ホラー演出）
//   ⑤ 橋上照明3灯
// ─────────────────────────────────────────────────────────────────────────
public class KisaragiSubwayOverpassBuilder
{
    // ── 階段パラメータ ──
    const float STAIR_HEIGHT = 3.5f;    // 昇降高さ（ホーム床 → 橋床）
    const int   STAIR_STEPS  = 10;      // 段数
    const float STAIR_STEP_D = 0.35f;   // 1段の奥行き（Z方向）
    const float STAIR_WIDTH  = 3.0f;    // 階段幅
    // STAIR_STEP_H = STAIR_HEIGHT / STAIR_STEPS = 0.35m/段

    // ── 橋パラメータ ──
    const float BRIDGE_DEPTH = 10.0f;   // 渡り廊下の奥行き（Z方向）
    const float CORRIDOR_H   = 2.4f;    // 廊下の室内高さ
    const float SLAB_T       = 0.15f;   // スラブ厚

    [MenuItem("Kisaragi/Add Overpass to Subway Scene（陸橋追加）")]
    public static void AddOverpass()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モード中は実行できません。", "OK");
            return;
        }

        if (GameObject.Find("SubwayOverpassRoot") != null)
        {
            if (!EditorUtility.DisplayDialog("確認",
                "SubwayOverpassRoot が既に存在します。再構築しますか？",
                "再構築", "キャンセル"))
                return;
            Object.DestroyImmediate(GameObject.Find("SubwayOverpassRoot"));
        }

        // ── 座標を自動検出（失敗時はハードコードにフォールバック）──
        float floorY        = DetectFloorY();        // 床面 Y  (≈-9.85)
        float platformZNorth = DetectPlatformNorthZ(); // ホーム北端 Z (≈-107)
        float outerLeftX    = DetectOuterLeftX();    // 左外壁 X  (≈-50)
        float outerRightX   = DetectOuterRightX();   // 右外壁 X  (≈-20)
        float islandLeftX   = DetectIslandWallX(left: true);  // 島ホーム左壁 X (≈-41.5)
        float islandRightX  = DetectIslandWallX(left: false); // 島ホーム右壁 X (≈-28.2)

        float bridgeFloorY  = floorY + STAIR_HEIGHT;         // 橋床 Y (≈-6.35)
        float bridgeCeilY   = bridgeFloorY + CORRIDOR_H;     // 橋天井 Y (≈-3.95)
        float stairStepH    = STAIR_HEIGHT / STAIR_STEPS;    // 1段高さ (=0.35)
        float stairTotalZ   = STAIR_STEPS * STAIR_STEP_D;    // 階段全長Z (=3.5)

        // 橋のZ範囲
        // 階段は platformZNorth から -Z 方向に stairTotalZ(3.5m) 伸びる。
        // bridgeZStart を階段終端の 0.5m さらに奥(北)に設定することで、
        // 橋床スラブが階段上空に被らず頭上クリアランス 1.8m 以上を確保する。
        float stairEndZ     = platformZNorth - stairTotalZ;          // 階段北端Z (≈-110.5)
        float bridgeZStart  = stairEndZ - 0.5f;                      // 橋南端 (≈-111.0)
        float bridgeZEnd    = bridgeZStart - BRIDGE_DEPTH;           // 橋北端 (≈-121.0)
        float bridgeZCenter = (bridgeZStart + bridgeZEnd) * 0.5f;   // (≈-116.0)

        // 橋のX範囲（外壁間フル）
        float bridgeXCenter = (outerLeftX + outerRightX) * 0.5f;
        float bridgeXSpan   = outerRightX - outerLeftX;

        // 階段センターX（島ホーム壁から1段内側）
        float stairAX = islandLeftX  + STAIR_WIDTH * 0.5f + 0.3f;  // ≈-39.7（左階段）
        float stairBX = islandRightX - STAIR_WIDTH * 0.5f - 0.3f;  // ≈-29.5（右階段）

        var root = new GameObject("SubwayOverpassRoot");
        root.isStatic = true;

        var concMat   = Mat("Mat_OvpConcrete", new Color(0.40f, 0.38f, 0.36f));
        var railMat   = Mat("Mat_OvpRailing",  new Color(0.28f, 0.28f, 0.30f));

        // ─────────────────────────────────────────────
        // 1. 橋床スラブ（全幅）
        // ─────────────────────────────────────────────
        Cube("Bridge_Slab", root,
            new Vector3(bridgeXCenter, bridgeFloorY + SLAB_T * 0.5f, bridgeZCenter),
            new Vector3(bridgeXSpan, SLAB_T, BRIDGE_DEPTH),
            concMat);

        // ─────────────────────────────────────────────
        // 2. 橋天井スラブ
        // ─────────────────────────────────────────────
        Cube("Bridge_Ceiling", root,
            new Vector3(bridgeXCenter, bridgeCeilY + SLAB_T * 0.5f, bridgeZCenter),
            new Vector3(bridgeXSpan, SLAB_T, BRIDGE_DEPTH),
            concMat);

        // ─────────────────────────────────────────────
        // 3. 壁: 北端（奥）
        // ─────────────────────────────────────────────
        Cube("Bridge_WallN", root,
            new Vector3(bridgeXCenter, bridgeFloorY + CORRIDOR_H * 0.5f,
                        bridgeZEnd - SLAB_T * 0.5f),
            new Vector3(bridgeXSpan, CORRIDOR_H, SLAB_T),
            concMat);

        // ─────────────────────────────────────────────
        // 4. 左右の外壁（外壁から橋床まで）
        // ─────────────────────────────────────────────
        Cube("Bridge_WallL", root,
            new Vector3(outerLeftX + SLAB_T * 0.5f,
                        bridgeFloorY + CORRIDOR_H * 0.5f, bridgeZCenter),
            new Vector3(SLAB_T, CORRIDOR_H, BRIDGE_DEPTH), concMat);

        Cube("Bridge_WallR", root,
            new Vector3(outerRightX - SLAB_T * 0.5f,
                        bridgeFloorY + CORRIDOR_H * 0.5f, bridgeZCenter),
            new Vector3(SLAB_T, CORRIDOR_H, BRIDGE_DEPTH), concMat);

        // ─────────────────────────────────────────────
        // 5. 線路上空の手すり（島ホームと外壁の間・見下ろし可能）
        //    左側手すり: outerLeftX 〜 islandLeftX
        //    右側手すり: islandRightX 〜 outerRightX
        // ─────────────────────────────────────────────
        float railH = 1.1f;
        // 南側手すり（橋入口側）
        BuildRailing("Railing_S_Left", root,
            (outerLeftX + islandLeftX) * 0.5f, bridgeFloorY + railH * 0.5f,
            bridgeZStart + SLAB_T * 0.5f,
            islandLeftX - outerLeftX, railH, SLAB_T, railMat);
        BuildRailing("Railing_S_Right", root,
            (islandRightX + outerRightX) * 0.5f, bridgeFloorY + railH * 0.5f,
            bridgeZStart + SLAB_T * 0.5f,
            outerRightX - islandRightX, railH, SLAB_T, railMat);

        // ─────────────────────────────────────────────
        // 6. 島ホーム側に橋入口の "袖壁"（天井から橋床まで）
        //    – 左右の階段を囲む垂直壁
        // ─────────────────────────────────────────────
        float wingH = bridgeCeilY - bridgeFloorY;
        float wingW = (stairAX - STAIR_WIDTH * 0.5f) - outerLeftX;   // 左袖壁幅
        Cube("WingWall_L", root,
            new Vector3(outerLeftX + wingW * 0.5f,
                        bridgeFloorY + wingH * 0.5f, bridgeZStart + SLAB_T * 0.5f),
            new Vector3(wingW, wingH, SLAB_T), concMat);

        float wingW2 = outerRightX - (stairBX + STAIR_WIDTH * 0.5f);  // 右袖壁幅
        Cube("WingWall_R", root,
            new Vector3(outerRightX - wingW2 * 0.5f,
                        bridgeFloorY + wingH * 0.5f, bridgeZStart + SLAB_T * 0.5f),
            new Vector3(wingW2, wingH, SLAB_T), concMat);

        // ─────────────────────────────────────────────
        // 7. 階段 A（左・島ホーム左壁側）
        // ─────────────────────────────────────────────
        BuildStairs(root, stairAX, stairStepH, platformZNorth, floorY, "A", concMat);

        // ─────────────────────────────────────────────
        // 8. 階段 B（右・島ホーム右壁側）
        // ─────────────────────────────────────────────
        BuildStairs(root, stairBX, stairStepH, platformZNorth, floorY, "B", concMat);

        // ─────────────────────────────────────────────
        // 9. 踊り場スラブ（階段頂上 → 橋床スラブの接続部）
        //    階段北端(stairEndZ) ～ 橋南端(bridgeZStart) の 0.5m を埋める
        // ─────────────────────────────────────────────
        float landingLen = Mathf.Abs(bridgeZStart - stairEndZ);     // 0.5m
        float landingZCenter = (stairEndZ + bridgeZStart) * 0.5f;  // 中点Z
        Cube("Landing_A", root,
            new Vector3(stairAX, bridgeFloorY + SLAB_T * 0.5f, landingZCenter),
            new Vector3(STAIR_WIDTH, SLAB_T, landingLen), concMat);
        Cube("Landing_B", root,
            new Vector3(stairBX, bridgeFloorY + SLAB_T * 0.5f, landingZCenter),
            new Vector3(STAIR_WIDTH, SLAB_T, landingLen), concMat);

        // ─────────────────────────────────────────────
        // 10. 橋上照明 3 灯（薄暗いホラー演出）
        // ─────────────────────────────────────────────
        float[] lampZs = { bridgeZCenter + 3f, bridgeZCenter, bridgeZCenter - 3f };
        foreach (float lz in lampZs)
        {
            var lgo = new GameObject("Bridge_Lamp");
            lgo.transform.SetParent(root.transform);
            lgo.transform.position = new Vector3(bridgeXCenter, bridgeCeilY - 0.15f, lz);
            var lt = lgo.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.intensity = 1.2f;
            lt.range     = 9f;
            lt.color     = new Color(0.82f, 0.88f, 0.74f);
            lt.shadows   = LightShadows.Soft;
        }

        // ─────────────────────────────────────────────
        // 11. PlayerBounds 更新（橋上まで移動範囲を拡張）
        // ─────────────────────────────────────────────
        var pb = Object.FindObjectOfType<PlayerBounds>();
        if (pb != null)
        {
            var so = new SerializedObject(pb);
            so.FindProperty("minX").floatValue = outerLeftX  - 1f;
            so.FindProperty("maxX").floatValue = outerRightX + 1f;
            so.FindProperty("minY").floatValue = floorY      - 1f;
            so.FindProperty("maxY").floatValue = bridgeCeilY + 0.5f;
            so.FindProperty("minZ").floatValue = bridgeZEnd  - 1f;
            so.FindProperty("maxZ").floatValue = -79f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pb);
        }

        // CharacterController stepOffset を階段の段差に合わせる
        var cc = Object.FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            cc.stepOffset = 0.4f;
            EditorUtility.SetDirty(cc);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SubwayOverpass] 陸橋追加完了 floorY={floorY:F2} bridgeY={bridgeFloorY:F2} Z={bridgeZStart:F0}~{bridgeZEnd:F0}");
        EditorUtility.DisplayDialog("陸橋追加完了",
            $"陸橋をSubwayStationDemoに追加しました。\n\n" +
            $"  床 Y       : {floorY:F2}\n" +
            $"  橋床 Y     : {bridgeFloorY:F2}\n" +
            $"  橋 Z 範囲  : {bridgeZStart:F1} 〜 {bridgeZEnd:F1}\n" +
            $"  橋 X 範囲  : {outerLeftX:F1} 〜 {outerRightX:F1}\n\n" +
            "Ctrl+S でシーンを保存してください。", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 階段生成（Z方向・ホーム北端から-Z方向に昇る）
    // ─────────────────────────────────────────────────────────────────────
    static void BuildStairs(GameObject parent, float centerX, float stepH,
                            float platformZNorth, float floorY, string side, Material mat)
    {
        var stairRoot = new GameObject($"Stairs_{side}");
        stairRoot.transform.SetParent(parent.transform);
        stairRoot.isStatic = true;

        for (int i = 0; i < STAIR_STEPS; i++)
        {
            float solidH   = stepH * (i + 1);
            float stepYctr = floorY + solidH * 0.5f;
            // Z: ホーム北端から -Z 方向（奥側）に向かって段を積む
            float stepZctr = platformZNorth - STAIR_STEP_D * i - STAIR_STEP_D * 0.5f;

            Cube($"Step_{side}_{i}", stairRoot,
                new Vector3(centerX, stepYctr, stepZctr),
                new Vector3(STAIR_WIDTH, solidH, STAIR_STEP_D),
                mat);
        }

        // 側壁（外側のみ）
        var wallMat = Mat("Mat_StairWall", new Color(0.34f, 0.32f, 0.30f));
        float wallH = STAIR_HEIGHT;
        float wallZ = platformZNorth - STAIR_STEPS * STAIR_STEP_D * 0.5f;
        float outerWallX = (side == "A")
            ? centerX - STAIR_WIDTH * 0.5f - 0.05f
            : centerX + STAIR_WIDTH * 0.5f + 0.05f;
        Cube($"SideWall_{side}", stairRoot,
            new Vector3(outerWallX, floorY + wallH * 0.5f, wallZ),
            new Vector3(0.1f, wallH, STAIR_STEPS * STAIR_STEP_D),
            wallMat);
    }

    static void BuildRailing(string name, GameObject parent,
                             float cx, float cy, float cz,
                             float sx, float sy, float sz, Material mat)
    {
        if (sx <= 0.01f) return;
        Cube(name, parent, new Vector3(cx, cy, cz), new Vector3(sx, sy, sz), mat);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 座標自動検出ヘルパー（シーン内SM_オブジェクトから推定）
    // ─────────────────────────────────────────────────────────────────────
    static float DetectFloorY()
    {
        // SM_Bench01 > SM_Gate > デフォルト
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.name == "SM_Bench01") return Mathf.Round(r.transform.position.y * 4f) / 4f;
        }
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.name.StartsWith("SM_Gate")) return Mathf.Round(r.transform.position.y * 4f) / 4f;
        }
        return -9.85f;
    }

    static float DetectPlatformNorthZ()
    {
        float minZ = -107.0f;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.name.StartsWith("SM_Column"))
                minZ = Mathf.Min(minZ, r.transform.position.z);
        }
        return minZ - 2.0f;
    }

    static float DetectOuterLeftX()
    {
        float minX = -47.0f;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.name.StartsWith("SM_Lamp0") || r.name.StartsWith("SM_SubwayLamp"))
                minX = Mathf.Min(minX, r.transform.position.x);
        }
        return minX - 2.5f;
    }

    static float DetectOuterRightX()
    {
        float maxX = -22.0f;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.name.StartsWith("SM_Lamp0") || r.name.StartsWith("SM_SubwayLamp"))
                maxX = Mathf.Max(maxX, r.transform.position.x);
        }
        return maxX + 2.5f;
    }

    static float DetectIslandWallX(bool left)
    {
        // SM_SubwayLamp が島ホームの壁面に設置されているのでそこから推定
        if (left)
        {
            float minX = -41.5f;
            foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
            {
                if (r.name == "SM_SubwayLamp" || r.name.StartsWith("SM_SubwayLamp ("))
                    minX = Mathf.Min(minX, r.transform.position.x);
            }
            return minX;
        }
        else
        {
            float maxX = -28.2f;
            foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
            {
                if (r.name == "SM_SubwayLamp" || r.name.StartsWith("SM_SubwayLamp ("))
                    maxX = Mathf.Max(maxX, r.transform.position.x);
            }
            return maxX;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────────────────────────────────
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

    static Material Mat(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { name = name, color = color };
        return m;
    }
}
