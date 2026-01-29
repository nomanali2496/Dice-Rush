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
    
    /// <summary>
    /// Internal representation of transaction history data
    /// </summary>
    private SmallList<TransactionHistoryRowData> _transactionData;
    
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeTransactionHistory();
    }
    
    private void OnEnable()
    {
        // Reinitialize when GameObject becomes active
        if (isInitialized)
        {
            RefreshTransactionHistory();
        }
        else
        {
            InitializeTransactionHistory();
        }
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
        
        // Load test data on start (use coroutine to wait for scroller initialization)
        StartCoroutine(LoadTestDataDelayed());
        isInitialized = true;
    }
    
    /// <summary>
    /// Refreshes the transaction history when GameObject becomes active again
    /// </summary>
    private void RefreshTransactionHistory()
    {
        // Ensure delegate is set
        if (scroller != null)
        {
            scroller.Delegate = this;
            
            // Reload data if we have data (use coroutine to wait for scroller initialization)
            if (_transactionData != null && _transactionData.Count > 0)
            {
                StartCoroutine(ReloadDataDelayed());
            }
            else
            {
                // If no data, load test data
                StartCoroutine(LoadTestDataDelayed());
            }
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
    /// Coroutine to load test data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator LoadTestDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        LoadTestData();
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
    
    #endregion
}

