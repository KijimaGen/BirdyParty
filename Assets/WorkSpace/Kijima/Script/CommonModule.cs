/**
 * @file CommonModule.cs
 * @brief 汎用ユーティリティクラス
 * @author yao
 * @date 2025/4/15
 */

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

public class CommonModule {

    /// <summary>
    /// リストが空か判定
    /// </summary>
    /// <typeparam myName="T"></typeparam>
    /// <param myName="list"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(List<T> list) {
        // 短絡評価なので高速
        return list == null || list.Count <= 0;
    }

    public static bool IsEmpty<T>(T[] array) {
        return array == null || array.Length <= 0;
    }

    /// <summary>
    /// リストに対して有効なインデックスか判定
    /// </summary>
    /// <returns></returns>
    public static bool IsEnableIndex<T>(List<T> list, int index) {
        if (IsEmpty(list)) return false;

        return index >= 0 && list.Count > index;
    }

    public static bool IsEnableIndex<T>(T[] array, int index) {
        if (IsEmpty(array)) return false;

        return index >= 0 && array.Length > index;
    }

    public static void InitializeList<T>(ref List<T> list, int capacity = -1) {
        if (list == null) {
            if (capacity < 1) {
                list = new List<T>();
            }
            else {
                list = new List<T>(capacity);
            }
        }
        else {
            if (list.Capacity < capacity) list.Capacity = capacity;

            list.Clear();
        }
    }

    /// <summary>
    /// リストを重複なしでマージ
    /// </summary>
    /// <typeparam myName="T"></typeparam>
    /// <param myName="main"></param>
    /// <param myName="sub"></param>
    // メインにサブを統合
    public static void MergeList<T>(ref List<T> main, List<T> sub) {
        if (IsEmpty(sub)) {
            Console.Error.WriteLine($"エラーが発生しました");
            return;
        }
        int mergeCount = sub.Count;
        if (main == null) main = new List<T>(mergeCount);
        for (int i = 0, max = mergeCount; i < max; i++) {
            // 重複する要素は追加しない
            // Listクラスの関数でEqualsを使っている（便利）
            if (main.Exists(mainElem => mainElem.Equals(sub[i]))) continue;

            main.Add(sub[i]);
        }

    }

    /// <summary>
    /// 複数のタスクの終了を待つ
    /// </summary>
    /// <param myName="taskList"></param>
    /// <returns></returns>
    public static async UniTask WaitTask(List<UniTask> taskList) {
        // 終了したタスクをリストから除去し、リストが空になるまで待つ
        while (!IsEmpty(taskList)) {
            // 逆順で要素を削除する可能性があるので後ろから走査
            for (int i = taskList.Count - 1; i >= 0; i--) {
                if (!taskList[i].Status.IsCompleted()) continue;
                // タスクが終了していたらリストから削除
                taskList.RemoveAt(i);
            }
            await UniTask.DelayFrame(1);
        }
    }


    /// <summary>
    /// 複数のタスクの終了を待つ（キャンセレーション対応）
    /// </summary>
    /// <param myName="taskList"></param>
    /// <returns></returns>
    public static async UniTask WaitTask(List<UniTask> taskList, CancellationToken token) {
        // 終了したタスクをリストから除去し、リストが空になるまで待つ
        while (!IsEmpty(taskList)) {
            // 逆順で要素を削除する可能性があるので後ろから走査
            for (int i = taskList.Count - 1; i >= 0; i--) {
                if (!taskList[i].Status.IsCompleted()) continue;
                // タスクが終了していたらリストから削除
                taskList.RemoveAt(i);
            }
            await UniTask.DelayFrame(1, PlayerLoopTiming.Update, token);
        }
    }

}