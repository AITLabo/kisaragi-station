using UnityEngine;
using UnityEditor;

// Kisaragi > Fix Player Position
// デモシーンの座標系に合わせてプレイヤーを正しい位置に配置する
// SM_TunnelStart: X:-48.3, Y:-5, Z:-52.9
// SM_Tunnel_10m:  X:-41.5, Y:-7.7, Z:-83.6
// トンネル内部中央に配置する
public class KisaragiPlayerFix
{
    // デモシーンで確認したトンネル内部の座標
    // SM_Tunnel_20m: X:-48.3, Y:-5, Z:-22.9
    // SM_TunnelStart: X:-48.3, Y:-5, Z:-52.9
    // → トンネル内部中央（地下）に配置
    static readonly Vector3 PLAYER_POS      = new Vector3(-48.3f, -3.2f, -40f);
    static readonly Vector3 RESET_POS       = new Vector3(-48.3f, -3.2f, -40f);
    static readonly Vector3 LOOP_TRIGGER_POS = new Vector3(-48.3f, -3.2f, -90f);
    static readonly Vector3 PLAYER_EULER    = new Vector3(0f, 180f, 0f); // トンネル奥向き

    [MenuItem("Kisaragi/Fix Player Position (プレイヤー位置修正)")]
    public static void FixPlayerPosition()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[PlayerFix] 'Player' オブジェクトが見つかりません");
            EditorUtility.DisplayDialog("エラー", "'Player' オブジェクトが見つかりません。\nKisaragi > Build Scene を先に実行してください。", "OK");
            return;
        }

        // CharacterController の設定を修正
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.stepOffset = 0.4f;
            cc.skinWidth = 0.08f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            EditorUtility.SetDirty(cc);
        }

        // Player の Scale を正常化（Y:4 になっている場合がある）
        player.transform.localScale = Vector3.one;

        // Player の位置・回転を設定
        player.transform.position = PLAYER_POS;
        player.transform.eulerAngles = PLAYER_EULER;
        EditorUtility.SetDirty(player);

        Debug.Log($"[PlayerFix] Player を {PLAYER_POS} に配置しました");

        // ResetPoint の更新
        GameObject resetPoint = GameObject.Find("ResetPoint");
        if (resetPoint != null)
        {
            resetPoint.transform.position = RESET_POS;
            EditorUtility.SetDirty(resetPoint);
            Debug.Log($"[PlayerFix] ResetPoint を {RESET_POS} に移動");
        }

        // LoopTrigger の更新
        GameObject loopTrigger = GameObject.Find("LoopTrigger");
        if (loopTrigger != null)
        {
            loopTrigger.transform.position = LOOP_TRIGGER_POS;
            EditorUtility.SetDirty(loopTrigger);
            Debug.Log($"[PlayerFix] LoopTrigger を {LOOP_TRIGGER_POS} に移動");
        }

        // Camera の位置修正（PlayerCamera の子オブジェクト）
        Transform camTransform = player.transform.Find("PlayerCamera");
        if (camTransform == null)
        {
            // 子の中からCameraを探す
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null) camTransform = cam.transform;
        }
        if (camTransform != null)
        {
            camTransform.localPosition = new Vector3(0f, 0.75f, 0f);
            camTransform.localEulerAngles = Vector3.zero;
            EditorUtility.SetDirty(camTransform.gameObject);
            Debug.Log("[PlayerFix] PlayerCamera の位置を修正しました");
        }

        EditorUtility.DisplayDialog("完了",
            $"Player を配置しました。\n位置: {PLAYER_POS}\n\n※ Play ボタンで動作確認してください。\nもし壁の中や地下にいたら Kisaragi > Scan Scene Player Pos を使ってください。", "OK");
    }

    // シーン内の全メッシュをスキャンして最適な開始位置を自動検出するツール
    [MenuItem("Kisaragi/Scan Scene for Player Start Position (自動スキャン)")]
    public static void ScanSceneForPlayerPos()
    {
        // シーン内の全 MeshRenderer を取得
        MeshRenderer[] renderers = GameObject.FindObjectsOfType<MeshRenderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError("[PlayerFix] シーンにメッシュが見つかりません");
            return;
        }

        // バウンディングボックスの中心を計算
        Bounds total = renderers[0].bounds;
        foreach (var r in renderers)
            total.Encapsulate(r.bounds);

        Vector3 center = total.center;
        Debug.Log($"[PlayerFix] シーン全体の中心: {center}");
        Debug.Log($"[PlayerFix] シーン範囲: min={total.min}, max={total.max}");

        // SM_Tunnel で始まるオブジェクトを特別に探す
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        Vector3 tunnelCenter = center;
        int tunnelCount = 0;

        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("SM_Tunnel") || go.name.StartsWith("SM_Basement"))
            {
                tunnelCenter += go.transform.position;
                tunnelCount++;
                Debug.Log($"[PlayerFix] Tunnel オブジェクト: {go.name} @ {go.transform.position}");
            }
        }

        if (tunnelCount > 0)
        {
            tunnelCenter /= (tunnelCount + 1);
            Debug.Log($"[PlayerFix] トンネル平均座標: {tunnelCenter}");

            // プレイヤーをトンネル中央付近に配置
            Vector3 newPos = new Vector3(tunnelCenter.x, tunnelCenter.y + 1.0f, tunnelCenter.z);

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = newPos;
                player.transform.eulerAngles = Vector3.zero;
                EditorUtility.SetDirty(player);
                Debug.Log($"[PlayerFix] Player を自動スキャン位置 {newPos} に移動しました");
                EditorUtility.DisplayDialog("スキャン完了",
                    $"トンネル中心付近にプレイヤーを配置しました。\n位置: {newPos}\n\nPlay して確認してください。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("スキャン結果",
                    $"推奨プレイヤー位置: {tunnelCenter}\n'Player' オブジェクトが見つからないため自動移動できませんでした。", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("スキャン結果",
                $"シーン中心: {center}\nトンネルオブジェクトが見つかりませんでした。", "OK");
        }
    }
}
