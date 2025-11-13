using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Security.Cryptography;

public class DebagScript : MonoBehaviourPunCallbacks {
    [SerializeField] private TMP_Text roomInfoText;
    [SerializeField] private TMP_Text playerNumber;

    void Start() {
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
            roomInfoText.text = "サーバーに接続中...";
        }
        else {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster() {
        roomInfoText.text = "マスターサーバーに接続完了！\nロビーに参加中...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby() {
        roomInfoText.text = "ロビーに参加完了！\nルームを作成／参加中...";
        PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom() {
        UpdateRoomInfo();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        UpdateRoomInfo();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        UpdateRoomInfo();
    }

    private void UpdateRoomInfo() {
        if (PhotonNetwork.CurrentRoom != null) {
            string roomName = PhotonNetwork.CurrentRoom.Name;
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

            roomInfoText.text = $"ルーム名: {roomName}\n人数: {currentPlayers}/{maxPlayers}";
        }
        else {
            roomInfoText.text = "ルーム情報が取得できません。";
        }

        if (PhotonNetwork.IsMasterClient) {
            roomInfoText.text += "\n貴方がマスターです";
        }
       
    }

    private void Update() {
        DropPlayer myPlayer = DropGameManager.instance.GetMyPlayer();
        int myNumber = 114514;

        if (myPlayer != null) {
            myNumber = myPlayer.GetMyNumber();
        }


        playerNumber.text = $"\n貴方はP{myNumber}です";
    }
}