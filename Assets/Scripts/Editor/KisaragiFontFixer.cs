using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.TextCore.LowLevel;

// Unity メニュー「Kisaragi > Fix Japanese Font（日本語フォント修正）」
// Windows システムフォント（游ゴシック/メイリオ）から日本語 TMP FontAsset を自動生成し
// シーン内の全 TextMeshProUGUI に割り当てる
[UnityEditor.InitializeOnLoad]
public class KisaragiFontFixer
{
    // ── エディタ起動時に自動実行：material==null の壊れたフォントを fallback から除去 ──
    static KisaragiFontFixer()
    {
        EditorApplication.delayCall += AutoCleanupBrokenFallbacks;
    }

    static void AutoCleanupBrokenFallbacks()
    {
        bool anyFixed = false;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa == null || fa.fallbackFontAssetTable == null) continue;

            for (int i = fa.fallbackFontAssetTable.Count - 1; i >= 0; i--)
            {
                TMP_FontAsset fb = fa.fallbackFontAssetTable[i];
                if (fb == null || fb.material == null)
                {
                    string fbName = fb != null ? fb.name : "(null)";
                    fa.fallbackFontAssetTable.RemoveAt(i);
                    EditorUtility.SetDirty(fa);
                    anyFixed = true;
                    Debug.LogWarning($"[FontFixer] 壊れたフォント '{fbName}' を {fa.name} の fallback から除去しました");
                }
            }
        }
        if (anyFixed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[FontFixer] 壊れた fallback フォントの自動クリーンアップ完了");
        }
    }


    const string FONTS_DIR      = "Assets/Fonts";
    const string ASSET_PATH     = "Assets/Fonts/NotoSansJP_Dynamic.asset";
    const string FONT_ASSET_DIR = "Assets/Fonts";

    // Windows に入っている日本語フォント候補（優先順）
    static readonly string[] WINDOWS_FONT_CANDIDATES = new[]
    {
        @"C:\Windows\Fonts\YuGothR.ttc",    // 游ゴシック Regular
        @"C:\Windows\Fonts\YuGothM.ttc",    // 游ゴシック Medium
        @"C:\Windows\Fonts\msgothic.ttc",   // MS ゴシック
        @"C:\Windows\Fonts\meiryo.ttc",     // メイリオ
        @"C:\Windows\Fonts\meiryob.ttc",    // メイリオ Bold
        @"C:\Windows\Fonts\YUGOTHB.TTC",    // 游ゴシック Bold
        @"C:\Windows\Fonts\YUGOTHM.TTC",
        @"C:\Windows\Fonts\YUGOTHR.TTC",
    };

    [MenuItem("Kisaragi/Fix Japanese Font（日本語フォント修正）")]
    public static void FixJapaneseFont()
    {
        // ── 1. Assets/Fonts フォルダを確保 ──
        if (!Directory.Exists(FONTS_DIR))
            Directory.CreateDirectory(FONTS_DIR);

        // ── 2. 既存アセットがあれば再利用（material が null なら壊れているので再生成） ──
        TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ASSET_PATH);

        TMP_FontAsset jpFont = (existingAsset != null && existingAsset.material != null) ? existingAsset : null;

        if (jpFont == null)
        {
            // ── 3. Windows フォントをプロジェクトにコピー ──
            string srcPath = null;
            string dstFontName = null;

            foreach (string candidate in WINDOWS_FONT_CANDIDATES)
            {
                if (File.Exists(candidate))
                {
                    srcPath = candidate;
                    dstFontName = Path.GetFileName(candidate);
                    break;
                }
            }

            if (srcPath == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    "日本語フォントが見つかりません。\n\n" +
                    "以下のいずれかのフォントをWindowsにインストールしてください：\n" +
                    "  游ゴシック、MS ゴシック、メイリオ",
                    "OK");
                return;
            }

            string dstPath = $"{FONTS_DIR}/{dstFontName}";

            // TTC はコピー先も .ttc として保存（TMP は TTC に対応）
            if (!File.Exists(dstPath))
            {
                File.Copy(srcPath, dstPath, overwrite: false);
                AssetDatabase.Refresh();
                Debug.Log($"[FontFixer] フォントコピー: {srcPath} → {dstPath}");
            }

            // ── 4. Unity Font アセットとして読み込み ──
            Font unityFont = AssetDatabase.LoadAssetAtPath<Font>(dstPath);
            if (unityFont == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    $"フォントの読み込みに失敗しました:\n{dstPath}\n\n" +
                    "Unityプロジェクトを再起動してから再試行してください。",
                    "OK");
                return;
            }

            // ── 5. TMP_FontAsset を Dynamic モードで生成 ──
            jpFont = TMP_FontAsset.CreateFontAsset(
                unityFont,
                samplingPointSize:       32,
                atlasPadding:             4,
                renderMode:               GlyphRenderMode.SDFAA,
                atlasWidth:            1024,
                atlasHeight:           1024,
                atlasPopulationMode:      AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport:  true
            );

            if (jpFont == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    "TMP_FontAsset の生成に失敗しました。\n" +
                    "TextMeshPro パッケージが正しくインポートされているか確認してください。",
                    "OK");
                return;
            }

            jpFont.name = "NotoSansJP_Dynamic";

            // 既存の壊れたアセットを削除してから保存
            if (existingAsset != null)
                AssetDatabase.DeleteAsset(ASSET_PATH);

            AssetDatabase.CreateAsset(jpFont, ASSET_PATH);

            // ★ マテリアルとアトラステクスチャをサブアセットとして保存
            // これをしないと実行時に material == null エラーが発生する
            if (jpFont.material != null)
            {
                jpFont.material.name = "NotoSansJP_Dynamic Atlas Material";
                AssetDatabase.AddObjectToAsset(jpFont.material, ASSET_PATH);
            }
            if (jpFont.atlasTextures != null)
            {
                for (int ti = 0; ti < jpFont.atlasTextures.Length; ti++)
                {
                    var tex = jpFont.atlasTextures[ti];
                    if (tex != null)
                    {
                        tex.name = "NotoSansJP_Dynamic Atlas " + ti;
                        AssetDatabase.AddObjectToAsset(tex, ASSET_PATH);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FontFixer] TMP_FontAsset を生成しました（マテリアル・テクスチャ含む）: {ASSET_PATH}");
        }

        // ★ material が null のままなら fallback に追加しない（クラッシュ防止）
        if (jpFont.material == null)
        {
            Debug.LogError("[FontFixer] material が null のためフォントアセットは使用できません。プロジェクトを再起動して再実行してください。");
            EditorUtility.DisplayDialog("エラー",
                "フォントのマテリアルが生成できませんでした。\n\nUnityを再起動してからもう一度\n「Fix Japanese Font」を実行してください。",
                "OK");
            return;
        }

        // ── 6. TMP Settings の Fallback Font List に追加 ──
        TMP_Settings settings = TMP_Settings.instance;
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty fallbacksProp = so.FindProperty("m_fallbackFontAssets");
            if (fallbacksProp != null)
            {
                bool alreadyIn = false;
                for (int i = 0; i < fallbacksProp.arraySize; i++)
                {
                    if (fallbacksProp.GetArrayElementAtIndex(i).objectReferenceValue == jpFont)
                    {
                        alreadyIn = true;
                        break;
                    }
                }
                if (!alreadyIn)
                {
                    fallbacksProp.InsertArrayElementAtIndex(0);
                    fallbacksProp.GetArrayElementAtIndex(0).objectReferenceValue = jpFont;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[FontFixer] TMP Settings の Fallback に追加しました");
                }
            }
        }

        // ── 7. シーン内の全 TextMeshProUGUI に直接割り当て ──
        int changed = 0;
        TextMeshProUGUI[] allTMP = Object.FindObjectsOfType<TextMeshProUGUI>(includeInactive: true);
        foreach (TextMeshProUGUI tmp in allTMP)
        {
            bool needsUpdate = false;

            // フォントが未設定 or Liberation Sans のみの場合は置き換え
            if (tmp.font == null || tmp.font.name.StartsWith("LiberationSans"))
            {
                tmp.font = jpFont;
                needsUpdate = true;
            }
            else
            {
                // fallbackFontAssetTable に未登録なら追加
                if (!tmp.font.fallbackFontAssetTable.Contains(jpFont))
                {
                    tmp.font.fallbackFontAssetTable.Add(jpFont);
                    EditorUtility.SetDirty(tmp.font);
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                tmp.SetAllDirty();
                EditorUtility.SetDirty(tmp);
                changed++;
            }
        }

        // ── 8. TMP_Text（3D Text も含む）も処理 ──
        TextMeshPro[] allTMP3D = Object.FindObjectsOfType<TextMeshPro>(includeInactive: true);
        foreach (TextMeshPro tmp in allTMP3D)
        {
            if (tmp.font == null || tmp.font.name.StartsWith("LiberationSans"))
            {
                tmp.font = jpFont;
                tmp.SetAllDirty();
                EditorUtility.SetDirty(tmp);
                changed++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完了",
            $"日本語フォント設定が完了しました！\n\n" +
            $"フォントアセット: {ASSET_PATH}\n" +
            $"更新コンポーネント: {changed} 個\n\n" +
            "Ctrl+S でシーンを保存してください。",
            "OK");

        Debug.Log($"[FontFixer] 完了 – {changed} 個の TMP コンポーネントを更新しました");
    }

    // ── InteractionSystem のテキストを日本語UIに更新 ──
    [MenuItem("Kisaragi/Fix Interaction Prompt Text（プロンプトテキスト修正）")]
    public static void FixInteractionPromptText()
    {
        int fixed2 = 0;
        TextMeshProUGUI[] allTMP = Object.FindObjectsOfType<TextMeshProUGUI>(includeInactive: true);

        foreach (TextMeshProUGUI tmp in allTMP)
        {
            if (tmp.gameObject.name == "InteractPrompt")
            {
                // 文字コードを明示的に設定
                tmp.text = "[ E ]  \u8abf\u3079\u308b";  // 「調べる」をUnicodeエスケープ
                tmp.fontSize = 14f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.9f, 0.7f, 1f);
                tmp.enableWordWrapping = false;
                EditorUtility.SetDirty(tmp);
                fixed2++;
                Debug.Log($"[FontFixer] InteractPrompt テキスト修正: {tmp.gameObject.scene.name}");
            }
        }

        if (fixed2 == 0)
        {
            Debug.LogWarning("[FontFixer] InteractPrompt が見つかりませんでした");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完了",
            $"InteractPrompt テキスト修正: {fixed2} 個\n\n" +
            "先に「Fix Japanese Font」を実行してフォントを設定してください。",
            "OK");
    }
}
