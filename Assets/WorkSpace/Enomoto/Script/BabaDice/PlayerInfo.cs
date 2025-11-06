using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public int PlayerID { get; private set; }
    public string PlayerName { get; private set; } 
    public int TotalScore { get; set; } = 0;
    public bool IsEliminated { get; set; } = false; // 脱落したかどうか
    public int EliminationTurn { get; set; } = 0;   // 何ターン目に脱落したか


    // １ターン中に出した出目（判定のために一時的に保存）
    public int CurrentDiceResult { get; set; } = 0;

    public PlayerInfo(int id, string name)
    {
        PlayerID = id;
        PlayerName = name;
    }

    public void Reset()
    { 
        TotalScore = 0;
        IsEliminated = false;
        EliminationTurn = 0;
        CurrentDiceResult = 0;
    }
}
