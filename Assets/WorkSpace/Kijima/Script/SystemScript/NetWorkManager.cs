using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks {
    [SerializeField] private string roomName = "TestRoom"; // デフォルト部屋名
    [SerializeField] private GameObject playerPrefab;

    void Start() {
        Debug.Log("Photonに接続中...");
        PhotonNetwork.ConnectUsingSettings(); // 接続開始
    }

    public override void OnConnectedToMaster() {
        Debug.Log("サーバーに接続完了！");
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom() {
        Debug.Log("部屋に参加しました！");
        // プレイヤーを生成
        Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}
