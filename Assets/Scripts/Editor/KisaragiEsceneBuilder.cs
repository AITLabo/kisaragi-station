using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

// Kisaragi > Build Escape Scene
// EscapeScene（帰還・エンディング）を自動構築する
// プレイヤーが10個の違和感を正しい順で見つけた後に遷移するシーン
public class KisaragiEsceneBuilder
{
    [MenuItem("Kisaragi/Build Escape Scene (帰還エンディングシーン構築)")]
    public static void BuildEscapeScene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // デフォルトカメラを削除（AudioListener重複を防ぐ）
        var defaultCam = Camera.main;
        if (defaultCam != null)
            Object.DestroyImmediate(defaultCam.gameObject);

        BuildEscapeEnvironment();

        string scenePath = "Assets/Scenes/EscapeScene.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"[EsceneBuilder] EscapeScene を作成しました: {scenePath}");
        EditorUtility.DisplayDialog("完了",
            "EscapeScene（帰還エンディング）を作成しました。\n\nAssets/Scenes/EscapeScene.unity\n\n次のステップ:\n" +
            "Kisaragi > Add Scenes to Build Settings を実行して\n全シーンをビルドに追加してください。",
            "OK");
    }

    static void BuildEscapeEnvironment()
    {
        // デフォルトの Directional Light を暗くする
        var dirLight = Object.FindObjectOfType<Light>();
        if (dirLight != null) dirLight.intensity = 0.8f;

        // ── 電車内（帰還）環境 ──
        // TrainScene より明るい・暖色系

        // 床
        CreateBox("EscapeFloor", new Vector3(0, -0.05f, 0), new Vector3(3f, 0.1f, 20f), new Color(0.3f, 0.27f, 0.24f));
        // 天井
        CreateBox("EscapeCeiling", new Vector3(0, 2.5f, 0), new Vector3(3f, 0.1f, 20f), new Color(0.85f, 0.83f, 0.80f));
        // 左壁
        CreateBox("EscapeWallLeft", new Vector3(-1.5f, 1.25f, 0), new Vector3(0.1f, 2.5f, 20f), new Color(0.8f, 0.78f, 0.75f));
        // 右壁
        CreateBox("EscapeWallRight", new Vector3(1.5f, 1.25f, 0), new Vector3(0.1f, 2.5f, 20f), new Color(0.8f, 0.78f, 0.75f));

        // ── 窓（外が明るくなっていく） ──
        for (int i = -3; i <= 3; i++)
        {
            CreateBox($"EscapeWindowLeft_{i}", new Vector3(-1.45f, 1.6f, i * 2.5f),
                new Vector3(0.05f, 0.8f, 1.5f), new Color(0.7f, 0.75f, 0.9f));
            CreateBox($"EscapeWindowRight_{i}", new Vector3(1.45f, 1.6f, i * 2.5f),
                new Vector3(0.05f, 0.8f, 1.5f), new Color(0.7f, 0.75f, 0.9f));
        }

        // ── 蛍光灯（全部正常に点灯） ──
        for (int i = -3; i <= 3; i++)
        {
            CreateLight($"EscapeLight_{i}", new Vector3(0, 2.4f, i * 2.5f), new Color(1f, 0.95f, 0.85f), 1.8f);
        }

        // ── プレイヤー ──
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.stepOffset = 0.4f;
        cc.skinWidth = 0.08f;
        cc.center = new Vector3(0, 0.9f, 0);
        var pc = player.AddComponent<PlayerController>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var escapeCam = camObj.AddComponent<Camera>();
        escapeCam.backgroundColor = Color.black;
        escapeCam.clearFlags = CameraClearFlags.SolidColor;
        camObj.AddComponent<AudioListener>();
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0, 0.75f, 0);

        // PlayerController に cameraTransform をセット
        {
            var so = new SerializedObject(pc);
            so.FindProperty("cameraTransform").objectReferenceValue = camObj.transform;
            so.ApplyModifiedProperties();
        }

        player.transform.position = new Vector3(0, 0.9f, -5f);
        player.transform.eulerAngles = new Vector3(0, 0, 0);

        // ── エンディングコントローラー ──
        GameObject endingCtrl = new GameObject("EndingController");
        endingCtrl.AddComponent<EscapeEndingController>();

        // ── Global Volume（明るく・暖色） ──
        GameObject volumeObj = new GameObject("Global Volume");
        var volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, "Assets/Settings/EscapeVolumeProfile.asset");

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.saturation.value = 10f;
        colorAdj.saturation.overrideState = true;
        colorAdj.postExposure.value = 0.3f;
        colorAdj.postExposure.overrideState = true;

        var vignette = profile.Add<Vignette>();
        vignette.intensity.value = 0.1f;
        vignette.intensity.overrideState = true;

        volume.profile = profile;
        EditorUtility.SetDirty(profile);

        // ── UI Canvas ──
        SetupUI();

        // ── SceneFlowManager ──
        var sfm = new GameObject("SceneFlowManager");
        sfm.AddComponent<SceneFlowManager>();
    }

    static void SetupUI()
    {
        GameObject canvasObj = new GameObject("UI Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // フェードパネル
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(canvasObj.transform, false);
        var fadeImage = fadePanel.AddComponent<Image>();
        fadeImage.color = Color.black;
        var fadeCg = fadePanel.AddComponent<CanvasGroup>();
        fadeCg.alpha = 1f;
        var fadeRect = fadePanel.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        // エンディングテキスト
        GameObject endingTextObj = new GameObject("EndingText");
        endingTextObj.transform.SetParent(canvasObj.transform, false);
        var endingText = endingTextObj.AddComponent<TextMeshProUGUI>();
        endingText.text = "";
        endingText.fontSize = 36;
        endingText.alignment = TextAlignmentOptions.Center;
        endingText.color = new Color(0.95f, 0.92f, 0.82f);
        var endingRect = endingTextObj.GetComponent<RectTransform>();
        endingRect.anchorMin = new Vector2(0.1f, 0.4f);
        endingRect.anchorMax = new Vector2(0.9f, 0.7f);
        endingRect.offsetMin = Vector2.zero;
        endingRect.offsetMax = Vector2.zero;

        // サブテキスト
        GameObject subTextObj = new GameObject("SubText");
        subTextObj.transform.SetParent(canvasObj.transform, false);
        var subText = subTextObj.AddComponent<TextMeshProUGUI>();
        subText.text = "";
        subText.fontSize = 20;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.7f, 0.7f, 0.7f);
        var subRect = subTextObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.2f, 0.25f);
        subRect.anchorMax = new Vector2(0.8f, 0.4f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
    }

    static GameObject CreateBox(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    static void CreateLight(string name, Vector3 pos, Color color, float intensity)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        var light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = 5f;

        CreateBox($"{name}_Tube", pos, new Vector3(1.5f, 0.05f, 0.1f), new Color(1f, 0.98f, 0.9f));
    }
}

