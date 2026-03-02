using UnityEngine;
using UnityEditor;

// Assets/Models/ 内の FBX をインポートするとき、
// Blender のデフォルトカメラ・ライトを自動削除する。
// → Build All でシーンを再構築しても余分なカメラが混入しない。
public class KisaragiFbxPostprocessor : AssetPostprocessor
{
    void OnPostprocessModel(GameObject g)
    {
        // Assets/Models/ 内のファイルだけ処理
        if (!assetPath.StartsWith("Assets/Models/")) return;

        // Camera コンポーネントを持つ GameObject を削除
        foreach (var cam in g.GetComponentsInChildren<Camera>(true))
        {
            Debug.Log($"[FbxPostprocessor] カメラを除去: {cam.gameObject.name} ({assetPath})");
            Object.DestroyImmediate(cam.gameObject);
        }

        // Light コンポーネントを持つ GameObject を削除（Blender デフォルトライト）
        foreach (var lt in g.GetComponentsInChildren<Light>(true))
        {
            Debug.Log($"[FbxPostprocessor] ライトを除去: {lt.gameObject.name} ({assetPath})");
            Object.DestroyImmediate(lt.gameObject);
        }
    }
}
