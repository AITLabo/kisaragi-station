using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// きさらぎ駅ゲーム – 電車内シーンビルダー
// SubwayModelSet のプレハブを最大限活用したハイブリッド構築
//
// 【ハイブリッド方針】
//   構造（床・天井・壁）: プリミティブ
//   ディテール（座席・照明・ドア・配線）: SubwayModelSet Prefab
//
// 【ドアの位置】
//   左壁（X=-CW）の側面。プレイヤーはそちらを向いてスタート
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class KisaragiTrainBuilder
{
    const string PF = "Assets/SubwayModelSet/Prefabs/";
    const string MA = "Assets/SubwayModelSet/Materials/";

    const float CW  = 1.28f;   // 車幅半分
    const float CH  = 2.20f;   // 車高
    const float CHL = 10.0f;   // 車両半長

    static Material mFloor, mCeiling, mWall, mWaist, mSeat, mSeatBack;
    static Material mGlass, mWinFrame, mDoorMat, mDoorOrange, mRail;
    static Material mFloorY, mCabPanel, mGhost;

    [MenuItem("Kisaragi/Build Train Scene (電車内シーン構築)")]
    public static void BuildTrainScene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (Camera.main != null)
            Object.DestroyImmediate(Camera.main.gameObject);

        // Directional Light は最小限（内部照明メイン）
        var dl = Object.FindObjectOfType<Light>();
        if (dl != null) { dl.intensity = 0.02f; dl.shadowStrength = 0f; }

        // アンビエントライト：電車内の均一な明るさ（現実と異世界のはざまの白昼夢感）
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.44f, 0.48f);

        // スカイボックスをなし → カメラ背景色は黒（車窓外が真っ暗）
        RenderSettings.skybox = null;
        // フォグは黒・無効化（余計な色を足さない）
        RenderSettings.fog = false;

        InitMats();
        var ui    = BuildUI();
        var vol   = BuildVolume();
        var door  = BuildCar();
        var player= BuildPlayer(ui);

        var reset = new GameObject("ResetPoint");
        reset.transform.position    = new Vector3(0.3f, 0.9f, -3.5f);
        reset.transform.eulerAngles = new Vector3(0, 270f, 0);

        WireAll(player, ui, vol, reset, door);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TrainScene.unity");
        EditorUtility.DisplayDialog("完了",
            "TrainScene 構築完了\n\n" +
            "左側のドアをクリックすると\nきさらぎ駅へ移動します。\n\n" +
            "Console の警告（ベンチ等が見つからない場合）は\n" +
            "Prefabパスを確認してください。", "OK");
    }

    // ──────────────────────────────────────
    // マテリアル
    // ──────────────────────────────────────
    static void InitMats()
    {
        // 床：ベージュ/クリーム（実物電車の床に近い色）
        mFloor     = FlatMat(new Color(0.82f, 0.75f, 0.64f));
        mCeiling   = LoadMat("Ceiling01",       new Color(0.90f, 0.89f, 0.87f));
        mWall      = LoadMat("Metal01",         new Color(0.82f, 0.82f, 0.84f));
        mWaist     = LoadMat("Tile02",          new Color(0.68f, 0.68f, 0.70f));
        mWinFrame  = LoadMat("Metal01",         new Color(0.70f, 0.70f, 0.72f));
        // 窓外は夜なので完全黒（SubwayDetailsGlassは無視してFlatMatで黒を確保）
        mGlass     = FlatMat(new Color(0.00f, 0.00f, 0.00f));
        mDoorMat   = LoadMat("SubwayDetails04NoEmissive", new Color(0.75f, 0.75f, 0.77f));
        mRail      = LoadMat("Metal01",         new Color(0.80f, 0.80f, 0.82f));
        mCabPanel  = LoadMat("SubwayDetails02", new Color(0.60f, 0.58f, 0.56f));

        // 座席：ティール（青緑、参考写真に合わせた色）
        mSeat      = FlatMat(new Color(0.08f, 0.38f, 0.40f));
        mSeatBack  = FlatMat(new Color(0.06f, 0.32f, 0.34f));
        mDoorOrange= FlatMat(new Color(0.92f, 0.42f, 0.04f));
        mFloorY    = FlatMat(new Color(0.95f, 0.85f, 0.05f));
        mGhost     = FlatMat(new Color(0.04f, 0.04f, 0.06f));

        // 手すり：ステンレスシルバー
        mRail2     = FlatMat(new Color(0.88f, 0.88f, 0.90f));

        // mGlass は InitMats の最初の段階で設定済み
    }

    static Material mRail2;   // 手すり・吊り革用

    static Material LoadMat(string name, Color fallback)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(MA + name + ".mat");
        if (m != null) return m;
        Debug.LogWarning($"[TrainBuilder] {name}.mat 未発見 → フォールバック");
        return FlatMat(fallback);
    }

    static Material FlatMat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c; return m;
    }

    // ──────────────────────────────────────
    // 車両構築
    // ──────────────────────────────────────
    static GameObject BuildCar()
    {
        // ── 基本シェル（プリミティブ）──
        Cube("Floor",   P(0,-0.03f,0),        S(CW*2,0.06f,CHL*2), mFloor);
        Cube("Ceiling", P(0,CH+0.04f,0),      S(CW*2+0.1f,0.08f,CHL*2), mCeiling);
        // 左壁（ドア開口あり）
        BuildSideWall(true);
        // 右壁
        BuildSideWall(false);
        // 前端：運転台
        BuildCabEnd(-CHL);
        // 後端：連結部
        BuildConnEnd(CHL);
        // 腰板
        Cube("WaistL", P(-CW,0.55f,0), S(0.03f,1.1f,CHL*2), mWaist);
        Cube("WaistR", P( CW,0.55f,0), S(0.03f,1.1f,CHL*2), mWaist);

        // ── 側面窓（ドアのない壁区間に配置）──
        // ドア位置: -7.5, -2.5, +2.5, +7.5
        float[] wzs = {-9.0f, -5.5f, -4.5f, -0.8f, 0.8f, 4.5f, 5.5f, 9.0f};
        foreach (float wz in wzs) { SideWindow(true,wz); SideWindow(false,wz); }

        // ── 床の黄色ライン（全ドア4箇所に対応）──
        foreach (float dz in DOOR_ZS)
        {
            Cube($"FYL_L_{dz}", P(-0.42f,0.003f,dz), S(0.04f,0.01f,DOOR_HW*2+0.3f), mFloorY);
            Cube($"FYL_R_{dz}", P( 0.42f,0.003f,dz), S(0.04f,0.01f,DOOR_HW*2+0.3f), mFloorY);
        }

        // ── 【SubwayModelSet Prefab】SM_Bench01 で座席 ──
        BuildBenches();

        // ── 【SubwayModelSet Prefab】SM_SubwayLamp で天井照明 ──
        BuildLamps();

        // ── 【SubwayModelSet Prefab】SM_Cable で天井配線 ──
        BuildCables();

        // ── 手すり・吊り革（円柱）──
        BuildHandrails();

        // ── 中吊り広告・LCD画面 ──
        BuildAdvertisements();

        // ── スライド両開きドア（プリミティブ自作）──
        return BuildSlidingDoors();
    }

    // ── 側面壁（ドア4箇所の開口あり） ──
    // ドア位置: z = -7.5, -2.5, +2.5, +7.5
    static readonly float[] DOOR_ZS = { -7.5f, -2.5f, 2.5f, 7.5f };
    const float DOOR_HW = 0.68f;  // ドア半幅（全幅1.36m）
    const float DOOR_H  = 2.08f;  // ドア高さ

    static void BuildSideWall(bool isLeft)
    {
        float wx = isLeft ? -CW : CW;
        string s = isLeft ? "L" : "R";
        float th = 0.05f;

        // ドア開口を除いた壁セグメント
        float[] edges = {
            -CHL,
            DOOR_ZS[0] - DOOR_HW,
            DOOR_ZS[0] + DOOR_HW,
            DOOR_ZS[1] - DOOR_HW,
            DOOR_ZS[1] + DOOR_HW,
            DOOR_ZS[2] - DOOR_HW,
            DOOR_ZS[2] + DOOR_HW,
            DOOR_ZS[3] - DOOR_HW,
            DOOR_ZS[3] + DOOR_HW,
            CHL
        };

        // 壁セグメント（ドア間の通し壁）
        for (int i = 0; i < edges.Length - 1; i += 2)
        {
            float z0 = edges[i], z1 = edges[i + 1];
            float cz = (z0 + z1) * 0.5f;
            float len = z1 - z0;
            if (len > 0.01f)
                Cube($"WSeg{i}{s}", P(wx, CH*0.5f, cz), S(th, CH, len), mWall);
        }

        // ドア上鴨居（4箇所）
        foreach (float dz in DOOR_ZS)
            Cube($"WLintel{s}{dz}", P(wx, CH - 0.06f, dz),
                S(th, (CH - DOOR_H), DOOR_HW * 2 + 0.04f), mWall);
    }

    // ── 運転台端 ──
    static void BuildCabEnd(float z)
    {
        Cube("CabWall",  P(0,CH*0.5f,z),       S(CW*2,CH,0.08f),   mCabPanel);
        Cube("CabWin",   P(0,CH*0.62f,z+0.01f),S(0.65f,0.38f,0.02f),mGlass);
        Cube("CabWinFr", P(0,CH*0.62f,z+0.01f),S(0.71f,0.44f,0.015f),mWinFrame);
        Cube("CabDoor",  P(0,CH*0.40f,z+0.01f),S(0.52f,CH*0.74f,0.03f),mCabPanel);
        Cube("CabDoorFr",P(0,CH*0.40f,z+0.01f),S(0.58f,CH*0.75f,0.015f),mWinFrame);
    }

    // ── 連結端 ──
    static void BuildConnEnd(float z)
    {
        Cube("ConnWall",  P(0,CH*0.5f,z),        S(CW*2,CH,0.08f),    mCabPanel);
        Cube("ConnWin",   P(0,CH*0.55f,z-0.01f), S(0.48f,0.48f,0.02f),mGlass);
        Cube("ConnWinFr", P(0,CH*0.55f,z-0.01f), S(0.54f,0.54f,0.015f),mWinFrame);
        Cube("ConnDoor",  P(0,CH*0.40f,z-0.01f), S(0.58f,CH*0.76f,0.03f),mCabPanel);
    }

    // ── 側面窓（外は漆黒）──
    static void SideWindow(bool isLeft, float z)
    {
        float wx  = isLeft ? -CW : CW;
        float gx  = isLeft ? -(CW - 0.035f) : (CW - 0.035f);
        float fx  = isLeft ? -(CW - 0.060f) : (CW - 0.060f);
        float shx = isLeft ? -(CW - 0.15f)  : (CW - 0.15f);
        // 外側に置く真っ黒バックドロップ
        float bdx = isLeft ? -(CW + 0.12f)  : (CW + 0.12f);
        string s  = isLeft ? "L" : "R";
        float fw  = 0.046f;

        // ガラス（不透明黒）
        Cube($"WG{s}{z}",    P(gx, 1.28f, z),               S(0.018f,0.72f,1.50f), mGlass);
        // ウィンドウフレーム
        Cube($"WFT{s}{z}",   P(fx,1.28f+0.38f+fw*.5f,z),    S(0.04f,fw,1.58f),   mWinFrame);
        Cube($"WFB{s}{z}",   P(fx,1.28f-0.38f-fw*.5f,z),    S(0.04f,fw,1.58f),   mWinFrame);
        Cube($"WFL{s}{z}",   P(fx,1.28f,z-0.78f),           S(0.04f,0.72f+fw*2,fw),mWinFrame);
        Cube($"WFR{s}{z}",   P(fx,1.28f,z+0.78f),           S(0.04f,0.72f+fw*2,fw),mWinFrame);
        Cube($"WSill{s}{z}", P(shx,1.28f-0.41f,z),          S(0.22f,0.04f,1.52f),mWinFrame);
        // 外側の漆黒バックドロップ（車窓の外が見えないように）
        var bdMat = FlatMat(Color.black);
        Cube($"WBD{s}{z}",   P(bdx, 1.28f, z),              S(0.01f,0.80f,1.60f), bdMat);
    }

    // ──────────────────────────────────────
    // 【Prefab】SM_Bench01 で座席を配置
    // ──────────────────────────────────────
    // ドア除外ゾーンを避けて座席を密に配置（緑色）
    static void BuildBenches()
    {
        var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PF + "SM_Bench01.prefab");

        // ドア4箇所（DOOR_ZS）を避けた座席Z座標リスト
        var benchZs = new System.Collections.Generic.List<float>();
        float step = 0.63f;
        for (float z = -(CHL - 0.5f); z <= (CHL - 0.5f); z += step)
        {
            bool nearDoor = false;
            foreach (float dz in DOOR_ZS)
                if (Mathf.Abs(z - dz) < 0.90f) { nearDoor = true; break; }
            if (!nearDoor) benchZs.Add(z);
        }

        float bxL = -(CW - 0.24f);
        float bxR =  (CW - 0.24f);

        foreach (float bz in benchZs)
        {
            PlaceBench(benchPrefab, new Vector3(bxL, 0f, bz), new Vector3(0,  90f, 0));
            PlaceBench(benchPrefab, new Vector3(bxR, 0f, bz), new Vector3(0, 270f, 0));
        }

        Debug.Log($"[TrainBuilder] 座席配置完了（左右×{benchZs.Count}個ずつ）");
    }

    static void PlaceBench(GameObject prefab, Vector3 pos, Vector3 euler)
    {
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        }
        else
        {
            // キューブ代替（緑）
            go = new GameObject("FallbackBench");
            float bx = pos.x < 0 ? -(CW-0.055f) : (CW-0.055f);
            Cube($"SeatC_{pos.z:F1}", P(pos.x, 0.41f, pos.z), S(0.36f,0.07f,0.50f), mSeat);
            Cube($"BakC_{pos.z:F1}",  P(bx, 0.72f, pos.z),    S(0.055f,0.55f,0.50f), mSeatBack);
        }
        go.transform.position    = pos;
        go.transform.eulerAngles = euler;

        // 全レンダラーを緑マテリアルに
        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = mr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mSeat;
            mr.sharedMaterials = mats;
        }
    }

    // ──────────────────────────────────────
    // 【Prefab】SM_SubwayLamp で天井照明
    // ──────────────────────────────────────
    static void BuildLamps()
    {
        // SM_SubwayLamp → 見つからなければ SM_Lamp01 → さらに見つからなければ Cube代替
        var lampPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PF + "SM_SubwayLamp.prefab")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(PF + "SM_SubwayLamp02.prefab")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(PF + "SM_Lamp01.prefab");

        // ランプは密に配置（明るい車内）
        float[] lampZs = {-9f,-7f,-5f,-3f,-1f,1f,3f,5f,7f,9f};

        // 蛍光灯エミッシブマテリアル（Prefab有無に関わらず共通）
        var emMat = FlatMat(new Color(1f, 0.97f, 0.90f));
        emMat.SetColor("_EmissionColor", new Color(3.2f, 3.0f, 2.6f));
        emMat.EnableKeyword("_EMISSION");

        foreach (float lz in lampZs)
        {
            if (lampPrefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab);
                go.name = $"Lamp_{lz}";
                go.transform.position    = new Vector3(0, CH - 0.01f, lz);
                go.transform.eulerAngles = new Vector3(0, 0, 180f);
                go.transform.localScale  = new Vector3(1f, 1f, 1f);
                // Prefab のマテリアルに強制的に Emission を有効化
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats = mr.sharedMaterials;
                    for (int mi = 0; mi < mats.Length; mi++)
                    {
                        if (mats[mi] == null) continue;
                        mats[mi].SetColor("_EmissionColor", new Color(2.5f, 2.3f, 2.0f));
                        mats[mi].EnableKeyword("_EMISSION");
                    }
                    mr.sharedMaterials = mats;
                }
            }
            else
            {
                // Cube 蛍光灯
                Cube($"LitL{lz}", P(-0.33f,CH,lz), S(0.12f,0.025f,1.8f), emMat);
                Cube($"LitR{lz}", P( 0.33f,CH,lz), S(0.12f,0.025f,1.8f), emMat);
            }

            // PointLight は Prefab 有無に関わらず常に追加
            var lo = new GameObject($"PL_{lz}");
            lo.transform.position = P(0, CH - 0.18f, lz);
            var pl = lo.AddComponent<Light>();
            pl.type      = LightType.Point;
            pl.range     = 7.5f;
            pl.intensity = 5.5f;
            pl.color     = new Color(0.97f, 0.97f, 1.00f);
        }
        Debug.Log($"[TrainBuilder] ランプ＆PointLight {lampZs.Length} 個配置");
    }

    // ──────────────────────────────────────
    // 天井配線（SM_Cable は暗く見えるためスキップ、
    //           代わりに細いキューブで白いケーブルを描画）
    // ──────────────────────────────────────
    static void BuildCables()
    {
        var mCable = FlatMat(new Color(0.90f, 0.90f, 0.92f));
        // 天井の左右端に沿って細い白ケーブル
        Cube("CableRunL", P(-CW+0.06f, CH+0.01f, 0), S(0.015f, 0.015f, CHL*2), mCable);
        Cube("CableRunR", P( CW-0.06f, CH+0.01f, 0), S(0.015f, 0.015f, CHL*2), mCable);
    }

    // ──────────────────────────────────────
    // スライド両開きドア（プリミティブ自作）
    // 各開口に左右それぞれ2枚のパネルが向かい合う
    // ──────────────────────────────────────
    static GameObject BuildSlidingDoors()
    {
        // ドアパネル: ステンレスシルバー（Prefab非依存でFlatMatを使う）
        var mPanel   = FlatMat(new Color(0.82f, 0.82f, 0.84f));
        // ドアガラス: 暗い半透明グレー（閉じているので少し暗め）
        var mDoorGlass = FlatMat(new Color(0.06f, 0.07f, 0.09f));
        // ドア中央のゴムシール: 黒
        var mSeal    = FlatMat(new Color(0.06f, 0.06f, 0.06f));
        // 上部LEDライン
        var mLed = FlatMat(new Color(0.3f, 0.7f, 1.0f));
        mLed.SetColor("_EmissionColor", new Color(0.5f, 1.2f, 2.0f));
        mLed.EnableKeyword("_EMISSION");

        // パネルサイズ
        float panW  = DOOR_HW - 0.03f; // 各パネルの幅（gap分引く）
        float panH  = DOOR_H;
        float glH   = 0.46f;           // 窓（ガラス）高さ（小さく → 金属が主役）
        float glW   = panW - 0.10f;    // 窓幅（左右に金属フレーム）
        float metH  = panH - glH - 0.18f; // 下部金属（1.44m → 69%が金属）
        float panTh = 0.04f;           // パネル厚

        GameObject interactiveDoor = null;

        foreach (bool isLeft in new[] { true, false })
        {
            float wx = isLeft ? -CW : CW;
            // パネルは壁面より数cm内側（車内側に突き出る）
            float px = isLeft ? wx + panTh * 0.5f : wx - panTh * 0.5f;
            string s = isLeft ? "L" : "R";

            foreach (float dz in DOOR_ZS)
            {
                bool isInteractive = (isLeft && Mathf.Approximately(dz, -2.5f));

                // ── ドア枠（細いオレンジ線、実物は数cmの細いゴム/塗装ライン）──
                // 上鴨居（細め）
                Cube($"DF_Top{s}{dz}",  P(wx, DOOR_H + (CH-DOOR_H)*0.5f, dz),
                    S(0.055f, CH-DOOR_H+0.02f, DOOR_HW*2 + 0.06f), mDoorOrange);
                // 左柱（細め）
                Cube($"DF_PilL{s}{dz}", P(wx, DOOR_H*0.5f, dz - DOOR_HW - 0.02f),
                    S(0.055f, DOOR_H, 0.05f), mDoorOrange);
                // 右柱（細め）
                Cube($"DF_PilR{s}{dz}", P(wx, DOOR_H*0.5f, dz + DOOR_HW + 0.02f),
                    S(0.055f, DOOR_H, 0.05f), mDoorOrange);

                // ── 左パネル・右パネル（閉じた状態）──
                float lPanCz = dz - panW * 0.5f;
                float rPanCz = dz + panW * 0.5f;

                // 窓の配置 Y
                float gBot = metH + 0.06f;          // 窓下端Y
                float gTop = gBot + glH;              // 窓上端Y
                float gCz  = gBot + glH * 0.5f;      // 窓中心Y
                // 窓より上の金属（窓上部〜ドア上端）
                float topMetH = panH - gTop - 0.01f;
                float topMetCy = gTop + topMetH * 0.5f;

                foreach (float pcz in new[] { lPanCz, rPanCz })
                {
                    string side = pcz < dz ? "L" : "R";
                    // 下部金属パネル（大きい）
                    Cube($"DP_Bot_{s}{dz}{side}", P(px, metH*0.5f,   pcz), S(panTh, metH, panW-0.01f), mPanel);
                    // 上部金属パネル（窓より上）
                    Cube($"DP_Top_{s}{dz}{side}", P(px, topMetCy,     pcz), S(panTh, topMetH, panW-0.01f), mPanel);
                    // 窓ガラス（小さな窓）
                    Cube($"DG_{s}{dz}{side}",     P(px, gCz, pcz),    S(panTh*0.5f, glH-0.04f, glW), mDoorGlass);
                    // 窓フレーム（上下左右の細い金属枠）
                    Cube($"DGF_T_{s}{dz}{side}",  P(px, gTop+0.015f, pcz), S(panTh, 0.030f, glW+0.08f), mPanel);
                    Cube($"DGF_B_{s}{dz}{side}",  P(px, gBot-0.015f, pcz), S(panTh, 0.030f, glW+0.08f), mPanel);
                    Cube($"DGF_SL_{s}{dz}{side}", P(px, gCz, pcz-(glW+0.06f)*0.5f), S(panTh, glH+0.06f, 0.045f), mPanel);
                    Cube($"DGF_SR_{s}{dz}{side}", P(px, gCz, pcz+(glW+0.06f)*0.5f), S(panTh, glH+0.06f, 0.045f), mPanel);
                }
                // 中央ゴムシール
                Cube($"DS_Seal{s}{dz}", P(px, DOOR_H*0.5f, dz), S(panTh+0.01f, DOOR_H, 0.018f), mSeal);

                // LED ライン（鴨居下）
                Cube($"D_LED{s}{dz}", P(px, DOOR_H + 0.03f, dz),
                    S(panTh, 0.03f, DOOR_HW * 2 - 0.04f), mLed);

                // ── インタラクティブコライダー ──
                if (isInteractive)
                {
                    var root = new GameObject("TrainDoor");
                    root.transform.position = new Vector3(wx, DOOR_H * 0.5f, dz);
                    var col = root.AddComponent<BoxCollider>();
                    col.size   = new Vector3(0.4f, DOOR_H, DOOR_HW * 2 + 0.12f);
                    col.center = Vector3.zero;

                    var co  = root.AddComponent<CorrectObject>();
                    var soC = new SerializedObject(co);
                    soC.FindProperty("actionID").stringValue           = "train_door";
                    soC.FindProperty("destroyAfterInteract").boolValue = false;
                    soC.ApplyModifiedProperties();

                    int lay = LayerMask.NameToLayer("Interactable");
                    root.layer = lay >= 0 ? lay : 0;
                    interactiveDoor = root;
                }
            }
        }

        Debug.Log("[TrainBuilder] スライド両開きドア配置完了（左右×4箇所 = 計8ドア）");
        return interactiveDoor;
    }

    // ──────────────────────────────────────
    // 手すり・吊り革（円柱）
    // 縦ポール（各ドア脇）＋横バー＋吊り革
    // ──────────────────────────────────────
    static void BuildHandrails()
    {
        var mStrap = FlatMat(new Color(0.78f, 0.78f, 0.80f)); // ベルト（グレー）
        var mRing  = FlatMat(new Color(0.88f, 0.88f, 0.90f)); // リング（白いプラスチック）

        float poleR   = 0.022f;   // 縦ポール半径
        float barR    = 0.018f;   // 横バー半径
        float strapR  = 0.010f;   // 吊り革ベルト半径（細め）
        float poleTop = CH - 0.08f;
        float barY    = CH - 0.15f;
        // 吊り革リングサイズ（参考写真の三角形に合わせる）
        float ringW   = 0.18f;    // 上辺の幅（18cm）
        float ringH   = 0.14f;    // 三角形の高さ（14cm）
        float ringTh  = 0.008f;   // チューブ半径（8mm）

        // ── 横バー（天井沿いに両端2本、全長通し）──
        float barHLen = CHL * 2 * 0.5f;
        Cyl("RailBarL", P(-0.55f, barY, 0), barR, CHL*2, mRail2, new Vector3(90,0,0));
        Cyl("RailBarR", P( 0.55f, barY, 0), barR, CHL*2, mRail2, new Vector3(90,0,0));

        // ── 縦ポール（各ドア脇）──
        foreach (float dz in DOOR_ZS)
        {
            // 各ドアの左右にポール
            float[] pzs = { dz - DOOR_HW - 0.05f, dz + DOOR_HW + 0.05f };
            foreach (float pz in pzs)
            {
                Cyl($"PoleL_{dz}_{pz}", P(-0.55f, poleTop*0.5f, pz), poleR, poleTop, mRail2);
                Cyl($"PoleR_{dz}_{pz}", P( 0.55f, poleTop*0.5f, pz), poleR, poleTop, mRail2);
            }
            // ドア上部の横連結バー（ドア幅分）
            Cyl($"DBarL_{dz}", P(-0.55f, barY, dz), barR, DOOR_HW*2+0.2f, mRail2, new Vector3(90,0,0));
            Cyl($"DBarR_{dz}", P( 0.55f, barY, dz), barR, DOOR_HW*2+0.2f, mRail2, new Vector3(90,0,0));
        }

        // ── 吊り革（バー全長に等間隔）──
        float strapLen = 0.24f;  // ベルト長さ
        float strapY   = barY - strapLen * 0.5f;
        for (float sz = -(CHL - 0.8f); sz <= (CHL - 0.8f); sz += 0.70f)
        {
            // ドア付近は省略
            bool nearDoor = false;
            foreach (float dz in DOOR_ZS)
                if (Mathf.Abs(sz - dz) < 0.8f) { nearDoor = true; break; }
            if (nearDoor) continue;

            // 左バー側
            Cyl($"StrapBeltL_{sz:F1}", P(-0.55f, strapY, sz), strapR, strapLen, mStrap);
            BuildStrapRing($"RingL_{sz:F1}", P(-0.55f, strapY - strapLen*0.5f - ringH*0.5f, sz),
                ringW, ringH, ringTh, mRing);
            // 右バー側
            Cyl($"StrapBeltR_{sz:F1}", P( 0.55f, strapY, sz), strapR, strapLen, mStrap);
            BuildStrapRing($"RingR_{sz:F1}", P( 0.55f, strapY - strapLen*0.5f - ringH*0.5f, sz),
                ringW, ringH, ringTh, mRing);
        }

        Debug.Log("[TrainBuilder] 手すり・吊り革配置完了");
    }

    // ──────────────────────────────────────
    // 中吊り広告・壁広告・天井LCD・座席仕切り
    // ──────────────────────────────────────
    static void BuildAdvertisements()
    {
        var mAdFrame  = FlatMat(new Color(0.88f, 0.88f, 0.88f));
        var mAdPaper  = FlatMat(new Color(0.95f, 0.94f, 0.92f));
        var mLcd      = FlatMat(new Color(0.05f, 0.10f, 0.20f));
        mLcd.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f));
        mLcd.EnableKeyword("_EMISSION");
        var mDivider  = FlatMat(new Color(0.90f, 0.90f, 0.92f));

        // ── 壁上部の広告パネル ──
        float[] adZs = { -8.2f, -6.5f, -4.0f, -0.5f, 1.5f, 4.0f, 6.5f, 8.2f };
        foreach (float az in adZs)
        {
            foreach (bool isLeft in new[] { true, false })
            {
                float wx = isLeft ? -CW + 0.03f : CW - 0.03f;
                float ox = isLeft ?  0.01f : -0.01f;
                string s = isLeft ? "L" : "R";
                Cube($"AdFrame{s}{az}", P(wx,       1.76f, az), S(0.04f, 0.28f, 0.48f), mAdFrame);
                Cube($"AdPaper{s}{az}", P(wx + ox,  1.76f, az), S(0.02f, 0.24f, 0.44f), mAdPaper);
            }
        }

        // ── 天井中央の LCD ディスプレイ ──
        float[] lcdZs = { -4.5f, 0f, 4.5f };
        foreach (float lz in lcdZs)
        {
            Cube($"LcdBody{lz}",   P(0, CH - 0.18f, lz), S(0.06f, 0.22f, 0.52f), mAdFrame);
            Cube($"LcdScreen{lz}", P(0, CH - 0.26f, lz), S(0.03f, 0.18f, 0.48f), mLcd);
            var ll = new GameObject($"LcdLight{lz}");
            ll.transform.position = new Vector3(0f, CH - 0.32f, lz);
            var lc = ll.AddComponent<Light>();
            lc.type = LightType.Point; lc.range = 1.8f;
            lc.intensity = 0.4f; lc.color = new Color(0.3f, 0.5f, 1.0f);
        }

        // ── 座席仕切り（白いアームレスト型、小さめ）──
        // 実物: 座席の区切りに立つ薄いプラスチック板（高さ約20cm、幅14cm程度）
        // 座面高さ 0.44m の上に立てる → Y中心 = 0.44 + 0.10 = 0.54m
        float[] dvZs = { -7.4f, -6.1f, -4.8f, -3.2f, -0.9f, 0.9f, 3.2f, 4.8f, 6.1f, 7.4f };
        foreach (float dv in dvZs)
        {
            foreach (bool isLeft in new[] { true, false })
            {
                // 壁から内側にずらして座席端に置く
                float bx = isLeft ? -(CW - 0.18f) : (CW - 0.18f);
                // 本体（小さな仕切り板）
                Cube($"Div{(isLeft?"L":"R")}{dv}",
                    P(bx, 0.54f, dv), S(0.16f, 0.20f, 0.030f), mDivider);
                // 上部丸み表現（少し幅広の薄い板を重ねる）
                Cube($"DivTop{(isLeft?"L":"R")}{dv}",
                    P(bx, 0.65f, dv), S(0.14f, 0.06f, 0.025f), mDivider);
            }
        }

        Debug.Log("[TrainBuilder] 広告・LCD・仕切り配置完了");
    }

    // ──────────────────────────────────────
    // プレイヤー
    // ──────────────────────────────────────
    static GameObject BuildPlayer(UIRefs ui)
    {
        var player = new GameObject("Player");
        player.tag = "Player";

        var cc = player.AddComponent<CharacterController>();
        cc.height=1.8f; cc.radius=0.28f;
        cc.stepOffset=0.3f; cc.skinWidth=0.08f;
        cc.center=new Vector3(0,0.9f,0);

        var pc = player.AddComponent<PlayerController>();

        // カメラ（揺れはサブオブジェクトに）
        var pivot = new GameObject("CamPivot");
        pivot.transform.SetParent(player.transform);
        pivot.transform.localPosition    = new Vector3(0, 0.84f, 0);
        pivot.transform.localEulerAngles = Vector3.zero;
        pivot.AddComponent<TrainSway>();

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.backgroundColor = new Color(0.01f,0.01f,0.02f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 72f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.SetParent(pivot.transform);
        camGO.transform.localPosition    = Vector3.zero;
        camGO.transform.localEulerAngles = Vector3.zero;

        var soPC = new SerializedObject(pc);
        soPC.FindProperty("cameraTransform").objectReferenceValue = camGO.transform;
        soPC.ApplyModifiedProperties();

        var ia = player.AddComponent<InteractionSystem>();
        var soIA = new SerializedObject(ia);
        soIA.FindProperty("playerCamera").objectReferenceValue       = cam;
        soIA.FindProperty("interactPromptText").objectReferenceValue = ui.promptText;
        soIA.ApplyModifiedProperties();

        // 通路中央、左壁ドア（-X方向）を向く
        player.transform.position    = new Vector3(0.3f, 0.9f, -3.5f);
        player.transform.eulerAngles = new Vector3(0, 270f, 0);

        // 幽霊乗客（2体）
        SpawnGhost(true,  -7.0f);
        SpawnGhost(false,  5.5f);

        return player;
    }

    static void SpawnGhost(bool isLeft, float z)
    {
        float bx = isLeft ? -(CW - 0.22f) : (CW - 0.22f);
        var g = new GameObject($"GhostPassenger_{(isLeft?"L":"R")}_{z}");
        g.transform.position = new Vector3(bx, 0.44f, z);
        Sub(g,"Body", P(0,0.36f,0),  S(0.28f,0.54f,0.15f),mGhost);
        Sub(g,"Head", P(0,0.76f,0),  S(0.20f,0.20f,0.18f),mGhost);
        Sub(g,"ArmL", P(-0.19f,0.36f,0),S(0.08f,0.44f,0.10f),mGhost);
        Sub(g,"ArmR", P( 0.19f,0.36f,0),S(0.08f,0.44f,0.10f),mGhost);
        Sub(g,"LegL", P(-0.08f,-0.03f,0.08f),S(0.10f,0.25f,0.22f),mGhost);
        Sub(g,"LegR", P( 0.08f,-0.03f,0.08f),S(0.10f,0.25f,0.22f),mGhost);
        g.AddComponent<GhostPassenger>();
    }

    // ──────────────────────────────────────
    // UI・Volume・配線は省略なし
    // ──────────────────────────────────────
    struct UIRefs
    {
        public CanvasGroup fadeCG;
        public TextMeshProUGUI annoText, stationText, promptText;
    }

    static UIRefs BuildUI()
    {
        var r = new UIRefs();
        var cv = new GameObject("UI Canvas");
        cv.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        cv.AddComponent<CanvasScaler>(); cv.AddComponent<GraphicRaycaster>();

        var fp = MakePanel(cv,"FadePanel",Color.black);
        r.fadeCG = fp.AddComponent<CanvasGroup>(); r.fadeCG.alpha=1f;

        r.annoText   = TMP(cv,"AnnouncementText",
            new Vector2(0.05f,0.04f),new Vector2(0.95f,0.26f),22,new Color(0.95f,0.92f,0.78f));
        r.stationText= TMP(cv,"StationNameText",
            new Vector2(0.20f,0.68f),new Vector2(0.80f,0.84f),46,new Color(0.95f,0.90f,0.60f));
        r.stationText.fontStyle=FontStyles.Bold;
        r.promptText = TMP(cv,"InteractPrompt",
            new Vector2(0.25f,0.22f),new Vector2(0.75f,0.30f),18,Color.white);
        r.promptText.gameObject.SetActive(false);

        var ret=new GameObject("Reticle"); ret.transform.SetParent(cv.transform,false);
        ret.AddComponent<Image>().color=new Color(1,1,1,0.55f);
        var rr=ret.GetComponent<RectTransform>();
        rr.anchorMin=rr.anchorMax=new Vector2(0.5f,0.5f); rr.sizeDelta=new Vector2(7,7);
        return r;
    }

    static GameObject MakePanel(GameObject cv,string nm,Color c)
    {
        var go=new GameObject(nm); go.transform.SetParent(cv.transform,false);
        go.AddComponent<Image>().color=c;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
        return go;
    }

    static TextMeshProUGUI TMP(GameObject cv,string nm,Vector2 amin,Vector2 amax,float sz,Color col)
    {
        var go=new GameObject(nm); go.transform.SetParent(cv.transform,false);
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=""; t.fontSize=sz; t.alignment=TextAlignmentOptions.Center; t.color=col;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=amin; rt.anchorMax=amax; rt.offsetMin=rt.offsetMax=Vector2.zero;
        return t;
    }

    static GameObject BuildVolume()
    {
        if (!System.IO.Directory.Exists("Assets/Settings"))
            System.IO.Directory.CreateDirectory("Assets/Settings");
        var vo=new GameObject("Global Volume");
        var v=vo.AddComponent<Volume>(); v.isGlobal=true;
        var p=ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p,"Assets/Settings/TrainVolumeProfile.asset");
        var vig=p.Add<Vignette>(); vig.intensity.value=0.28f; vig.intensity.overrideState=true;
        var ca=p.Add<ChromaticAberration>(); ca.intensity.value=0f; ca.intensity.overrideState=true;
        var fg=p.Add<FilmGrain>(); fg.intensity.value=0.10f; fg.intensity.overrideState=true;
        var adj=p.Add<ColorAdjustments>();
        adj.saturation.value=-12f; adj.saturation.overrideState=true;
        adj.postExposure.value=-0.15f; adj.postExposure.overrideState=true;
        v.profile=p; EditorUtility.SetDirty(p);
        return vo;
    }

    static void WireAll(GameObject player,UIRefs ui,GameObject volObj,GameObject reset,GameObject door)
    {
        var acGO=new GameObject("AnomalyController"); var ac=acGO.AddComponent<AnomalyController>();
        {var s=new SerializedObject(ac); s.FindProperty("globalVolume").objectReferenceValue=volObj.GetComponent<Volume>(); s.ApplyModifiedProperties();}

        var amGO=new GameObject("AudioManager"); var am=amGO.AddComponent<AudioManager>();
        {var ag=new GameObject("Ambient"); ag.transform.SetParent(amGO.transform);
         var aas=ag.AddComponent<AudioSource>(); aas.loop=true; aas.volume=0.6f;
         var ng=new GameObject("Noise"); ng.transform.SetParent(amGO.transform);
         var nas=ng.AddComponent<AudioSource>(); nas.loop=true; nas.volume=0f;
         var s=new SerializedObject(am);
         s.FindProperty("ambientSource").objectReferenceValue=aas;
         s.FindProperty("noiseSource").objectReferenceValue=nas;
         s.ApplyModifiedProperties();}

        var lmGO=new GameObject("LogManager"); var lm=lmGO.AddComponent<LogManager>();

        var lcGO=new GameObject("LoopController"); var lc=lcGO.AddComponent<LoopController>();
        {var s=new SerializedObject(lc);
         s.FindProperty("resetPoint").objectReferenceValue=reset.transform;
         s.FindProperty("playerTransform").objectReferenceValue=player.transform;
         s.FindProperty("fadeCanvasGroup").objectReferenceValue=ui.fadeCG;
         s.ApplyModifiedProperties();}

        var gmGO=new GameObject("GameManager"); var gm=gmGO.AddComponent<GameManager>();
        {var s=new SerializedObject(gm);
         s.FindProperty("anomalyController").objectReferenceValue=ac;
         s.FindProperty("audioManager").objectReferenceValue=am;
         s.FindProperty("logManager").objectReferenceValue=lm;
         s.FindProperty("loopController").objectReferenceValue=lc;
         s.ApplyModifiedProperties();}

        var tcGO=new GameObject("TrainController"); var tc=tcGO.AddComponent<TrainController>();
        {var s=new SerializedObject(tc);
         s.FindProperty("doorObject").objectReferenceValue=door;
         s.FindProperty("announcementText").objectReferenceValue=ui.annoText;
         s.FindProperty("stationNameText").objectReferenceValue=ui.stationText;
         s.ApplyModifiedProperties();}

        new GameObject("SceneFlowManager").AddComponent<SceneFlowManager>();
    }

    // ──────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────
    static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
    static Vector3 S(float x,float y,float z)=>new Vector3(x,y,z);

    static GameObject Cube(string name,Vector3 pos,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name; o.transform.position=pos; o.transform.localScale=scale;
        o.GetComponent<Renderer>().material=mat;
        return o;
    }

    // 吊り革リング（▽型の三角形、実物の吊り革に近い形）
    // 上辺が水平、左右の辺が下に向かって中央で合流
    static void BuildStrapRing(string name, Vector3 center,
                                float ringW, float ringH, float tubeR, Material mat)
    {
        float hw = ringW * 0.5f;
        float hh = ringH * 0.5f;
        // 脚の長さとZ軸からの傾き角（斜辺）
        float legLen   = Mathf.Sqrt(ringH * ringH + hw * hw);
        float legAngle = Mathf.Atan2(hw, ringH) * Mathf.Rad2Deg;

        // 上辺（水平バー）
        Cyl($"{name}_Top", center + Vector3.up * hh,
            tubeR, ringW, mat, new Vector3(90, 0, 0));
        // 左脚（上左→下中央、X軸を-方向に傾ける）
        Cyl($"{name}_L", center + new Vector3(0, 0, -hw * 0.5f),
            tubeR, legLen, mat, new Vector3(-legAngle, 0, 0));
        // 右脚（上右→下中央）
        Cyl($"{name}_R", center + new Vector3(0, 0, hw * 0.5f),
            tubeR, legLen, mat, new Vector3(legAngle, 0, 0));
    }

    // Cylinder ヘルパー（縦方向が Y 軸、radius は半径、height は全長）
    // euler を指定すると横倒し・斜めにできる
    static GameObject Cyl(string name, Vector3 pos, float radius, float height, Material mat,
                           Vector3 euler = default)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        o.name = name;
        o.transform.position    = pos;
        // Unity Cylinder: デフォルト高さ2、半径0.5 → scale (r*2, h/2, r*2)
        o.transform.localScale  = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        o.transform.eulerAngles = euler;
        o.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(o.GetComponent<Collider>());
        return o;
    }

    static void Sub(GameObject parent,string name,Vector3 lpos,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name; o.transform.SetParent(parent.transform);
        o.transform.localPosition=lpos; o.transform.localScale=scale;
        o.GetComponent<Renderer>().material=mat;
        Object.DestroyImmediate(o.GetComponent<Collider>());
    }
}
