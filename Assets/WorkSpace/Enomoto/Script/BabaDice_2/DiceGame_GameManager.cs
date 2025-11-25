using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用する場合
using Cysharp.Threading.Tasks;
using System.Linq; // LINQを使用
// using static GameConst; // GameConstの依存関係があれば解除

// --------------------------------------------------
// DiceGame専用のゲーム状態列挙体
// --------------------------------------------------
public enum E_DICE_GAME_STATE
{
    WAITING_FOR_PLAYERS, // プレイヤー待機中
    INITIALIZING,       // 初期化中
    TURN_START,         // ターン開始 (入力待ち)
    DICE_ROLLING,       // ダイスが転がっている最中
    RESULT_CALCULATING, // 結果計算中
    TURN_END,           // ターン終了
    GAME_END            // ゲーム終了
}
// --------------------------------------------------

/// <summary>
/// ダイスゲーム全体の進行管理、BABA判定、スコア計算、UI表示を行う
/// </summary>
public class DiceGame_GameManager : MonoBehaviourPunCallbacks
{

    public static DiceGame_GameManager instance;

    // プレイヤーごとの1〜6の出目画像を保持するためのシリアライズ可能なクラス
    [System.Serializable]
    public class DiceFaceImageSet
    {
        // 1〜6の出目を保持する配列 (インスペクターで表示される)
        [Tooltip("出目 1 から 6 に対応する画像を順に登録してください。")]
        public Sprite[] faces = new Sprite[6];
    }

    // --- 【ゲーム進行管理変数】 ---
    [Header("Game Settings")]
    [SerializeField] private int maxTurns = 5;
    [SerializeField] private float rollTimeLimit = 5.0f; // 制限時間

    // 現在のゲーム状態
    [SerializeField]
    private E_DICE_GAME_STATE gameState = E_DICE_GAME_STATE.WAITING_FOR_PLAYERS;

    // 現在のターン数とプレイヤー番号
    [SerializeField] private int currentTurn = 1;
    [SerializeField] private int currentPlayerNumber = 1;

    /// <summary>ダイスを振れる状態かどうかのプロパティ</summary>
    public bool CanRoll => gameState == E_DICE_GAME_STATE.TURN_START;

    // ダイス結果を一時的に保持する辞書
    private Dictionary<int, int> turnResults = new Dictionary<int, int>();

