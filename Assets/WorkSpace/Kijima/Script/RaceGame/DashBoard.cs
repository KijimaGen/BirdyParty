/**
 * @file DashBoad.cs
 * @brief レースゲームのダッシュボード(加速アイテム)
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashBoard: MonoBehaviour{
    /// <summary>
    /// 入処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Player") {
            other.gameObject.GetComponent<RacePlayer>().Boost();
        }
    }
}
