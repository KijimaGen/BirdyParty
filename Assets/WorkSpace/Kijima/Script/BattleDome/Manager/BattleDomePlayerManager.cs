/**
* @file BattleDomePlayerManager.cs
* @brief バトルドームシーンのプレイヤー管理者
* @author Sum1r3
* @date 2025/11/26
*/

using System.Collections.Generic;
using UnityEngine;
using static CommonModule;

public class BattleDomePlayerManager : BattleDomeManagerOrigin {
    //自身の参照
    public static BattleDomePlayerManager instance;

    //プレイヤーを置く座標
    private readonly List<Vector3> _playerPositionList = new List<Vector3>();
    //プレイヤーを置く角度
    private readonly List<Vector3> _playerAngleList = new List<Vector3>();

    //プレイヤーリスト
    private List<BattleDomePlayer> _playerList = new List<BattleDomePlayer>();
    //プレイヤーの最大値
    private const int _PLAYER_MAX = 4;

    /// <summary>
    /// 初期化処理
    /// </summary>
    public override void Initialize() {
        instance = this;
        InitializePlayerPosition();
        InitializePlayerAngles();
    }

    /// <summary>
    /// プレイヤーの座標の初期化
    /// </summary>
    private void InitializePlayerPosition() {
        _playerPositionList.Add(new Vector3(-1f, -1.4f, -2.8f));
        _playerPositionList.Add(new Vector3(-0.9f, -1.8f, -4.3f));
        _playerPositionList.Add(new Vector3(2.5f, -1.3f, -1.3f));
        _playerPositionList.Add(new Vector3(-2.7f, -1.4f, 0.8f));
    }

    /// <summary>
    /// プレイヤーの角度の初期化
    /// </summary>
    private void InitializePlayerAngles() {
        _playerAngleList.Add(new Vector3(-39f, 0, 0));
        _playerAngleList.Add(new Vector3(-23f,180f, 0));
        _playerAngleList.Add(new Vector3(-43f, -90f, 0));
        _playerAngleList.Add(new Vector3(-39f, 90f, 0));
    }

    /// <summary>
    /// プレイヤーのエントリー
    /// </summary>
    /// <param name="entryPlayer"></param>
    public void Enty(BattleDomePlayer entryPlayer) {
        //これ途中参加限定だからそのうち変えると思う
        //プレイヤーの最大値確認
        if (_playerList.Count == _PLAYER_MAX) return;

        //エントリー重複確認
        if (IsEntry(entryPlayer)) return;
        //プレイヤーリストに登録
        _playerList.Add(entryPlayer);
    }

    /// <summary>
    /// 引数に来たプレイヤーがエントリー済みかどうか返す
    /// </summary>
    /// <param name="player"></param>
    public bool IsEntry(BattleDomePlayer player) {
        //リストの空っぽかチェック
        if (IsEmpty(_playerList)) return false;

        for(int i = 0, max = _playerList.Count; i < max; i++) {
            //リストの中に居たらエントリーしてる
            if (_playerList[i] == player) return true;
        }

        //for文を抜ける = エントリーしてない
        return false;
    }

    /// <summary>
    /// 引数に渡されたプレイヤーが何番目に来たプレイヤーかを渡す
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public int GetPlayerNumber(BattleDomePlayer player) {
        for (int i = 0, max = _playerList.Count; i < max; i++) {
            //リストの中に居たらその番号を渡す
            if (_playerList[i] == player) return i;
        }
        return -1;
    }

    /// <summary>
    /// プレイヤーのポジションをあげる
    /// </summary>
    /// <param name="player"></param>
    /// <param name="playerNumber"></param>
    public Vector3 GetPlayerPosition(int playerNumber) {
        //nullと未割当の値を弾く
        if (_playerPositionList == null) return Vector3.zero;
        if (playerNumber == -1) return Vector3.zero;
        if (playerNumber > _playerPositionList.Count) return Vector3.zero;

        //番号に対応したポジションを渡す
        return _playerPositionList[playerNumber];
    }

    /// <summary>
    /// プレイヤーのポジションをあげる
    /// </summary>
    /// <param name="player"></param>
    /// <param name="playerNumber"></param>
    public Vector3 GetPlayerRotation(int playerNumber) {
        //nullと未割当の値を弾く
        if (_playerAngleList == null) return Vector3.zero;
        if (playerNumber == -1) return Vector3.zero;
        if (playerNumber > _playerAngleList.Count) return Vector3.zero;

        //番号に対応したポジションを渡す
        return _playerAngleList[playerNumber];
    }

    /// <summary>
    /// プレイヤースコアマネージャーにスコア加算依頼
    /// </summary>
    /// <param name="point"></param>
    /// <param name="number"></param>
    public void AddScoreReqest(int point,int number) {
        if (IsEmpty(_playerList)) return;
        if(number >= _playerList.Count) return;
        //指定された番号のところに加算
        _playerList[number].gameObject.GetComponent<BattleDomePlayerScoreManager>().AddPoint(point);
    }

}