// エンディング演出を制御するランタイムスクリプト
public class EscapeEndingController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(PlayEnding());
    }

    private System.Collections.IEnumerator PlayEnding()
    {
        // フェードイン（SceneFlowManager が自動でやるが念のため）
        yield return new WaitForSeconds(1.5f);

        // テキスト表示
        var endingText = FindObjectOfType<TMPro.TextMeshProUGUI>();
        if (endingText != null && endingText.gameObject.name == "EndingText")
        {
            yield return TypeText(endingText, "現実へ帰還した。", 0.08f);
            yield return new WaitForSeconds(2f);

            yield return TypeText(endingText,
                "あの駅で見たものは\n本当に存在したのだろうか。", 0.06f);
            yield return new WaitForSeconds(3f);

            endingText.text = "";
        }

        // 窓の外が明るくなる演出
        yield return new WaitForSeconds(1f);

        // ログを保存
        var logManager = FindObjectOfType<LogManager>();
        logManager?.SaveLog("ending_reached", "clear");

        yield return new WaitForSeconds(2f);

        // タイトルに戻るか終了
        Debug.Log("[EscapeEnding] エンディング完了");
    }

    private System.Collections.IEnumerator TypeText(TMPro.TextMeshProUGUI text, string message, float delay)
    {
        text.text = "";
        foreach (char c in message)
        {
            text.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}
