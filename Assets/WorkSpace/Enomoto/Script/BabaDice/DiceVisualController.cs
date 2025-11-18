using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceVisualController : MonoBehaviour
{
    [Header("Dice UI Elements")]
    [SerializeField] private GameObject dice1;
    [SerializeField] private GameObject dice2;
    [SerializeField] private GameObject dice3;
    [SerializeField] private GameObject dice4;
    [SerializeField] private GameObject dice5;
    [SerializeField] private GameObject dice6;

    // DiceRollから呼ばれる表示メソッド
    public void DisplayDiceResult(int result)
    {
        GameObject[] dices = { dice1, dice2, dice3, dice4, dice5, dice6 };

        // 全て非表示
        foreach (var d in dices)
        {
            if (d != null) d.SetActive(false);
        }

        // 結果の出目のみ表示
        if (result >= 1 && result <= 6 && dices[result - 1] != null)
        {
            dices[result - 1].SetActive(true);
        }
    }
}
