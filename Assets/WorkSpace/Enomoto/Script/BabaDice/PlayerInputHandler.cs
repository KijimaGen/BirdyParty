using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // GameManagerからPlayerInfoとManagerの参照を設定される

    public PlayerInfo PlayerData { get; set; }
    public BABADiceGameManager GameManager { get; set; }

    private PlayerInput playerInput;
    private DiceRoll diceRoll;

    public DiceRoll DiceRollComponent => diceRoll;

    void Awake()
    {
        // 1. 必要なコンポーネントを取得
        playerInput = GetComponent<PlayerInput>();
        diceRoll = GetComponent<DiceRoll>(); 

        // 2. シーン上の唯一のDiceGameManagerを探す
        BABADiceGameManager manager = FindObjectOfType<BABADiceGameManager>();

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

        if (playerInput != null)
        {
            Debug.Log($"[Input Check] PlayerInput Index: {playerInput.playerIndex} | GameObject: {gameObject.name}");
        }

        if (manager != null && playerInput != null)
        {
            manager.TryRegisterNewPlayer(playerInput, diceRoll, this);
        }
    }

    // Input Action Assetで定義したアクション名 'Roll' に対応する関数
    public void OnRoll()
    {
        // 1. 必要な参照が設定されているかチェック
        if (GameManager == null || PlayerData == null)
        {
            Debug.LogError("PlayerInputHandler: GameManagerまたはPlayerDataが未設定です。", this);
            return;
        }

        // 2. ダイスが回っている、または脱落している場合は拒否
        if (diceRoll != null && diceRoll.isRolling) return;
        if (PlayerData.IsEliminated) return;

        // 3. 【重要】ゲームの状態と、既にロールしたかをチェック
        // PlayerRolling 状態でのみ入力受付。かつ、CurrentDiceResultが0（未ロール）であること。
        if (BABADiceGameManager.currentState != GameState.PlayerRolling || PlayerData.CurrentDiceResult > 0)
        {
            // Debug.Log($"ロール入力拒否: 状態={DiceGameManager.currentState}, ロール済み={PlayerData.CurrentDiceResult > 0}");
            return;
        }

        // 4. 入力処理を DiceGameManager に委譲

        if (GameManager.IsOnline())
        {
            // オンライン時: Master ClientにRPCを投げて、Masterにロール開始を依頼する
            if (GameManager.photonView != null)
            {
                GameManager.photonView.RPC(
                    "HandlePlayerRollInput",
                    RpcTarget.MasterClient,
                    PlayerData.PlayerName
                );
                Debug.Log($"入力検知: {PlayerData.PlayerName} が MasterClient にロールリクエストを送信。");
            }
        }
        else
        {
            // オフライン時: 即座に処理（PlayerInfoを渡すオーバーロードを呼ぶ）
            GameManager.HandlePlayerRollInput(PlayerData);
            Debug.Log($"入力検知: {PlayerData.PlayerName} がオフラインロールを試行。");
        }
    }
}
