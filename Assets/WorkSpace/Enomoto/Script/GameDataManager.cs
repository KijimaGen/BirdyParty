using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager instance;

    public string selectedMiniGame;
    public bool comeBackFromGame;

    public bool playOnline;

    //タイトルに固定で置いてあるからこいつに鳥のモデル情報を持っててもらいたい
    [SerializeField]
    private List<GameObject> titleToriList = new List<GameObject>();

    //タイトルに固定で置いてあるからこいつに鳥のモデル情報を持っててもらいたい
    [SerializeField]
    private List<TextMeshProUGUI> titleToriNameList = new List<TextMeshProUGUI>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetData()
    {
        selectedMiniGame = null;
        comeBackFromGame = false;
    }

    /// <summary>
    /// ナンバー指定でプレイヤーを渡す
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public GameObject GetToriFromNumber(int i) {
        return titleToriList[i];
    }

    /// <summary>
    /// タイトルのプレイヤーのリストをあげる
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetToriList() {
        return titleToriList;
    }

    /// <summary>
    /// 何人エントリー済みのプレイヤーがいるのかを取得
    /// </summary>
    /// <returns></returns>
    public int GetEntriedPlayerCount() {
        int playerCount = 0;
        for(int i = 0,max = titleToriList.Count; i < max; i++) {
            if (titleToriList[i].activeSelf)
                playerCount++;
        }
        return playerCount;
    }

    /// <summary>
    /// プレイヤーの名前を入れる
    /// </summary>
    /// <param name="player"></param>
    public void EntryPlayer(PlayerInfomation player) {
        if (player.myName == "") return;
        titleToriNameList[player.myNumber].text = player.myName;
    }

}
