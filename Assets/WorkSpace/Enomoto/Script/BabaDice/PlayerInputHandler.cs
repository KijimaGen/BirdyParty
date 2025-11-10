using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // GameManagerからPlayerInfoとManagerの参照を設定される

    public PlayerInfo PlayerData; //{ get; set; }
    public DiceGameManager GameManager; //{ get; set; }

    private PlayerInput playerInput;
    private DiceRoll diceRoll;

    void Awake()
    {
        // 1. 必要なコンポーネントを取得
        playerInput = GetComponent<PlayerInput>();
        diceRoll = GetComponent<DiceRoll>();

        // 2. シーン上の唯一のDiceGameManagerを探す
        // FindObjectOfTypeはAwake()内で安全に実行できます。
        DiceGameManager manager = FindObjectOfType<DiceGameManager>();

        if (manager != null && playerInput != null && diceRoll != null)
        {
            // 3. Managerに新しいプレイヤーとして自身を登録
            // この呼び出しにより、PlayerInfoやGameManagerの参照がこのハンドラーに設定される
            manager.RegisterNewPlayerDice(playerInput, diceRoll, this);
        }
        else
        {
            Debug.LogError("PlayerInputHandler: 必要なコンポーネントまたはDiceGameManagerが見つかりません。Prefabの設定を確認してください。", this);
        }
    }

    // Input Action Assetで定義したアクション名 'Roll' に対応する関数
    public void OnRoll(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log($"入力検知: {PlayerData.PlayerName} がロールを試行。");

            GameManager?.HandlePlayerRollInput(PlayerData);
        }
    }

    public void OnTest(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("【緊急テスト】: OnTestが呼ばれました！");
        }
    }
}
