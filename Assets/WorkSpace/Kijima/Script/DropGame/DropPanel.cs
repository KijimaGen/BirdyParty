/**
 * @file DropPanel.cs
 * @brief ドロップゲームで落ちる先のパネル壱枚壱枚のスクリプト
 * @author Sum1r3
 * @date 2025/10/30
 */
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameConst;

public class DropPanel : MonoBehaviour{
    //自分のパネルバリエーション
    DropGamePanelVariation myVariation;
    //プレイヤーにあげるポイント
    private const int TO_ADD_SCORE = 1;
    //スキンメッシュレンダラー
    private MeshRenderer myMeshRendrer;


    private void Awake() {
        DropGameManager.instance.AddPanelList(this);
        myMeshRendrer = GetComponent<MeshRenderer>();
    }

    private void Update() {
        //常に自身のバリエーションを反映
        SetMyVariation();
    }

    /// <summary>
    /// 正解不正解の判定
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other) {
        //プレイヤーだったら加算
        if(other.gameObject.tag == PLAYER_TAG) {

            //自身のプレイヤーのみ加算
            if (!other.gameObject.GetComponent<PhotonView>().IsMine && GameManager.instance.IsOnline())
                return;

            Debug.Log($"自分のバリエーション({myVariation})");

            //正解だったら加算
            if (DropGameManager.instance.CheckingAnswers(myVariation)) {
                //オンラインだった時
                if (GameManager.instance.IsOnline()) {
                    //キャッシュ
                    var player = other.GetComponent<DropGameScoreManager>();
                    player.AddScore(TO_ADD_SCORE);
                }
                //オフラインだった時
                else {
                    //キャッシュ
                    var player = other.GetComponent<DropPlayer>();
                    player.SetPoint(player.GetPoint() + TO_ADD_SCORE);
                }
                
                _ = AudioManager.instance.PlaySE(3);
            }
            //不正解だったら効果音だけ
            else {
                _=AudioManager.instance.PlaySE(4);
            }
        }
    }

    //自身のレンダラー
    public void SetMyVariation() {
        // 自分のRendererコンポーネントを取得
        Renderer renderer = GetComponent<Renderer>();

        //これだと元の名前が取得できる
        string originalName = renderer.sharedMaterial.name;

        // 元の名前で変換
        DropGamePanelVariation variation = DropGameManager.instance.GetMyVariationFromMaterial(originalName);

        //変更を自身に保存
        myVariation = variation;
    }

    /// <summary>
    /// 自身のパネルバリエーションを引き渡す
    /// </summary>
    /// <returns></returns>
    public DropGamePanelVariation GetPanelVariation() {
        return myVariation;
    }

    /// <summary>
    /// 自身のマテリアルを変える
    /// </summary>
    /// <param name="newMat"></param>
    public void SetMeshRenderer(Material newMat) {
        //一応nullチェック
        if(myMeshRendrer == null) {
            myMeshRendrer = GetComponent<MeshRenderer>();
        }
        myMeshRendrer.material = newMat;
    }
}
