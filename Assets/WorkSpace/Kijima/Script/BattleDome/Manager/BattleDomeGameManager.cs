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
using UnityEngine.SceneManagement;
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
    //結果発表のキャンバス
    [SerializeField]
    private GameObject _resultCanvas;

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
       
        //制限時間テキストの更新
        UpdateLimitTimeUI();
        //結果発表キャンバスを表示させない
        _resultCanvas.SetActive(false);

        //開始のカウントダウン


        //制限時間をスタート
        _ = CountTime();

    }

    /// <summary>
    /// 制限時間を減らす
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
        //制限時間終了の効果音
        await AudioManager.instance.PlaySE(5);
        //リザルトキャンバスのランキング表示関数を呼ぶ
        _resultCanvas.GetComponent<BattleDomePointRanking>()?.SortRank();
        _resultCanvas.GetComponent<BattleDomePointRanking>()?.ShowRank();

        //PlayerInfomationのパーティモードスコア加算処理を呼び出す
        if (GameManager.instance.isPartyMode) {
            BattleDomePlayerManager.instance.PlayerListAddScore();
        }


        //一定時間結果表示の時間を取る
        _ = ShowPointRanking();
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

    /// <summary>
    /// 結果発表
    /// </summary>
    private async UniTask ShowPointRanking() {
        //結果発表を行うキャンバスを表示
        _resultCanvas.SetActive(true);
        //10秒まつ
        await UniTask.Delay(10000);

        //フェードアウト
        await FadeManager.instance.FadeOut();
        if (GameManager.instance != null && GameManager.instance.isPartyMode && PartyModeManager.instance != null) {
            // パーティ：次へ進めてルーレット（タイトル）へ戻す
            PartyModeManager.instance.OnMiniGameFinishedAndReturnToRoulette();
            return;
        }

        //画面遷移
        SceneManager.LoadScene(TITLE_SCENE_NAME);
    }

    // もしミニゲーム中にウィンドウを落としたらタイトルに戻るように
    private void OnApplicationQuit() {
        PlayerPrefs.SetInt(PartyModeManager.PREF_PARTY_RUNNING, 0);
        PlayerPrefs.SetInt(PartyModeManager.PREF_BACK_TO_PARTY, 0);
        PlayerPrefs.SetInt(PartyModeManager.PREF_PARTY_SHOW_RESULT, 0);
        PlayerPrefs.SetInt("ComeBackFromGame", 0);
        PlayerPrefs.Save();
    }
}
