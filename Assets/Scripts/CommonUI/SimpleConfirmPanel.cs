using UnityEngine;
using UnityEngine.UI;

public class SimpleConfirmPanel : MonoBehaviour
{
    public Text textTitle;
    public Text textMessages;
    public GameObject closeBtnObj;

    public delegate void DelegatorOnOk();
    public DelegatorOnOk OnOk;
    public delegate void DelegatorOnClose();
    public DelegatorOnClose OnClose;

    public void OnClickOK()
    {
        if (OnOk != null)
        {
            OnOk();
        }
        Close();
    }

    void Close()
    {
        Destroy(gameObject);
    }

    public void OnClickClose()
    {
        if(OnClose != null)
        {
            OnClose();
        }
        Close();
    }    
}
