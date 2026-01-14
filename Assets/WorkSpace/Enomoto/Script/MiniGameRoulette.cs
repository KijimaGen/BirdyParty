using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameRoulette : MonoBehaviour
{
    [Header("選ばせる画像")]
    [SerializeField] private Sprite[] sprites;

    [Header("流す画像")]
    [SerializeField] private Image[] slots = new Image[3];

    [Header("ループ画像設定")]
    [SerializeField] private float speed = 450f;          // 流すスピード
    [SerializeField] private float spacing = 80f;         // 画像の間隔
    [SerializeField] private float slotWidth = 520f;      // 画像1枚の横幅

    [Header("決まった画像を大きく表示")]
    [SerializeField] private Image bigOverlay;
    [SerializeField] private float bigPopScale = 2.2f;    // 拡大時の最大スケール
    [SerializeField] private float popInTime = 0.12f;     // 拡大にかかる時間
    [SerializeField] private float holdTime = 0.6f;       // 表示保持時間
    [SerializeField] private float popOutTime = 0.12f;    // 縮小にかかる時間

    [Header("Random Select Timing")]
    [SerializeField] private Vector2 randomSelectDelayRange = new Vector2(1.0f, 2.5f);

    private float Step => slotWidth + spacing;

    private Dictionary<Sprite, Action> spriteActions;

    private bool isRunning;
    private bool isPopping;

    private void Awake()
    {
        spriteActions = new Dictionary<Sprite, Action>();

        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                var s = sprites[i];
                if (s == null) continue;

                int idx = i;
                spriteActions[s] = () => OnSelectedSprite(idx, s);
            }
        }
    }

    private void Start()
    {
        InitSlots();
        StartRoulette();
    }

    public void StartRoulette()
    {
        if (isRunning) return;
        isRunning = true;

        StartCoroutine(FlowLoop());
        StartCoroutine(RandomPickLoop());
    }

    public void StopRoulette()
    {
        isRunning = false;
    }

    private void InitSlots()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("sprites が空です。Inspectorで追加してください。");
            return;
        }

        // 初期配置：Slot0,1,2 を横並び
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            var rt = slots[i].rectTransform;
            rt.anchoredPosition = new Vector2(i * Step, 0f);

            slots[i].sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
            slots[i].preserveAspect = true;
        }

        if (bigOverlay != null)
        {
            bigOverlay.gameObject.SetActive(false);
            bigOverlay.preserveAspect = true;
        }
    }

    private IEnumerator FlowLoop()
    {
        while (isRunning)
        {
            float move = speed * Time.deltaTime;

            // 左へ移動
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = slots[i].rectTransform;
                rt.anchoredPosition += Vector2.left * move;
            }

            // 一番左に抜けたものを右端へ回す（ループ）
            // leftMostX の閾値：-Step を超えたら、右端へ
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = slots[i].rectTransform;

                if (rt.anchoredPosition.x <= -Step)
                {
                    float rightMost = GetRightMostX();
                    rt.anchoredPosition = new Vector2(rightMost + Step, 0f);

                    // 次の画像を補充（配列からランダム）
                    slots[i].sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                }
            }

            yield return null;
        }
    }

    private float GetRightMostX()
    {
        float max = float.MinValue;
        for (int i = 0; i < slots.Length; i++)
        {
            float x = slots[i].rectTransform.anchoredPosition.x;
            if (x > max) max = x;
        }
        return max;
    }

    private IEnumerator RandomPickLoop()
    {
        while (isRunning)
        {
            float wait = UnityEngine.Random.Range(randomSelectDelayRange.x, randomSelectDelayRange.y);
            yield return new WaitForSeconds(wait);

            if (!isRunning) yield break;
            if (isPopping) continue; // 連続ドン防止

            // 裏でランダム選定：sprites配列から1枚
            var selected = sprites[UnityEngine.Random.Range(0, sprites.Length)];
            yield return StartCoroutine(PopBig(selected));
        }
    }

    private IEnumerator PopBig(Sprite selected)
    {
        if (bigOverlay == null) yield break;

        isPopping = true;

        // 表示準備
        bigOverlay.sprite = selected;
        bigOverlay.gameObject.SetActive(true);

        // ドン！演出（スケール）
        var rt = bigOverlay.rectTransform;
        rt.localScale = Vector3.one * 0.1f;

        // pop in
        yield return Scale(rt, 0.1f, bigPopScale, popInTime);

        // 選ばれた画像に応じて関数呼び分け（このタイミングで実行）
        InvokeBySprite(selected);

        // hold
        yield return new WaitForSeconds(holdTime);

        // pop out
        yield return Scale(rt, bigPopScale, 0.1f, popOutTime);

        bigOverlay.gameObject.SetActive(false);
        isPopping = false;
    }

    private void InvokeBySprite(Sprite s)
    {
        if (s == null) return;

        if (spriteActions != null && spriteActions.TryGetValue(s, out var act) && act != null)
        {
            act.Invoke();
        }
        else
        {
            Debug.Log($"No action registered for sprite: {s.name}");
        }
    }

    private IEnumerator Scale(RectTransform rt, float from, float to, float time)
    {
        if (time <= 0f)
        {
            rt.localScale = Vector3.one * to;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / time);

            // ちょい気持ちいいイージング
            float eased = 1f - Mathf.Pow(1f - a, 3f); // easeOutCubic
            float v = Mathf.Lerp(from, to, eased);

            rt.localScale = Vector3.one * v;
            yield return null;
        }

        rt.localScale = Vector3.one * to;
    }

    // -------------------------------------------------------
    // ここから下：選ばれた時に呼びたい関数（例）
    // -------------------------------------------------------
    private void OnSelectedSprite(int idx, Sprite sprite)
    {
        Debug.Log($"Selected idx={idx}, sprite={sprite.name}");

        switch (idx)
        {
            case 0:
            StartRaceGame();
            break;
            case 1:
            StartDropBird();
            break;
            case 2:
            StartBabaDice();
            break;
            default:
            Debug.Log("未割当のスプライトです");
            break;
        }
    }

    // 出た画像によってスタンバイUIを替える

    private void StartRaceGame()
    {
        Debug.Log("バドスタクルレース開始（例）");
        // SceneManager.LoadScene(...) 等
    }

    private void StartDropBird()
    {
        Debug.Log("ドロップバード開始（例）");
    }

    private void StartBabaDice()
    {
        Debug.Log("BABAダイス開始（例）");
    }
}