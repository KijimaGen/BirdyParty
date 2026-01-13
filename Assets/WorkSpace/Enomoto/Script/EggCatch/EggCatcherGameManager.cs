using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EggCatcherGameManager : MonoBehaviour
{
    public static EggCatcherGameManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 60f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultsText; // 任意（終了表示）

    // playerId -> score
    private readonly Dictionary<int, int> scores = new Dictionary<int, int>();
    private float timeLeft;
    private bool running;

    public bool IsRunning => running;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        timeLeft = matchDurationSeconds;
        UpdateTimerUI();
        if (resultsText != null) resultsText.text = "";
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
    }

    private void EndMatch()
    {
        running = false;
        UpdateTimerUI();

        // 結果表示（任意）
        if (resultsText != null)
        {
            resultsText.text = BuildResultsText();
        }
    }

    private string BuildResultsText()
    {
        // 簡易ランキング表示
        var list = new List<(int playerId, int score)>();
        foreach (var kv in scores) list.Add((kv.Key, kv.Value));
        list.Sort((a, b) => b.score.CompareTo(a.score));

        string s = "RESULTS\n";
        for (int i = 0; i < list.Count; i++)
        {
            s += $"P{list[i].playerId + 1}: {list[i].score}\n";
        }
        return s;
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

        // プレイヤーUI更新はプレイヤー側で引く方式にする（下で実装）
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
