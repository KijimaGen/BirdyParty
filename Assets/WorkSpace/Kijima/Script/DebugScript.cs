/**
 * @file DebugScript.cs
 * @brief デバッグ用のスクリプト、主に動作確認用に使うよ
 * @author kijima
 * @date 2025/10/02
 */
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class DebugScript : MonoBehaviour{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async UniTask Uni() {
        await UniTask.Delay(TimeSpan.FromSeconds(2f));
    }
}
