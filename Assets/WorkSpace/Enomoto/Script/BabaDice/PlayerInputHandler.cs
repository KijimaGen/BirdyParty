using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// コントローラー等が接続されたらサイコロを動かせるようにする。（複数プレイヤー対応用）
public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInfo PlayerData { get; set; }
    public DiceGameManager GameManager { get; set; }

    // ダイスを転がす際に使用（InputSystemから呼びだし）
    public void OnDiceRoll(InputValue value)
    {
        if (value.isPressed)
        {
            GameManager?.HandlePlayerRollInput(PlayerData);
        }
    }
}
