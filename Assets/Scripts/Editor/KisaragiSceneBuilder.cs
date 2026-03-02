using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

// Unity メニュー「Kisaragi > Build Scene」からシーンを自動構築する
public class KisaragiSceneBuilder
{
    [MenuItem("Kisaragi/Build Scene (全オブジェクト自動生成)")]
    public static void BuildScene()
    {
        // ──────────────────────────────────────
        // GameManager
        // ──────────────────────────────────────
        GameObject gm = GetOrCreate("GameManager");
        AddComponentIfMissing<GameManager>(gm);
        AddComponentIfMissing<LogManager>(gm);

        // ──────────────────────────────────────
        // AnomalyController
        // ──────────────────────────────────────
        GameObject ac = GetOrCreate("AnomalyController");
        AddComponentIfMissing<AnomalyController>(ac);

        // ──────────────────────────────────────
        // AudioManager
        // ──────────────────────────────────────
        GameObject am = GetOrCreate("AudioManager");
        AddComponentIfMissing<AudioManager>(am);
        EnsureAudioSource(am, "AmbientSource");
        EnsureAudioSource(am, "NoiseSource");
        EnsureAudioSource(am, "AnnouncementSource");

        // ──────────────────────────────────────
        // LoopController
        // ──────────────────────────────────────
        GameObject lc = GetOrCreate("LoopController");
        AddComponentIfMissing<LoopController>(lc);

        GameObject trigger = GetOrCreate("LoopTrigger");
        BoxCollider bc = AddComponentIfMissing<BoxCollider>(trigger);
        bc.isTrigger = true;
        bc.size = new Vector3(4f, 3f, 0.5f);
        trigger.transform.position = new Vector3(0, 1.5f, 20f);

        GameObject resetPoint = GetOrCreate("ResetPoint");
        resetPoint.transform.position = new Vector3(0, 0, 0);

        // ──────────────────────────────────────
        // Player
        // ──────────────────────────────────────
        GameObject player = GetOrCreate("Player");
        player.tag = "Player";
        CapsuleCollider oldCol = player.GetComponent<CapsuleCollider>();
        if (oldCol != null) Object.DestroyImmediate(oldCol);
        AddComponentIfMissing<CharacterController>(player);
        AddComponentIfMissing<PlayerController>(player);
        AddComponentIfMissing<InteractionSystem>(player);
        player.transform.position = new Vector3(0, 1f, 0);

        if (player.GetComponent<MeshFilter>() == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlayerVisual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.zero;
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
        }

        // ──────────────────────────────────────
        // Main Camera を Player の子に設定
        // ──────────────────────────────────────
        GameObject cam = GetOrCreate("Main Camera");
        cam.tag = "MainCamera";
        if (cam.GetComponent<Camera>() == null) cam.AddComponent<Camera>();
        if (cam.GetComponent<AudioListener>() == null) cam.AddComponent<AudioListener>();
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 0.7f, 0);
        cam.transform.localRotation = Quaternion.identity;

        // ──────────────────────────────────────
        // UI Canvas（フェード + プロンプト）
        // Canvas の子オブジェクトは SetParent 後に RectTransform が付く
        // ──────────────────────────────────────
        GameObject canvasGO = GetOrCreate("UI Canvas");
        Canvas canvas = AddComponentIfMissing<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = AddComponentIfMissing<CanvasScaler>(canvasGO);
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        AddComponentIfMissing<GraphicRaycaster>(canvasGO);

        // FadePanel
        GameObject fadePanel = GetOrCreateChild(canvasGO, "FadePanel");
        // RectTransform が無い場合は明示的に追加してから Image を付ける
        if (fadePanel.GetComponent<RectTransform>() == null)
            fadePanel.AddComponent<RectTransform>();
        Image img = AddComponentIfMissing<Image>(fadePanel);
        img.color = new Color(0, 0, 0, 0);
        RectTransform fadeRect = fadePanel.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        CanvasGroup cg = AddComponentIfMissing<CanvasGroup>(fadePanel);

