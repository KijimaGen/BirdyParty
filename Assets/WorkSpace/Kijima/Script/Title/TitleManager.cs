/**
 * @file TitleManager.cs
 * @brief タイトルのマネージャー
 * @author Sum1r3
 * @date 2025/10/6
 */

using Cysharp.Threading.Tasks;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    //テキスト
    [SerializeField]
    private TextMeshProUGUI _resultText;
    //次へボタン（リザルト用）
    [SerializeField]
    private GameObject _resultNextButton;

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
    public async UniTask ShowRankingResult() {
        //次へボタンを非アクティブにする
        _resultNextButton.SetActive(false);
        //ランキングを取得
        var playerRankingList = PartyModeManager.instance.GetPlayerRankList();

        //ドラムロールの効果音
        _ = AudioManager.instance.PlaySE(6);

        
        //順位フレーム保管用のリスト
        List<GameObject> rankFrameList = new List<GameObject>();

        //ランキングを元にリザルト用のランキングUIを作成
        for (int i = 0, max = playerRankingList.Count ; i < max; i++) {
            //ランキングのi番目のプレイヤーの番号に合わせたUIを作成
            var rankFrame = Instantiate(_playerRankingFrame[playerRankingList[i].myNumber],UIMakeRoot.transform);
            //ランキングUI作成ルート検索
            GameObject rankUIRoot = rankFrame.transform.Find("PlayerRankRoot")?.gameObject;
            //ランキングUI作成
            if(rankUIRoot != null) {
                rankUIRoot.GetComponent<Image>().sprite = _playerRankingNumberUI[i];
            }
            //順位の枠に得点の反映をさせる
            rankFrame.transform.Find("ResultPointText").GetComponent<TextMeshProUGUI>().text = playerRankingList[i].myPoint.ToString();
            //順位フレーム保管リストに追加
            rankFrameList.Add(rankFrame);
            //アクティブを切っておく
            rankFrame.SetActive(false);
        }

        //ドラムロールの時間待ち
        await UniTask.Delay(4000);

        //逆順for文
        //順位を下から表示する
        for (int i = rankFrameList.Count - 1; i >= 0; i--) {
            //アクティブにしておく
            rankFrameList[i].SetActive(true);
            //ドラムロール締めの効果音
            _ = AudioManager.instance.PlaySE(7);
            //間を開ける
            await UniTask.Delay(1000);
        }

        //テキストを変更
        _resultText.text = playerRankingList[0].GetName() + "が優勝！！！";
        //次へボタンをアクティブにする
        _resultNextButton.SetActive(true);

    }

}
