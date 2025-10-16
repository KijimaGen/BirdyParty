/**
 * @file GameManager.cs
 * @brief ゲーム全体で管理したい物の管理者
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SystemObject{
    //オンラインかどうか
    private bool isOnline;
    //


    public override async UniTask Initialize() {
        isOnline = false;


        await UniTask.CompletedTask;
    }

    /// <summary>
    /// オンラインかどうか
    /// </summary>
    /// <returns></returns>
    public bool IsOnline() {
        return isOnline;
    }

    /// <summary>
    /// オンラインかどうかをセット
    /// </summary>
    public void SetIsOnline(bool t) {
        isOnline = t;
    }

}
