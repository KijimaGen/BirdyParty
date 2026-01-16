using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EggCatcherGameManager : MonoBehaviour
{
    public static EggCatcherGameManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 60f;

    [Header("Start")]
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultsText;

    private readonly Dictionary<int, int> scores = new Dictionary<int, int>();
    private float timeLeft;
    private bool running;

    public bool IsRunning => running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        timeLeft = matchDurationSeconds;
        UpdateTimerUI();
        if (resultsText != null) resultsText.text = "";
    }

    private void Start()
    {
        if (autoStartOnPlay)
            StartMatch();
    }

    private void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndMatch();
        }
        UpdateTimerUI();
    }

    public void StartMatch()
    {
        timeLeft = matchDurationSeconds;
        running = true;

        if (resultsText != null) resultsText.text = "";
        UpdateTimerUI();

        Debug.Log("[EggCatcher] Match Started");
    }

    private void EndMatch()
    {
        running = false;
        UpdateTimerUI();
        Debug.Log("[EggCatcher] Match Ended");
    }

    public void RegisterPlayer(int playerId)
    {
        if (!scores.ContainsKey(playerId))
            scores[playerId] = 0;
    }

    public int GetScore(int playerId)
    {
        return scores.TryGetValue(playerId, out var v) ? v : 0;
    }

    public void AddScore(int playerId, int add)
    {
        if (!running) return;
        RegisterPlayer(playerId);
        scores[playerId] += add;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int sec = Mathf.CeilToInt(timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        timerText.text = $"{m:0}:{s:00}";
    }
}
