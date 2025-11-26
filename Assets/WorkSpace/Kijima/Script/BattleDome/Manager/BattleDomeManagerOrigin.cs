/**
* @file BattleDomeManagerOrigin.cs
* @brief バトルドームシーンのマネージャークラスの基底クラス
* @author Sum1r3
* @date 2025/11/26
*/
using Photon.Pun;

public abstract class BattleDomeManagerOrigin : MonoBehaviourPunCallbacks {
    /// <summary>
    /// 初期化処理
    /// </summary>
    public abstract void Initialize();
}
