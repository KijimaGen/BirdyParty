/**
 * @file TitleManager.cs
 * @brief タイトルのマネージャー
 * @author Sum1r3
 * @date 2025/10/6
 */

using Photon.Pun;
using System.Collections.Generic;
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

    //プレイヤーの名前を書き込む先のリスト
    [SerializeField]
    private List<TextMeshProUGUI> _playerNameList;

    //
    [Header("リザルトで使う用の変数")]
    //プレイヤーのポイントのフレーム
    [SerializeField]
    private List<GameObject> _playerRankingFrame; 
    //プレイヤーの順位UI
    [SerializeField]
    private List<Sprite> _playerRankingNumberUI;

    //UIを作るルート
    [SerializeField]
    private GameObject UIMakeRoot;

    /// <summary>
    /// ルームコードのセット
    /// </summary>
    /// <param name="newCode"></param>
    public void SetRoomCode() {
        //ルームコードを設定
        roomCodeText.text ="ルームコード : " +PhotonNetwork.CurrentRoom.Name;
        //オフラインだったら何も表示しない
        if (!GameManager.instance.IsOnline()) {
            roomCodeText.text = "";
        }
    }

    /// <summary>
    /// 次へボタンを出したり消したり
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveNextButton(bool isActive) {
        NextButton.SetActive(isActive);
    }

    private void Start() {
        //インスタンスの作成
        if(instance == null) {
            instance = this;
        }else {
            Destroy(gameObject);
        }
        AudioManager.instance.PlayBGM(0);
    }


    /// <summary>
    /// 対象のリストの名前をプレイヤーの物にする
    /// </summary>
    /// <param name="PlayerIndex"></param>
    /// <param name="PlayerName"></param>
    public void SetPlayerNameList(int PlayerIndex,string PlayerName) {
        _playerNameList[PlayerIndex].text = PlayerName;
    }

    /// <summary>
    /// タイトルのリザルトにプレイヤーのリストを表示させる
    /// </summary>
    public void ShowRankingResult() {
        //ランキングを取得
        var playerRankingList = PartyModeManager.instance.GetPlayerRankList();
        //ランキングを元にリザルト用のランキングUIを作成
        for(int i = 0, max = playerRankingList.Count ; i < max; i++) {
            //ランキングのi番目のプレイヤーの番号に合わせたUIを作成
            var rankFrame = Instantiate(_playerRankingFrame[playerRankingList[i].myNumber],UIMakeRoot.transform);
            //ランキングUI作成ルート検索
            var rankUIRoot = rankFrame.transform.GetChild(2);
            //ランキングUI作成
            Instantiate(_playerRankingNumberUI[i],rankUIRoot.transform);
        }
    }

}
