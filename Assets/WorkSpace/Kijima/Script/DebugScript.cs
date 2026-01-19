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

public class DebugScript : MonoBehaviour{
    public TextMeshProUGUI firstGameName;
    public TextMeshProUGUI secondGameName;
    public TextMeshProUGUI thirdGameName;
    public TextMeshProUGUI NowGameCount;

    public List<string> GameNames = new List<string>();

    void Start(){
        
    }

    void Update(){
        GameNames = PartyModeManager.instance.GetChoicedGameList();
        if(GameNames.Count < 3) {
            GameNames.Add("A");
            GameNames.Add("B");
            GameNames.Add("C");
        }

        firstGameName.text = GameNames[0];
        secondGameName.text = GameNames[1];
        thirdGameName.text = GameNames[2];

        NowGameCount.text = PartyModeManager.instance.NowGameIndex.ToString();
    }

}
