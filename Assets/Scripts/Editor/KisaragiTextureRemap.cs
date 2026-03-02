using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Unity メニュー「Kisaragi > Remap Textures (強制)」
// SubwayModelSet のマテリアルに対して _BaseMap を強制的に再設定する
// テクスチャ命名規則: T_<マテリアル名>_BC.png が Base Color (BaseMap) に対応
public class KisaragiTextureRemap
{
    [MenuItem("Kisaragi/Remap Textures (テクスチャ強制再設定)")]
    public static void RemapTextures()
    {
        string texFolder = "Assets/SubwayModelSet/Textures";
        string matFolder = "Assets/SubwayModelSet/Materials";

        // テクスチャを全部ロード（名前→Texture2Dのマップ）
        string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texFolder });
        Dictionary<string, Texture2D> texDict = new Dictionary<string, Texture2D>();

        foreach (string guid in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null && !texDict.ContainsKey(tex.name.ToLower()))
                texDict[tex.name.ToLower()] = tex;
        }

        Debug.Log($"[TextureRemap] テクスチャ {texDict.Count} 個をロード");
        foreach (var kv in texDict)
            Debug.Log($"  テクスチャ: {kv.Key}");

        // マテリアルを全部処理
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { matFolder });
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogError("[TextureRemap] URP Lit シェーダーが見つかりません！");
            return;
        }

        int remapped = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // シェーダーをURPに強制設定
            mat.shader = urpLit;

            string matName = mat.name; // 例: "BasementFloor", "Tile01"

            // T_<MatName>_BC を優先検索（Base Color テクスチャ命名規則）
            string bcKey = $"t_{matName.ToLower()}_bc";
            Texture2D baseMap = null;

            if (texDict.TryGetValue(bcKey, out Texture2D bc))
            {
                baseMap = bc;
            }
            else
            {
                // フォールバック: マテリアル名で部分一致検索（_BC を含むもの優先）
                foreach (var kv in texDict)
                {
                    if (kv.Key.Contains(matName.ToLower()) && kv.Key.EndsWith("_bc"))
                    {
                        baseMap = kv.Value;
                        break;
                    }
                }

                // さらにフォールバック: _BC なしで部分一致
                if (baseMap == null)
                {
                    foreach (var kv in texDict)
                    {
                        if (kv.Key.Contains(matName.ToLower()))
                        {
                            baseMap = kv.Value;
                            break;
                        }
                    }
                }
            }

            if (baseMap != null)
            {
                mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(mat);
                remapped++;
                Debug.Log($"[TextureRemap] ✓ {mat.name} → BaseMap = {baseMap.name}");
            }
            else
            {
                // テクスチャが見つからない場合はコンクリート色
                mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f));
                EditorUtility.SetDirty(mat);
                Debug.LogWarning($"[TextureRemap] △ {mat.name} → テクスチャ未発見。グレーに設定。(探したキー: {bcKey})");
            }

            // 法線マップも設定
            string normalKey = $"t_{matName.ToLower()}_n";
            if (texDict.TryGetValue(normalKey, out Texture2D normalTex))
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            // AOマップも設定
            string aoKey = $"t_{matName.ToLower()}_ao";
            if (texDict.TryGetValue(aoKey, out Texture2D aoTex))
            {
                mat.SetTexture("_OcclusionMap", aoTex);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TextureRemap] 完了: {remapped}/{matGuids.Length} 個のマテリアルにBaseMapを設定");
        EditorUtility.DisplayDialog("Texture Remap 完了",
            $"{remapped} / {matGuids.Length} 個のマテリアルにテクスチャを設定しました。\nConsole で詳細を確認してください。", "OK");
    }
}
