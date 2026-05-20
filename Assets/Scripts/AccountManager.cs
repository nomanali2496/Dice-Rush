using UnityEngine;
using System.Collections;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Manages the transaction history display using Enhanced Scroller
/// Handles data loading, cell creation, and scrolling for transaction history rows
/// </summary>
public class AccountManager : MonoBehaviour, IEnhancedScrollerDelegate
{
    [Header("Enhanced Scroller References")]
    [SerializeField] private EnhancedScroller scroller; // The Enhanced Scroller component
    [SerializeField] private TransactionHistoryRowUI cellViewPrefab; // Prefab for transaction history row cells
    
    [Header("Test Settings")]
    [SerializeField] private int testDataCount = 30; // Number of test transactions to generate
    [SerializeField] private float cellSize = 100f; // Height of each transaction row cell

    [Header("Balance Display")]
    [SerializeField] private TMPro.TextMeshProUGUI accountBalanceText;        // Main account balance label
    [SerializeField] private TMPro.TextMeshProUGUI accountBalanceShadowText;  // Shadow copy of account balance
    [SerializeField] private TMPro.TextMeshProUGUI winningBalanceText;        // Main winning balance label
    [SerializeField] private TMPro.TextMeshProUGUI winningBalanceShadowText;  // Shadow copy of winning balance

    [Header("Withdrawal References")]
    [SerializeField] private UnityEngine.UI.Button withdrawButton;
    [SerializeField] private TMPro.TMP_InputField withdrawAmountInput;
    
