using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// シーン遷移を一元管理するSingleton
// DontDestroyOnLoad で全シーンに跨って存在する
// ゲームフロー: TrainScene → KisaragiScene → EscapeScene
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    public const string SCENE_TRAIN    = "TrainScene";
    public const string SCENE_KISARAGI = "SubwayStationDemo";
    public const string SCENE_ESCAPE   = "EscapeScene";

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.2f;

    // フェード用CanvasGroup（各シーンのFadePanelを動的に取得）
    private CanvasGroup fadeGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // シーンごとにプレイヤーを正しい位置に配置してからフェードイン
        if (scene.name == SCENE_KISARAGI)
            StartCoroutine(SetupKisaragiPlayer());
        else
            StartCoroutine(FadeIn());
    }

    // ── きさらぎ駅シーン用プレイヤー配置 ──
    // SubwayStationDemo の地下トンネル内の正しい座標へ強制移動
    private IEnumerator SetupKisaragiPlayer()
    {
        // 1フレーム待ってシーン初期化を完了させる
        yield return null;
        yield return null;

        // Player を検索してトンネル内に配置
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // CharacterController を無効にしてからテレポート（重要）
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position    = new Vector3(-48.3f, -3.2f, -40f);
            player.transform.eulerAngles = new Vector3(0f, 180f, 0f);

            if (cc != null) cc.enabled = true;
            Debug.Log("[SceneFlowManager] きさらぎ駅プレイヤー配置完了");
        }
        else
        {
            Debug.LogWarning("[SceneFlowManager] Player タグのオブジェクトが見つかりません");
        }

        // フェードイン
        yield return StartCoroutine(FadeIn());
    }

    // ──────────────────────────────────────
    // 公開API
    // ──────────────────────────────────────

    /// <summary>電車内シーンへ遷移（ゲーム開始時）</summary>
    public void LoadTrainScene()
    {
        if (!isTransitioning)
            StartCoroutine(TransitionTo(SCENE_TRAIN));
    }

    /// <summary>きさらぎ駅シーンへ遷移（電車到着時）</summary>
    public void LoadKisaragiScene()
    {
        if (!isTransitioning)
            StartCoroutine(TransitionTo(SCENE_KISARAGI));
    }

    /// <summary>帰還シーンへ遷移（10個クリア時）</summary>
    public void LoadEscapeScene()
    {
        if (!isTransitioning)
            StartCoroutine(TransitionTo(SCENE_ESCAPE));
    }

    // ──────────────────────────────────────
    // 内部処理
    // ──────────────────────────────────────

    private IEnumerator TransitionTo(string sceneName)
    {
        isTransitioning = true;

        // フェードアウト
        yield return StartCoroutine(FadeOut());

        // シーンロード
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        isTransitioning = false;
        // FadeInはOnSceneLoadedで自動実行
    }

    private IEnumerator FadeOut()
    {
        RefreshFadeGroup();
        if (fadeGroup == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        // 少し待ってシーン初期化を待つ
        yield return new WaitForSeconds(0.1f);

        RefreshFadeGroup();
        if (fadeGroup == null) yield break;

        fadeGroup.alpha = 1f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(1f - t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    private void RefreshFadeGroup()
    {
        // シーン内の FadePanel を探して CanvasGroup を取得
        GameObject fadeObj = GameObject.Find("FadePanel");
        if (fadeObj != null)
        {
            fadeGroup = fadeObj.GetComponent<CanvasGroup>();
            if (fadeGroup == null)
                fadeGroup = fadeObj.AddComponent<CanvasGroup>();
        }
        else
        {
            fadeGroup = null;
        }
    }
}
