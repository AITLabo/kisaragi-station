using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Kisaragi > Setup All Systems
// LoopManager / ActionManager / SaveManager / EventManager / EnvironmentManager / SceneFlowManager
// の生成・参照設定・AudioManager子オブジェクト・線路延長・マウス感度を全自動で行う
public class KisaragiSystemSetup
{
    [MenuItem("Kisaragi/Setup All Systems \u2013 \u5168\u30b7\u30b9\u30c6\u30e0\u81ea\u52d5\u8a2d\u5b9a")]
    public static void SetupAllSystems()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "全システム自動設定",
            "以下を自動で実行します:\n\n" +
            "1. LoopManager / ActionManager / SaveManager\n" +
            "   EventManager / EnvironmentManager / SceneFlowManager を追加\n" +
            "2. AudioManager の AudioSource を子オブジェクトで自動生成\n" +
            "3. 全コンポーネント間の参照を自動アサイン\n" +
            "4. LoopDatabase を自動検索してアサイン\n" +
            "5. 線路の長さを 200 に修正\n" +
            "6. マウス感度を 150 に設定\n\n" +
            "実行しますか？",
            "実行", "キャンセル");

        if (!confirm) return;

        int count = 0;

        count += SetupSystemManagers();
        count += SetupAudioManagerSources();
        count += AssignAllReferences();
        count += FixRails();
        count += FixMouseSensitivity();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "完了",
            $"自動設定が完了しました（{count} 項目を処理）\n\nCtrl+S でシーンを保存してください。",
            "OK");
    }

    // ──────────────────────────────────────
    // 1. システムマネージャーを追加
    // ──────────────────────────────────────
    static int SetupSystemManagers()
    {
        int count = 0;

        count += EnsureManager<LoopManager>("LoopManager");
        count += EnsureManager<ActionManager>("ActionManager");
        count += EnsureManager<SaveManager>("SaveManager");
        count += EnsureManager<EventManager>("EventManager");
        count += EnsureManager<EnvironmentManager>("EnvironmentManager");
        count += EnsureManager<SceneFlowManager>("SceneFlowManager");
        count += EnsureManager<TimelineDirectorManager>("TimelineDirectorManager");

        // ActionManager に LoopController を自動アサイン
        var actionMgr = Object.FindObjectOfType<ActionManager>();
        var loopCtrl  = Object.FindObjectOfType<LoopController>();
        if (actionMgr != null && loopCtrl != null)
        {
            var so = new SerializedObject(actionMgr);
            so.FindProperty("loopController").objectReferenceValue = loopCtrl;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[SystemSetup] システムマネージャーを設定しました");
        return count;
    }

    static int EnsureManager<T>(string goName) where T : Component
    {
        var existing = Object.FindObjectOfType<T>();
        if (existing != null) return 0;

        var go = GameObject.Find(goName) ?? new GameObject(goName);
        go.AddComponent<T>();
        Debug.Log($"[SystemSetup] {goName} を追加しました");
        return 1;
    }

    // ──────────────────────────────────────
    // 2. AudioManager の AudioSource を子オブジェクトで生成
    // ──────────────────────────────────────
    static int SetupAudioManagerSources()
    {
        var audioMgr = Object.FindObjectOfType<AudioManager>();
        if (audioMgr == null)
        {
            Debug.LogWarning("[SystemSetup] AudioManager が見つかりません");
            return 0;
        }

        AudioSource ambient      = EnsureChildAudioSource(audioMgr.gameObject, "AmbientSource");
        AudioSource noise        = EnsureChildAudioSource(audioMgr.gameObject, "NoiseSource");
        AudioSource announcement = EnsureChildAudioSource(audioMgr.gameObject, "AnnouncementSource");

        var so = new SerializedObject(audioMgr);
        so.FindProperty("ambientSource").objectReferenceValue      = ambient;
        so.FindProperty("noiseSource").objectReferenceValue        = noise;
        so.FindProperty("announcementSource").objectReferenceValue = announcement;
        so.ApplyModifiedProperties();

        Debug.Log("[SystemSetup] AudioManager の AudioSource を設定しました");
        return 3;
    }

    static AudioSource EnsureChildAudioSource(GameObject parent, string childName)
    {
        Transform t = parent.transform.Find(childName);
        if (t != null)
        {
            var existing = t.GetComponent<AudioSource>();
            if (existing != null) return existing;
            return t.gameObject.AddComponent<AudioSource>();
        }
        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform);
        return child.AddComponent<AudioSource>();
    }

    // ──────────────────────────────────────
    // 3. 全コンポーネント間の参照を自動アサイン
    // ──────────────────────────────────────
    static int AssignAllReferences()
    {
        int count = 0;

        var gameMgr    = Object.FindObjectOfType<GameManager>();
        var anomaly    = Object.FindObjectOfType<AnomalyController>();
        var audioMgr   = Object.FindObjectOfType<AudioManager>();
        var logMgr     = Object.FindObjectOfType<LogManager>();
        var loopCtrl   = Object.FindObjectOfType<LoopController>();
        var loopMgr    = Object.FindObjectOfType<LoopManager>();
        var envMgr     = Object.FindObjectOfType<EnvironmentManager>();
        var eventMgr   = Object.FindObjectOfType<EventManager>();
        var varCtrl    = Object.FindObjectOfType<VariantController>();

        // GameManager
        if (gameMgr != null)
        {
            var so = new SerializedObject(gameMgr);
            if (anomaly  != null) so.FindProperty("anomalyController").objectReferenceValue = anomaly;
            if (audioMgr != null) so.FindProperty("audioManager").objectReferenceValue      = audioMgr;
            if (logMgr   != null) so.FindProperty("logManager").objectReferenceValue        = logMgr;
            if (loopCtrl != null) so.FindProperty("loopController").objectReferenceValue    = loopCtrl;
            so.ApplyModifiedProperties();
            count++;
        }

        // LoopManager
        if (loopMgr != null)
        {
            var so = new SerializedObject(loopMgr);
            if (envMgr  != null) so.FindProperty("environmentManager").objectReferenceValue = envMgr;
            if (eventMgr != null) so.FindProperty("eventManager").objectReferenceValue      = eventMgr;
            if (varCtrl != null) so.FindProperty("variantController").objectReferenceValue  = varCtrl;

            // LoopDatabase を AssetDatabase から自動検索
            var dbGuids = AssetDatabase.FindAssets("t:LoopDatabase");
            if (dbGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
                var db = AssetDatabase.LoadAssetAtPath<LoopDatabase>(path);
                if (db != null)
                {
                    so.FindProperty("loopDatabase").objectReferenceValue = db;
                    Debug.Log($"[SystemSetup] LoopDatabase を自動アサイン: {path}");
                    count++;
                }
            }
            so.ApplyModifiedProperties();
            count++;
        }

        // EnvironmentManager
        if (envMgr != null)
        {
            var so = new SerializedObject(envMgr);

            // Directional Light を自動検索
            var dirLight = Object.FindObjectOfType<Light>();
            if (dirLight != null)
                so.FindProperty("mainLight").objectReferenceValue = dirLight;

            // Global Volume を自動検索
            var vol = Object.FindObjectOfType<Volume>();
            if (vol != null)
                so.FindProperty("globalVolume").objectReferenceValue = vol;

            // PlayerController を自動検索
            var playerCtrl = Object.FindObjectOfType<PlayerController>();
            if (playerCtrl != null)
                so.FindProperty("playerController").objectReferenceValue = playerCtrl;

            so.ApplyModifiedProperties();
            count++;
        }

        // AnomalyController に Global Volume をアサイン
        if (anomaly != null)
        {
            var so  = new SerializedObject(anomaly);
            var vol = Object.FindObjectOfType<Volume>();
            if (vol != null)
                so.FindProperty("globalVolume").objectReferenceValue = vol;

            var dirLight = Object.FindObjectOfType<Light>();
            if (dirLight != null)
                so.FindProperty("mainDirectionalLight").objectReferenceValue = dirLight;
            so.ApplyModifiedProperties();
            count++;
        }

        // LoopController にリセット参照をアサイン
        if (loopCtrl != null)
        {
            var so = new SerializedObject(loopCtrl);
            var resetPt = GameObject.Find("ResetPoint");
            if (resetPt != null)
                so.FindProperty("resetPoint").objectReferenceValue = resetPt.transform;

            var player = GameObject.FindWithTag("Player");
            if (player != null)
                so.FindProperty("playerTransform").objectReferenceValue = player.transform;

            // FadePanel の CanvasGroup を検索
            var fadePanel = GameObject.Find("FadePanel");
            if (fadePanel != null)
            {
                var cg = fadePanel.GetComponent<CanvasGroup>() ?? fadePanel.AddComponent<CanvasGroup>();
                so.FindProperty("fadeCanvasGroup").objectReferenceValue = cg;
            }
            so.ApplyModifiedProperties();
            count++;
        }

        // InteractionSystem に参照をアサイン
        var iSys = Object.FindObjectOfType<InteractionSystem>();
        if (iSys != null)
        {
            var so  = new SerializedObject(iSys);
            var cam = Camera.main;
            if (cam != null)
                so.FindProperty("playerCamera").objectReferenceValue = cam;

            var promptObj = GameObject.Find("InteractPrompt");
            if (promptObj != null)
            {
                var tmp = promptObj.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                    so.FindProperty("interactPromptText").objectReferenceValue = tmp;
            }
            so.ApplyModifiedProperties();
            count++;
        }

        // PlayerController に cameraTransform をアサイン
        var pc = Object.FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            var so  = new SerializedObject(pc);
            var cam = Camera.main;
            if (cam != null)
                so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
            so.ApplyModifiedProperties();
            count++;
        }

        Debug.Log($"[SystemSetup] {count} 個の参照をアサインしました");
        return count;
    }

    // ──────────────────────────────────────
    // 4. 線路の長さを 200 に修正
    // ──────────────────────────────────────
    static int FixRails()
    {
        int count = 0;
        string[] railNames = { "Rail_L", "Rail_R" };

        foreach (string railName in railNames)
        {
            GameObject rail = GameObject.Find(railName);
            if (rail == null) continue;

            Undo.RecordObject(rail.transform, "Fix Rail Length");
            Vector3 scale = rail.transform.localScale;
            if (scale.z < 150f)
            {
                scale.z = 200f;
                rail.transform.localScale = scale;
                Debug.Log($"[SystemSetup] {railName} の長さを 200 に修正しました");
                count++;
            }
        }

        if (count == 0)
            Debug.Log("[SystemSetup] 線路はすでに修正済みか、Rail_L/Rail_R が見つかりません");

        return count;
    }

    // ──────────────────────────────────────
    // 5. マウス感度を 150 に設定
    // ──────────────────────────────────────
    static int FixMouseSensitivity()
    {
        var pc = Object.FindObjectOfType<PlayerController>();
        if (pc == null) return 0;

        var so = new SerializedObject(pc);
        var prop = so.FindProperty("mouseSensitivity");
        if (prop != null && prop.floatValue < 100f)
        {
            prop.floatValue = 150f;
            so.ApplyModifiedProperties();
            Debug.Log("[SystemSetup] マウス感度を 150 に設定しました");
            return 1;
        }
        return 0;
    }
}
