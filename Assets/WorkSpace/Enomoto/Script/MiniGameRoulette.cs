using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MiniGameRoulette : MonoBehaviour
{
    // =========================
    // どのミニゲームか
    // =========================
    public enum MiniGameType
    {
        BatoStacle,
        DropBird,
        BabaDice,
        BattleDome
    }

    [Serializable]
    public class SpriteToGame
    {
        public Sprite sprite;
        public MiniGameType gameType;
    }

    [Header("画像を貼り付ける場所設定")]
    [SerializeField] private Sprite[] sprites;

    [Header("出た画像のUI切り替え先設定")]
    [SerializeField] private SpriteToGame[] spriteToGames;

    [Header("流れる画像の設定（現在3枚）")]
    [SerializeField] private RectTransform flowArea;
    [SerializeField] private Image[] slots = new Image[3];

    [Header("ループの開始点・終了点")]
    [SerializeField] private RectTransform startPoint; // 右側の出現地点
    [SerializeField] private RectTransform endPoint;   // 左側の消滅地点

    [Header("ループ設定")]
    [SerializeField] private float speed = 450f;     // px/sec
    [SerializeField] private float spacing = 80f;    // 各画像の間隔
    [SerializeField] private float slotWidth = 520f; // 画像1枚の横幅(実サイズに合わせる)

    private float Step => slotWidth + spacing;

    [Header("ルーレット後拡大表示")]
    [SerializeField] private Image bigOverlay;
    [SerializeField] private float bigPopScale = 2.2f;
    [SerializeField] private float popInTime = 0.12f;
    [SerializeField] private float holdTime = 0.6f;
    [SerializeField] private float popOutTime = 0.12f;

    [Header("UI Switch (ルーレット後は必ず GameReadyUI へ)")]
    [SerializeField] private GameObject rouletteUIRoot;
    [SerializeField] private GameObject gameReadyUI;

    [Header("GameReadyUI内のゲームごとに切り替え")]
    [SerializeField] private GameObject batoStacleUI;
    [SerializeField] private GameObject dropBirdUI;
    [SerializeField] private GameObject babaDiceUI;

    [Header("フェードイン・アウト用")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool stopRouletteOnDecide = true;

    [Header("ゲームの内部抽選")]
    [SerializeField] private bool preselectOnStart = true;
    [SerializeField] private float spinTimeBeforeShow = 3.0f;

    private Dictionary<Sprite, MiniGameType> spriteMap;
    private bool isRunning;
    private bool isPopping;
    private bool isSwitchingUI;

    private Coroutine flowCo;
    private Coroutine randomPickCo;

    private Sprite preselectedSprite;

    private void Start()
    {
        BuildSpriteMap();
        InitSlots();
        StartRoulette();
    }

    public void StartRoulette()
    {
        if (isRunning) return;

        _ = AudioManager.instance.PlaySE(8);

        if (preselectOnStart)
            PreselectResult();

        isRunning = true;

        flowCo = StartCoroutine(FlowLoop());
        randomPickCo = StartCoroutine(RandomPickLoop());
    }

    public void StopRoulette()
    {
        isRunning = false;

        if (flowCo != null)
        {
            StopCoroutine(flowCo);
            flowCo = null;
        }

        if (randomPickCo != null)
        {
            StopCoroutine(randomPickCo);
            randomPickCo = null;
        }
    }

    // -------------------------
    // 初期化
    // -------------------------
    private void BuildSpriteMap()
    {
        spriteMap = new Dictionary<Sprite, MiniGameType>();

        if (spriteToGames == null) return;

        foreach (var e in spriteToGames)
        {
            if (e == null || e.sprite == null) continue;
            spriteMap[e.sprite] = e.gameType;
        }
    }

    private void InitSlots()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("sprites が空です。Inspectorで追加してください。");
            return;
        }

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("startPoint / endPoint が未設定です。FlowArea配下にStartPoint/EndPointを作って割り当ててください。");
            return;
        }

        float startX = startPoint.anchoredPosition.x;

        // 初期配置：StartPointから右方向へ Step 間隔で3枚置く
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            var rt = slots[i].rectTransform;
            rt.anchoredPosition = new Vector2(startX + i * Step, 0f);

            slots[i].sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
            slots[i].preserveAspect = true;
        }

        if (bigOverlay != null)
        {
            bigOverlay.gameObject.SetActive(false);
            bigOverlay.preserveAspect = true;
        }
    }

    // -------------------------
    // ループ：流し続ける（始点/終点対応）
    // -------------------------
    private IEnumerator FlowLoop()
    {
        if (startPoint == null || endPoint == null)
            yield break;

        float startX = startPoint.anchoredPosition.x;
        float endX = endPoint.anchoredPosition.x;

        while (isRunning)
        {
            float move = speed * Time.deltaTime;

            // 左へ移動
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = slots[i].rectTransform;
                rt.anchoredPosition += Vector2.left * move;
            }

            // 終点を超えたら右端へ回す
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = slots[i].rectTransform;

                if (rt.anchoredPosition.x <= endX)
                {
                    float rightMost = GetRightMostX();

                    // 右端の次の位置に出す。ただし startX より左には出さない
                    float newX = Mathf.Max(rightMost + Step, startX);
                    rt.anchoredPosition = new Vector2(newX, 0f);

                    // 次の画像を補充
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

    // 抽選結果を必ず表示
    private IEnumerator RandomPickLoop()
    {
        yield return new WaitForSeconds(spinTimeBeforeShow);

        if (!isRunning) yield break;
        if (isPopping || isSwitchingUI) yield break;

        //ここで事前に決めてあった抽選結果を反映させる
        preselectedSprite = sprites[ReflectNextGame()];

        _ = AudioManager.instance.PlaySE(9);

        //ボタンマネージャーの次のゲームの設定をしてもらう
        PartyModeManager.instance.SetNextSceneName();

        //抽選結果がなかった場合、自らランダムに作成
        if (preselectedSprite == null)
            PreselectResult();

        yield return StartCoroutine(PopBig(preselectedSprite));
    }

    // 画像の内部抽選
    private void PreselectResult()
    {
        if (sprites == null || sprites.Length == 0)
        {
            preselectedSprite = null;
            return;
        }

        preselectedSprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
        Debug.Log($"[Roulette] Preselected: {preselectedSprite.name}");
    }


    private IEnumerator PopBig(Sprite selected)
    {
        if (bigOverlay == null) yield break;

        isPopping = true;

        bigOverlay.sprite = selected;
        bigOverlay.gameObject.SetActive(true);

        var rt = bigOverlay.rectTransform;
        rt.localScale = Vector3.one * 0.1f;

        // pop in
        yield return Scale(rt, 0.1f, bigPopScale, popInTime);

        InvokeBySprite(selected);

        // hold
        yield return new WaitForSeconds(holdTime);

        // pop out
        yield return Scale(rt, bigPopScale, 0.1f, popOutTime);

        bigOverlay.gameObject.SetActive(false);
        isPopping = false;
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

            float eased = 1f - Mathf.Pow(1f - a, 3f);
            float v = Mathf.Lerp(from, to, eased);

            rt.localScale = Vector3.one * v;
            yield return null;
        }

        rt.localScale = Vector3.one * to;
    }

    private void InvokeBySprite(Sprite selected)
    {
        if (selected == null) return;

        if (spriteMap != null && spriteMap.TryGetValue(selected, out var gameType))
        {
            _ = SwitchToGameReadyAsync(gameType);
            return;
        }

        Debug.LogWarning($"Spriteが未登録です: {selected.name} / とりあえずBatoStacleにします");
        _ = SwitchToGameReadyAsync(MiniGameType.BatoStacle);
    }

    // FadeManager対応：フェードして切替

    private async UniTask SwitchToGameReadyAsync(MiniGameType gameType)
    {
        if (isSwitchingUI) return;
        isSwitchingUI = true;

        if (stopRouletteOnDecide)
            StopRoulette();

        var fm = FadeManager.instance;

        if (fm != null)
            await fm.FadeOut(fadeDuration);

        ApplyGameReadyUI(gameType);

        await UniTask.Yield();

        if (fm != null)
            await fm.FadeIn(fadeDuration);

        isSwitchingUI = false;
    }

    private void ApplyGameReadyUI(MiniGameType gameType)
    {
        if (rouletteUIRoot != null)
            rouletteUIRoot.SetActive(false);

        if (gameReadyUI != null)
            gameReadyUI.SetActive(true);

        if (batoStacleUI != null) batoStacleUI.SetActive(false);
        if (dropBirdUI != null) dropBirdUI.SetActive(false);
        if (babaDiceUI != null) babaDiceUI.SetActive(false);

        switch (gameType)
        {
            case MiniGameType.BatoStacle:
            if (batoStacleUI != null) batoStacleUI.SetActive(true);
            break;

            case MiniGameType.DropBird:
            if (dropBirdUI != null) dropBirdUI.SetActive(true);
            break;

            case MiniGameType.BabaDice:
            if (babaDiceUI != null) babaDiceUI.SetActive(true);
            break;
        }
    }

    /// <summary>
    /// 次のゲームをもらってくる
    /// </summary>
    private int ReflectNextGame() {
        return PartyModeManager.instance.GetNextGameIndex();
    }

    
}
