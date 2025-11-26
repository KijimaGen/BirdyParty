/**
* @file BattleDomeManager.cs
* @brief バトルドームの各種マネージャー初期化処理を任せる
* @author Sum1r3
* @date 2025/11/26
*/using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDomeManager : MonoBehaviour{
    [Header("各種マネージャープレファブ")]
    [SerializeField]
    private List<BattleDomeManagerOrigin> _battleDomeManagerList = new List<BattleDomeManagerOrigin>();


    /// <summary>
    /// 各種マネージャーの生成と初期化処理呼び出し
    /// </summary>
    void Awake(){
        for(int i = 0, max = _battleDomeManagerList.Count; i < max; i++) {
            //オブジェクトの日実態を作成
            BattleDomeManagerOrigin ManagerObject = _battleDomeManagerList[i];
            //ぬるちぇっく
            if (ManagerObject == null) continue;
            // システムオブジェクト生成
            BattleDomeManagerOrigin createObject = Instantiate(ManagerObject, transform); 
            // 初期化
            createObject.Initialize();
        }
    }

}
