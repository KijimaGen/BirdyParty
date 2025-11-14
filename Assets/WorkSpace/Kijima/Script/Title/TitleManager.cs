/**
 * @file TitleManager.cs
 * @brief タイトルのマネージャー
 * @author Sum1r3
 * @date 2025/10/6
 */

using Photon.Pun;
using TMPro;
using UnityEngine;

public class TitleManager : MonoBehaviour{
    //ルームコードのテキスト
    [SerializeField]
    private TextMeshProUGUI roomCodeText;
    //次へボタン
    [SerializeField]
    private GameObject NextButton;
    //自身のインスタンス
    public static TitleManager instance;

    /// <summary>
    /// ルームコードのセット
    /// </summary>
    /// <param name="newCode"></param>
    public void SetRoomCode() {
        
        roomCodeText.text ="ルームコード : " +PhotonNetwork.CurrentRoom.Name;
        //オフラインだったら何も表示しない
        if (!GameManager.instance.IsOnline()) {
            roomCodeText.text = "";
        }
    }

    /// <summary>
    /// 次へボタンを消す
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveNextButton(bool isActive) {
        NextButton.SetActive(isActive);
    }


    private void Awake() {
        //インスタンスの作成
        if(instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
        
    }

}
