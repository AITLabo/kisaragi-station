using UnityEngine;

// 画面中央に「+」を描画する MonoBehaviour
// OnGUI + GUI.skin.label ベース（フォントnull問題を回避）
public class ReticleHUD : MonoBehaviour
{
    public Color color = new Color(0.2f, 1f, 0.5f, 1f); // 通常: 緑
    private GUIStyle _style;

    private void OnGUI()
    {
        // GUI.skin.label を基底にする → フォントが常に有効
        if (_style == null)
        {
            _style           = new GUIStyle(GUI.skin.label);
            _style.fontSize  = 28;
            _style.fontStyle = FontStyle.Bold;
            _style.alignment = TextAnchor.MiddleCenter;
        }

        GUI.color = color;
        GUI.Label(
            new Rect(Screen.width * .5f - 20f, Screen.height * .5f - 20f, 40f, 40f),
            "+", _style);
        GUI.color = Color.white; // 他のGUIへの影響をリセット
    }
}
