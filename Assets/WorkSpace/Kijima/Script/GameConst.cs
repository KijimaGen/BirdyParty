/**
* @file GameConst.cs
* @brief 定数定義
* @author Sum1r3
* @date 2025/10/6
*/
using UnityEngine;

public static class GameConst {
    // プレイヤーの最大数
    public static readonly int PLAYER_MAX = 4;

    // プレイヤーのスキンの種類
    public enum SkinVariation {
        None,   // スキンなし
        Straw   // 麦わらのトリ
    }

    //ドロップゲームのリストのまま入れる
    public enum DropGamePanelVariation {
        None,   //一応何もないも実装
        Grape,  
        Banana,
        Apple,
        Melon,
        Momo,
        Orange,
        Mango,
        Pine,
        Stra
    }

    //プレイヤーのタグ名
    public static readonly string PLAYER_TAG = "Player";
    //ドロップゲームのポイントのキーの名前
    public static readonly string KEY_NAME_POINT = "Point";

    //各種プレイヤー色
    public static readonly Color[] PLAYER_COLOR = new Color[] {
        Color.blue, Color.red, Color.green, Color.yellow
    };

    //１秒
    public const int _DELAY_SECOND = 1000;

    #region シーン名一覧
    public static readonly string TITLE_SCENE_NAME = "Title";
    public static readonly string RACEGAME_SCENE_NAME = "Race"; 
    public static readonly string DROPGAME_SCENE_NAME = "DropBird";
    public static readonly string DICE_SCENE_NAME = "DiceGame";
    #endregion

    #region ミニゲームのアクション名マップ一覧
    public static readonly string DROPGAME_ACTION_NAME = "DropGame";
    #endregion

}
