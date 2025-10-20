/**
 * @file RoomCodeUI.cs
 * @brief UI manager for room code display and input
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomCodeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject roomCodePanel;
    [SerializeField] private TextMeshProUGUI roomCodeDisplay;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Room Code Settings")]
    [SerializeField] private bool autoShowPanel = true;
    
    private NetworkManager networkManager;
    
    void Start()
    {
        // Find NetworkManager in scene
        networkManager = FindObjectOfType<NetworkManager>();
        
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            return;
        }
        
        // Subscribe to NetworkManager events
        NetworkManager.OnRoomCodeGenerated += OnRoomCodeGenerated;
        NetworkManager.OnRoomJoined += OnRoomJoined;
        NetworkManager.OnRoomJoinFailed += OnRoomJoinFailed;
        NetworkManager.OnConnectedToServer += OnConnectedToServer;
        
        // Setup UI
        SetupUI();
        
        if (autoShowPanel)
        {
            ShowRoomCodePanel();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        NetworkManager.OnRoomCodeGenerated -= OnRoomCodeGenerated;
        NetworkManager.OnRoomJoined -= OnRoomJoined;
        NetworkManager.OnRoomJoinFailed -= OnRoomJoinFailed;
        NetworkManager.OnConnectedToServer -= OnConnectedToServer;
    }
    
    private void SetupUI()
    {
        // Setup button listeners
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            
        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            
        if (copyCodeButton != null)
            copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
        
        // Setup input field
        if (roomCodeInput != null)
        {
            roomCodeInput.characterLimit = 5;
            roomCodeInput.onValueChanged.AddListener(OnRoomCodeInputChanged);
        }
        
        // Initial UI state
        UpdateUI("Photonに接続中...");
        SetButtonsInteractable(false);
    }
    
    private void OnConnectedToServer()
    {
        UpdateUI("サーバーに接続しました");
        SetButtonsInteractable(true);
    }
    
    private void OnCreateRoomClicked()
    {
        if (networkManager != null)
        {
            UpdateUI("ルームを作成中...");
            SetButtonsInteractable(false);
            networkManager.CreateRandomRoom();
        }
    }
    
    private void OnJoinRoomClicked()
    {
        if (networkManager != null && roomCodeInput != null)
        {
            string inputCode = roomCodeInput.text.Trim().ToUpper();
            
            if (string.IsNullOrEmpty(inputCode))
            {
                UpdateUI("ルームコードを入力してください");
                return;
            }
            
            if (inputCode.Length != 5)
            {
                UpdateUI("ルームコードは5文字である必要があります");
                return;
            }
            
            UpdateUI("ルームに参加中...");
            SetButtonsInteractable(false);
            networkManager.JoinRoomWithCode(inputCode);
        }
    }
    
    private void OnCopyCodeClicked()
    {
        if (roomCodeDisplay != null && !string.IsNullOrEmpty(roomCodeDisplay.text))
        {
            GUIUtility.systemCopyBuffer = roomCodeDisplay.text;
            UpdateUI("ルームコードをコピーしました！");
        }
    }
    
    private void OnRoomCodeInputChanged(string value)
    {
        // Convert to uppercase and filter invalid characters
        string filteredValue = "";
        foreach (char c in value.ToUpper())
        {
            if (RoomCodeGenerator.IsValidRoomCode(c.ToString()))
            {
                filteredValue += c;
            }
        }
        
        if (filteredValue != value)
        {
            roomCodeInput.text = filteredValue;
        }
    }
    
    private void OnRoomCodeGenerated(string roomCode)
    {
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = roomCode;
        }
        
        UpdateUI($"ルーム作成完了！コード: {roomCode}");
        SetButtonsInteractable(true);
        
        // Enable copy button
        if (copyCodeButton != null)
            copyCodeButton.interactable = true;
    }
    
    private void OnRoomJoined(string roomCode)
    {
        UpdateUI($"ルームに参加しました: {roomCode}");
        
        // Hide the room code panel after successful join
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(false);
        }
    }
    
    private void OnRoomJoinFailed(string errorMessage)
    {
        UpdateUI($"参加失敗: {errorMessage}");
        SetButtonsInteractable(true);
    }
    
    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"RoomCodeUI: {message}");
    }
    
    private void SetButtonsInteractable(bool interactable)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = interactable;
            
        if (joinRoomButton != null)
            joinRoomButton.interactable = interactable;
    }
    
    public void ShowRoomCodePanel()
    {
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(true);
        }
    }
    
    public void HideRoomCodePanel()
    {
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Generate a new room code for testing purposes
    /// </summary>
    [ContextMenu("Generate Test Room Code")]
    public void GenerateTestRoomCode()
    {
        string testCode = RoomCodeGenerator.GenerateRoomCode();
        OnRoomCodeGenerated(testCode);
    }
}