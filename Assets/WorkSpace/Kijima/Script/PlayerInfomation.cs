/**
 * @file PlayerInfomation.cs
 * @brief プレイヤーの各々の情報
 * @author Sum1r3
 * @date 2025/10/14
 */
using Photon.Pun;
using System.Drawing;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConst;

public class PlayerInfomation:MonoBehaviour{
    //今持っているポイント
    public int point;
    //現在の順位
    public int rank;
    //自分の名前
    public string Name;
    //自分のskin
    public SkinVariation mySkin;
    //自分の番号
    public int myNumber;

    //自身のフォトンビュー
    PhotonView photonView;
    //自身のモデルが動く場所
    [SerializeField]
    private Transform myObjectRoot;

    /// <summary>
    /// スタート
    /// </summary>
    void Start() {
        //ポイントを初期化
        point = 0;
        //自身のフォトンビュー取得
        photonView = GetComponent<PhotonView>();
        //自身の番号を取得
        myNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        // シーン読み込み時のコールバック登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    //自身が消えるときにコールバックを止める
    private void OnDisable() {
        // 忘れずに解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //一回実働オブジェクトを破壊
        DestroySelectedChildren();
        if (scene.name == RACEGAME_SCENE_NAME) {

        }
    }
    
    /// <summary>
    /// 自身の実働オブジェクトの中身を破壊
    /// </summary>
    public void DestroySelectedChildren() {
        if (myObjectRoot == null) return;

        // 子オブジェクトの孫を順番に破壊
        foreach (Transform grandChild in myObjectRoot) {
            if (grandChild != null)
                Destroy(grandChild.gameObject);
        }
    }

    public void LoadRaceScene() {

    }

    #region 各種ゲッターとセッター
    // Point
    public int GetPoint() { return point; }
    public void SetPoint(int value) { point = value; }

    // Rank
    public int GetRank() { return rank; }
    public void SetRank(int value) { rank = value; }

    // Name
    public string GetName() { return Name; }
    public void SetName(string value) { Name = value; }

    // Skin
    public SkinVariation GetMySkin() { return mySkin; }
    public void SetMySkin(SkinVariation value) { mySkin = value; }

    // Number
    public int GetMyNumber() { return myNumber; }
    public void SetMyNumber(int value) { myNumber = value; }

    #endregion
}

