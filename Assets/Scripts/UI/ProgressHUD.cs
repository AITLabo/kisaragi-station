using UnityEngine;

// 画面上に進行状況（異変X/10）とヒントを表示する OnGUI オーバーレイ
// ReticleHUD と同じく Canvas・フォント不要
public class ProgressHUD : MonoBehaviour
{
    private string _hintText    = "";
    private int    _progress    = 0;
    private int    _total       = 10;
    private float  _showUntil   = 0f;

    private GUIStyle _hintStyle;
    private GUIStyle _progressStyle;

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogWarning("[ProgressHUD] GameManager が見つかりません"); return; }

        _total = gm.correctSequence.Length;
        gm.OnProgressAdvanced += OnProgressAdvanced;

        // Start 時に最初のヒントを受け取るため、GameManager.Start() より後に呼ばれる必要がある
        // GameManager.Start() → NotifyHint() → OnProgressAdvanced イベント発火
    }

    private void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.OnProgressAdvanced -= OnProgressAdvanced;
    }

    private void OnProgressAdvanced(string hint, int progress)
    {
        _hintText  = hint;
        _progress  = progress;
        _showUntil = Time.time + 6f; // 6秒間表示
    }

    private void OnGUI()
    {
        if (_hintStyle == null)
        {
            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _progressStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
        }

        // 右上に常時表示: 異変 X/10
        GUI.color = new Color(1f, 0.85f, 0.5f, 0.9f);
        GUI.Label(
            new Rect(Screen.width - 130, 12, 120, 28),
            $"異変  {_progress} / {_total}",
            _progressStyle);

        // ヒント: 正解後6秒間、画面下部に表示
        if (!string.IsNullOrEmpty(_hintText) && Time.time < _showUntil)
        {
            GUI.color = new Color(0.7f, 1f, 0.7f, Mathf.Clamp01(_showUntil - Time.time));
            GUI.Label(
                new Rect(0, Screen.height - 90, Screen.width, 40),
                _hintText,
                _hintStyle);
        }

        GUI.color = Color.white;
    }
}
