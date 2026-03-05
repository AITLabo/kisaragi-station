using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// Unity メニュー「Kisaragi > Build Reticle」で照準UI・インタラクトプロンプトを自動生成
// InteractionSystem コンポーネントも Player に追加し、全参照を接続する
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
            Debug.LogError("[Reticle] UI Canvas が見つかりません！先に Station Prototype を実行してください。");
            EditorUtility.DisplayDialog("エラー",
                "UI Canvas が見つかりません。\n先に Station Prototype ビルドを実行してください。", "OK");
            return;
        }

        // ── Player に InteractionSystem を追加（なければ） ──
        GameObject player = GameObject.Find("Player");
        InteractionSystem iSys = null;
        if (player != null)
        {
            iSys = player.GetComponent<InteractionSystem>();
            if (iSys == null)
            {
                iSys = player.AddComponent<InteractionSystem>();
                Debug.Log("[Reticle] Player に InteractionSystem を追加しました");
            }
        }
        else
        {
            Debug.LogWarning("[Reticle] Player が見つかりません。InteractionSystem の参照設定をスキップします。");
        }

        // ── レティクル（画面中央の十字）TMP_Text で確実に描画 ──
        GameObject reticleGO = GetOrCreateChild(canvasGO, "Reticle");
        RectTransform rt = GetOrAddComponent<RectTransform>(reticleGO);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(40f, 40f);
        rt.anchoredPosition = Vector2.zero;

        // Image は sprite 未設定だと描画されないため TMP_Text で代替
        var reticleTxt = GetOrAddComponent<TextMeshProUGUI>(reticleGO);
        reticleTxt.text      = "+";   // 半角（全角だとフォント未設定で非表示になる）
        reticleTxt.fontSize  = 22f;
        reticleTxt.color     = new Color(1f, 1f, 1f, 0.8f);
        reticleTxt.alignment = TextAlignmentOptions.Center;
        reticleTxt.fontStyle = FontStyles.Bold;

        // Image は TMP_Text と同一 GO に追加不可のため使用しない（reticleImage 参照は null のまま）

        // ── InteractPrompt（インタラクト促進テキスト） ──
        // 既存を探すか、Canvas の子として新規作成
        GameObject promptGO = GameObject.Find("InteractPrompt")
                           ?? GetOrCreateChild(canvasGO, "InteractPrompt");

        RectTransform promptRT = GetOrAddComponent<RectTransform>(promptGO);
        promptRT.anchorMin       = new Vector2(0.5f, 0.5f);
        promptRT.anchorMax       = new Vector2(0.5f, 0.5f);
        promptRT.anchoredPosition = new Vector2(0f, -40f); // レティクルの少し下
        promptRT.sizeDelta       = new Vector2(400f, 40f);

        TextMeshProUGUI promptTxt = GetOrAddComponent<TextMeshProUGUI>(promptGO);
        promptTxt.fontSize          = 14f;
        promptTxt.alignment         = TextAlignmentOptions.Center;
        promptTxt.color             = new Color(1f, 0.9f, 0.7f, 1f);
        promptTxt.text              = "[ E ]  調べる";
        promptTxt.enableWordWrapping = false;
        promptGO.SetActive(false); // 初期は非表示

        // ── InteractionSystem に reticleImage / interactPromptText / playerCamera を設定 ──
        if (iSys != null)
        {
            var so = new SerializedObject(iSys);

            so.FindProperty("reticleText").objectReferenceValue        = reticleTxt;
            // reticleImage は使用しない（TMP_Text に置き換え済み）
            so.FindProperty("interactPromptText").objectReferenceValue = promptTxt;

            // playerCamera: MainCamera を自動検索
            var cam = Camera.main;
            if (cam == null)
            {
                // tag が付いていない場合は名前で検索
                var camGO = GameObject.Find("Main Camera");
                if (camGO != null) cam = camGO.GetComponent<Camera>();
            }
            if (cam != null)
                so.FindProperty("playerCamera").objectReferenceValue = cam;

            so.ApplyModifiedProperties();
            Debug.Log($"[Reticle] InteractionSystem 参照設定完了:" +
                      $"\n  reticleText  = {(reticleTxt != null ? reticleTxt.name : "NULL")}" +
                      $"\n  promptText   = {(promptTxt  != null ? promptTxt.name  : "NULL")}" +
                      $"\n  camera       = {(cam        != null ? cam.name        : "NULL")}");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            "照準UI・インタラクトプロンプトを生成しました！\n\n" +
            "生成されたもの:\n" +
            "  ・Reticle (画面中央の白い点)\n" +
            "  ・InteractPrompt (調べるテキスト)\n" +
            "  ・InteractionSystem → Player に自動追加\n\n" +
            "操作: 画面中央をオブジェクトに合わせて [ E ] キー\n\n" +
            "Ctrl+S で保存してください。", "OK");
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
