using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class DebagScript : MonoBehaviourPunCallbacks {
    [SerializeField] private TMP_Text roomInfoText;

    void Start() {
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
            roomInfoText.text = "�T�[�o�[�ɐڑ���...";
        }
        else {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster() {
        roomInfoText.text = "�}�X�^�[�T�[�o�[�ɐڑ������I\n���r�[�ɎQ����...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby() {
        roomInfoText.text = "���r�[�ɎQ�������I\n���[�����쐬�^�Q����...";
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

            roomInfoText.text = $"���[����: {roomName}\n�l��: {currentPlayers}/{maxPlayers}";
        }
        else {
            roomInfoText.text = "���[����񂪎擾�ł��܂���B";
        }
    }
}