using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Kisaragi > Apply Subway Materials to Train Scene
// SubwayModelSet のマテリアルを開いているシーンの電車オブジェクトに一括適用する
// TrainScene が開いている状態で実行すること
public class KisaragiTrainMaterialApplier
{
    const string MAT_ROOT = "Assets/SubwayModelSet/Materials/";

    // オブジェクト名プレフィックス → SubwayModelSet マテリアル名 のマッピング
    // 複数のマテリアル候補を優先順に列挙。最初に見つかったものを使用する
    static readonly (string prefix, string[] matCandidates)[] RULES = new[]
    {
        // 床
        ("Floor",           new[]{ "BasementFloor",      "Concrete01" }),
        // 天井
        ("Ceiling",         new[]{ "Ceiling01",          "Concrete03" }),
        // 壁・腰板
        ("Wall",            new[]{ "Metal01",            "SubwayDetails02" }),
        ("Waist",           new[]{ "Tile01",             "Tile02" }),
        // 窓ガラス
        ("WG",              new[]{ "SubwayDetails01Glass","SubwayDetails03Glass" }),
        // 窓枠・棚
        ("WF",              new[]{ "Metal01",            "SubwayDetails02" }),
        ("WSill",           new[]{ "Metal01",            "SubwayDetails02" }),
        // ドアパネル
        ("DoorPanel",       new[]{ "SubwayDetails04NoEmissive","Metal01" }),
        ("DoorPanelL",      new[]{ "SubwayDetails04NoEmissive","Metal01" }),
        ("DoorPanelR",      new[]{ "SubwayDetails04NoEmissive","Metal01" }),
        ("DoorWin",         new[]{ "SubwayDetails01Glass","SubwayDetails03Glass" }),
        ("DoorFr",          new[]{ "Metal01",            "SubwayDetails02" }),
        // 運転台・連結部
        ("CabWall",         new[]{ "SubwayDetails02",   "Metal01" }),
        ("CabDoor",         new[]{ "SubwayDetails02",   "Metal01" }),
        ("ConnWall",        new[]{ "SubwayDetails02",   "Metal01" }),
        ("ConnDoor",        new[]{ "SubwayDetails02",   "Metal01" }),
        ("CabWin",          new[]{ "SubwayDetails01Glass","SubwayDetails03Glass" }),
        ("ConnWin",         new[]{ "SubwayDetails01Glass","SubwayDetails03Glass" }),
        // 手すり・吊り革
        ("VR",              new[]{ "Metal01" }),
        ("SB",              new[]{ "Metal01" }),    // StrapBar
        ("SR",              new[]{ "SubwayDetails03","Metal01" }),
        ("SH",              new[]{ "SubwayDetails03","Metal01" }),
        // 蛍光灯（Emissive）
        ("Lit",             new[]{ "SubwayDetails01Emissive","SubwayDetails03Emissive" }),
        ("PL",              new[]{ "SubwayDetails01Emissive","SubwayDetails03Emissive" }),
        // 中吊り広告
        ("Ad",              new[]{ "SubwayDetails01",   "SubwayDetails02" }),
        // 座席下カバー
        ("Sk",              new[]{ "Concrete02",        "Concrete01" }),
        ("Back",            new[]{ "SubwayDetails02",   "Concrete02" }),
        // 壁セグメント
        ("WallSeg",         new[]{ "Metal01",           "SubwayDetails02" }),
    };

    [MenuItem("Kisaragi/Apply Subway Materials to Train Scene (マテリアル適用)")]
    public static void ApplyMaterials()
    {
        // マテリアルをロード（キャッシュ）
        var matCache = new Dictionary<string, Material>();
        foreach (var (_, candidates) in RULES)
        {
            foreach (var name in candidates)
            {
                if (!matCache.ContainsKey(name))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(MAT_ROOT + name + ".mat");
                    if (m != null) matCache[name] = m;
                }
            }
        }

        // シーン内の全 Renderer を取得
        var renderers = Object.FindObjectsOfType<Renderer>();
        int applied   = 0;
        int skipped   = 0;

        foreach (var r in renderers)
        {
            string objName = r.gameObject.name;

            // GhostPassenger はスキップ（ランタイムで設定）
            if (objName.StartsWith("Ghost") || r.gameObject.GetComponent<GhostPassenger>() != null
                || IsChildOfGhost(r.transform))
            {
                skipped++;
                continue;
            }

            // RULES に従ってマテリアルを選択
            Material best = FindBestMaterial(objName, matCache);
            if (best != null)
            {
                r.sharedMaterial = best;
                applied++;
                Debug.Log($"[MatApplier] {objName} → {best.name}");
            }
        }

        // シーンをDirtyにする
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        string msg = $"マテリアル適用完了\n\n" +
                     $"適用: {applied} オブジェクト\n" +
                     $"スキップ: {skipped} オブジェクト\n\n" +
                     $"Ctrl+S でシーンを保存してください。";
        EditorUtility.DisplayDialog("完了", msg, "OK");
        Debug.Log($"[MatApplier] 適用: {applied}, スキップ: {skipped}");
    }

    [MenuItem("Kisaragi/List Subway Materials (マテリアル一覧確認)")]
    public static void ListMaterials()
    {
        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SubwayModelSet" });
        string list = $"SubwayModelSet マテリアル一覧 ({guids.Length}個)\n\n";
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            list += $"  • {name}\n";
        }
        EditorUtility.DisplayDialog("マテリアル一覧", list, "OK");
        Debug.Log(list);
    }

    // ── 内部処理 ──

    static Material FindBestMaterial(string objName, Dictionary<string, Material> cache)
    {
        foreach (var (prefix, candidates) in RULES)
        {
            if (!objName.StartsWith(prefix)) continue;

            foreach (var cand in candidates)
            {
                if (cache.TryGetValue(cand, out var mat))
                    return mat;
            }
        }
        return null;
    }

    static bool IsChildOfGhost(Transform t)
    {
        var p = t.parent;
        while (p != null)
        {
            if (p.GetComponent<GhostPassenger>() != null) return true;
            p = p.parent;
        }
        return false;
    }
}
