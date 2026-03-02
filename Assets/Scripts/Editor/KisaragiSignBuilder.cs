using UnityEngine;
using UnityEditor;
using TMPro;

// Unity メニュー「Kisaragi > Build Signs」で
// 駅名看板・時計・ポスターに TextMeshPro テキストを自動生成する
public class KisaragiSignBuilder
{
    [MenuItem("Kisaragi/Build Signs (看板・時計・ポスターにテキスト追加)")]
    public static void BuildSigns()
    {
        // ──────────────────────────────────────
        // 駅名看板
        // ──────────────────────────────────────
        GameObject signPost = GameObject.Find("StationSign");
        if (signPost != null)
        {
            // 既存プレースホルダーを削除
            Transform placeholder = signPost.transform.Find("StationNamePlaceholder");
            if (placeholder != null) Object.DestroyImmediate(placeholder.gameObject);

            // TextMeshPro 3D テキストを追加
            GameObject textGO = new GameObject("StationNameText3D");
            textGO.transform.SetParent(signPost.transform);
            textGO.transform.localPosition = new Vector3(0, 0, -0.06f);
            textGO.transform.localRotation = Quaternion.identity;
            textGO.transform.localScale    = Vector3.one;

            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text      = "き さ ら ぎ 駅";
            tmp.fontSize  = 0.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            Debug.Log("[Signs] 駅名看板テキストを追加しました");
        }

        // ──────────────────────────────────────
        // 時計
        // ──────────────────────────────────────
        GameObject clock = GameObject.Find("Clock");
        if (clock != null)
        {
            GameObject clockTextGO = new GameObject("ClockText3D");
            clockTextGO.transform.SetParent(clock.transform);
            clockTextGO.transform.localPosition = new Vector3(0, 0, -1.2f);
            clockTextGO.transform.localRotation = Quaternion.identity;
            clockTextGO.transform.localScale    = Vector3.one;

            TextMeshPro tmp = clockTextGO.AddComponent<TextMeshPro>();
            tmp.text      = "23:58";
            tmp.fontSize  = 0.12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.1f, 0.1f, 0.1f);

            Debug.Log("[Signs] 時計テキストを追加しました");
        }

        // ──────────────────────────────────────
        // ポスター（3枚）
        // ──────────────────────────────────────
        string[] posterTexts = {
            "き さ ら ぎ 行\n最終電車\n23:52 発",
            "乗り越しにご注意ください\n\n次は──",
            "この先に\n出口はありません"
        };

        for (int i = 0; i < 3; i++)
        {
            GameObject poster = GameObject.Find($"Poster_{i}");
            if (poster == null) continue;

            // 既存テキストがあれば削除
            Transform existing = poster.transform.Find($"PosterText_{i}");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject textGO = new GameObject($"PosterText_{i}");
            textGO.transform.SetParent(poster.transform);
            textGO.transform.localPosition = new Vector3(0, 0, -0.6f);
            textGO.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            textGO.transform.localScale    = Vector3.one;

            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text      = posterTexts[i];
            tmp.fontSize  = 0.07f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.15f, 0.1f, 0.08f);

            Debug.Log($"[Signs] ポスター {i} にテキストを追加しました");
        }

        // ──────────────────────────────────────
        // PsychologicalEffects に参照を設定
        // ──────────────────────────────────────
        GameObject psyGO = GameObject.Find("PsychologicalEffects");
        if (psyGO != null)
        {
            PsychologicalEffects psy = psyGO.GetComponent<PsychologicalEffects>();
            if (psy != null)
            {
                SerializedObject so = new SerializedObject(psy);

                GameObject signTextGO  = GameObject.Find("StationNameText3D");
                GameObject clockTextGO = GameObject.Find("ClockText3D");
                if (signTextGO != null)
                    so.FindProperty("stationNameText3D").objectReferenceValue = signTextGO.GetComponent<TextMeshPro>();
                if (clockTextGO != null)
                    so.FindProperty("clockText3D").objectReferenceValue = clockTextGO.GetComponent<TextMeshPro>();
                so.ApplyModifiedProperties();
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            "駅名看板・時計・ポスターにテキストを追加しました！\n\nCtrl+S で保存してください。", "OK");
    }
}
