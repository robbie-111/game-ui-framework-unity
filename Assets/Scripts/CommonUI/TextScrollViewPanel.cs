using UnityEngine;
using UnityEngine.UI;

public class TextScrollViewPanel : MonoBehaviour
{
    public Text textView;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnClickClose()
    {
        ScreenManager.Close();
    }
}
