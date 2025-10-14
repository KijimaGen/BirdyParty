/**
 * @file Hurdle.cs
 * @brief レースゲームのハードル(障害物)
 * @author Sum1r3
 * @date 2025/10/06
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hurdle : MonoBehaviour{
    /// <summary>
    /// 入処理
    /// </summary>
    /// <param Name="other"></param>
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Player") {
            other.gameObject.GetComponent<RacePlayer>().Slow();
        }
    }
}
