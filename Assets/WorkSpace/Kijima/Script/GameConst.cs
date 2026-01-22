/**
* @file GameConst.cs
* @brief 定数定義
* @author Sum1r3
* @date 2025/10/6
*/
using System.Collections.Generic;
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

    public enum BattleDomeBallVariation {
        Yellow,
        Black
    }

    #region シーン名一覧
    public static readonly string TITLE_SCENE_NAME = "Title";
    public static readonly string RACEGAME_SCENE_NAME = "Race"; 
    public static readonly string DROPGAME_SCENE_NAME = "DropBird";
    public static readonly string DICEGAME_SCENE_NAME = "DiceGame";
    public static readonly string BATTLEDOME_SCENE_NAME = "BattleDome";
    #endregion

    #region ミニゲームのアクションマップ一覧
    public static readonly string DROPGAME_ACTION_NAME = "DropGame";
    #endregion

    //プレイヤーの色のマテリアル一覧
    public static List<Material> PlayerColors = new List<Material>();

}
