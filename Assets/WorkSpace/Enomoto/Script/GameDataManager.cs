using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDataManager : MonoBehaviourPunCallbacks {
    public static GameDataManager instance;

    public string selectedMiniGame;
    public bool comeBackFromGame;

    //オンラインか否か
    public bool playOnline;

    //タイトルに固定で置いてあるからこいつに鳥のモデル情報を持っててもらいたい
    [SerializeField]
    private List<GameObject> titleToriList = new List<GameObject>();

    //タイトルに固定で置いてあるからこいつに鳥のモデル情報を持っててもらいたい
    [SerializeField]
    private List<TextMeshProUGUI> titleToriNameList = new List<TextMeshProUGUI>();

    //テキストボックス
    [SerializeField]
    private GameObject textBox;
    //自分のキャンバス
    [SerializeField]
    private Transform myCanvas;

    void Awake() {
        //インスタンスの設定
        instance = this;
    }

    void OnDestroy() {
        // 自分がインスタンスの場合はクリア
        if (instance == this) {
            instance = null;
        }
    }

    /// <summary>
    /// リセット
    /// </summary>
    public void ResetData(){
        selectedMiniGame = null;
        comeBackFromGame = false;
    }

    /// <summary>
    /// ナンバー指定でプレイヤーを渡す
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public GameObject GetToriFromNumber(int i) {
        return titleToriList[i];
    }

    /// <summary>
    /// タイトルのプレイヤーのリストをあげる
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetToriList() {
        return titleToriList;
    }

    /// <summary>
    /// 何人エントリー済みのプレイヤーがいるのかを取得
    /// </summary>
    /// <returns></returns>
    public int GetEntriedPlayerCount() {
        int playerCount = 0;
        for(int i = 0,max = titleToriList.Count; i < max; i++) {
            if (titleToriList[i].activeSelf)
                playerCount++;
        }
        return playerCount;
    }

    /// <summary>
    /// プレイヤーの名前を入れる
    /// </summary>
    /// <param name="player"></param>
    public void EntryPlayer(PlayerInfomation player) {
        //名前がないかつオンラインだったらエラーが発生するので行わない
        if (player.myName != "" && !GameManager.instance.IsOnline()) {
            //プレイヤーリストにプレイヤーの名前を登録
            titleToriNameList[player.myNumber].text = player.myName;
        }
        //エントリ～したときにプレイヤーの人数が2人以上だったら
        if (GetEntriedPlayerCount() < 2) return;
        //オンラインかつホストじゃなかったらリターン
        if (GameManager.instance.IsOnline() && !PhotonNetwork.IsMasterClient) return;
        //オフラインもしくは、オンラインかつマスターだったら
        TitleManager.instance.SetActiveNextButton(true);
    }

    /// <summary>
    /// プレイヤーのエントリーを外す
    /// </summary>
    /// <param name="player"></param>
    public void WithdrawPlayer(PlayerInfomation player) {
        titleToriNameList.RemoveAt(player.myNumber);
    }

    /// <summary>
    /// エントリーしましたテキストを作る
    /// </summary>
    /// <param name="name"></param>
    [PunRPC]
    public void InstantiateNameBox(string name) {
        //エントリーボックス型をもらう
        EntryTextBox box = textBox.GetComponent<EntryTextBox>();
        //なかったらリターン
        if (box == null) return;
        //座標は元から置いてあるのでそのまま生成
        EntryTextBox newNameBox = Instantiate(box, myCanvas);
        //ボックスの中身の設定
        newNameBox.SetmyText(name);
    }

    /// <summary>
    /// 見た目上エントリーしているプレイヤーを見えなくする
    /// </summary>
    public void AllToriListEliminate() {
        for(int i =0,max = titleToriList.Count; i < max; i++) {
            titleToriList[i].gameObject.SetActive(false);
        }
    }
}
