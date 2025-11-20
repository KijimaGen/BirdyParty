using Photon.Pun;
using UnityEngine;
using System.Linq;

public class DicePlayerSpawner : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private DiceGame_GameManager gameManager; // インスペクターでセット、またはStartで取得

    private void Start()
    {
        if (gameManager == null) gameManager = GetComponent<DiceGame_GameManager>();
    }

    /// <summary>
    /// プレイヤー情報を受け取り、オンライン/オフライン判定を行ってダイスを生成する
    /// </summary>
    public void SpawnDiceForPlayer(PlayerInfomation playerInfo, GameObject prefab)
    {
        bool isOnline = GameManager.instance.IsOnline();

        // --- オンライン時の生成チェック ---
        if (isOnline)
        {
            // 自分の持ちキャラでなければ生成しない（PUNの原則）
            if (!playerInfo.GetComponent<PhotonView>().IsMine) return;

            SpawnOnline(playerInfo, prefab.name);
        }
        // --- オフライン時の生成チェック ---
        else
        {
            SpawnOffline(playerInfo, prefab);
        }
    }

    // オンライン生成 (PhotonNetwork.Instantiate)
    private void SpawnOnline(PlayerInfomation playerInfo, string prefabName)
    {
        Vector3 pos = GetSpawnPosition(PhotonNetwork.LocalPlayer.ActorNumber - 1);

        // ※注意: prefabNameはResourcesフォルダにあるプレハブ名と一致している必要があります
        GameObject obj = PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity);

        Debug.Log($"[Online] Spawned Dice for {playerInfo.myName}");
    }

    // オフライン生成 (Instantiate)
    private void SpawnOffline(PlayerInfomation playerInfo, GameObject prefab)
    {
        Vector3 pos = GetSpawnPosition(playerInfo.myNumber);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        obj.name = prefab.name; // (Clone)を消すなどの整形

        // オフライン時は手動でセットアップが必要
        var controller = obj.GetComponent<DiceController>();
        if (controller != null)
        {
            controller.SetupOffline(playerInfo.myNumber);
        }

        Debug.Log($"[Offline] Spawned Dice for {playerInfo.myName}");
    }

    // スポーン位置の計算ロジック
    private Vector3 GetSpawnPosition(int index)
    {
        if (gameManager != null && gameManager.playerSpawnPoints != null && gameManager.playerSpawnPoints.Length > 0)
        {
            // 配列外参照エラーを防ぐため剰余算を使用
            int safeIndex = Mathf.Abs(index) % gameManager.playerSpawnPoints.Length;
            return gameManager.playerSpawnPoints[safeIndex].position;
        }
        return Vector3.zero;
    }
}
