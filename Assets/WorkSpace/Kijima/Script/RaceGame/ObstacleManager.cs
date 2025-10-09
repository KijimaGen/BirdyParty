/**
 * @file ObstacleManager.cs
 * @brief 色を自由自在にするもの
 * @author Sum1r3
 * @date 2025/10/7
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour{
    //障害物と加速アイテム
    [SerializeField]
    private GameObject obstacle;
    [SerializeField]
    private GameObject boost;

    //障害物を置くに当たり、必要なランダムの上限ライン
    private const float MAX_Z = 10;
    private const float MIN_Z = -20;
    //スタートライン(ちゃんとマージンは取ってます)
    private const float MAX_X = 315;
    private const float MIN_X = -60;
    //アイテムの数のマックス
    private const int ITEM_MAX = 100;
    private const int ITEM_MIN = 50;

    /// <summary>
    /// 最初に行う処理
    /// </summary>
    void Start(){
        _ = InstantiateItem();
    }

    private async UniTask InstantiateItem() {
        //アイテムの数をランダムに決定
        int itemCount = Random.Range(ITEM_MIN, ITEM_MAX);
        for(int i = 0,max = itemCount; i < max; i++) {

            //アイテムの出現座標をランダムに生成
            Vector3 itemSpawnPos = Vector3.zero;
            itemSpawnPos.x = Random.Range(MIN_X, MAX_X);
            itemSpawnPos.z = Random.Range(MIN_Z, MAX_Z);
            itemSpawnPos.y = 0.05f;

            //実際にアイテムを生成
            int rand = Random.Range(0, 2);
            if (rand == 0) {
                transform.SpawnChildWorld(obstacle, itemSpawnPos, new Vector3(-90, 0, -90));
            }
            else {
                transform.SpawnChildWorld(boost, itemSpawnPos, new Vector3(-90, 0, -90));
            }
        }

        await UniTask.CompletedTask;
    }
}
