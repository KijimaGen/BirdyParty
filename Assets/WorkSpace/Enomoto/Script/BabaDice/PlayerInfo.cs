using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class PlayerInfo
{
    public int PlayerID { get; private set; }
    public string PlayerName { get; private set; } 
    public int TotalScore { get; set; } = 0;
    public bool IsEliminated { get; set; } = false;
    public int EliminationTurn { get; set; } = 0;

    public int CurrentDiceResult { get; set; } = 0;

    public PlayerInfo(int id, string name)
    {
        PlayerID = id;
        PlayerName = name;
    }

    public void Reset()
    { 
        IsEliminated = false;
        EliminationTurn = 0;
        CurrentDiceResult = 0;
    }

    public void ResetTurnResult()
    {
        CurrentDiceResult = 0;
    }
}
