using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ArenaUIBuilder
{
    [MenuItem("DiceRush/Create Arena Host-Join UI")]
    public static void CreateArenaUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main Panel
        GameObject popupPanel = new GameObject("ArenaHostJoinPanel");
        popupPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = popupPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        Image bgImage = popupPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        ArenaHostJoinPanel script = popupPanel.AddComponent<ArenaHostJoinPanel>();

        // 1. Main Action Panel
        GameObject mainPanel = CreateSubPanel("MainActionPanel", popupPanel.transform);
        Button btnHost = CreateButton("Btn_Host", "Host Arena", mainPanel.transform, new Vector2(0, 50));
        Button btnJoin = CreateButton("Btn_Join", "Join Arena", mainPanel.transform, new Vector2(0, -50));
        Button btnClose = CreateButton("Btn_Close", "Close", mainPanel.transform, new Vector2(0, -150));
        
        // Wire main panel buttons to script methods via UnityEvent
        btnHost.onClick.AddListener(() => script.StartHostFlow());
        btnJoin.onClick.AddListener(() => script.StartJoinFlow());
        btnClose.onClick.AddListener(() => popupPanel.SetActive(false));
        script.mainActionPanel = mainPanel;

        // 2. Host Panel
        GameObject hostPanel = CreateSubPanel("HostPanel", popupPanel.transform);
        TextMeshProUGUI titleHost = CreateText("Title", "Hosting Arena", hostPanel.transform, new Vector2(0, 150));
        TextMeshProUGUI codeHost = CreateText("GeneratedCodeText", "----", hostPanel.transform, new Vector2(0, 50));
        Button btnCopy = CreateButton("Btn_Copy", "Copy Code", hostPanel.transform, new Vector2(0, -20));
        Button btnStart = CreateButton("Btn_StartArena", "Start Game", hostPanel.transform, new Vector2(0, -90));
        Button btnCancelHost = CreateButton("Btn_CancelHost", "Cancel", hostPanel.transform, new Vector2(0, -160));
        
        script.hostPanel = hostPanel;
        script.generatedCodeText = codeHost;
        script.copyCodeButton = btnCopy;
        script.startArenaButton = btnStart;
        script.cancelHostButton = btnCancelHost;

        // 3. Join Panel
        GameObject joinPanel = CreateSubPanel("JoinPanel", popupPanel.transform);
        TextMeshProUGUI titleJoin = CreateText("Title", "Join Arena", joinPanel.transform, new Vector2(0, 150));
        
        // Input Field
        GameObject inputObj = new GameObject("JoinCodeInput");
        inputObj.transform.SetParent(joinPanel.transform, false);
        RectTransform inputRT = inputObj.AddComponent<RectTransform>();
        inputRT.sizeDelta = new Vector2(200, 50);
        inputRT.anchoredPosition = new Vector2(0, 50);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = Color.white;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = new Vector2(-10, -10);
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.color = Color.black;
        inputText.alignment = TextAlignmentOptions.Center;
        inputText.fontSize = 24;
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textComponent = inputText;
        
        Button btnSubmit = CreateButton("Btn_Submit", "Join", joinPanel.transform, new Vector2(0, -20));
        Button btnCancelJoin = CreateButton("Btn_CancelJoin", "Cancel", joinPanel.transform, new Vector2(0, -90));
        TextMeshProUGUI errorTxt = CreateText("ErrorText", "", joinPanel.transform, new Vector2(0, -160));
        errorTxt.color = Color.red;
        
        script.joinPanel = joinPanel;
        script.joinCodeInput = inputField;
        script.submitJoinButton = btnSubmit;
        script.cancelJoinButton = btnCancelJoin;
        script.joinErrorText = errorTxt;

        // Finalize
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        popupPanel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(popupPanel, "Create Arena UI");
        Selection.activeGameObject = popupPanel;
        Debug.Log("ArenaHostJoinPanel UI successfully created and wired up!");
    }

    private static GameObject CreateSubPanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 500);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        return panel;
    }

    private static Button CreateButton(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 50);
        rt.anchoredPosition = pos;
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.5f, 0.8f);
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24;
        tmp.color = Color.white;

        return btn;
    }

    private static TextMeshProUGUI CreateText(string name, string text, Transform parent, Vector2 pos)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);
        rt.anchoredPosition = pos;
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        return tmp;
    }
}
