// きさらぎ駅ループ – 歪みレベル定義
// distortionValue の累積値に応じて自動変換される

public enum DistortionLevel
{
    None,       // 通常
    Low,        // 微違和感
    Medium,     // 認識干渉
    High,       // 孤立・存在崩壊寸前
    Collapse    // 完全崩壊
}
