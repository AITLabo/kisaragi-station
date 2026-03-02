using UnityEngine;

// きさらぎ駅 – 「見ていないと発生する恐怖」（一定時間ターゲットを見ていないとイベント発火）
public class GazeTrigger : MonoBehaviour
{
    [Tooltip("この Transform をプレイヤーが直視しているか判定する（null ならこのオブジェクト）")]
    public Transform target;
    [Tooltip("この秒数見ていないと発火")]
    public float notLookingTime = 5f;
    [Tooltip("発火するイベントID（EventManager に登録されていること）")]
    public string eventID = "BehindFootstep";

    private float _timer;

    private void Start()
    {
        if (target == null) target = transform;
    }

    private void Update()
    {
        if (IsLookingAtTarget())
            _timer = 0f;
        else
        {
            _timer += Time.deltaTime;
            if (_timer >= notLookingTime)
            {
                TriggerFear();
                _timer = 0f;
            }
        }
    }

    private bool IsLookingAtTarget()
    {
        if (Camera.main == null || target == null) return false;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out var hit, 50f))
            return hit.transform == target;
        return false;
    }

    private void TriggerFear()
    {
        EventManager.Instance?.TryTriggerEvent(eventID);
    }
}