    // --- 【UI & Prefab】 ---
    [Header("Prefabs & Materials")]
    [SerializeField] private string dicePrefabName = "PlayerDice"; // Resourcesフォルダ内のPrefab名
    [SerializeField] private Material[] playerMaterials; // プレイヤーごとのマテリアル（4色）

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] diceSpawnPoints; // ダイス生成位置のTransform配列

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI infoText; // ゲーム全体の情報表示テキスト
    [SerializeField] private TextMeshProUGUI turnText; // 現在のターン数表示テキスト

    // プレイヤーごとの結果画像表示 (UI Image)
    // プレイヤー番号(1〜)とインデックス(0〜)の対応に注意
    [SerializeField] private Image[] playerResultImages;

    // 出目画像セット (プレイヤー数分)
    [SerializeField] private DiceFaceImageSet[] diceFaceImageSets;


    // --- 【ダイス管理】 ---
    private Dictionary<int, DiceController> activeDices = new Dictionary<int, DiceController>();

    private void Awake()
    {
        // インスタンス設定
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲームの初期化 (SystemObject.Initializeを代替)
    /// </summary>
    public async UniTask InitializeGame()
    {
        // ... (他の初期化処理) ...

        // 初期状態を設定
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(SetGameStateRPC), RpcTarget.All, E_DICE_GAME_STATE.INITIALIZING);
            StartGameSetup().Forget();
        }
        else
        {
            // 非マスタークライアントもUIを初期化
            UpdateInfoText("Waiting for Master Client to start...");
            // ターン数の表示を初期化
            UpdateTurnTextUI(currentTurn);
        }

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// ゲームの初期セットアップ (マスタークライアントのみ実行)
    /// </summary>
    private async UniTask StartGameSetup()
    {
        UpdateInfoText("Initializing...");

        // プレイヤーの準備が整うまで待機するロジックをここに追加
        await UniTask.Delay(System.TimeSpan.FromSeconds(1));

        SpawnDices();

        // 最初のターン開始
        GoToNextPlayerTurn();
    }

    /// <summary>
    /// ダイスを生成し、初期化する (MasterClientのみ実行)
    /// </summary>
    public void SpawnDices()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        List<PlayerInfomation> activePlayers = PlayerManager.instance.GetPlayerList();

        for (int i = 0; i < activePlayers.Count; i++)
        {
            // PlayerInfomationクラスの定義が必要
            PlayerInfomation player = activePlayers[i];
            int playerNumber = player.GetMyNumber();

            // 生成位置をSpawnPointsから取得 (iは0から始まるインデックス)
            Vector3 spawnPos = (i < diceSpawnPoints.Length && diceSpawnPoints[i] != null)
                                ? diceSpawnPoints[i].position
                                : new Vector3(0, 5, 0) + Vector3.right * i * 2f;

            GameObject diceObj = PhotonNetwork.InstantiateRoomObject(
                dicePrefabName,
                spawnPos,
                Quaternion.identity
            );

            DiceController diceController = diceObj.GetComponent<DiceController>();

            if (diceController != null)
            {
                // 全員に対してRPCでダイスを初期化
                diceController.photonView.RPC(nameof(DiceController.InitializeDice), RpcTarget.All, playerNumber);
                Debug.Log($"Spawned Dice for Player {playerNumber} at {spawnPos}");
            }
        }
    }

    /// <summary>
    /// ダイスをリストに登録 (全クライアント実行)
    /// DiceController.InitializeDiceから呼ばれる
    /// </summary>
    public void RegisterDice(int playerNumber, DiceController dice)
    {
        activeDices[playerNumber] = dice;
        Debug.Log($"Dice for Player {playerNumber} registered.");
    }

    /// <summary>
    /// DiceGamePlayer.csから呼ばれるゲッター
    /// </summary>
    public DiceController GetDiceForPlayer(int playerNumber)
    {
        if (activeDices.ContainsKey(playerNumber))
        {
            return activeDices[playerNumber];
        }
        return null;
    }

    /// <summary>
    /// プレイヤー番号に対応するマテリアルを取得
    /// </summary>
    /// <param name="index">プレイヤー番号 (1から始まる)</param>
    public Material GetPlayerMaterial(int index)
    {
        // プレイヤー番号は1から始まるため、インデックスは-1
        int actualIndex = index - 1;

        if (playerMaterials != null && actualIndex >= 0 && actualIndex < playerMaterials.Length)
        {
            return playerMaterials[actualIndex];
        }
        // マテリアルが見つからなかった場合、エラーログを出力
        Debug.LogWarning($"Player Material not found for player number {index}. Check DiceGameManager Inspector.");
        return null; // マテリアルが見つからない場合はnullを返す
    }

    // --------------------------------------------------
    // ★ ゲーム進行管理の核となるメソッド ★
    // --------------------------------------------------

    /// <summary>
    /// ダイス結果をDiceControllerから受け取る (全クライアント実行)
    /// </summary>
    /// <param name="playerNumber">結果を出したプレイヤー番号</param>
    /// <param name="resultValue">ダイスの出目 (1-6)</param>
    [PunRPC]
    public void ReportDiceResult(int playerNumber, int resultValue)
    {
        Debug.Log($"Player {playerNumber} reported dice result: {resultValue}");

        // 結果をUIに表示
        UpdateResultImage(playerNumber, resultValue);

        // 結果を一時保存
        if (turnResults.ContainsKey(playerNumber))
        {
            turnResults[playerNumber] = resultValue;
        }
        else
        {
            turnResults.Add(playerNumber, resultValue);
        }

        // マスタークライアントで結果の集計と次の進行を判定
        if (PhotonNetwork.IsMasterClient)
        {
            if (turnResults.Count == PlayerManager.instance.GetPlayerList().Count)
            {
                // 全員の結果が出たら計算フェーズへ
                photonView.RPC(nameof(SetGameStateRPC), RpcTarget.All, E_DICE_GAME_STATE.RESULT_CALCULATING);
                CalculateTurnResults(); // 結果の集計処理を実行
            }
        }
    }

    /// <summary>
    /// 結果計算フェーズ (マスタークライアントのみ実行)
    /// </summary>
    private void CalculateTurnResults()
    {
        // BABA（ババ）の判定ロジックをここに追加
        // ... (例: 最も小さい出目を出したプレイヤーを特定し、そのプレイヤーにババを渡す)

        UpdateInfoText("Results Calculated! Next Turn in 3 seconds.");

        // ターン終了処理へ
        StartCoroutine(GoToNextTurnSequence());
    }

    private IEnumerator GoToNextTurnSequence()
    {
        // ターン終了UI表示など
        yield return new WaitForSeconds(3.0f);

        // ターン数チェック
        if (currentTurn >= maxTurns)
        {
            EndGame();
        }
        else
        {
            // 次のターンへ移行
            GoToNextPlayerTurn();
        }
    }

    /// <summary>
    /// 次のターンに移行する (マスタークライアントのみ実行)
    /// </summary>
    private void GoToNextPlayerTurn()
    {
        currentTurn++; // ターン数を増やす
        turnResults.Clear(); // 結果をリセット

        // UIの結果画像を非表示にするなど、リセット処理
        // ResetResultImages();

        // ターン数の更新を全クライアントにRPCで通知
        photonView.RPC(nameof(UpdateTurnTextRPC), RpcTarget.All, currentTurn);

        // 状態をTURN_STARTに戻す
        photonView.RPC(nameof(SetGameStateRPC), RpcTarget.All, E_DICE_GAME_STATE.TURN_START);

        // 情報テキストを更新
        UpdateInfoText($"Turn {currentTurn} Start! Roll the Dice.");
    }

    /// <summary>
    /// ゲーム終了処理 (マスタークライアントのみ実行)
    /// </summary>
    private void EndGame()
    {
        photonView.RPC(nameof(SetGameStateRPC), RpcTarget.All, E_DICE_GAME_STATE.GAME_END);
        UpdateInfoText("Game Set!");

        // 順位付けロジック
        // PlayerInfomationのPointを見てRank付け
        // ...

        // GameManagerに戻る等の処理
        StartCoroutine(ReturnToTitle());
    }

    private IEnumerator ReturnToTitle()
    {
        yield return new WaitForSeconds(3.0f);

        // GameManager.csの定義にOnBackToSelect()が存在することを前提とする
        if (GameManager.instance != null)
        {
            GameManager.instance.OnBackToSelect();
        }
    }

    // --------------------------------------------------
    // ★ UI制御メソッド (RPCで呼ばれるものを含む) ★
    // --------------------------------------------------

    /// <summary>
    /// ゲーム情報テキストを更新 (全クライアント実行)
    /// </summary>
    private void UpdateInfoText(string msg)
    {
        if (infoText) infoText.text = msg;
    }

    /// <summary>
    /// ターン数表示を更新するRPC (全クライアント実行)
    /// </summary>
    [PunRPC]
    private void UpdateTurnTextRPC(int newTurn)
    {
        currentTurn = newTurn;
        // RPCで受け取った値を使ってUIを更新
        UpdateTurnTextUI(newTurn);
    }

    /// <summary>
    /// ターン数表示UIをローカルで更新する
    /// </summary>
    private void UpdateTurnTextUI(int turn)
    {
        if (turnText != null)
        {
            turnText.text = $"Turn: {turn} / {maxTurns}";
        }
    }

    /// <summary>
    /// ダイスの出目画像を更新 (全クライアント実行)
    /// </summary>
    /// <param name="playerNumber">プレイヤー番号 (1から)</param>
    /// <param name="resultValue">出目 (1から6)</param>
    private void UpdateResultImage(int playerNumber, int resultValue)
    {
        // プレイヤー番号(1〜)をインデックス(0〜)に変換
        int playerIndex = playerNumber - 1;

        // resultValue (1〜6) を画像のインデックス (0〜5) に変換
        int imageIndex = resultValue - 1;

        if (playerIndex >= 0 && playerIndex < playerResultImages.Length && playerResultImages[playerIndex] != null)
        {
            if (playerIndex < diceFaceImageSets.Length && imageIndex >= 0 && imageIndex < diceFaceImageSets[playerIndex].faces.Length)
            {
                playerResultImages[playerIndex].sprite = diceFaceImageSets[playerIndex].faces[imageIndex];
                playerResultImages[playerIndex].gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Image set or face index out of bounds for Player {playerNumber}. Result: {resultValue}");
            }
        }
        else
        {
            Debug.LogWarning($"Player result image UI not found for Player {playerNumber}. Check Inspector setup.");
        }
    }

    /// <summary>
    /// ゲーム状態を同期するRPC
    /// </summary>
    [PunRPC]
    private void SetGameStateRPC(E_DICE_GAME_STATE newState)
    {
        gameState = newState;
        Debug.Log($"Game State changed to: {newState}");

        // 状態変更時のUI処理などをここで行う
    }

    /// <summary>
    /// 現在のゲーム状態を取得
    /// </summary>
    public E_DICE_GAME_STATE GetGameState()
    {
        return gameState;
    }
}