/**
 * @file NetworkButtonManager.cs
 * @brief Enhanced button manager with room code integration
 * @author Kiro
 * @date 2025/10/20
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkButtonManager : MonoBehaviour
{
    [Header("Network UI References")]
    [SerializeField] private GameObject networkMenuPanel;
    [SerializeField] private GameObject roomCodePanel;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button backButton;
    
    [Header("Room Code UI")]
    [SerializeField] private TextMeshProUGUI roomCodeDisplay;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    private NetworkManager networkManager;
    private RoomCodeUI roomCodeUI;
    private bool isHostMode = false;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        roomCodeUI = FindObjectOfType<RoomCodeUI>();
        
        SetupButtons();
        SetupNetworkEvents();
        
        // Initially show network menu
        ShowNetworkMenu();
    }
    
    void OnDestroy()
    {
        // Cleanup event subscriptions
        NetworkManager.OnRoomCodeGenerated -= OnRoomCodeGenerated;
        NetworkManager.OnRoomJoined -= OnRoomJoined;
        NetworkManager.OnRoomJoinFailed -= OnRoomJoinFailed;
    }
    
    private void SetupButtons()
    {
        if (hostGameButton != null)
            hostGameButton.onClick.AddListener(OnHostGameClicked);
            
        if (joinGameButton != null)
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
            
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            
        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            
        if (copyCodeButton != null)
            copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
    }
    
    private void SetupNetworkEvents()
    {
        NetworkManager.OnRoomCodeGenerated += OnRoomCodeGenerated;
        NetworkManager.OnRoomJoined += OnRoomJoined;
        NetworkManager.OnRoomJoinFailed += OnRoomJoinFailed;
    }
    
    private void OnHostGameClicked()
    {
        isHostMode = true;
        ShowRoomCodePanel();
        UpdateStatus("ルームを作成してください");
    }
    
    private void OnJoinGameClicked()
    {
        isHostMode = false;
        ShowRoomCodePanel();
        UpdateStatus("参加するルームコードを入力してください");
    }
    
    private void OnBackClicked()
    {
        ShowNetworkMenu();
    }
    
    private void OnCreateRoomClicked()
    {
        if (networkManager != null)
        {
            UpdateStatus("ルームを作成中...");
            SetRoomCodeButtonsInteractable(false);
            networkManager.CreateRandomRoom();
        }
    }
    
    private void OnJoinRoomClicked()
    {
        if (networkManager != null && roomCodeInput != null)
        {
            string inputCode = roomCodeInput.text.Trim().ToUpper();
            
            if (ValidateRoomCodeInput(inputCode))
            {
                UpdateStatus("ルームに参加中...");
                SetRoomCodeButtonsInteractable(false);
                networkManager.JoinRoomWithCode(inputCode);
            }
        }
    }
    
    private void OnCopyCodeClicked()
    {
        if (roomCodeDisplay != null && !string.IsNullOrEmpty(roomCodeDisplay.text))
        {
            GUIUtility.systemCopyBuffer = roomCodeDisplay.text;
            UpdateStatus("ルームコードをコピーしました！");
        }
    }
    
    private bool ValidateRoomCodeInput(string inputCode)
    {
        if (string.IsNullOrEmpty(inputCode))
        {
            UpdateStatus("ルームコードを入力してください");
            return false;
        }
        
        if (inputCode.Length != 5)
        {
            UpdateStatus("ルームコードは5文字である必要があります");
            return false;
        }
        
        if (!RoomCodeGenerator.IsValidRoomCode(inputCode))
        {
            UpdateStatus("無効な文字が含まれています");
            return false;
        }
        
        return true;
    }
    
    private void OnRoomCodeGenerated(string roomCode)
    {
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = roomCode;
        }
        
        UpdateStatus($"ルーム作成完了！\nコード: {roomCode}\n他のプレイヤーの参加を待っています...");
        SetRoomCodeButtonsInteractable(true);
        
        // Enable copy button for host
        if (copyCodeButton != null)
            copyCodeButton.interactable = true;
    }
    
    private void OnRoomJoined(string roomCode)
    {
        UpdateStatus($"ルームに参加しました: {roomCode}");
        
        // Hide UI panels after successful join
        HideAllPanels();
        
        // Notify other systems that we're ready to start the game
        // You can add game start logic here
    }
    
    private void OnRoomJoinFailed(string errorMessage)
    {
        UpdateStatus($"参加失敗: {errorMessage}");
        SetRoomCodeButtonsInteractable(true);
    }
    
    private void ShowNetworkMenu()
    {
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(true);
            
        if (roomCodePanel != null)
            roomCodePanel.SetActive(false);
    }
    
    private void ShowRoomCodePanel()
    {
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(false);
            
        if (roomCodePanel != null)
            roomCodePanel.SetActive(true);
            
        // Configure UI based on mode
        ConfigureRoomCodeUI();
    }
    
    private void ConfigureRoomCodeUI()
    {
        if (isHostMode)
        {
            // Host mode: show create button, hide join input
            if (createRoomButton != null)
                createRoomButton.gameObject.SetActive(true);
                
            if (joinRoomButton != null)
                joinRoomButton.gameObject.SetActive(false);
                
            if (roomCodeInput != null)
                roomCodeInput.gameObject.SetActive(false);
                
            if (roomCodeDisplay != null)
                roomCodeDisplay.gameObject.SetActive(true);
        }
        else
        {
            // Join mode: show input and join button, hide create button
            if (createRoomButton != null)
                createRoomButton.gameObject.SetActive(false);
                
            if (joinRoomButton != null)
                joinRoomButton.gameObject.SetActive(true);
                
            if (roomCodeInput != null)
            {
                roomCodeInput.gameObject.SetActive(true);
                roomCodeInput.text = "";
            }
                
            if (roomCodeDisplay != null)
                roomCodeDisplay.gameObject.SetActive(false);
        }
        
        // Always hide copy button initially
        if (copyCodeButton != null)
            copyCodeButton.gameObject.SetActive(isHostMode);
    }
    
    private void HideAllPanels()
    {
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(false);
            
        if (roomCodePanel != null)
            roomCodePanel.SetActive(false);
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"NetworkButtonManager: {message}");
    }
    
    private void SetRoomCodeButtonsInteractable(bool interactable)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = interactable;
            
        if (joinRoomButton != null)
            joinRoomButton.interactable = interactable;
    }
    
    /// <summary>
    /// Public method to show network UI from other scripts
    /// </summary>
    public void ShowNetworkUI()
    {
        ShowNetworkMenu();
    }
    
    /// <summary>
    /// Public method to hide network UI
    /// </summary>
    public void HideNetworkUI()
    {
        HideAllPanels();
    }
}