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
    public TextMeshProUGUI firstPlayerPoint;
    public TextMeshProUGUI secondPlayerPoint;
    public TextMeshProUGUI thirdPlayerPoint;
    public TextMeshProUGUI forthPlayerPoint;
   

    public List<PlayerInfomation> Players = new List<PlayerInfomation>();
    public List<int> PlayerPoints = new List<int>();

    void Start(){
        
    }

    void Update(){
        
        Players = PartyModeManager.instance.GetPlayerRankList();
        
        for(int i = 0,max = Players.Count; i< max; i++) {
            if (Players[i] == null)
                continue;
            PlayerPoints[i] = Players[i].point;
        }

        if(PlayerPoints.Count < 4) {
            PlayerPoints.Add(0);
            PlayerPoints.Add(0);
            PlayerPoints.Add(0);
            PlayerPoints.Add(0);
        }

        firstPlayerPoint.text =  PlayerPoints[0].ToString();
        secondPlayerPoint.text = PlayerPoints[1].ToString();
        thirdPlayerPoint.text =  PlayerPoints[2].ToString();
        forthPlayerPoint.text =  PlayerPoints[2].ToString();


        
    }

}
