using UnityEngine;

namespace Common
{
    public static class UIHelper
    {
        /// <summary>
        /// 로드된 프리펩(GameObject)를 부모(parent GameObject)에 생성합니다.
        /// </summary>
        /// <param name="parent">저장할 키값.</param>
        /// <param name="prefab">파일디렉토리 경로.</param>
        public static GameObject AddChild(GameObject parent, GameObject prefab)
        {
            GameObject go = Object.Instantiate(prefab);

            if (go != null && parent != null)
            {
                Transform t = go.transform;
                t.SetParent(parent.transform);

                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                go.layer = parent.layer;
                go.name = prefab.name;

                go.GetComponent<RectTransform>().sizeDelta = prefab.GetComponent<RectTransform>().sizeDelta;
            }

            go.GetComponent<RectTransform>().anchoredPosition = prefab.GetComponent<RectTransform>().anchoredPosition;
            return go;
        }

        // ex) Util.ShowSimpleConfirmPanel("알림", "Two Button", delegate (){ Debug.Log("yes"); },delegate(){ Debug.Log("no"); });
        // ex) Util.ShowSimpleConfirmPanel("알림", "One Button", delegate (){ Debug.Log("yes"); });
        public static void ShowSimpleConfirmPanel(
            string title,
            string contents, 
            SimpleConfirmPanel.DelegatorOnOk onOk = null, 
            SimpleConfirmPanel.DelegatorOnClose onClose = null)
        {
            ScreenManager.Add<SimpleConfirmPanel>(screenName: "SimpleConfirmPanel",
                showAnimation: ScreenAnimation.ScaleShow,
                hideAnimation: ScreenAnimation.ScaleHide,
                onScreenLoad: (screen) =>
                {
                    screen.textTitle.text = title;
                    screen.textMessages.text = contents;
                    screen.OnOk = onOk;
                    screen.OnClose = onClose;
                    
                    SetActive(screen.closeBtnObj, (onClose != null) ? true : false);
                });
        }

        public static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        // public static LoadingPanel ShowLoadingWait(bool show)
        // {
        //     return ShowLoadingWait(show, string.Empty); //"Loading...");
        // }

        // public static LoadingPanel ShowLoadingWait(bool show, string text = "")
        // {
        //     LoadingPanel panel = null;
        //     ObjectTag ot = ObjectTag.Find(ObjectType.UI, "loading_wait");
        //     if (ot == null)
        //     {
        //         if (show)
        //         {
        //             GameObject prefab = ObjectMgr.GetUIPrefab("LoadingPanel");
        //             GameObject go = AddChild(GameObject.Find("TopRoot"), prefab);
        //             go.transform.localPosition = new Vector3(0f, 0f, go.transform.localPosition.z);
        //             go.transform.localScale = Vector3.one;
        //
        //             ot = ObjectTag.Tagging(go, ObjectType.UI, "loading_wait");
        //         }
        //     }
        //
        //     if (ot != null)
        //     {
        //         panel = ot.GetComponent<LoadingPanel>();
        //         if (panel != null)
        //         {
        //             panel.ShowText(text);
        //         }
        //
        //         ot.MyGameObject.SetActive(show);
        //     }
        //
        //     return panel;
        // }
    }

    
}