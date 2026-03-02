using UnityEngine;
using System.Collections;
using TMPro;

// 電車内シーン（TrainScene）の演出を管理する
// 一定時間後に「きさらぎ駅」到着演出 → KisaragiScene へ遷移
public class TrainController : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("電車が到着するまでの時間（秒）")]
    [SerializeField] private float arrivalDelay = 15f;
    [Tooltip("駅名アナウンスを表示する時間（秒）")]
    [SerializeField] private float announcementDuration = 5f;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI announcementText;
    [SerializeField] private TextMeshProUGUI stationNameText;
    [SerializeField] private AudioSource trainAmbientSource;
    [SerializeField] private AudioSource announcementSource;

    [Header("Train Door")]
    [SerializeField] private GameObject doorObject;

    private bool hasArrived = false;

    private void Start()
    {
        StartCoroutine(ArrivalSequence());
    }

    private IEnumerator ArrivalSequence()
    {
        // 電車走行中の演出
        if (announcementText != null)
            announcementText.text = "";

        // 走行中の待機
        yield return new WaitForSeconds(arrivalDelay * 0.5f);

        // 途中アナウンス
        ShowAnnouncement("まもなく、終点です。\nお忘れ物のないよう\nご注意ください。");
        yield return new WaitForSeconds(announcementDuration);
        HideAnnouncement();

        // さらに走行
        yield return new WaitForSeconds(arrivalDelay * 0.5f);

        // 到着アナウンス（異変あり）
        ShowAnnouncement("まもなく、\n━━━━━━ です。\nお出口は左側です。");

        yield return new WaitForSeconds(2f);

        // 駅名が「きさらぎ」に変化
        if (stationNameText != null)
        {
            stationNameText.text = "きさらぎ";
            stationNameText.color = new Color(0.8f, 0.8f, 0.8f);
        }

        ShowAnnouncement("まもなく、<color=red>きさらぎ</color> です。\nお出口は左側です。");

        yield return new WaitForSeconds(announcementDuration);
        HideAnnouncement();

        // ドアを開く演出
        if (doorObject != null)
            StartCoroutine(OpenDoor());

        hasArrived = true;

        // ドアが開いたことをプレイヤーに知らせる
        // → 遷移はプレイヤーがドアをクリックした時のみ（自動遷移なし）
        // OnBoardTrain() が InteractionSystem または CorrectObject から呼ばれる
        Debug.Log("[TrainController] ドアが開きました。ドアをクリックして乗車してください。");
    }

    private void ShowAnnouncement(string message)
    {
        if (announcementText != null)
        {
            announcementText.gameObject.SetActive(true);
            announcementText.text = message;
        }
    }

    private void HideAnnouncement()
    {
        if (announcementText != null)
            announcementText.gameObject.SetActive(false);
    }

    private IEnumerator OpenDoor()
    {
        if (doorObject == null) yield break;

        float elapsed = 0f;
        float duration = 1.5f;
        Vector3 startPos = doorObject.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 2.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            doorObject.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        doorObject.transform.localPosition = endPos;
    }

    // 外から呼べるAPI（CorrectObject などから）
    public void OnBoardTrain()
    {
        if (hasArrived && SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.LoadKisaragiScene();
    }
}
