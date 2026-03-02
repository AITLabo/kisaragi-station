using UnityEngine;
using UnityEditor;
using TMPro;

// Unity メニュー「Kisaragi > Build Effects」で
// 心理演出・DebugUI・ProceduralAudio を一括セットアップする
public class KisaragiEffectsBuilder
{
    [MenuItem("Kisaragi/Build Effects (心理演出・UI一括セットアップ)")]
    public static void BuildEffects()
    {
        // ──────────────────────────────────────
        // PsychologicalEffects オブジェクト
        // ──────────────────────────────────────
        GameObject psyGO = GetOrCreate("PsychologicalEffects");
        PsychologicalEffects psy = AddIfMissing<PsychologicalEffects>(psyGO);

        SerializedObject psySO = new SerializedObject(psy);

        // Player 参照
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            psySO.FindProperty("playerTransform").objectReferenceValue  = player.transform;
            psySO.FindProperty("footstepSource").objectReferenceValue   =
                player.GetComponentInChildren<AudioSource>();
        }

        // ささやき声 AudioSource を追加
        GameObject whisperGO = GetOrCreateChild(psyGO, "WhisperSource");
        AudioSource whisper  = AddIfMissing<AudioSource>(whisperGO);
        whisper.loop         = false;
        whisper.playOnAwake  = false;
        whisper.volume       = 0.4f;
        psySO.FindProperty("whisperSource").objectReferenceValue = whisper;
        psySO.ApplyModifiedProperties();

        // ──────────────────────────────────────
        // 環境音 AudioSource をプレイヤーに追加
        // ──────────────────────────────────────
        if (player != null)
        {
            GameObject ambientGO = GetOrCreateChild(player, "FootstepAudio");
            AudioSource footAudio = AddIfMissing<AudioSource>(ambientGO);
            footAudio.loop       = false;
            footAudio.playOnAwake = false;
            footAudio.volume     = 0.6f;

            // PlayerController に footstepSource を設定
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject pcSO = new SerializedObject(pc);
                pcSO.FindProperty("footstepSource").objectReferenceValue = footAudio;
                pcSO.ApplyModifiedProperties();
            }

            // PsychologicalEffects にも設定
            SerializedObject psySO2 = new SerializedObject(psy);
            psySO2.FindProperty("footstepSource").objectReferenceValue = footAudio;
            psySO2.ApplyModifiedProperties();
        }

        // ──────────────────────────────────────
        // DebugUI をCanvas に追加
        // ──────────────────────────────────────
        GameObject canvasGO = GameObject.Find("UI Canvas");
        if (canvasGO != null)
        {
            GameObject debugGO = GetOrCreateChild(canvasGO, "DebugUI");
            if (debugGO.GetComponent<RectTransform>() == null)
                debugGO.AddComponent<RectTransform>();

            RectTransform drt = debugGO.GetComponent<RectTransform>();
            drt.anchorMin        = new Vector2(0f, 1f);
            drt.anchorMax        = new Vector2(0f, 1f);
            drt.anchoredPosition = new Vector2(10f, -10f);
            drt.sizeDelta        = new Vector2(220f, 70f);
            drt.pivot            = new Vector2(0f, 1f);

            TextMeshProUGUI debugText = AddIfMissing<TextMeshProUGUI>(debugGO);
            debugText.fontSize  = 13f;
            debugText.color     = new Color(0f, 1f, 0.5f, 0.8f);
            debugText.alignment = TextAlignmentOptions.TopLeft;
            debugText.text      = "Debug";

            DistortionDebugUI debugUI = AddIfMissing<DistortionDebugUI>(debugGO);
            SerializedObject debugSO  = new SerializedObject(debugUI);
            debugSO.FindProperty("debugText").objectReferenceValue = debugText;
            debugSO.ApplyModifiedProperties();
        }

        // ──────────────────────────────────────
        // AnomalyController に PsychologicalEffects を連携
        // ──────────────────────────────────────
        // GameManager から distortion 変化時に PsychologicalEffects も呼ぶよう
        // GameManager の参照に追加する
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm != null)
            {
                SerializedObject gmSO = new SerializedObject(gm);
                // psychologicalEffects フィールドは GameManager に追加が必要
                // → 今回は PsychologicalEffects が Update() で自律監視するので不要
                gmSO.ApplyModifiedProperties();
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("=== Effects Build 完了 ===");
        EditorUtility.DisplayDialog("完了",
            "心理演出・DebugUI のセットアップが完了しました！\n\n" +
            "次に：\n" +
            "・Kisaragi > Build Signs で看板テキストを追加\n" +
            "・Ctrl+S で保存\n" +
            "・Play して歪み値を Inspector から上げてみてください", "OK");
    }

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

    static T AddIfMissing<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }
}
