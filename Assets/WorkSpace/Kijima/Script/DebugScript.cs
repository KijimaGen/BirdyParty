/**
 * @file DebugScript.cs
 * @brief デバッグ用のスクリプト、主に動作確認用に使うよ
 * @author kijima
 * @date 2025/10/02
 */
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugScript : MonoBehaviour{
    

    void Start(){
        if (GameManager.instance != null && GameManager.instance.isPartyMode && PartyModeManager.instance != null) {
            // パーティ：次へ進めてルーレット（タイトル）へ戻す
            PartyModeManager.instance.OnMiniGameFinishedAndReturnToRoulette();
            return;
        }
        SceneManager.LoadScene("Title");
    }

    void Update(){

    }

}
