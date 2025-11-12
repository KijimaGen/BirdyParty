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
        DiceGameManager manager = FindObjectOfType<DiceGameManager>();

        if (manager != null && playerInput != null)
        {
            // DiceRollはnullの可能性があるが、そのまま渡す
            manager.TryRegisterNewPlayer(playerInput, diceRoll, this);
        }
        else
        {
            // エラーログを修正: DiceRollがなくてもエラーにしない
            Debug.LogError("PlayerInputHandler: 必要なコンポーネント(DiceGameManagerまたはPlayerInput)が見つかりません。Prefabの設定を確認してください。", this);
        }
    }

    // Input Action Assetで定義したアクション名 'Roll' に対応する関数
    public void OnRoll()
    {
          if (diceRoll != null && diceRoll.isRolling) return;

          Debug.Log($"入力検知: {PlayerData.PlayerName} がロールを試行。");

          GameManager?.HandlePlayerRollInput(PlayerData);
    }
}
