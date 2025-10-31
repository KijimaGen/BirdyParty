/**
 * @file DropPanel.cs
 * @brief ドロップゲームで落ちる先のパネル壱枚壱枚のスクリプト
 * @author Sum1r3
 * @date 2025/10/30
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameConst;

public class DropPanel : MonoBehaviour{
    //自分のドロップゲームのバリエーション
    
    DropGamePanelVariation myVariation;

    
    void Update(){
        
    }

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == PLAYER_TAG) {
            if (DropGameManager.instance.CheckingAnswers(myVariation)) {
                Debug.Log("正解！");
            }
            else {
                Debug.Log("不正解！！！");
            }
        }
    }

    public void SetMyPanel() {
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
}
