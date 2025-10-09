using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks {
    [SerializeField] private string roomName = "TestRoom"; // �f�t�H���g������
    [SerializeField] private GameObject playerPrefab;

    void Start() {
        Debug.Log("Photon�ɐڑ���...");
        PhotonNetwork.ConnectUsingSettings(); // �ڑ��J�n
    }

    public override void OnConnectedToMaster() {
        Debug.Log("�T�[�o�[�ɐڑ������I");
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        Debug.Log("[Photon] AppID: " + PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime);
        Debug.Log("[Photon] �g�p���̃��[�W����: " + PhotonNetwork.CloudRegion);
        Debug.Log("[Photon] �ʐM�^�C�v: " + PhotonNetwork.NetworkingClient.LoadBalancingPeer.TransportProtocol);
    }

    public override void OnJoinedRoom() {
        Debug.Log("�����ɎQ�����܂����I");
        // �v���C���[�𐶐�
        Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.Euler(0, -90, 0));
    }
}
