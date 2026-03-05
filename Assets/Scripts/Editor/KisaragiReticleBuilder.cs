using UnityEngine;
using UnityEditor;
using TMPro;

// Unity メニュー「Kisaragi > Build Reticle」で照準UI・インタラクトプロンプトを自動生成
public class KisaragiReticleBuilder
{
    [MenuItem("Kisaragi/Build Reticle (照準UI自動生成)")]
    public static void BuildReticle()
    {
        // ── UI Canvas を確認 ──
        GameObject canvasGO = GameObject.Find("UI Canvas");
        Debug.Log($"[Reticle] BuildReticle 開始 / UI Canvas = {(canvasGO != null ? canvasGO.name : "NULL")}");
        if (canvasGO == null)
        {
            Debug.LogError("[Reticle] UI Canvas が見つかりません！");
            EditorUtility.DisplayDialog("エラー", "UI Canvas が見つかりません。先に Station Prototype ビルドを実行してください。", "OK");
            return;
        }

        // ── Player に InteractionSystem と ReticleHUD を追加（なければ） ──
        GameObject player = GameObject.Find("Player");
        InteractionSystem iSys = null;
        ReticleHUD reticleHUD = null;
        if (player != null)
        {
            iSys      = player.GetComponent<InteractionSystem>() ?? player.AddComponent<InteractionSystem>();
            reticleHUD = player.GetComponent<ReticleHUD>()      ?? player.AddComponent<ReticleHUD>();
            Debug.Log($"[Reticle] Player に ReticleHUD を追加: {reticleHUD.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[Reticle] Player が見つかりません。");
        }

        // ── InteractPrompt ──
        GameObject promptGO = GameObject.Find("InteractPrompt") ?? GetOrCreateChild(canvasGO, "InteractPrompt");
        var promptRT = GetOrAddComponent<RectTransform>(promptGO);
        promptRT.anchorMin        = new Vector2(0.5f, 0.5f);
        promptRT.anchorMax        = new Vector2(0.5f, 0.5f);
        promptRT.anchoredPosition = new Vector2(0f, -40f);
        promptRT.sizeDelta        = new Vector2(400f, 40f);
        var promptTxt = GetOrAddComponent<TextMeshProUGUI>(promptGO);
        promptTxt.fontSize           = 14f;
        promptTxt.alignment          = TextAlignmentOptions.Center;
        promptTxt.color              = new Color(1f, 0.9f, 0.7f, 1f);
        promptTxt.text               = "[ E ]  調べる";
        promptTxt.enableWordWrapping = false;
        promptGO.SetActive(false);

        // ── InteractionSystem に参照を設定 ──
        if (iSys != null)
        {
            var so = new SerializedObject(iSys);
            if (reticleHUD != null)
                so.FindProperty("reticleHUD").objectReferenceValue = reticleHUD;
            so.FindProperty("interactPromptText").objectReferenceValue = promptTxt;
            var cam = Camera.main ?? GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (cam != null) so.FindProperty("playerCamera").objectReferenceValue = cam;
            so.ApplyModifiedProperties();
            Debug.Log($"[Reticle] 参照設定完了: reticleHUD={reticleHUD?.gameObject.name} cam={cam?.name}");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了", "照準UI・インタラクトプロンプトを生成しました！\nCtrl+S で保存してください。", "OK");
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}
