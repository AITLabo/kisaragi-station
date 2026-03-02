// きさらぎ駅ループ – インタラクション共通インターフェース
// 正解・不正解を問わず、触れられる全オブジェクトに実装する
public interface IInteractable
{
    /// <summary>プレイヤーがEキーで対象に触れた時に呼ばれる</summary>
    void OnInteract();

    /// <summary>このオブジェクトのシーケンスID（"gate", "clock" など）</summary>
    string ActionID { get; }
}
