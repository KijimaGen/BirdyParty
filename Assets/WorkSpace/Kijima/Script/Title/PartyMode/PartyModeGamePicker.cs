using Cysharp.Threading.Tasks;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyModeGamePicker : SystemObject
{
    //ゲーム名のシーンの配列
    private string[] gameSceneNames = new string[] { "Race", "DropBird", "DiceGame" };

    //どのゲームを選んだかの配列
    private List<int> gameIndexList = new List<int>();

    //何回抽選を行うか
    private int randomCount;

    //一応インスタンス
    public static PartyModeGamePicker instance;

    // オンライン非Masterは抽選しない（受信でセットされる）
    private bool canLottery = true;

    public override async UniTask Initialize()
    {
        // 破壊しない。非Masterは「抽選禁止」にするだけ
        if (GameManager.instance.IsOnline() && !PhotonNetwork.IsMasterClient)
        {
            canLottery = false;
        }

        instance = this;

        // null対策
        if (gameIndexList == null)
            gameIndexList = new List<int>();

        await UniTask.CompletedTask;
    }

    public int PickUpIndex()
    {
        return Random.Range(0, gameSceneNames.Length);
    }

    public List<string> BuildGameIndexs()
    {
        // 非Masterは抽選しない（RoomSync/RPCで SetGameIndexList されるのを待つ）
        if (!canLottery)
        {
            Debug.LogWarning("[PartyModeGamePicker] Not Master: skip lottery.");
            // ここで空返ししてもいいが、呼ばれないのが理想
            return new List<string>();
        }

        var cashIndexs = new HashSet<int>();

        while (cashIndexs.Count < randomCount)
        {
            int randomIndex = PickUpIndex();
            cashIndexs.Add(randomIndex);
        }

        List<int> returnIndexs = new List<int>(cashIndexs);

        // 必ずListとして保持
        gameIndexList.Clear();
        gameIndexList.AddRange(returnIndexs);

        List<string> returnSceneNames = new List<string>(returnIndexs.Count);
        for (int i = 0, max = returnIndexs.Count; i < max; i++)
        {
            returnSceneNames.Add(gameSceneNames[returnIndexs[i]]);
        }

        return returnSceneNames;
    }

    public void ChangeSceneWithName(string sceneName)
    {
        if (GameManager.instance.IsOnline())
            PhotonNetwork.LoadLevel(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void SetRandomCount(int setCount)
    {
        randomCount = setCount;
    }

    public void CheckGameList()
    {
        for (int i = 0, max = gameIndexList.Count; i < max; i++)
            Debug.Log("選ばれたゲームは : " + gameSceneNames[gameIndexList[i]]);
    }

    public List<int> GetGameIndexList()
    {
        return gameIndexList;
    }

    // オンラインで受信した index リストを適用
    public void SetGameIndexList(int[] indexList)
    {
        if (gameIndexList == null) gameIndexList = new List<int>();
        gameIndexList.Clear();
        for (int i = 0; i < indexList.Length; i++)
            gameIndexList.Add(indexList[i]);

        Debug.Log($"[PartyModeGamePicker] SetGameIndexList count={gameIndexList.Count}");
    }
}
