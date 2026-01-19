using UnityEngine;
using UnityEngine.UI;

public class ClickButtonSound : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(SettingManager.instance.PlayTapSound);
    }
}