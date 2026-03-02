using UnityEngine;
using UnityEditor;

// Unity メニュー「Kisaragi > Set Interactable Layers」で
// シーン内の全 CorrectObject（IInteractable）に Interactable レイヤーを一括設定する
// ※ 旧バージョンは "Interactables" という名前のrootを探していたが、
//    KisaragiPrototypeAnomalyBuilder は "KisaragiAnomalies" を使うため修正
public class KisaragiLayerSetter
{
    [MenuItem("Kisaragi/Set Interactable Layers (レイヤー一括設定)")]
    public static void SetLayers()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer == -1)
        {
            EditorUtility.DisplayDialog("エラー",
                "'Interactable' レイヤーが見つかりません。\n\n" +
                "Edit > Project Settings > Tags and Layers を開いて\n" +
                "User Layer 8 ～ 31 のいずれかに 'Interactable' を追加してから\n" +
                "再実行してください。", "OK");
            return;
        }

        // "Interactables" や "KisaragiAnomalies" などのroot名に依存せず
        // CorrectObject コンポーネントを持つ全オブジェクトを対象にする
        CorrectObject[] allCO = Object.FindObjectsOfType<CorrectObject>(includeInactive: true);
        if (allCO.Length == 0)
        {
            EditorUtility.DisplayDialog("エラー",
                "CorrectObject を持つオブジェクトが見つかりません。\n\n" +
                "先に「Kisaragi > Build Prototype Anomalies」を実行して\n" +
                "インタラクション対象を配置してください。", "OK");
            return;
        }

        int count = 0;
        foreach (CorrectObject co in allCO)
        {
            co.gameObject.layer = layer;
            count++;
            Debug.Log($"レイヤー設定: {co.gameObject.name} → Interactable");
        }

        // InteractionSystem の layerMask も更新（Player 直下でなくても FindObjectOfType で検索）
        InteractionSystem iSys = Object.FindObjectOfType<InteractionSystem>();
        if (iSys != null)
        {
            SerializedObject so = new SerializedObject(iSys);
            so.FindProperty("interactableLayer").intValue = 1 << layer;
            so.ApplyModifiedProperties();
            Debug.Log($"InteractionSystem の layerMask を Layer {layer} (Interactable) に設定しました");
        }
        else
        {
            Debug.LogWarning("[LayerSetter] InteractionSystem が見つかりません。Player に AddComponent してください。");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            $"{count} 個のオブジェクトに Interactable レイヤーを設定しました！\n\n" +
            (iSys != null ? "InteractionSystem の layerMask も更新しました。\n\n" : "") +
            "Ctrl+S で保存してください。", "OK");
    }
}
