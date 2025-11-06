using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropGameManaCreater : MonoBehaviourPunCallbacks {
    [SerializeField]
    GameObject DropGameManager;
    public override void OnJoinedRoom() {
        //マスターだったら
        if(PhotonNetwork.IsMasterClient)
            // ルームに入ったらInstantiateする
            PhotonNetwork.Instantiate(DropGameManager.name, Vector3.zero, Quaternion.identity);
        Destroy(this.gameObject);
    }

    public void Awake() {
        if (GameManager.instance == null)
            return;

        if (!GameManager.instance.IsOnline()) {
            //Instantiate(DropGameManager);
            //Destroy(this.gameObject);
        }
    }
}
