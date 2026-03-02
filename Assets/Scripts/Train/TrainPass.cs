using UnityEngine;

// きさらぎ駅 – 駅シーンで電車が通過するだけ（Z -80 → +80）。空間圧迫・異常トリガー用
public class TrainPass : MonoBehaviour
{
    [Tooltip("進行速度（m/s）")]
    public float speed = 40f;
    [Tooltip("出現 Z 座標")]
    public float spawnZ = -80f;
    [Tooltip("消失 Z 座標（超えたら非表示 or 破棄）")]
    public float despawnZ = 80f;

    private bool _moving;
    private Vector3 _forward;
    private bool _reverse;

    private void Start()
    {
        UpdateForward();
    }

    private void UpdateForward()
    {
        _forward = transform.forward;
        if (Mathf.Abs(_forward.y) > 0.99f)
            _forward = Vector3.forward;
        _forward.y = 0f;
        _forward.Normalize();
        if (_reverse) _forward = -_forward;
    }

    private void Update()
    {
        if (!_moving) return;
        transform.Translate(_forward * speed * Time.deltaTime, Space.World);
        if (_reverse)
        {
            if (transform.position.z <= -despawnZ)
            {
                _moving = false;
                gameObject.SetActive(false);
            }
        }
        else
        {
            if (transform.position.z >= despawnZ)
            {
                _moving = false;
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Z = spawnZ の位置で出現し、通過開始</summary>
    public void SpawnAndPass(Vector3 position)
    {
        _reverse = false;
        UpdateForward();
        position.z = spawnZ;
        transform.position = position;
        gameObject.SetActive(true);
        _moving = true;
    }

    /// <summary>逆方向。Z = +80 から -80 へ通過（Loop4 異常電車用）</summary>
    public void SpawnAndPassReverse(Vector3 position)
    {
        _reverse = true;
        UpdateForward();
        position.z = -spawnZ;
        transform.position = position;
        gameObject.SetActive(true);
        _moving = true;
    }

    /// <summary>AbnormalTrain から逆走設定を渡す用</summary>
    public void ReverseDirection(bool reverse)
    {
        _reverse = reverse;
        UpdateForward();
    }
}
