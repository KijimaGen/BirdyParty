/**
 * @file PartyModeManager.cs
 * @brief パーティモードの管理者
 * @author Sum1r3
 * @date 2026/01/14
 */
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class PartyModeManager : SystemObject {
    //抽選されたシーン
    private List<string> ChoicedSceneList = new List<string>();
    //今何個目のゲームをやっているか
    private int NowGameIndex;
    //プレイヤーのランキングリスト(必要かどうかは不明)

    //自身のインスタンス
    public static PartyModeManager instance;
    //初期化処理
    public override async UniTask Initialize() {
        //インスタンスを作成
        instance = this;

        //UniTaskの使命
        await UniTask.CompletedTask;
    }
}
