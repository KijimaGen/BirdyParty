using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UITextMover : MonoBehaviour
{
    [Header("参照設定")]
    public RectTransform textTransform;    // TextMeshProUGUI の RectTransform
    public TextMeshProUGUI textMeshPro;    // TextMeshProUGUI コンポーネント
    public RectTransform maskImage;        // 左に配置された Image や Mask の RectTransform

    [Header("テキスト設定")]
    [TextArea(3, 10)]
    public string infoText;                // 改行OK
    public float spaceBetweenLines = 8f;   // 改行間のスペース
    public float scrollSpeed = 100f;       // スクロール速度
    public float fadeDistance = 60f;       // フェードアウト開始距離
    public float fadeSpeed = 2f;           // フェード速度
    public float restartDelay = 0.5f;      // 再出現までの待ち時間

    private float screenRightX;
    private float maskLeftX;
    private Vector3 startPos;
    private bool resetting = false;
    private float[] charAlpha;

    void Start()
    {
        if (textTransform == null || textMeshPro == null || maskImage == null)
        {
            Debug.LogError("参照が足りません。textTransform, textMeshPro, maskImage を割り当ててください。");
            enabled = false;
            return;
        }

        // 改行を一定間隔でスペース結合して1行に
        string[] lines = infoText.Split('\n');
        textMeshPro.text = string.Join(new string(' ', Mathf.RoundToInt(spaceBetweenLines)), lines);

        // 画面右端と mask 左端をワールド座標で取得
        screenRightX = Screen.width + textMeshPro.preferredWidth / 2f;
        maskLeftX = maskImage.position.x - maskImage.rect.width * 0.5f;

        textMeshPro.ForceMeshUpdate();
        charAlpha = new float[textMeshPro.textInfo.characterCount];
        for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;

        // テキストを画面右端からスタート
        Vector3 worldPos = textTransform.position;
        worldPos.x = screenRightX + 200.0f;
        textTransform.position = worldPos;
    }

    void Update()
    {
        if (resetting) return;

        // 左へ滑らかに移動
        textTransform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // フェード＆削除処理
        UpdateFade();

        // 完全にマスクを通過したらリセット
        float textRight = textTransform.position.x + textMeshPro.preferredWidth * 0.5f;
        if (textRight < maskLeftX - 50f)
        {
            StartCoroutine(RestartAfterDelay());
        }
    }

    private void UpdateFade()
    {
        textMeshPro.ForceMeshUpdate();
        var textInfo = textMeshPro.textInfo;

        if (charAlpha.Length != textInfo.characterCount)
        {
            charAlpha = new float[textInfo.characterCount];
            for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vi = textInfo.characterInfo[i].vertexIndex;
            int mi = textInfo.characterInfo[i].materialReferenceIndex;
            var colors = textInfo.meshInfo[mi].colors32;

            // 各文字のワールド位置を求める
            float charRightX = textTransform.TransformPoint(textInfo.characterInfo[i].topRight).x;

            // マスク右端から fadeDistance 内に入ったらフェードアウト
            if (charRightX <= maskLeftX + fadeDistance && charRightX > maskLeftX)
                charAlpha[i] = Mathf.MoveTowards(charAlpha[i], 0f, fadeSpeed * Time.deltaTime);
            else if (charRightX > maskLeftX + fadeDistance)
                charAlpha[i] = Mathf.MoveTowards(charAlpha[i], 1f, fadeSpeed * Time.deltaTime);

            byte a = (byte) (Mathf.Clamp01(charAlpha[i]) * 255);
            for (int j = 0; j < 4; j++) colors[vi + j].a = a;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.colors32 = meshInfo.colors32;
            textMeshPro.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private System.Collections.IEnumerator RestartAfterDelay()
    {
        resetting = true;
        yield return new WaitForSeconds(restartDelay);

        // テキストを再配置（画面右端へ戻す）
        Vector3 pos = textTransform.position;
        pos.x = screenRightX + 200.0f;
        textTransform.position = pos;

        // 透明度をリセット
        for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;

        resetting = false;
    }
}