using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceRoll : MonoBehaviour
{
    private Rigidbody rb;
    private bool isRolling = false;
    private string currentBottomFace = "";
    private string resultFace = "";

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RollDice();
        }

        if (!isRolling && !string.IsNullOrEmpty(resultFace))
        {
            Debug.Log($"出目は {resultFace} です！");
            resultFace = "";
        }
    }

    void RollDice()
    {
        if (isRolling) return;
        isRolling = true;
        currentBottomFace = "";
        resultFace = "";

        transform.position = new Vector3(8.92f, 8, 10.34f);
        transform.rotation = Random.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 force = new Vector3(Random.Range(-2f, 2f), 6f, Random.Range(-2f, 2f));
        Vector3 torque = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);

        Invoke(nameof(CheckIfStopped), 2f);
    }

    void CheckIfStopped()
    {
        if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        {
            isRolling = false;
            // 下を向いてる面から出目を反転（上面の出目を求める）
            if (int.TryParse(currentBottomFace.Replace("Face_", ""), out int bottom))
            {
                int top = 7 - bottom; // 対面の数字
                resultFace = top.ToString();
            }
        }
        else
        {
            Invoke(nameof(CheckIfStopped), 0.5f);
        }
    }

    public void SetBottomFace(string faceName)
    {
        currentBottomFace = faceName;
    }
}