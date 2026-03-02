using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Unity メニュー「Kisaragi > Fix References」で全参照を再設定する
public class KisaragiRefFixer
{
    [MenuItem("Kisaragi/Fix References (参照を再設定)")]
    public static void FixReferences()
    {
        // ──────────────────────────────────────
        // AudioManager の AudioSource 参照を修正
        // ──────────────────────────────────────
        GameObject amGO = GameObject.Find("AudioManager");
        if (amGO != null)
        {
            AudioManager am = amGO.GetComponent<AudioManager>();
            if (am != null)
            {
                SerializedObject so = new SerializedObject(am);

                Transform ambient      = amGO.transform.Find("AmbientSource");
                Transform noise        = amGO.transform.Find("NoiseSource");
                Transform announcement = amGO.transform.Find("AnnouncementSource");

                // 子オブジェクトがなければ作成
                if (ambient == null)      ambient      = CreateAudioChild(amGO, "AmbientSource").transform;
                if (noise == null)        noise        = CreateAudioChild(amGO, "NoiseSource").transform;
                if (announcement == null) announcement = CreateAudioChild(amGO, "AnnouncementSource").transform;

                so.FindProperty("ambientSource").objectReferenceValue      = ambient.GetComponent<AudioSource>();
                so.FindProperty("noiseSource").objectReferenceValue        = noise.GetComponent<AudioSource>();
                so.FindProperty("announcementSource").objectReferenceValue = announcement.GetComponent<AudioSource>();
                so.ApplyModifiedProperties();
                Debug.Log("[Fix] AudioManager の参照を修正しました");
            }
        }

        // ──────────────────────────────────────
        // GameManager の参照を修正
        // ──────────────────────────────────────
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm != null)
            {
                SerializedObject so = new SerializedObject(gm);

                GameObject ac = GameObject.Find("AnomalyController");
                GameObject am = GameObject.Find("AudioManager");
                GameObject lc = GameObject.Find("LoopController");

                if (ac != null) so.FindProperty("anomalyController").objectReferenceValue = ac.GetComponent<AnomalyController>();
                if (am != null) so.FindProperty("audioManager").objectReferenceValue      = am.GetComponent<AudioManager>();
                so.FindProperty("logManager").objectReferenceValue = gmGO.GetComponent<LogManager>();
                if (lc != null) so.FindProperty("loopController").objectReferenceValue    = lc.GetComponent<LoopController>();
                so.ApplyModifiedProperties();
                Debug.Log("[Fix] GameManager の参照を修正しました");
            }
        }

        // ──────────────────────────────────────
        // AnomalyController の参照を修正
        // ──────────────────────────────────────
        GameObject acGO = GameObject.Find("AnomalyController");
        if (acGO != null)
        {
            AnomalyController ac = acGO.GetComponent<AnomalyController>();
            if (ac != null)
            {
                SerializedObject so = new SerializedObject(ac);

                // Volume を名前で探す → 型で探す → なければ新規作成
                Volume vol = null;
                GameObject volGO = GameObject.Find("Global Volume");
                if (volGO != null) vol = volGO.GetComponent<Volume>();
                if (vol == null)   vol = Object.FindObjectOfType<Volume>();
                if (vol == null)
                {
                    GameObject newVolGO = new GameObject("Global Volume");
                    vol = newVolGO.AddComponent<Volume>();
                    vol.isGlobal = true;
                    vol.profile  = ScriptableObject.CreateInstance<VolumeProfile>();
                    if (!vol.profile.Has<Vignette>())            vol.profile.Add<Vignette>();
                    if (!vol.profile.Has<ChromaticAberration>()) vol.profile.Add<ChromaticAberration>();
                    if (!vol.profile.Has<FilmGrain>())           vol.profile.Add<FilmGrain>();
                    if (!vol.profile.Has<ColorAdjustments>())    vol.profile.Add<ColorAdjustments>();
                    Debug.Log("[Fix] Global Volume を新規作成しました");
                }

                Light dirLight = Object.FindObjectOfType<Light>();
                so.FindProperty("globalVolume").objectReferenceValue         = vol;
                if (dirLight != null) so.FindProperty("mainDirectionalLight").objectReferenceValue = dirLight;
                so.ApplyModifiedProperties();
                Debug.Log("[Fix] AnomalyController の参照を修正しました");
            }
        }

        // ──────────────────────────────────────
        // LoopController の参照を修正
        // ──────────────────────────────────────
        GameObject lcGO = GameObject.Find("LoopController");
        if (lcGO != null)
        {
            LoopController lc = lcGO.GetComponent<LoopController>();
            if (lc != null)
            {
                SerializedObject so = new SerializedObject(lc);
                GameObject player     = GameObject.Find("Player");
                GameObject resetPoint = GameObject.Find("ResetPoint");
                GameObject fadePanel  = GameObject.Find("FadePanel");

                if (player != null)     so.FindProperty("playerTransform").objectReferenceValue = player.transform;
                if (resetPoint != null) so.FindProperty("resetPoint").objectReferenceValue      = resetPoint.transform;
                if (fadePanel != null)
                {
                    CanvasGroup cg = fadePanel.GetComponent<CanvasGroup>();
                    if (cg != null) so.FindProperty("fadeCanvasGroup").objectReferenceValue = cg;
                }
                so.ApplyModifiedProperties();
                Debug.Log("[Fix] LoopController の参照を修正しました");
            }
        }

        // ──────────────────────────────────────
        // InteractionSystem の参照を修正
        // ──────────────────────────────────────
        GameObject playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            InteractionSystem iSys = playerGO.GetComponent<InteractionSystem>();
            if (iSys != null)
            {
                SerializedObject so = new SerializedObject(iSys);
                GameObject camGO   = GameObject.Find("Main Camera");
                GameObject promptGO = GameObject.Find("InteractPrompt");

                if (camGO != null)
                    so.FindProperty("playerCamera").objectReferenceValue = camGO.GetComponent<Camera>();
                if (promptGO != null)
                {
                    var txt = promptGO.GetComponent<TMPro.TextMeshProUGUI>();
                    if (txt != null) so.FindProperty("interactPromptText").objectReferenceValue = txt;
                }

                // layerMask も再設定
                int layer = LayerMask.NameToLayer("Interactable");
                if (layer != -1) so.FindProperty("interactableLayer").intValue = 1 << layer;

                so.ApplyModifiedProperties();
                Debug.Log("[Fix] InteractionSystem の参照を修正しました");
            }

            // PlayerController の参照
            PlayerController pc = playerGO.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject so = new SerializedObject(pc);
                GameObject camGO = GameObject.Find("Main Camera");
                if (camGO != null) so.FindProperty("cameraTransform").objectReferenceValue = camGO.transform;
                so.ApplyModifiedProperties();
                Debug.Log("[Fix] PlayerController の参照を修正しました");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            "全参照の再設定が完了しました！\n\nCtrl+S で保存してください。", "OK");
    }

    static GameObject CreateAudioChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.AddComponent<AudioSource>();
        return child;
    }
}
