using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerController
{
    public string playerName;
    public bool isNPC;
    public bool isEliminated;
    public int totalScore;

    public void AddScore(int value)
    {
        totalScore += value;
        Debug.Log($"{playerName} の合計スコア: {totalScore}");
    }

    public void Eliminate()
    {
        isEliminated = true;
        Debug.Log($"{playerName} は脱落しました。");
    }
}
