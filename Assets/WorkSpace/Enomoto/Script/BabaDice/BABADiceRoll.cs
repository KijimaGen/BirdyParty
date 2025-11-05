using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BABADiceRoll : MonoBehaviour
{
    private Rigidbody rb;
    public bool isRolling = false;
    private string currentBottomFace = "";
    private string babaFace = "";
    public string babaDice = "";

    [Header("表示させるサイコロ画像")]
    [SerializeField] private GameObject dice1;
    [SerializeField] private GameObject dice2;
    [SerializeField] private GameObject dice3;
    [SerializeField] private GameObject dice4;
    [SerializeField] private GameObject dice5;
    [SerializeField] private GameObject dice6;

    [SerializeField] private GameObject UseBABADice;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            RollDice();
        }

        if (!isRolling && !string.IsNullOrEmpty(babaFace))
        {
            DiceCheck();
            babaFace = "";
        }
    }

    public void RollDice()
    {
        if (isRolling) return;
        isRolling = true;
        currentBottomFace = "";
        babaFace = "";

        transform.position = UseBABADice.transform.position;
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
                babaFace = top.ToString();
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

    private void DiceCheck()
    {
        dice1.SetActive(false);
        dice2.SetActive(false);
        dice3.SetActive(false);
        dice4.SetActive(false);
        dice5.SetActive(false);
        dice6.SetActive(false);

        if (babaFace == "1")
        {
            dice1.SetActive(true);
        }
        else if (babaFace == "2")
        {
            dice2.SetActive(true);
        }
        else if (babaFace == "3")
        {
            dice3.SetActive(true);
        }
        else if (babaFace == "4")
        {
            dice4.SetActive(true);
        }
        else if (babaFace == "5")
        {
            dice5.SetActive(true);
        }
        else if (babaFace == "6")
        {
            dice6.SetActive(true);
        }

        babaDice = babaFace;
    }
}
