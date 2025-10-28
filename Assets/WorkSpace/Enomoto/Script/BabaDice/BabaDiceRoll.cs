using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BabaDiceRoll : DiceRoll
{
    public bool IsBabaRolled { get; private set; }
    public int BabaResult { get; private set; }

    public override void RollDice()
    {
        if (isRolling) return;
        base.RollDice(); // Šù‘¶‚Ì•¨—“]‚ª‚µˆ—
        IsBabaRolled = true;
    }

    protected override void OnDiceResult(int result)
    {
        BabaResult = result;
        Debug.Log($"yBABAƒ_ƒCƒXzo–Ú‚Í {BabaResult}");
    }
}