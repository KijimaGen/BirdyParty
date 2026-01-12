using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceUIController : MonoBehaviour
{
    public static DiceUIController Instance { get; private set; }

    [Header("Dice Face Sprites (Index 1..6)")]
    [SerializeField] private Sprite[] faceSprites = new Sprite[7]; // 0 unused

    [Header("UI")]
    [SerializeField] private Image babaImage;
    [SerializeField] private Image[] playerImages = new Image[4]; // myNumber 0..3想定
    [SerializeField] private GameObject[] eliminatedMarks = new GameObject[4]; // 例：×表示など
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resultText;

    private int maxTurns;
    private float rollSeconds;
    private double deadline;
    private bool onlineTimer;

    private void Awake()
    {
        Instance = this;
    }

    public void OnGameStart(int maxTurn, float rollWindowSeconds)
    {
        maxTurns = maxTurn;
        rollSeconds = rollWindowSeconds;
        if (resultText != null) resultText.text = "";
        for (int i = 0; i < playerImages.Length; i++)
        {
            if (playerImages[i] != null) playerImages[i].sprite = null;
            if (eliminatedMarks != null && i < eliminatedMarks.Length && eliminatedMarks[i] != null)
                eliminatedMarks[i].SetActive(false);
        }
        if (babaImage != null) babaImage.sprite = null;
    }

    public void OnTurnStart(int turn, int baba, double deadlineTime, bool isOnline)
    {
        deadline = deadlineTime;
        onlineTimer = isOnline;

        if (turnText != null) turnText.text = $"TURN {turn}/{maxTurns}";
        if (babaImage != null) babaImage.sprite = GetFaceSprite(baba);
        if (resultText != null) resultText.text = "";

        // turn開始時にプレイヤー出目をクリアしたいなら
        for (int i = 0; i < playerImages.Length; i++)
        {
            if (playerImages[i] != null) playerImages[i].sprite = null;
        }
    }

    public void OnSingleRollRevealed(int playerNumber, int faceValue)
    {
        if (playerNumber < 0 || playerNumber >= playerImages.Length) return;
        if (playerImages[playerNumber] != null)
            playerImages[playerNumber].sprite = GetFaceSprite(faceValue);
    }

    public void OnTurnResult(
        int turn, int baba,
        Dictionary<int, int> rolls,
        int[] eliminatedThisTurn,
        Dictionary<int, int> totalPoints,
        HashSet<int> eliminatedAll
    )
    {
        // reflect roll sprites
        foreach (var kv in rolls)
            OnSingleRollRevealed(kv.Key, kv.Value);

        // eliminated marks
        foreach (var n in eliminatedThisTurn)
        {
            if (n >= 0 && n < eliminatedMarks.Length && eliminatedMarks[n] != null)
                eliminatedMarks[n].SetActive(true);
        }

        // text summary
        if (resultText != null)
        {
            var order = totalPoints.OrderByDescending(x => x.Value).ToList();
            string s = $"BABA={baba}\n";
            foreach (var kv in rolls.OrderBy(x => x.Key))
            {
                bool outNow = eliminatedThisTurn.Contains(kv.Key);
                s += $"P{kv.Key}: {kv.Value}" + (outNow ? "  OUT" : "") + "\n";
            }
            s += "\nTOTAL\n";
            foreach (var kv in order)
            {
                s += $"P{kv.Key}: {kv.Value}" + (eliminatedAll.Contains(kv.Key) ? " (OUT)" : "") + "\n";
            }
            resultText.text = s;
        }
    }

    public void OnGameEnd(Dictionary<int, int> totalPoints, int[] eliminatedNums)
    {
        if (resultText == null) return;
        var alive = totalPoints.Keys.Except(eliminatedNums).ToList();

        string s = "GAME END\n";
        if (alive.Count == 1)
        {
            int winner = alive[0];
            s += $"WINNER: P{winner}\n";
        }
        else
        {
            s += "RANKING (by total points)\n";
            foreach (var kv in totalPoints.OrderByDescending(x => x.Value))
                s += $"P{kv.Key}: {kv.Value}\n";
        }
        resultText.text = s;
    }

    private void Update()
    {
        if (timerText == null) return;

        double now = onlineTimer ? Photon.Pun.PhotonNetwork.Time : Time.timeAsDouble;
        double remain = deadline - now;
        if (remain < 0) remain = 0;
        timerText.text = $"ROLL: {remain:0.0}s";
    }

    private Sprite GetFaceSprite(int face)
    {
        if (faceSprites == null || faceSprites.Length <= face) return null;
        return faceSprites[face];
    }
}
