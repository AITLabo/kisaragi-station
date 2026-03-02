using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// きさらぎ駅 – セーブ/ロード（JSON・簡易 XOR 暗号化・AutoBackup 対応）
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool useEncryption = true;
    [SerializeField] private byte encryptionKey = 0x5A;
    [SerializeField] private bool autoBackup = true;

    private string _path;
    private string PathJson => _path ??= Application.persistentDataPath + "/save.json";
    private string PathBackup => Application.persistentDataPath + "/save_backup.json";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SaveGame()
    {
        var data = new SaveData();
        if (GameManager.Instance != null)
            data.loop = GameManager.Instance.CurrentLoop;
        if (ActionManager.Instance != null)
            data.step = ActionManager.Instance.GetCurrentStep();
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var t = player.transform.position;
            data.posX = t.x; data.posY = t.y; data.posZ = t.z;
        }
        if (EventManager.Instance != null)
            data.triggeredEvents = EventManager.Instance.GetTriggeredEvents()?.ToArray();

        string json = JsonUtility.ToJson(data, true);
        if (useEncryption) json = XorEncrypt(json);
        if (autoBackup && File.Exists(PathJson))
            File.Copy(PathJson, PathBackup, true);
        File.WriteAllText(PathJson, json);
    }

    public void LoadGame()
    {
        if (!File.Exists(PathJson)) return;
        string json = File.ReadAllText(PathJson);
        if (useEncryption) json = XorEncrypt(json);
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        if (GameManager.Instance != null) GameManager.Instance.LoadState(data.loop);
        if (ActionManager.Instance != null) ActionManager.Instance.SetStep(data.step);
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        if (EventManager.Instance != null)
            EventManager.Instance.SetTriggeredEvents(data.triggeredEvents != null ? new List<string>(data.triggeredEvents) : null);
    }

    private string XorEncrypt(string s)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)(chars[i] ^ encryptionKey);
        return new string(chars);
    }
}
