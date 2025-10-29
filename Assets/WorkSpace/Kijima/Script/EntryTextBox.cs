/**
 * @file EntryTextBox.cs
 * @brief プレイヤーがエントリーしたときに出てくるテキストボックスのコントローラー
 * @author Sum1r3
 * @date 2025/10/27
 */
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntryTextBox : MonoBehaviour{
    //移動スピード
    [SerializeField]
    private float speed = 1f;
    //自身のテキスト
    [SerializeField]
    private TextMeshProUGUI myText;
    //消えるまでの時間
    [SerializeField]
    private float duration = 1;
    //子供のテキストボックス
    [SerializeField]
    private Image myTextBox;
    //固定で使う文字列
    private const string ENTRY_TEXT = "が参加しました";
    private void Start() {
        _=FadeOut();
        _= FadeOutChildBox();
        _= FadeOutText();
    }

    void Update(){
        Move();
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {
        Vector3 moveValue = Vector3.zero;
        moveValue.y = 1;
        transform.position += moveValue * speed;
    }

    //フェードアウト
    private async UniTask FadeOut() {

        Image _fadeImage = GetComponent<Image>();

        float elapsedTime = 0.0f;//経過時間
        float startAlpha = _fadeImage.color.a;  //開始透明度
        Color targetColor = _fadeImage.color;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            //保管した不透明度をフェード画像に設定
            float t = elapsedTime / duration;

            targetColor.a = Mathf.Lerp(startAlpha, 0, t);
            _fadeImage.color = targetColor;
            //1フレーム待ち
            await UniTask.Delay(1);
        }
        targetColor.a = 0;
        _fadeImage.color = targetColor;
        Destroy(gameObject);
    }

    //子オブジェクトのフェードアウト
    private async UniTask FadeOutChildBox() {
        float elapsedTime = 0.0f;//経過時間
        float startAlpha = myTextBox.color.a;  //開始透明度
        Color targetColor = myTextBox.color;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            //保管した不透明度をフェード画像に設定
            float t = elapsedTime / duration;

            targetColor.a = Mathf.Lerp(startAlpha, 0, t);
            myTextBox.color = targetColor;
            //1フレーム待ち
            await UniTask.Delay(1);
        }
        targetColor.a = 0;
        myTextBox.color = targetColor;
        
    }

    //テキストのフェードアウト
    private async UniTask FadeOutText() {
        float elapsedTime = 0.0f;//経過時間
        float startAlpha = myText.color.a;  //開始透明度
        Color targetColor = myText.color;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            //保管した不透明度をフェード画像に設定
            float t = elapsedTime / duration;

            targetColor.a = Mathf.Lerp(startAlpha, 0, t);
            myText.color = targetColor;
            //1フレーム待ち
            await UniTask.Delay(1);
        }
        targetColor.a = 0;
        myText.color = targetColor;
        
    }

    //自身のテキストを変更
    public void SetmyText(string text) {
        myText.text = text + ENTRY_TEXT;
    }

}
