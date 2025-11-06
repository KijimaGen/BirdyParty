using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class DiceRoll : MonoBehaviour
{

    [Header("表示するテキスト")]
    public TextMeshProUGUI textMeshPro;
    private int diceScore = 0;

    private Rigidbody rb;
    public bool isRolling = false;
    private string currentBottomFace = "";
    private string resultFace = "";

    private Action<string> onRollComplete;

    [Header("表示させるサイコロ画像")]
    [SerializeField] private GameObject dice1;
    [SerializeField] private GameObject dice2;
    [SerializeField] private GameObject dice3;
    [SerializeField] private GameObject dice4;
    [SerializeField] private GameObject dice5;
    [SerializeField] private GameObject dice6;

    [SerializeField] private GameObject UseDice;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void StartRoll(Action<string> callback)
    {
        if (isRolling) return;

        onRollComplete = callback;
        isRolling = true;
        currentBottomFace = "";
        resultFace = "";

        transform.position = UseDice.transform.position;
        transform.rotation = UnityEngine.Random.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 force = new Vector3(UnityEngine.Random.Range(-2f, 2f), 6f, UnityEngine.Random.Range(-2f, 2f));
        Vector3 torque = new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f));
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

                DiceCheck();

                onRollComplete?.Invoke(resultFace);
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

        if (resultFace == "1")
        {
            diceScore += 1;
            dice1.SetActive(true);
            
        }
        else if (resultFace == "2")
        {
            diceScore += 2;
            dice2.SetActive(true);
        }
        else if (resultFace == "3")
        {
            diceScore += 3;
            dice3.SetActive(true);
        }
        else if (resultFace == "4")
        {
            diceScore += 4;
            dice4.SetActive(true);
        }
        else if (resultFace == "5")
        {
            diceScore += 5;
            dice5.SetActive(true);
        }
        else if (resultFace == "6")
        {
            diceScore += 6;
            dice6.SetActive(true);
        }
    }
}