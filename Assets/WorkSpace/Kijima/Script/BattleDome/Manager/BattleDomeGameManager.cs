/**
* @file BattleDomeManager.cs
* @brief バトルドームゲームフローの管理者
* @author Sum1r3
* @date 2025/11/26
*/
using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using static GameConst;

public class BattleDomeGameManager : BattleDomeManagerOrigin {
    //制限時間
    private int _limitTime;
    //マックス制限時間
    private const int _MAX_LIMIT_TIME = 60;

    //制限時間がこの数字以下だったら黒ボールを抽選する
    private const int _BLACK_BALL_TIME = 4;
    //ボールを子の変数の数だけ出す
    private int _ballMakeCount;
    //制限時間がこの数の倍数だったらボールカウントを上げる
    private const int _BALL_COUNT_UP_TIME = 10;

    //黄色ボールのプレファブ
    [SerializeField]
    private GameObject _yellowBallPrefab;
    //黒ボールのプレファブ
    [SerializeField]
    private GameObject _blackBallPrefab;

    //自身の制限時間を表示するキャンバス
    [SerializeField]
    private TextMeshProUGUI _limitTimeText;

    //ボールが出る間隔
    private const int _BALL_OUT_TIME = 2;
    //ボールのランダム出現座標の最大値
    private const float _BALL_POS_MAX = 10;
    //ボールのランダム出現座標の最大値
    private const float _BALL_POS_MIN = -10;
    //ボールのY座標(固定)
    private const float _BALL_POS_Y = 4;

    //ボールルート
    [SerializeField]
    private GameObject _ballRoot;
    //バトルドームプレファブ
    [SerializeField]
    private GameObject _battleDomePrefab;

    //=================================================================================
    //↑変数宣言部
    //=================================================================================

    /// <summary>
    /// 各種初期化設定
    /// </summary>
    public override void Initialize() {
        //各種変数初期化
        _limitTime = _MAX_LIMIT_TIME;
        _ballMakeCount = 0;
        //各種アイテム呼び出し
        Instantiate(_battleDomePrefab);
        //制限時間をスタート
        _ = CountTime();
        //制限時間テキストの更新
        UpdateLimitTimeUI();
    }

    /// <summary>
    /// 制限時間を測る
    /// </summary>
    /// <returns></returns>
    private async UniTask CountTime() {
        while(_limitTime > 0) {
            //ボールを呼び出すかどうかの判定(呼び出し)関数
            InstantiateBall();
            //制限時間によってボールを出す回数を変える
            CheckBallCount();
            //1秒待ち
            await UniTask.Delay(_DELAY_SECOND);
            _limitTime -= 1;
            //制限時間UIの更新
            UpdateLimitTimeUI();

        }
        await AudioManager.instance.PlaySE(5);
    }

    /// <summary>
    /// 現在の制限時間を引き渡す
    /// </summary>
    /// <returns></returns>
    public int GetTime() {
        return _limitTime;
    }

    /// <summary>
    /// 制限時間表示テキストの更新 
    /// </summary>
    private void UpdateLimitTimeUI() {
        _limitTimeText.text = _limitTime.ToString();
    }

    /// <summary>
    /// 制限時間が来たかどうかを返す
    /// </summary>
    /// <returns></returns>
    public bool GetIsEnd() {
        return _limitTime == 0;
    }

    /// <summary>
    /// ゲームが始まってるかどうかを返す
    /// </summary>
    /// <returns></returns>
    public bool GetIsStart() {
        return _limitTime < 60;
    }

    /// <summary>
    /// ボール呼び出し
    /// </summary>
    private void InstantiateBall() {
        //ここでもし制限時間が偶数だったらボールを呼び出す
        if (_limitTime % _BALL_OUT_TIME != 0) return;

        //ボールメイクカウント回作る
        for(int i = 0,max = _ballMakeCount; i < max; i++) {
            //座標をランダムで選出
            Vector3 BallPos = Vector3.zero;
            //X軸Z軸でそれぞれランダム抽選
            BallPos.x = Random.Range(_BALL_POS_MIN, _BALL_POS_MAX) / 10;    //　÷10して小数座標に
            BallPos.z = Random.Range(_BALL_POS_MIN, _BALL_POS_MAX) / 10;    //　÷10して小数座標に
            BallPos.y = _BALL_POS_Y;

            //オンラインとオフラインで処理を分ける
            if (GameManager.instance.IsOnline()) {
                //マスターじゃなかったら作らない
                if (!PhotonNetwork.IsMasterClient) return;

                
                GameObject ball = PhotonNetwork.Instantiate(_yellowBallPrefab.name, BallPos, Quaternion.identity);
                ball.transform.SetParent(_ballRoot.transform);
            }
            else {
                //黄色ボール固定呼び出し
                GameObject ball = Instantiate(_yellowBallPrefab, BallPos, Quaternion.identity);
                ball.transform.SetParent(_ballRoot.transform);
            }
        }
    }

    /// <summary>
    /// 制限時間によってボールの数を変える
    /// </summary>
    private void CheckBallCount() {
        //制限時間が_BALL_COUNT_UP_TIMEの倍数だったらボールを出す数アップ
        if (_limitTime % _BALL_COUNT_UP_TIME == 0)
            _ballMakeCount++;
    }

    //デバッグ用、サーバーに接続完了したときに部屋を作るorはいる
    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinOrCreateRoom("BattleDomeDebugRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }
}