    /// <summary>
    /// Internal representation of transaction history data
    /// </summary>
    private SmallList<TransactionHistoryRowData> _transactionData;
    
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeTransactionHistory();
        if (withdrawButton != null)
        {
            withdrawButton.onClick.AddListener(OnWithdrawClicked);
        }
        if (withdrawAmountInput != null)
        {
            withdrawAmountInput.text = "$";
            withdrawAmountInput.onValueChanged.AddListener(OnAmountValueChanged);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to PlayerModel updates so balance refreshes whenever data changes
        PlayerModel.OnDataUpdated += RefreshBalanceUI;

        // Reinitialize when GameObject becomes active
        if (isInitialized)
        {
            RefreshTransactionHistory();
        }
        else
        {
            InitializeTransactionHistory();
        }

        // Fetch latest profile from backend to show live balance
        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            BackendAuthService.Instance.GetProfile((success, msg) =>
            {
                if (!success)
                    Debug.LogWarning("[AccountManager] GetProfile failed: " + msg);
            });
        }
        else
        {
            // PlayerModel already has cached data — display it immediately
            RefreshBalanceUI();
        }
    }

    private void OnDisable()
    {
        PlayerModel.OnDataUpdated -= RefreshBalanceUI;
    }

    /// <summary>
    /// Updates the account balance and winning balance labels (and their shadows)
    /// from the current PlayerModel values.
    /// </summary>
    private void RefreshBalanceUI()
    {
        string cashText    = $"${PlayerModel.CashBalance:F2}";
        string winningText = $"${PlayerModel.WinningBalance:F2}";

        if (accountBalanceText       != null) accountBalanceText.text       = cashText;
        if (accountBalanceShadowText != null) accountBalanceShadowText.text = cashText;
        if (winningBalanceText       != null) winningBalanceText.text       = winningText;
        if (winningBalanceShadowText != null) winningBalanceShadowText.text = winningText;

        Debug.Log($"[AccountManager] Balance UI refreshed — Cash: {cashText}, Winning: {winningText}");
    }
    
    /// <summary>
    /// Initializes the transaction history for the first time
    /// </summary>
    private void InitializeTransactionHistory()
    {
        // Set this script as the scroller's delegate
        if (scroller != null)
        {
            scroller.Delegate = this;
        }
        else
        {
            Debug.LogError("[AccountManager] Enhanced Scroller not assigned!");
            return;
        }
        
        // Load data on start (use coroutine to wait for scroller initialization)
        StartCoroutine(LoadDataDelayed());
        isInitialized = true;
    }
    
    private void RefreshTransactionHistory()
    {
        // Ensure delegate is set
        if (scroller != null)
        {
            scroller.Delegate = this;
            StartCoroutine(LoadDataDelayed());
        }
    }
    
    /// <summary>
    /// Coroutine to reload data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator ReloadDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    /// <summary>
    /// Coroutine to load data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator LoadDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        LoadTransactionHistory();
    }

    /// <summary>
    /// Loads transaction history data, falling back to test data if the API call fails or is not logged in.
    /// </summary>
    public void LoadTransactionHistory()
    {
        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            Debug.Log("[AccountManager] Requesting transaction history from backend...");
            BackendAuthService.Instance.GetTransactionHistory((success, items) =>
            {
                if (success && items != null)
                {
                    PopulateRealData(items);
                }
                else
                {
                    Debug.LogWarning("[AccountManager] Backend transaction history load failed. Falling back to test data.");
                    LoadTestData();
                }
            });
        }
        else
        {
            Debug.Log("[AccountManager] No active JWT or BackendAuthService missing. Loading test data.");
            LoadTestData();
        }
    }

    /// <summary>
    /// Populates real transaction history data parsed from the backend API.
    /// </summary>
    private void PopulateRealData(System.Collections.Generic.List<BackendAuthService.TransactionHistoryItem> items)
    {
        if (_transactionData == null)
        {
            _transactionData = new SmallList<TransactionHistoryRowData>();
        }
        else
        {
            _transactionData.Clear();
        }

        foreach (var item in items)
        {
            // Parse date from ISO timestamp (e.g. "2026-05-18T10:22:45.000Z")
            string dateString = "";
            if (System.DateTime.TryParse(item.createdAt, out System.DateTime parsedDate))
            {
                dateString = parsedDate.ToString("MM/dd/yy");
            }
            else
            {
                dateString = System.DateTime.Now.ToString("MM/dd/yy"); // fallback
            }

            // Map status string to TransactionStatus enum
            TransactionStatus status = TransactionStatus.Pending;
            if (!string.IsNullOrEmpty(item.status))
            {
                string statusLower = item.status.ToLower();
                if (statusLower == "successful" || statusLower == "success" || statusLower == "completed")
                {
                    status = TransactionStatus.Successful;
                }
                else if (statusLower == "cancelled" || statusLower == "cancel" || statusLower == "failed")
                {
                    status = TransactionStatus.Cancel;
                }
            }

            _transactionData.Add(new TransactionHistoryRowData(dateString, item.amount, status));
        }

        // Sort by date (newest first)
        SortTransactionsByDate();

        // Reload scroller
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }

        Debug.Log($"[AccountManager] Successfully loaded {items.Count} real transactions from backend");
    }

    /// <summary>
    /// Loads test transaction history data for testing purposes
    /// </summary>
    public void LoadTestData()
    {
        if (_transactionData == null)
        {
            _transactionData = new SmallList<TransactionHistoryRowData>();
        }
        else
        {
            _transactionData.Clear();
        }
        
        // Generate test data with random dates, amounts, and statuses
        System.Random random = new System.Random();
        System.DateTime baseDate = System.DateTime.Now;
        
        for (int i = 0; i < testDataCount; i++)
        {
            // Generate date (going back in time)
            int daysAgo = random.Next(0, 90); // Random date within last 90 days
            System.DateTime transactionDate = baseDate.AddDays(-daysAgo);
            string dateString = transactionDate.ToString("MM/dd/yy");
            
            // Generate random amount (between $1 and $1000)
            float amount = (float)(random.NextDouble() * 999.0 + 1.0);
            
            // Generate random status (weighted: 60% successful, 25% pending, 15% cancel)
            TransactionStatus status;
            int statusRoll = random.Next(0, 100);
            if (statusRoll < 60)
            {
                status = TransactionStatus.Successful;
            }
            else if (statusRoll < 85)
            {
                status = TransactionStatus.Pending;
            }
            else
            {
                status = TransactionStatus.Cancel;
            }
            
            _transactionData.Add(new TransactionHistoryRowData(dateString, amount, status));
        }
        
        // Sort by date (newest first)
        SortTransactionsByDate();
        
        // Reload the scroller with new data (only if scroller is active and initialized)
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log($"[AccountManager] Loaded {_transactionData.Count} test transactions");
    }
    
    /// <summary>
    /// Sorts transactions by date (newest first)
    /// </summary>
    private void SortTransactionsByDate()
    {
        // Simple bubble sort by date (newest first)
        for (int i = 0; i < _transactionData.Count - 1; i++)
        {
            for (int j = i + 1; j < _transactionData.Count; j++)
            {
                // Parse dates and compare
                if (System.DateTime.TryParse(_transactionData[i].date, out System.DateTime date1) &&
                    System.DateTime.TryParse(_transactionData[j].date, out System.DateTime date2))
                {
                    if (date1 < date2)
                    {
                        // Swap
                        TransactionHistoryRowData temp = _transactionData[i];
                        _transactionData[i] = _transactionData[j];
                        _transactionData[j] = temp;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Adds a single transaction entry
    /// </summary>
    public void AddTransaction(string date, float amount, TransactionStatus status)
    {
        if (_transactionData == null)
        {
            _transactionData = new SmallList<TransactionHistoryRowData>();
        }
        
        _transactionData.Add(new TransactionHistoryRowData(date, amount, status));
        
        // Sort by date after adding
        SortTransactionsByDate();
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
    }
    
    /// <summary>
    /// Adds a transaction with current date
    /// </summary>
    public void AddTransaction(float amount, TransactionStatus status)
    {
        string currentDate = System.DateTime.Now.ToString("MM/dd/yy");
        AddTransaction(currentDate, amount, status);
    }
    
    /// <summary>
    /// Clears all transaction data
    /// </summary>
    public void ClearData()
    {
        if (_transactionData != null)
        {
            _transactionData.Clear();
        }
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log("[AccountManager] Transaction data cleared");
    }
    
    /// <summary>
    /// Sets the cell size and reloads the scroller data
    /// Used by AccountRowScaler to apply scaled cell size
    /// </summary>
    public void SetCellSizeAndReload(float newCellSize)
    {
        cellSize = newCellSize;
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log($"[AccountManager] Cell size updated to {cellSize:F2} and scroller reloaded");
    }
    
    #region EnhancedScroller Handlers
    
    /// <summary>
    /// Returns the number of cells in the transaction history
    /// </summary>
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _transactionData != null ? _transactionData.Count : 0;
    }
    
    /// <summary>
    /// Returns the size of each cell (height for vertical scroller)
    /// </summary>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellSize;
    }
    
    /// <summary>
    /// Gets the cell view for a specific data index
    /// </summary>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Check if prefab is assigned
        if (cellViewPrefab == null)
        {
            Debug.LogError("[AccountManager] Cell View Prefab is not assigned!");
            return null;
        }
        
        // Get a cell from the scroller (recycled or new)
        TransactionHistoryRowUI cellView = scroller.GetCellView(cellViewPrefab) as TransactionHistoryRowUI;
        
        if (cellView == null)
        {
            Debug.LogError("[AccountManager] Failed to get TransactionHistoryRowUI cell view!");
            return null;
        }
        
        // Set the name for debugging
        cellView.name = $"Transaction {dataIndex}";
        
        // Set the data for this cell
        if (_transactionData != null && dataIndex < _transactionData.Count)
        {
            cellView.SetData(_transactionData[dataIndex]);
        }
        else
        {
            Debug.LogWarning($"[AccountManager] Data index {dataIndex} is out of range!");
        }
        
        return cellView;
    }
    
    #endregion
    
    #region Test Methods
    
    /// <summary>
    /// Test method: Loads a small set of test data
    /// </summary>
    [ContextMenu("Load Small Test Data")]
    public void LoadSmallTestData()
    {
        testDataCount = 10;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Loads a large set of test data
    /// </summary>
    [ContextMenu("Load Large Test Data")]
    public void LoadLargeTestData()
    {
        testDataCount = 50;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Scrolls to top of transaction history
    /// </summary>
    [ContextMenu("Scroll To Top")]
    public void ScrollToTop()
    {
        if (scroller != null)
        {
            scroller.JumpToDataIndex(0, 0f, 0f);
        }
    }
    
    /// <summary>
    /// Test method: Adds a test transaction
    /// </summary>
    [ContextMenu("Add Test Transaction")]
    public void AddTestTransaction()
    {
        System.Random random = new System.Random();
        float amount = (float)(random.NextDouble() * 500.0 + 10.0);
        TransactionStatus status = (TransactionStatus)random.Next(0, 3);
        AddTransaction(amount, status);
        Debug.Log($"[AccountManager] Added test transaction: ${amount:F2} - {status}");
    }
    
    private void OnWithdrawClicked()
    {
        float amount = 10.00f;
        if (withdrawAmountInput != null)
        {
            string amountText = withdrawAmountInput.text.Trim();
            if (amountText.StartsWith("$"))
            {
                amountText = amountText.Substring(1).Trim();
            }
            if (float.TryParse(amountText, out float parsedAmount))
            {
                amount = parsedAmount;
            }
        }

        if (withdrawButton != null)
        {
            withdrawButton.interactable = false;
        }

        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.Withdraw(amount, (success, response) =>
            {
                if (withdrawButton != null)
                {
                    withdrawButton.interactable = true;
                }

                if (success)
                {
                    Debug.Log($"[AccountManager] Withdrawal of ${amount:F2} successful!");
                    
                    if (withdrawAmountInput != null)
                    {
                        withdrawAmountInput.text = "$";
                    }
                    
                    // Refresh transaction history to show new withdrawal entry
                    RefreshTransactionHistory();
                }
                else
                {
                    Debug.LogError($"[AccountManager] Withdrawal failed: {response}");
                }
            });
        }
        else
        {
            if (withdrawButton != null)
            {
                withdrawButton.interactable = true;
            }
            Debug.LogError("[AccountManager] BackendAuthService Instance is null!");
        }
    }
    
    private void OnAmountValueChanged(string value)
    {
        if (withdrawAmountInput == null) return;
        
        // Remove listener temporarily to prevent recursion
        withdrawAmountInput.onValueChanged.RemoveListener(OnAmountValueChanged);
        
        if (string.IsNullOrEmpty(value))
        {
            withdrawAmountInput.text = "$";
            withdrawAmountInput.caretPosition = 1;
        }
        else if (!value.StartsWith("$"))
        {
            withdrawAmountInput.text = "$" + value.Replace("$", "");
            withdrawAmountInput.caretPosition = withdrawAmountInput.text.Length;
        }
        
        // Restore listener
        withdrawAmountInput.onValueChanged.AddListener(OnAmountValueChanged);
    }
    
    #endregion
}