        // InteractPrompt
        // TMP_Text は抽象クラスのため TextMeshProUGUI を使う
        GameObject prompt = GetOrCreateChild(canvasGO, "InteractPrompt");
        if (prompt.GetComponent<RectTransform>() == null)
            prompt.AddComponent<RectTransform>();
        TextMeshProUGUI promptText = AddComponentIfMissing<TextMeshProUGUI>(prompt);
        promptText.text = "[ E ]  調べる";
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 14;
        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.1f);
        promptRect.anchorMax = new Vector2(0.5f, 0.1f);
        promptRect.sizeDelta = new Vector2(200, 40);
        prompt.SetActive(false);

        // ──────────────────────────────────────
        // Global Volume
        // ──────────────────────────────────────
        Volume vol = Object.FindObjectOfType<Volume>();
        if (vol == null)
        {
            GameObject volGO = new GameObject("Global Volume");
            vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true;
        }
        if (vol.profile == null)
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!vol.profile.Has<Vignette>())            vol.profile.Add<Vignette>();
        if (!vol.profile.Has<ChromaticAberration>()) vol.profile.Add<ChromaticAberration>();
        if (!vol.profile.Has<FilmGrain>())           vol.profile.Add<FilmGrain>();
        if (!vol.profile.Has<ColorAdjustments>())    vol.profile.Add<ColorAdjustments>();

        // ──────────────────────────────────────
        // Inspector 参照をコードで設定
        // ──────────────────────────────────────
        GameManager gmComp = gm.GetComponent<GameManager>();
        SerializedObject gmSO = new SerializedObject(gmComp);
        gmSO.FindProperty("anomalyController").objectReferenceValue = ac.GetComponent<AnomalyController>();
        gmSO.FindProperty("audioManager").objectReferenceValue      = am.GetComponent<AudioManager>();
        gmSO.FindProperty("logManager").objectReferenceValue        = gm.GetComponent<LogManager>();
        gmSO.FindProperty("loopController").objectReferenceValue    = lc.GetComponent<LoopController>();
        gmSO.ApplyModifiedProperties();

        AnomalyController acComp = ac.GetComponent<AnomalyController>();
        SerializedObject acSO = new SerializedObject(acComp);
        acSO.FindProperty("globalVolume").objectReferenceValue = vol;
        Light dirLight = Object.FindObjectOfType<Light>();
        if (dirLight != null) acSO.FindProperty("mainDirectionalLight").objectReferenceValue = dirLight;
        acSO.ApplyModifiedProperties();

        LoopController lcComp = lc.GetComponent<LoopController>();
        SerializedObject lcSO = new SerializedObject(lcComp);
        lcSO.FindProperty("resetPoint").objectReferenceValue      = resetPoint.transform;
        lcSO.FindProperty("playerTransform").objectReferenceValue = player.transform;
        lcSO.FindProperty("fadeCanvasGroup").objectReferenceValue = cg;
        lcSO.ApplyModifiedProperties();

        InteractionSystem isComp = player.GetComponent<InteractionSystem>();
        SerializedObject isSO = new SerializedObject(isComp);
        isSO.FindProperty("playerCamera").objectReferenceValue       = cam.GetComponent<Camera>();
        isSO.FindProperty("interactPromptText").objectReferenceValue = promptText;
        isSO.ApplyModifiedProperties();

        PlayerController pcComp = player.GetComponent<PlayerController>();
        SerializedObject pcSO = new SerializedObject(pcComp);
        pcSO.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        pcSO.ApplyModifiedProperties();

        // ──────────────────────────────────────
        // シーン保存マーク
        // ──────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("=== Kisaragi Scene Build 完了 ===");

        EditorUtility.DisplayDialog(
            "Kisaragi Scene Builder",
            "シーン自動構築が完了しました！\n\nCtrl+S でシーンを保存してください。",
            "OK"
        );
    }

    // ──────────────────────────────────────
    // ユーティリティ
    // ──────────────────────────────────────
    static GameObject GetOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static T AddComponentIfMissing<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    static AudioSource EnsureAudioSource(GameObject go, string label)
    {
        Transform existing = go.transform.Find(label);
        if (existing != null) return existing.GetComponent<AudioSource>();
        GameObject child = new GameObject(label);
        child.transform.SetParent(go.transform);
        return child.AddComponent<AudioSource>();
    }
}
