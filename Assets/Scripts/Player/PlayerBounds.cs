using UnityEngine;

// 駅ホーム上でプレイヤーの移動範囲を制限（落ち防止・空気のための閉じた空間）
public class PlayerBounds : MonoBehaviour
{
    [Header("Bounds (ホームA/B＋陸橋対応: X=-8~+4.5, Y=-1~+5)")]
    [SerializeField] private float minX = -8.0f;
    [SerializeField] private float maxX =  4.5f;
    [SerializeField] private float minZ = -24f;
    [SerializeField] private float maxZ =  24f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY =  5.0f;

    private CharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.z = Mathf.Clamp(p.z, minZ, maxZ);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        if (_cc != null)
        {
            _cc.enabled = false;
            transform.position = p;
            _cc.enabled = true;
        }
        else
            transform.position = p;
    }
}
