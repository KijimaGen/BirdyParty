/**
 * @file RacePlayer.cs
 * @brief レースゲームのプレイヤー
 * @author Sum1r3
 * @date 2025/9/6
 */
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RacePlayer : MonoBehaviour {
    //移動速度
    [SerializeField] private float moveSpeed = 8f;

    //入力値
    private Vector2 moveInput;
    private Rigidbody rb;

    //ゴールしたかどうか
    private bool isGoal;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //カメラの参照に自身を入れる
        Camera.main.gameObject.GetComponent<RaceCameraController>().AddRacer(this.transform);
        isGoal = false;
    }

    //アップデート
    void FixedUpdate() {
        if (!RaceManager.instance.isStart) {
            rb.velocity = Vector3.zero;
            return;
        }

        Move();

        if(isGoal) {
            rb.velocity = Vector3.zero;
        }
    }

    //インプットシステムの入力値の受け取り
    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move() {
        // X方向は固定（常に前進）
        float x = 1f; // ← 進行方向固定したいならこれでOK
        float z = moveInput.y;

        // 正規化しないでそのまま適用
        Vector3 moveDir = new Vector3(x, 0, z);
        rb.velocity = moveDir * moveSpeed;
    }

    /// <summary>
    /// 減速
    /// </summary>
    /// <returns></returns>
    public async UniTask Slow() {
        if(moveSpeed * 0.75f < 2) {
            return;
        }

        moveSpeed *= 0.75f;
        await UniTask.Delay(3000);
        moveSpeed /= 0.75f;
    }

    /// <summary>
    /// 加速
    /// </summary>
    /// <returns></returns>
    public async UniTask Boost() {
        if (moveSpeed * 1.5f > 8) {
            return;
        }
        moveSpeed *= 1.5f; // 50%加速
        await UniTask.Delay(3000); // 3秒持続
        moveSpeed /= 1.5f; // 50%加速
    }

    /// <summary>
    /// ゴールしました
    /// </summary>
    public void Goal() {
        isGoal = true;
    }

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Finish") {
            Goal();
            RaceManager.instance.AddRanking(this.gameObject);
            Debug.Log(this.gameObject.name + "は" + RaceManager.instance.GetRankingCount(this.gameObject));

        }
    }
}
