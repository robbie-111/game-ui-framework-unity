using System.Collections;
using System.Collections.Generic;
using System.Text;
using Common;
using UnityEngine;

namespace Scenes
{
    public class MainScene : MonoBehaviour
    {
        private void Awake()
        {
        }
        
        public void OnBackBtnClick()
        {
            ScreenManager.Load<SelectScene>(sceneName: "01. SelectScene",
                                            onSceneLoaded: (scene) =>
                                            {
                                                Debug.Log("SelectScene Load Complete");
                                            });
        }

        public void OnFirestoreBtnClick()
        {
            // UIHelper.ShowSimpleConfirmPanel(
            //     title: "Firestore Test",
            //     contents: "최신 버전으로 업데이트가 필요합니다.(강제)",
            //     onOk: () => Debug.Log("스토어 이동"),
            //     onClose: () => Debug.Log("게임 종료"));
            
            UIHelper.ShowSimpleConfirmPanel(
                title: "Firestore Test",
                contents: "최신 버전으로 업데이트가 필요합니다.(강제)",
                onOk: () => Debug.Log("스토어 이동"));
        }

        public void OnTestLogBtnClick()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:01 - Server started successfully.").AppendLine();
            stringBuilder.Append("[DEBUG] 2025-03-28 12:00:02 - Initializing database connection...").AppendLine();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:03 - Database connected.").AppendLine();
            stringBuilder.Append("[WARNING] 2025-03-28 12:00:05 - High memory usage detected.").AppendLine();
            stringBuilder.Append("[ERROR] 2025-03-28 12:00:07 - Failed to load configuration file.").AppendLine();
            stringBuilder.Append("[DEBUG] 2025-03-28 12:00:10 - Retrying configuration load...").AppendLine();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:12 - Configuration loaded successfully.").AppendLine();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:15 - User 'admin' logged in.").AppendLine();
            stringBuilder.Append("[DEBUG] 2025-03-28 12:00:18 - Fetching user permissions...").AppendLine();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:20 - User permissions granted.").AppendLine();
            stringBuilder.Append("[DEBUG] 2025-03-28 12:00:22 - Checking system status...").AppendLine();
            stringBuilder.Append("[INFO] 2025-03-28 12:00:25 - System running normally.").AppendLine();
            stringBuilder.Append("[WARNING] 2025-03-28 12:00:27 - Disk space is running low.").AppendLine();
            
            ScreenManager.Add<TextScrollViewPanel>(screenName: "TextScrollViewPanel", 
                showAnimation: ScreenAnimation.RightShow, 
                hideAnimation: ScreenAnimation.RightHide, 
                onScreenLoad: (screen) =>
                {
                    screen.textView.text = stringBuilder.ToString();
                });
        }

        public void OnLoadingBtnClick()
        {
            StartCoroutine(CO_ShowLoadingWait());
        }

        private IEnumerator CO_ShowLoadingWait()
        {
            ScreenManager.Loading(true);
            yield return new WaitForSecondsRealtime(3);
            ScreenManager.Loading(false);
        }
    }
}