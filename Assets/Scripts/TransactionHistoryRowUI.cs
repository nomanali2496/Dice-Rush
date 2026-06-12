using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Transaction status enum
/// </summary>
public enum TransactionStatus
{
    Pending,
    Successful,
    Cancel
}

/// <summary>
/// Data class to hold transaction history row information
/// </summary>
[System.Serializable]
public class TransactionHistoryRowData
{
    public string date;
    public float amount;
    public TransactionStatus status;
    public string type;
    
    public TransactionHistoryRowData(string date, float amount, TransactionStatus status, string type = "")
    {
        this.date = date;
        this.amount = amount;
        this.status = status;
        this.type = type;
    }
}

/// <summary>
/// Manages the UI display for a single transaction history row
/// Works with Enhanced Scroller for efficient scrolling
/// </summary>
public class TransactionHistoryRowUI : EnhancedScrollerCellView
{
    [Header("Date Text")]
    [SerializeField] private TextMeshProUGUI dateText; // Date text component
    
    [Header("Amount Text")]
    [SerializeField] private TextMeshProUGUI amountText; // Amount text component (will show with $ prefix)
    
    [Header("Type Text")]
    [UnityEngine.Serialization.FormerlySerializedAs("balanceTypeText")]
    [SerializeField] private TextMeshProUGUI typeText; // Transaction type text component (e.g. Deposit, Win Prize, Entry Fee)
    
    [Header("Status Image")]
    [SerializeField] private Image statusImage; // Image to show transaction status
    [SerializeField] private Sprite pendingSprite; // Sprite for pending status
    [SerializeField] private Sprite successfulSprite; // Sprite for successful status
    [SerializeField] private Sprite cancelSprite; // Sprite for cancel status
    
    private TransactionHistoryRowData currentData;
    
    /// <summary>
    /// Sets the data for this cell view (called by Enhanced Scroller)
    /// </summary>
    /// <param name="data">The transaction history row data</param>
    public void SetData(TransactionHistoryRowData data)
    {
        currentData = data;
        SetupRow(data.date, data.amount, data.status, data.type);
    }
    
    /// <summary>
    /// Sets up the transaction history row with the specified date, amount, and status
    /// This method can still be called directly if not using Enhanced Scroller
    /// </summary>
    /// <param name="date">Transaction date</param>
    /// <param name="amount">Transaction amount</param>
    /// <param name="status">Transaction status (Pending, Successful, Cancel)</param>
    public void SetupRow(string date, float amount, TransactionStatus status)
    {
        SetupRow(date, amount, status, "");
    }
    
    /// <summary>
    /// Sets up the transaction history row with the specified date, amount, status, and transaction type
    /// </summary>
    public void SetupRow(string date, float amount, TransactionStatus status, string type)
    {
        // Setup date text
        if (dateText != null)
        {
            dateText.text = date;
        }
        
        // Setup amount text with $ prefix
        if (amountText != null)
        {
            amountText.text = "$" + Mathf.RoundToInt(amount).ToString();
        }
        
        // Setup status image
        SetupStatusImage(status);
        
        // Setup type text
        if (typeText != null)
        {
            if (string.IsNullOrEmpty(type))
            {
                typeText.text = "";
            }
            else
            {
                typeText.text = FormatTransactionType(type);
            }
        }
    }

    /// <summary>
    /// Formats raw transaction types (e.g. deposit -> Deposit, win_prize -> Win Prize)
    /// </summary>
    private string FormatTransactionType(string typeStr)
    {
        if (string.IsNullOrEmpty(typeStr)) return "";
        
        switch (typeStr.ToLower())
        {
            case "deposit":
                return "Deposit";
            case "win_prize":
                return "Win Prize";
            case "entry_fee":
                return "Entry Fee";
            case "withdraw":
                return "Withdraw";
            case "refund":
                return "Refund";
            default:
                // Replace underscores with spaces and capitalize
                string result = typeStr.Replace("_", " ");
                if (result.Length > 0)
                {
                    // Capitalize each word
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    string[] words = result.Split(' ');
                    for (int i = 0; i < words.Length; i++)
                    {
                        if (words[i].Length > 0)
                        {
                            sb.Append(char.ToUpper(words[i][0]));
                            if (words[i].Length > 1)
                            {
                                sb.Append(words[i].Substring(1).ToLower());
                            }
                            if (i < words.Length - 1) sb.Append(" ");
                        }
                    }
                    return sb.ToString();
                }
                return typeStr;
        }
    }
    
    /// <summary>
    /// Sets up the status image based on transaction status
    /// </summary>
    private void SetupStatusImage(TransactionStatus status)
    {
        if (statusImage == null) return;
        
        Sprite targetSprite = null;
        
        switch (status)
        {
            case TransactionStatus.Pending:
                targetSprite = pendingSprite;
                break;
            case TransactionStatus.Successful:
                targetSprite = successfulSprite;
                break;
            case TransactionStatus.Cancel:
                targetSprite = cancelSprite;
                break;
        }
        
        if (targetSprite != null)
        {
            statusImage.sprite = targetSprite;
            statusImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[TransactionHistoryRowUI] Status sprite for {status} is not assigned!");
            statusImage.enabled = false;
        }
    }
    
    /// <summary>
    /// Updates only the date without changing other properties
    /// </summary>
    public void UpdateDate(string newDate)
    {
        if (dateText != null)
        {
            dateText.text = newDate;
        }
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.date = newDate;
        }
    }
    
    /// <summary>
    /// Updates only the amount without changing other properties
    /// </summary>
    public void UpdateAmount(float newAmount)
    {
        if (amountText != null)
        {
            amountText.text = "$" + Mathf.RoundToInt(newAmount).ToString();
        }
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.amount = newAmount;
        }
    }
    
    /// <summary>
    /// Updates only the status without changing other properties
    /// </summary>
    public void UpdateStatus(TransactionStatus newStatus)
    {
        SetupStatusImage(newStatus);
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.status = newStatus;
        }
    }
    
    /// <summary>
    /// Gets the current data for this row
    /// </summary>
    public TransactionHistoryRowData GetCurrentData()
    {
        return currentData;
    }
    
    /// <summary>
    /// Gets the current transaction status
    /// </summary>
    public TransactionStatus GetCurrentStatus()
    {
        return currentData != null ? currentData.status : TransactionStatus.Pending;
    }
}

