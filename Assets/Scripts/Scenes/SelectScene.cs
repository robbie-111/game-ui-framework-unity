using UnityEngine;

namespace Scenes
{
    public class SelectScene : MonoBehaviour
    {

        public void OnBackBtnClick()
        {
            
        }

        public void OnMainSceneBtnClick()
        {
            ScreenManager.Load<MainScene>(sceneName: "02. Main",
                onSceneLoaded: (scene) =>
                {
                    Debug.Log("MainScene Load Complete");
                });
        }
    }
}
