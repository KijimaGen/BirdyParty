/**
 * @file NetworkHelper.cs
 * @brief ネットワーク関連のヘルパーメソッド集
 * オンライン/オフライン両対応のRPC呼び出しを簡単にする
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using Photon.Pun;
using System;

/// <summary>
/// ネットワーク関連の便利メソッドを提供するヘルパークラス
/// オンライン時はRPC、オフライン時は直接呼び出しを自動で切り替える
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// オンライン/オフラインを自動判定してメソッドを実行
    /// オンライン時：RPCで全プレイヤーに送信
    /// オフライン時：直接メソッドを呼び出し
    /// </summary>
    /// <param name="photonView">PhotonViewコンポーネント</param>
    /// <param name="methodName">実行するメソッド名</param>
    /// <param name="directCall">オフライン時に直接呼び出すAction</param>
    /// <param name="target">RPCの送信先（デフォルト：All）</param>
    public static void CallMethodSafely(PhotonView photonView, string methodName, Action directCall, RpcTarget target = RpcTarget.All)
    {
        // オンライン接続チェック
        if (IsOnlineAndReady())
        {
            // オンライン：RPCで送信
            Debug.Log($"オンライン：RPC '{methodName}' を送信中...");
            photonView.RPC(methodName, target);
        }
        else
        {
            // オフライン：直接呼び出し
            Debug.Log($"オフライン：'{methodName}' を直接実行中...");
            directCall?.Invoke();
        }
    }

    /// <summary>
    /// パラメータ付きでオンライン/オフラインを自動判定してメソッドを実行
    /// </summary>
    /// <param name="photonView">PhotonViewコンポーネント</param>
    /// <param name="methodName">実行するメソッド名</param>
    /// <param name="directCall">オフライン時に直接呼び出すAction</param>
    /// <param name="target">RPCの送信先</param>
    /// <param name="parameters">RPCに渡すパラメータ</param>
    public static void CallMethodSafely(PhotonView photonView, string methodName, Action directCall, RpcTarget target, params object[] parameters)
    {
        // オンライン接続チェック
        if (IsOnlineAndReady())
        {
            // オンライン：パラメータ付きRPCで送信
            Debug.Log($"オンライン：RPC '{methodName}' をパラメータ付きで送信中...");
            photonView.RPC(methodName, target, parameters);
        }
        else
        {
            // オフライン：直接呼び出し
            Debug.Log($"オフライン：'{methodName}' を直接実行中...");
            directCall?.Invoke();
        }
    }

    /// <summary>
    /// マスタークライアントのみが実行可能なメソッドの安全な呼び出し
    /// オフライン時は常に実行、オンライン時はマスタークライアントのみ実行
    /// </summary>
    /// <param name="photonView">PhotonViewコンポーネント</param>
    /// <param name="methodName">実行するメソッド名</param>
    /// <param name="directCall">オフライン時に直接呼び出すAction</param>
    /// <param name="target">RPCの送信先（デフォルト：All）</param>
    public static void CallMasterOnlyMethodSafely(PhotonView photonView, string methodName, Action directCall, RpcTarget target = RpcTarget.All)
    {
        // オンライン接続チェック
        if (IsOnlineAndReady())
        {
            // オンライン：マスタークライアントのみ実行
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"オンライン（マスター）：RPC '{methodName}' を送信中...");
                photonView.RPC(methodName, target);
            }
            else
            {
                Debug.Log($"オンライン（非マスター）：'{methodName}' の実行をスキップ");
            }
        }
        else
        {
            // オフライン：常に実行
            Debug.Log($"オフライン：'{methodName}' を直接実行中...");
            directCall?.Invoke();
        }
    }

    /// <summary>
    /// Photonがオンラインで準備完了かつルーム内にいるかチェック
    /// </summary>
    /// <returns>オンライン準備完了の場合true</returns>
    public static bool IsOnlineAndReady()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom;
    }

    /// <summary>
    /// 現在の接続状態を文字列で取得（デバッグ用）
    /// </summary>
    /// <returns>接続状態の説明文字列</returns>
    public static string GetConnectionStatus()
    {
        if (!PhotonNetwork.IsConnected)
            return "オフライン（未接続）";
        
        if (!PhotonNetwork.IsConnectedAndReady)
            return "接続中（準備未完了）";
        
        if (!PhotonNetwork.InRoom)
            return "接続済み（ルーム外）";
        
        return $"オンライン（ルーム内 - {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}人）";
    }

    /// <summary>
    /// 安全なInstantiate（オンライン/オフライン対応）
    /// </summary>
    /// <param name="prefabName">生成するプレハブ名</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成時の回転</param>
    /// <returns>生成されたGameObject</returns>
    public static GameObject SafeInstantiate(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (IsOnlineAndReady())
        {
            // オンライン：ネットワーク同期で生成
            Debug.Log($"オンライン：'{prefabName}' をネットワーク生成中...");
            return PhotonNetwork.Instantiate(prefabName, position, rotation);
        }
        else
        {
            // オフライン：通常のInstantiate
            Debug.Log($"オフライン：'{prefabName}' を通常生成中...");
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab != null)
            {
                return UnityEngine.Object.Instantiate(prefab, position, rotation);
            }
            else
            {
                Debug.LogError($"プレハブ '{prefabName}' がResourcesフォルダに見つかりません");
                return null;
            }
        }
    }
}