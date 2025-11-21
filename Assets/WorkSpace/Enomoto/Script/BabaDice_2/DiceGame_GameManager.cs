using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System.Xml.Serialization;

/* 
    BABADice全体の進行管理
    マスターが主導権を持つように（オフラインは自身を）
 */
public class DiceGame_GameManager : MonoBehaviourPunCallbacks
{
    [Header("プレイヤーダイスの定義")]
    [SerializeField] private string dicePrefabName = "DicePrefab";
    [SerializeField] private Material[] playerMaterials;

    [Header("ゲームの設定")]
    [SerializeField] private float turnLimitTime = 10.0f;
    [SerializeField] private Transform[] spawnPoints;

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI babaText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI[] scoreTexts;

    // 内部パラメータ
    private int currentTurn = 1;
    private const int MaxTurn = 5;
    private int currentBabaNumber = 0;
    private float currentTimer = 0.0f;
    private bool isTurnActive = false;

    // プレイヤーデータ
    private Dictionary<int, DiceObject> playerDiceMap = new Dictionary<int, DiceObject>();
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private List<int> droppedOutPlayers = new List<int>();


    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        yield return new WaitForSeconds(0.5f);

        currentTurn = 1;
        droppedOutPlayers.Clear();
        playerScores.Clear();
        for (int i = 0; i < 4; i++) playerScores[i] = 0;

        UpdateUI();

        SpawnDice();

        yield return new WaitForSeconds(1.0f);

        if (IsMaster())
        {
            StartNewTurn();
        }
    }

    // オンライン時：マスターがRoomオブジェクトを生成
    // オフライン時：ローカルで生成を行うように
    private void SpawnDice()
    {
        bool isOnline = GameManager.instance.IsOnline();

        // if (isOnline)  
    }

    private void UpdateUI()
    {

    }


    private bool IsMaster()
    {
        return false;
    }

    private void StartNewTurn()
    {

    }
}