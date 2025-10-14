/**
 * @file GameConst.cs
 * @brief 定数定義
 * @author Sum1r3
 * @date 2025/10/6
 */

using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameConst {
    // プレイヤーの最大数
    public static readonly int PLAYER_MAX = 4;

    //プレイヤーのスキンの種類
    public enum SkinVariation {
        None,   //スキン梨
        Straw   //麦藁のトリィ
    }

    #region ミニゲームのシーン名一覧
    public static readonly string RACEGAME_SCENE_NAME = "Race";
    #endregion


}
