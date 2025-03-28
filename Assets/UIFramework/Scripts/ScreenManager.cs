using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum ScreenAnimation
{
    BottomHide, // The screen slides from the center to the bottom when hiding.
    BottomShow, // The screen slides from the bottom to the center when showing.
    FadeHide,   // The screen fades out when hiding.
    FadeShow,   // The screen fades in when showing.
    LeftHide,   // The screen slides from the center to the left when hiding.
    LeftShow,   // The screen slides from the left to the center when showing.
    RightHide,  // The screen slides from the center to the right when hiding.
    RightShow,  // The screen slides from the right to the center when showing.
    RotateHide, // The screen rotates clockwise when hiding.
    RotateShow, // The screen rotates counterclockwise when showing.
    ScaleHide,  // The screen scales down to 0 when hiding.
    ScaleShow,  // The screen scales up to 1 when showing.
    TopHide,    // The screen slides from the center to the top when hiding.
    TopShow     // The screen slides from the top to the center when showing.
}

public class ScreenManager : MonoBehaviour
{
    class ScreenCoroutine
    {
        public Coroutine coroutine;
        public string screenName;

        public ScreenCoroutine(Coroutine coroutine, string screenName)
        {
            this.coroutine = coroutine;
            this.screenName = screenName;
        }
    }

    #region SerializeField
    [SerializeField] string m_ScreenPath = "Screens";
    [SerializeField] string m_ScreenAnimationPath = "Animations";
    [SerializeField] string m_SceneLoadingName;
    [SerializeField] string m_LoadingName;
    [SerializeField] string m_TooltipName;
    [SerializeField] Color m_ScreenShieldColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] float m_ScreenAnimationSpeed = 1;
    [SerializeField] Camera m_BackgroundCamera;
    [SerializeField] Canvas m_Canvas;
    [SerializeField] UnscaledAnimation m_SceneShield;
    [SerializeField] RectTransform m_Top;
    #endregion

    #region Delegate
    public delegate void OnSceneLoad<T>(T t);
    public delegate void OnScreenLoad<T>(T t);
    public delegate void Callback();
    public delegate void OnScreenAddedDelegate(string toScreen, string fromScreen, bool manually);
    public delegate void OnScreenChangedDelegate(int screenCount);
    public delegate bool AddConditionDelegate();
    #endregion

    #region Private Member
    private Scene m_LastLoadedScene;
    private List<Component> m_ScreenList = new List<Component>();
    private GameObject m_SceneLoading;
    private GameObject m_Loading;
    private TooltipBaseController m_Tooltip;
    private UnscaledAnimation m_ScreenShield;
    private GameObject m_ScreenShieldTop;
    private OnScreenAddedDelegate m_OnScreenAdded;
    private OnScreenChangedDelegate m_OnScreenChanged;
    private int m_PendingScreens = 0;
    private int m_AnimationPlayingScreens = 0;
    private List<ScreenCoroutine> m_ScreenCoroutines = new List<ScreenCoroutine>();
    private Coroutine m_LoadingCoroutine;
    #endregion

    #region Private Static
    private static ScreenManager m_Instance;

    private static ScreenManager instance
    {
        get
        {
            if (m_Instance == null)
            {
                Instantiate(Resources.Load<ScreenManager>("Prefabs/ScreenManager"));
            }

            return m_Instance;
        }
    }
    #endregion

    #region Public Static
    /// <summary>
    /// Get the scene loading operation. Can get some values like % progress.
    /// </summary>
    public static AsyncOperation asyncOperation
    {
        get;
        protected set;
    }

    /// <summary>
    /// Set some basic parameters of ScreenManager.
    /// </summary>
    /// <param name="screenShieldColor">The color of screen shield</param>
    /// <param name="screenPath">The path (in Resources folder) of screen's prefabs</param>
    /// <param name="screenAnimationPath">The path (in Resources folder) of screen's animation clips</param>
    /// <param name="sceneLoadingName">The name of the scene loading screen which is put in 'screenPath'. Set it to empty if you do not want to show the loading screen while loading a scene</param>
    /// <param name="loadingName">The name of the loading screen which is put in 'screenPath'. This screen can show/hide on the top of all screens at any time using Loading(bool). Set it to empty if you don't need</param>
    /// <param name="screenAnimationSpeed">Screen Animation speed</param>
    public static void Set(
        Color screenShieldColor, 
        string screenPath = "Screens", 
        string screenAnimationPath = "Animations", 
        string sceneLoadingName = "", 
        string loadingName = "", 
        float screenAnimationSpeed = 1, 
        string tooltipName = "")
    {
        instance.Setup(screenShieldColor, 
                       screenPath, 
                       screenAnimationPath, 
                       sceneLoadingName, 
                       loadingName, 
                       screenAnimationSpeed, 
                       tooltipName);
    }

    /// <summary>
    /// Set some basic parameters of ScreenManager.
    /// </summary>
    /// <param name="screenPath">The path (in Resources folder) of screen's prefabs</param>
    /// <param name="screenAnimationPath">The path (in Resources folder) of screen's animation clips</param>
    /// <param name="sceneLoadingName">The name of the scene loading screen which is put in 'screenPath'. Set it to empty if you do not want to show the loading screen while loading a scene</param>
    /// <param name="loadingName">The name of the loading screen which is put in 'screenPath'. This screen can show/hide on the top of all screens at any time using Loading(bool). Set it to empty if you don't need</param>
    /// <param name="screenAnimationSpeed">Screen Animation speed</param>
    public static void Set(
        string screenPath = "Screens", 
        string screenAnimationPath = "Animations", 
        string sceneLoadingName = "", 
        string loadingName = "", 
        float screenAnimationSpeed = 1, 
        string tooltipName = "")
    {
        instance.Setup(screenPath, screenAnimationPath, sceneLoadingName, loadingName, screenAnimationSpeed, tooltipName);
    }

    /// <summary>
    /// Load a scene.
    /// </summary>
    /// <typeparam name="T">The type of a (any) component in the scene</typeparam>
    /// <param name="sceneName">The name of scene</param>
    /// <param name="mode">The load scene mode. Single or Additive</param>
    /// <param name="onSceneLoaded">The callback when the scene is loaded. [IMPORTANT] It is called after the Awake & OnEnable, before the Start.</param>
    /// <param name="clearAllScreens">Clear all screens when the scene is loaded?</param>
    public static void Load<T>(
        string sceneName, 
        LoadSceneMode mode = LoadSceneMode.Single, 
        OnSceneLoad<T> onSceneLoaded = null, 
        bool clearAllScreens = true) where T : Component
    {
        StopAllAddScreenCoroutines();
        instance.LoadScene(sceneName, mode, onSceneLoaded, clearAllScreens);
    }

    /// <summary>
    /// Add a screen on top of all screens. [IMPORTANT] The code after the 'Add' method will be called after the Awake & OnEnable, before the Start.
    /// </summary>
    /// <typeparam name="T">The type of a (any) component in the screen</typeparam>
    /// <param name="screenName">The name of screen</param>
    /// <param name="showAnimation">The name of animation clip (which is put in 'screenAnimationPath') is used to animate the screen to show it</param>
    /// <param name="hideAnimation">The name of animation clip (which is put in 'screenAnimationPath') is used to animate the screen to hide it</param>
    /// <param name="animationObjectName">The name of gameobject contains screen's animation. If it is null or empty, the animation gameobject will be the root gameobject</param>
    /// <param name="useExistingScreen">If this is true, check if the screen is existing, bring it to the top. If not found, instantiate a new one</param>
    /// <param name="onScreenLoad">On Screen Loaded callback</param>
    /// <param name="hasShield">Has shield under this screen or not</param>
    /// <param name="manually">This screen is shown by user click or automatically. Just using this for analytics</param>
    /// <param name="addCondition">Only add this screen after this condition return true</param>
    /// <param name="waitUntilNoScreen">Only add this screen when no other screen is showing</param>
    /// <param name="destroyTopScreen">If this is true, destroy the top screen before adding this screen</param>
    /// <returns>The component type T in the screen.</returns>
    public static void Add<T>(
        string screenName, 
        string showAnimation = "ScaleShow", 
        string hideAnimation = "ScaleHide", 
        string animationObjectName = "", 
        bool useExistingScreen = false, 
        OnScreenLoad<T> onScreenLoad = null, 
        bool hasShield = true, 
        bool manually = true, 
        AddConditionDelegate addCondition = null, 
        bool waitUntilNoScreen = false, 
        bool destroyTopScreen = false) where T : Component
    {
        var c = instance.StartCoroutine(instance.AddScreen<T>(screenName, 
                                                                            showAnimation, 
                                                                            hideAnimation, 
                                                                            animationObjectName, 
                                                                            useExistingScreen, 
                                                                            onScreenLoad, 
                                                                            hasShield, 
                                                                            manually, 
                                                                            addCondition, 
                                                                            waitUntilNoScreen, 
                                                                            destroyTopScreen));
        instance.m_ScreenCoroutines.Add(new ScreenCoroutine(c, screenName));
    }

    /// <summary>
    /// Add a screen on top of all screens. Use ScreenAnimation enum instead of string for animations
    /// </summary>
    public static void Add<T>(
        string screenName, 
        ScreenAnimation showAnimation, 
        ScreenAnimation hideAnimation, 
        string animationObjectName = "", 
        bool useExistingScreen = false, 
        OnScreenLoad<T> onScreenLoad = null, 
        bool hasShield = true, 
        bool manually = true, 
        AddConditionDelegate addCondition = null, 
        bool waitUntilNoScreen = false, 
        bool destroyTopScreen = false) where T : Component
    {
        Add(screenName, 
            showAnimation.ToString(), 
            hideAnimation.ToString(), 
            animationObjectName, 
            useExistingScreen, 
            onScreenLoad, 
            hasShield, 
            manually, 
            addCondition, 
            waitUntilNoScreen, 
            destroyTopScreen);
    }

    /// <summary>
    /// Add a screen to the canvas, on top of all screens. But it's not added to the screen list for managing.
    /// </summary>
    /// <param name="screen">The GameObject of screen</param>
    public static void AddToCanvas(GameObject screen)
    {
        instance.AddScreenToCanvas(screen);
    }

    /// <summary>
    /// Destroy immediately the screen which is at the top of all screens, without playing animation.
    /// </summary>
    public static void Destroy()
    {
        instance.DestroyScreen();
    }

    /// <summary>
    /// Destroy immediately the specific screen, without playing animation.
    /// </summary>
    /// <param name="screen">The component in screen which is returned by the Add function.</param>
    public static void Destroy(Component screen)
    {
        instance.DestroyScreen(screen);
    }

    /// <summary>
    /// Destroy immediately all screens, without playing animation.
    /// </summary>
    public static void DestroyAll()
    {
        instance.ClearAllScreen();
    }

    /// <summary>
    /// Close the screen which is at the top of all screens.
    /// </summary>
    /// <param name="onScreenClosed">The callback when the screen is closed. [IMPORTANT] It is called right after the screen is destroyed.</param>
    /// <param name="hideAnimation">The name of animation clip (which is put in 'screenAnimationPath') is used to animate the screen to hide it. If null, the 'hideAnimation' which is declared in the Add function will be used.</param>
    public static void Close(Callback onScreenClosed = null, string hideAnimation = null)
    {
        instance.CloseScreen(onScreenClosed, hideAnimation);
    }

    /// <summary>
    /// Close the screen which is at the top of all screens. Use ScreenAnimation enum instead of string for animations
    /// </summary>\
    public static void Close(Callback onScreenClosed, ScreenAnimation hideAnimation)
    {
        Close(onScreenClosed, hideAnimation.ToString());
    }

    /// <summary>
    /// Close the screen which is at the top of all screens. Use ScreenAnimation enum instead of string for animations
    /// </summary>\
    public static void Close(ScreenAnimation hideAnimation)
    {
        Close(null, hideAnimation.ToString());
    }

    /// <summary>
    /// Close a specific screen.
    /// </summary>
    /// <param name="screen">The component in screen which is returned by the Add function.</param>
    /// <param name="onScreenClosed">The callback when the screen is closed. [IMPORTANT] It is called right after the screen is destroyed.</param>
    /// <param name="hideAnimation">The name of animation clip (which is put in 'screenAnimationPath') is used to animate the screen to hide it. If null, the 'hideAnimation' which is declared in the Add function will be used.</param>
    public static void Close(Component screen, Callback onScreenClosed = null, string hideAnimation = null)
    {
        instance.CloseScreen(screen, onScreenClosed, hideAnimation);
    }

    /// <summary>
    /// Close a specific screen. Use ScreenAnimation enum instead of string for animations
    /// </summary>
    public static void Close(Component screen, Callback onScreenClosed, ScreenAnimation hideAnimation)
    {
        Close(screen, onScreenClosed, hideAnimation.ToString());
    }

    /// <summary>
    /// Close a specific screen. Use ScreenAnimation enum instead of string for animations
    /// </summary>
    public static void Close(Component screen, ScreenAnimation hideAnimation)
    {
        Close(screen, null, hideAnimation.ToString());
    }

    /// <summary>
    /// Show/Hide the loading screen (which has the name 'loadingName') on top of all screens.
    /// </summary>
    /// <param name="isShow">True if show, False if hide</param>
    /// <param name="timeout">If timeout == 0, no timeout</param>
    public static void Loading(bool isShow, float timeout = 0)
    {
        instance.ShowLoading(isShow, timeout);
    }

    /// <summary>
    /// Remove a specific screen. But don't use this. We use this for an internal purpose.
    /// </summary>
    /// <param name="screen">The component in screen which is returned by Add function.</param>
    public static void RemoveScreen(Component screen)
    {
        instance.RemoveScreenFromList(screen);
    }

    /// <summary>
    /// Hide Shield Or Show Top Screen
    /// </summary>
    public static void HideShieldOrShowTop(string screenName)
    {
        if (instance != null && instance.isActiveAndEnabled)
        {
            instance.HideScreenShieldOrShowTop(screenName);
        }
    }

    /// <summary>
    /// Find child Breadth-First Search
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static GameObject FindChildBFS(GameObject parent, string name)
    {
        Queue<Transform> queue = new Queue<Transform>();

        queue.Enqueue(parent.transform);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            foreach (Transform child in current)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }

                queue.Enqueue(child);
            }
        }

        return null;
    }

    /// <summary>
    /// Add OnScreenTransition listener
    /// </summary>
    /// <param name="onScreenAdded"></param>
    public static void AddListener(OnScreenAddedDelegate onScreenAdded)
    {
        if (instance != null)
        {
            instance.m_OnScreenAdded += onScreenAdded;
        }
    }

    /// <summary>
    /// Remove OnScreenTransition listener
    /// </summary>
    /// <param name="onScreenTransition"></param>
    public static void RemoveListener(OnScreenAddedDelegate onScreenTransition)
    {
        if (m_Instance != null)
        {
            m_Instance.m_OnScreenAdded -= onScreenTransition;
        }
    }

    /// <summary>
    /// Add OnScreenChanged listener
    /// </summary>
    /// <param name="onScreenChanged"></param>
    public static void AddListener(OnScreenChangedDelegate onScreenChanged)
    {
        if (instance != null)
        {
            instance.m_OnScreenChanged += onScreenChanged;
        }
    }

    /// <summary>
    /// Remove OnScreenChanged listener
    /// </summary>
    /// <param name="onScreenChanged"></param>
    public static void RemoveListener(OnScreenChangedDelegate onScreenChanged)
    {
        if (m_Instance != null)
        {
            m_Instance.m_OnScreenChanged -= onScreenChanged;
        }
    }

    /// <summary>
    /// Get The Top RectTransform. UIs here are highest UIs.
    /// </summary>
    public static RectTransform Top
    {
        get
        {
            if (instance != null)
            {
                instance.m_Top.SetAsLastSibling();
                return instance.m_Top;
            }

            return null;
        }
    }

    /// <summary>
    /// Show the shield
    /// </summary>
    public static void ShowShield()
    {
        if (m_Instance != null && m_Instance.m_ScreenShield != null)
        {
            if (!m_Instance.m_ScreenShield.gameObject.activeInHierarchy)
            {
                m_Instance.m_ScreenShield.gameObject.SetActive(true);
            }

            m_Instance.m_ScreenShield.Play("ShieldShow", speed: m_Instance.m_ScreenAnimationSpeed);
        }
    }

    /// <summary>
    /// Hide the shield
    /// </summary>
    public static void HideShield()
    {
        if (m_Instance != null && m_Instance.m_ScreenShield != null)
        {
            m_Instance.m_ScreenShield.Play("ShieldHide", speed: m_Instance.m_ScreenAnimationSpeed);
        }
    }

    /// <summary>
    /// Stop All AddScreen Coroutines
    /// </summary>
    public static void StopAllAddScreenCoroutines()
    {
        if (m_Instance != null)
        {
            for (int i = 0; i < instance.m_ScreenCoroutines.Count; i++)
            {
                var sc = instance.m_ScreenCoroutines[i];
                if (sc != null && sc.coroutine != null)
                {
                    instance.StopCoroutine(sc.coroutine);
                    sc.coroutine = null;
                    Debug.LogWarning("CM: StopCoroutine " + sc.screenName.ToString());
                }
            }
            instance.m_ScreenCoroutines.Clear();
        }
    }

    /// <summary>
    /// Check if no screen is appearing, no screen is animating, and no screen is pending to be added.
    /// </summary>
    /// <returns></returns>
    public static bool IsNoMoreScreen()
    {
        if (m_Instance == null)
            return true;

        return (m_Instance.m_ScreenList.Count <= 0 && m_Instance.m_PendingScreens <= 0 && m_Instance.m_AnimationPlayingScreens <= 0);
    }

    /// <summary>
    /// Show tooltip
    /// </summary>
    /// <param name="text">Tooltip content</param>
    /// <param name="worldPosition">Tooltip position</param>
    /// <param name="tooltipName">Tooltip prefab name</param>
    /// <param name="targetY">Target Y</param>
    public static void ShowTooltip(string text, Vector3 worldPosition, float targetY = 100f)
    {
        if (m_Instance == null)
            return;

        m_Instance.LoadAndShowTooltip(text, worldPosition, targetY);
    }

    /// <summary>
    /// Hide Tooltip
    /// </summary>
    public static void HideTooltip()
    {
        if (m_Instance == null)
            return;

        m_Instance.HideTooltipImmediately();
    }
    #endregion

    #region Unity Functions
    private void Awake()
    {
        if (m_Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            m_Instance = this;
            DontDestroyOnLoad(gameObject);

            name = "ScreenManager";

            Application.targetFrameRate = 60;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

            SetupCameras();
            SetupCanvases();
            CreateShield();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_Loading == null || !m_Loading.activeInHierarchy)
            {
                if (m_ScreenList.Count > 0)
                {
                    var screen = m_ScreenList[m_ScreenList.Count - 1];

                    if (screen.TryGetComponent(out IKeyBack keyback))
                    {
                        keyback.OnKeyBack();
                    }
                    else
                    {
                        Close();
                    }
                }
            }
        }
    }

    private void SceneManager_sceneUnloaded(Scene scene)
    {
        m_BackgroundCamera.gameObject.SetActive(true);
    }

    private void SceneManager_activeSceneChanged(Scene scene1, Scene scene2)
    {
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupCameras();
        SetupCanvases();

        m_LastLoadedScene = scene;
    }
    #endregion

    #region Private Functions
    private void Setup(Color screenShieldColor, string screenPath = "Screens", string screenAnimationPath = "Animations", string sceneLoadingName = "", string loadingName = "", float screenAnimationSpeed = 1, string tooltipName = "")
    {
        m_ScreenShieldColor = screenShieldColor;
        Setup(screenPath, screenAnimationPath, sceneLoadingName, loadingName, screenAnimationSpeed, tooltipName);

        UpdateScreenShieldColor();
    }

    private void Setup(string screenPath = "Screens", string screenAnimationPath = "Animations", string sceneLoadingName = "", string loadingName = "", float screenAnimationSpeed = 1, string tooltipName = "")
    {
        m_ScreenPath = screenPath;
        m_ScreenAnimationPath = screenAnimationPath;
        m_SceneLoadingName = sceneLoadingName;
        m_LoadingName = loadingName;
        m_TooltipName = tooltipName;

        if (screenAnimationSpeed > 0)
        {
            m_ScreenAnimationSpeed = screenAnimationSpeed;
        }
    }

    private void LoadScene<T>(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, OnSceneLoad<T> onSceneLoaded = null, bool clearAllScreen = true) where T : Component
    {
        StartCoroutine(CoLoadScene(sceneName, mode, onSceneLoaded, clearAllScreen));
    }

    private IEnumerator CoLoadScene<T>(string sceneName, LoadSceneMode mode, OnSceneLoad<T> onSceneLoaded = null, bool clearAllScreen = true) where T : Component
    {
        m_SceneShield.transform.SetAsLastSibling();

        if (mode == LoadSceneMode.Single)
        {
            m_SceneShield.Play("ShieldShow", speed: m_ScreenAnimationSpeed);

            yield return new WaitForSecondsRealtime(m_SceneShield.GetLength("ShieldShow") / m_ScreenAnimationSpeed);

            if (clearAllScreen)
            {
                m_ScreenShield.gameObject.SetActive(false);

                ClearAllScreen();
            }
        }

        if (mode == LoadSceneMode.Single && !string.IsNullOrEmpty(m_SceneLoadingName))
        {
            if (m_SceneLoading == null)
            {
#if ADDRESSABLE
                var async = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(m_SceneLoadingName);
                async.Completed += (a => {
                    if (a.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        CreateSceneLoading(async.Result);
                        ShowSceneLoading();
                    }
                });
#else
                var prefab = Resources.Load<GameObject>(Path.Combine(m_ScreenPath, m_SceneLoadingName));
                CreateSceneLoading(prefab);
                ShowSceneLoading();
#endif
            }
            else
            {
                ShowSceneLoading();
            }
        }

        asyncOperation = SceneManager.LoadSceneAsync(sceneName, mode);
        asyncOperation.completed += (asyncOp) =>
        {
            onSceneLoaded?.Invoke(GetSceneComponent<T>(instance.m_LastLoadedScene));
        };

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        if (mode == LoadSceneMode.Single)
        {
            m_SceneShield.Play("ShieldHide", speed: m_ScreenAnimationSpeed);
        }

        if (mode == LoadSceneMode.Single && !string.IsNullOrEmpty(m_SceneLoadingName))
        {
            while (m_SceneLoading == null)
            {
                yield return 0;
            }
        }

        if (m_SceneLoading != null)
        {
            yield return 0;

            m_SceneLoading.SetActive(false);
        }
    }

    private void CreateSceneLoading(GameObject prefab)
    {
        m_SceneLoading = Instantiate(prefab);
        m_SceneLoading.name = m_SceneLoadingName;
        AddScreenToCanvas(m_SceneLoading);
    }

    private void ShowSceneLoading()
    {
        m_SceneLoading.transform.SetAsLastSibling();
        m_SceneLoading.SetActive(true);
    }

    private IEnumerator AddScreen<T>(string screenName, 
                                     string showAnimation = "ScaleShow", 
                                     string hideAnimation = "ScaleHide", 
                                     string animationObjectName = "", 
                                     bool useExistingScreen = false, 
                                     OnScreenLoad<T> onScreenLoad = null, 
                                     bool hasShield = true, 
                                     bool manually = true, 
                                     AddConditionDelegate addCondition = null, 
                                     bool waitUntilNoScreen = false, 
                                     bool destroyTopScreen = false) where T : Component
    {
        while (addCondition != null && !addCondition())
        {
            yield return 0;
        }

        while (m_PendingScreens > 0 || m_AnimationPlayingScreens > 0)
        {
            yield return 0;
        }

        while (waitUntilNoScreen && (m_PendingScreens > 0 || m_ScreenList.Count > 0))
        {
            yield return 0;
        }

        m_PendingScreens++;

        m_ScreenShield.name = ScreenShieldName(screenName);

        if (hasShield)
        {
            ShowScreenShield();
        }
        else
        {
            HideScreenShield();
        }

        if (m_ScreenShieldTop == null)
        {
            m_ScreenShieldTop = CreateTransparentShield();
            m_ScreenShieldTop.SetActive(false);
        }

        var fromScreen = m_LastLoadedScene != null ? m_LastLoadedScene.name : string.Empty;

        if (m_ScreenList.Count > 0)
        {
            var topScreen = m_ScreenList[m_ScreenList.Count - 1];

            if (topScreen != null)
            {
                if (destroyTopScreen)
                {
                    DestroyScreen(topScreen);
                }
                else
                {
                    //robbie test
                    topScreen.gameObject.SetActive(false);
                }

                fromScreen = topScreen.name;
            }
        }

        T screen = null;

        var hasExistingScreen = false;

        if (useExistingScreen)
        {
            for (int i = 0; i < m_ScreenList.Count; i++)
            {
                var existingScreen = m_ScreenList[i].GetComponentInChildren<T>();

                if (existingScreen != null)
                {
                    hasExistingScreen = true;

                    screen = existingScreen;
                    screen.transform.SetAsLastSibling();
                    screen.gameObject.SetActive(true);
                    PlayAnimation(screen, screen.GetComponent<ScreenController>().showAnimation, 4);

                    m_PendingScreens--;

                    var temp = m_ScreenList[i];
                    m_ScreenList[i] = m_ScreenList[m_ScreenList.Count - 1];
                    m_ScreenList[m_ScreenList.Count - 1] = temp;

                    onScreenLoad?.Invoke(screen);

                    break;
                }
            }
        }

        if (!hasExistingScreen)
        {
#if ADDRESSABLE
            var async = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(screenName);
            async.Completed += (a => {
                if (a.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    CreateScreen<T>(async.Result, screenName, showAnimation, hideAnimation, animationObjectName, onScreenLoad, hasShield);
                }
            });
#else
            var prefab = Resources.Load<GameObject>(Path.Combine(m_ScreenPath, screenName));
            CreateScreen<T>(prefab, 
                            screenName, 
                            showAnimation, 
                            hideAnimation, 
                            animationObjectName, 
                            onScreenLoad, 
                            hasShield);
#endif
        }

        OnScreenAdded(screenName, fromScreen, manually);
    }

    private void OnScreenAdded(
        string toScreen, 
        string fromScreen, 
        bool manually)
    {
        m_OnScreenAdded?.Invoke(toScreen, fromScreen, manually);
    }

    private void CreateScreen<T>(
        GameObject prefab, 
        string screenName, 
        string showAnimation = "ScaleShow", 
        string hideAnimation = "ScaleHide", 
        string animationObjectName = "", 
        OnScreenLoad<T> onScreenLoad = null, 
        bool hasShield = true) where T : Component
    {
        T screen = Instantiate(prefab.GetComponent<T>(), m_Canvas.transform);

        screen.name = screenName;
        AddScreenToCanvas(screen.gameObject);

        var controller = AddScreenController(screen);
        controller.screen = screen;
        controller.showAnimation = showAnimation;
        controller.hideAnimation = hideAnimation;
        controller.animationObjectName = animationObjectName;
        controller.hasShield = hasShield;

        AddAnimations(screen, animationObjectName, showAnimation, hideAnimation);
        PlayAnimation(screen, showAnimation, 4);

        AddScreenToList(screen);

        onScreenLoad?.Invoke(screen);
    }

    private void CloseScreen(Callback onScreenClosed = null, string hideAnimation = null)
    {
        if (m_ScreenList.Count > 0)
        {
            var screen = m_ScreenList[m_ScreenList.Count - 1];
            CloseScreen(screen, onScreenClosed, hideAnimation);
        }
    }

    private void CloseScreen(
        Component screen, 
        Callback onScreenClosed = null, 
        string hideAnimation = null)
    {
        if (m_ScreenList.Count > 0)
        {
            RemoveScreenFromList(screen);

            hideAnimation = (hideAnimation != null) ? hideAnimation : screen.GetComponent<ScreenController>().hideAnimation;
            PlayAnimation(screen, hideAnimation, 0, true, onScreenClosed);
        }
    }

    private void ClearAllScreen()
    {
        while (m_ScreenList.Count > 0)
        {
            var screen = m_ScreenList[0];
            m_ScreenList.RemoveAt(0);

            DestroyScreen(screen);
        }

        m_PendingScreens = 0;
        m_AnimationPlayingScreens = 0;
    }

    private void DestroyScreen()
    {
        if (m_ScreenList.Count > 0)
        {
            var screen = m_ScreenList[m_ScreenList.Count - 1];

            DestroyScreen(screen);
        }
    }

    private void DestroyScreen(Component screen)
    {
        if (screen != null && screen.gameObject != null)
        {
            Destroy(screen.gameObject);
        }
    }

    private void ShowLoading(bool isShow, float timeout = 0)
    {
        if (isShow)
        {
            if (!string.IsNullOrEmpty(m_LoadingName))
            {
                if (m_Loading == null)
                {
#if ADDRESSABLE
                    var async = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(m_LoadingName);
                    async.Completed += (a => {
                        if (a.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                        {
                            CreateLoading(async.Result);
                            ShowLoading(timeout);
                        }
                    });
#else
                    var prefab = Resources.Load<GameObject>(Path.Combine(m_ScreenPath, m_LoadingName));
                    CreateLoading(prefab);
                    ShowLoading(timeout);
#endif
                }
                else
                {
                    ShowLoading(timeout);
                }
            }
        }
        else
        {
            HideLoading();
        }
    }

    private void CreateLoading(GameObject prefab)
    {
        m_Loading = Instantiate(prefab);
        m_Loading.name = m_LoadingName;
        AddScreenToCanvas(m_Loading);
    }

    private void ShowLoading(float timeout = 0)
    {
        m_Loading.transform.SetAsLastSibling();

        StopLoadingCoroutine();

        if (timeout > 0)
        {
            m_LoadingCoroutine = StartCoroutine(CoShowLoading(timeout));
        }
        else
        {
            m_Loading.SetActive(true);
        }
    }

    private void HideLoading()
    {
        StopLoadingCoroutine();

        if (m_Loading != null)
        {
            m_Loading.SetActive(false);
        }
    }

    private void StopLoadingCoroutine()
    {
        if (m_LoadingCoroutine != null)
        {
            StopCoroutine(m_LoadingCoroutine);
            m_LoadingCoroutine = null;
        }
    }

    private IEnumerator CoShowLoading(float timeout)
    {
        m_Loading.SetActive(true);

        yield return new WaitForSecondsRealtime(timeout);

        m_Loading.SetActive(false);
    }

    private void AddScreenToCanvas(GameObject screen)
    {
        screen.transform.SetParent(m_Canvas.transform);
        screen.transform.localPosition = Vector3.zero;
        screen.transform.localScale = Vector3.one;
        screen.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
    }

    private void CreateShield()
    {
        m_ScreenShield = Instantiate(Resources.Load<GameObject>("Prefabs/Shield"), m_Canvas.transform).GetComponent<UnscaledAnimation>();
        m_ScreenShield.name = ScreenShieldName("Default");
        m_ScreenShield.transform.SetAsLastSibling();
        m_ScreenShield.gameObject.SetActive(false);

        UpdateScreenShieldColor();
    }

    private void UpdateScreenShieldColor()
    {
        var image = m_ScreenShield.GetComponent<Image>();
        image.color = instance.m_ScreenShieldColor;
    }

    private void ShowScreenShield()
    {
        if (!m_ScreenShield.gameObject.activeInHierarchy || (m_ScreenShield.isPlaying && m_ScreenShield.currentClipName == "ShieldHide"))
        {
            m_ScreenShield.gameObject.SetActive(true);
            m_ScreenShield.Play("ShieldShow", speed: m_ScreenAnimationSpeed);
        }
    }

    private void HideScreenShield()
    {
        if (m_ScreenShield.gameObject.activeInHierarchy)
        {
            m_ScreenShield.Play("ShieldHide", (anim) => {
                m_ScreenShield.gameObject.SetActive(false);
            }, speed: m_ScreenAnimationSpeed);
        }
    }

    private GameObject CreateTransparentShield()
    {
        var shield = Instantiate(Resources.Load<GameObject>("Prefabs/TransparentShield"), m_Canvas.transform);
        shield.name = "Transparent Shield";

        var image = shield.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0);

        return shield;
    }

    private Animation AddAnimations(Component screen, string animationObjectName = "", params string[] animationNames)
    {
        GameObject animObject = screen.gameObject;

        if (!string.IsNullOrEmpty(animationObjectName))
        {
            animObject = FindChildBFS(screen.gameObject, animationObjectName);

            if (animObject == null)
            {
                animObject = screen.gameObject;
            }
        }

        var anim = animObject.GetComponent<Animation>();
        if (anim == null)
        {
            anim = animObject.AddComponent<Animation>();
        }

        var unscaledAnim = animObject.GetComponent<UnscaledAnimation>();
        if (unscaledAnim == null)
        {
            animObject.AddComponent<UnscaledAnimation>();
        }

        anim.playAutomatically = false;

        for (int i = 0; i < animationNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(animationNames[i]))
            {
                if (anim.GetClip(animationNames[i]) == null)
                {
                    var path = Path.Combine(m_ScreenAnimationPath, animationNames[i]);
                    var clip = Resources.Load<AnimationClip>(path);

                    if (clip == null)
                    {
                        var defaultPath = Path.Combine("Animations", animationNames[i]);
                        clip = Resources.Load<AnimationClip>(defaultPath);
                    }

                    if (clip != null)
                    {
                        anim.AddClip(clip, animationNames[i]);
                    }
                    else
                    {
                        Debug.LogWarning("Animation Clip not found: " + path);
                    }
                }

                if (animObject.GetComponent<CanvasGroup>() == null)
                {
                    animObject.AddComponent<CanvasGroup>();
                }

                switch (animationNames[i])
                {
                    case "RightShow":
                    case "LeftShow":
                    case "TopShow":
                    case "BottomShow":
                    case "RightHide":
                    case "LeftHide":
                    case "TopHide":
                    case "BottomHide":
                        if (animObject.GetComponent<AnimationPosition>() == null)
                        {
                            animObject.AddComponent<AnimationPosition>();
                        }
                        break;
                }
            }
        }

        return anim;
    }

    private void PlayAnimation(Component screen, string animationName, int delayFrames = 0, bool destroyScreenAtAnimationEnd = false, Callback onAnimationEnd = null)
    {
        m_AnimationPlayingScreens++;

        var anim = AddAnimations(screen, screen.GetComponent<ScreenController>().animationObjectName, animationName);

        StartCoroutine(CoPlayAnimation(anim, animationName, delayFrames, onAnimationEnd, destroyScreenAtAnimationEnd ? screen : null));
    }

    private IEnumerator CoPlayAnimation(Animation anim, string animationName, int delayFrames, Callback onAnimationEnd = null, Component screenToBeDestroyed = null)
    {
        if (anim.GetClip(animationName) != null)
        {
            // Show screen shield before playing animation
            m_ScreenShieldTop.SetActive(true);
            m_ScreenShieldTop.transform.SetAsLastSibling();

            // Unscaled anim
            var unscaledAnim = anim.GetComponent<UnscaledAnimation>();
            unscaledAnim.PauseAtBeginning(animationName);

            // Reposition by screen width / height
            var animRepos = anim.GetComponent<AnimationPosition>();
            if (animRepos != null)
            {
                animRepos.Reposition();
            }

            // Wating some frames for smooth
            for (int i = 0; i < delayFrames; i++)
            {
                yield return 0;
            }

            // Play animation
            unscaledAnim.Play(animationName, speed: m_ScreenAnimationSpeed);

            // Wait animation
            yield return new WaitForSecondsRealtime(anim[animationName].length / m_ScreenAnimationSpeed);

            // Turn off screen shield after animation end
            m_ScreenShieldTop.SetActive(false);
        }

        if (screenToBeDestroyed != null)
        {
            DestroyScreen(screenToBeDestroyed);
        }

        onAnimationEnd?.Invoke();

        m_AnimationPlayingScreens--;
    }

    private ScreenController AddScreenController(Component screen)
    {
        var controller = screen.GetComponent<ScreenController>();

        if (controller == null)
        {
            controller = screen.gameObject.AddComponent<ScreenController>();
        }

        return controller;
    }

    private void SetupCameras()
    {
        var cameras = FindObjectsOfType<Camera>();

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != m_BackgroundCamera)
            {
                if (cameras[i].clearFlags == CameraClearFlags.Skybox || cameras[i].clearFlags == CameraClearFlags.SolidColor)
                {
                    m_BackgroundCamera.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    private void SetupCanvases()
    {
        var screenRatio = (float)Screen.width / Screen.height;

        var canvasScalers = FindObjectsOfType<CanvasScaler>(true);
        for (int i = 0; i < canvasScalers.Length; i++)
        {
            SetupCanvasScaler(canvasScalers[i], screenRatio);
        }
    }

    private void SetupCanvasScaler(CanvasScaler canvasScaler, float screenRatio)
    {
        canvasScaler.matchWidthOrHeight = screenRatio > 0.44f ? 1f : 0f;
    }

    private T GetSceneComponent<T>(Scene scene) where T : Component
    {
        var objects = scene.GetRootGameObjects();

        for (int i = 0; i < objects.Length; i++)
        {
            var t = objects[i].GetComponentInChildren<T>();

            if (t != null)
            {
                return t;
            }
        }

        return null;
    }

    private void HideScreenShieldOrShowTop(string screenName)
    {
        if (m_ScreenList.Count == 0)
        {
            //robbie test
            if (m_ScreenShield.name == ScreenShieldName(screenName))
            {
                Debug.Log($"{screenName} - HideScreenShield");
                HideScreenShield();
            }
        }
        else
        {
            var topScreen = m_ScreenList[m_ScreenList.Count - 1];

            if (topScreen != null && topScreen.gameObject != null && !topScreen.gameObject.activeInHierarchy)
            {
                m_ScreenShield.name = ScreenShieldName(topScreen.name);

                topScreen.transform.SetSiblingIndex(m_ScreenShield.transform.GetSiblingIndex() + 1);
                topScreen.gameObject.SetActive(true);

                var topController = topScreen.GetComponent<ScreenController>();

                if (topController.hasShield)
                {
                    ShowScreenShield();
                }
                else
                {
                    HideScreenShield();
                }

                //robbie test
                PlayAnimation(topScreen, topController.showAnimation);
            }
        }
    }

    private string ScreenShieldName(string screenName)
    {
        return "Screen Shield - " + screenName;
    }

    private void AddScreenToList(Component screen)
    {
        m_PendingScreens--;

        m_ScreenList.Add(screen);

        m_OnScreenChanged?.Invoke(m_ScreenList.Count);
    }

    private void RemoveScreenFromList(Component screen)
    {
        if (m_ScreenList.Contains(screen))
        {
            m_ScreenList.Remove(screen);

            m_OnScreenChanged?.Invoke(m_ScreenList.Count);
        }
    }

    private void CreateAndShowTooltip(GameObject tooltipPrefab, string text, Vector3 worldPosition, float targetY)
    {
        var tooltip = Instantiate(tooltipPrefab, Top);
        m_Tooltip = tooltip.GetComponent<TooltipBaseController>();

        if (m_Tooltip != null)
        {
            m_Tooltip.ShowTooltip(text, worldPosition, targetY);
        }
    }

    private void LoadAndShowTooltip(string text, Vector3 worldPosition, float targetY = 100f)
    {
        if (string.IsNullOrEmpty(m_TooltipName))
            return;

        if (m_Tooltip != null)
        {
            m_Tooltip.transform.SetParent(Top, true);
            m_Tooltip.ShowTooltip(text, worldPosition, targetY);
            return;
        }

#if ADDRESSABLE
            var async = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(m_TooltipName);
            async.Completed += (a => {
                if (a.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    CreateAndShowTooltip(async.Result, text, worldPosition, targetY);
                }
            });
#else
        var tooltipPrefab = Resources.Load<GameObject>(Path.Combine(m_ScreenPath, m_TooltipName));
        CreateAndShowTooltip(tooltipPrefab, text, worldPosition, targetY);
#endif
    }

    private void HideTooltipImmediately()
    {
        if (m_Tooltip != null)
        {
            m_Tooltip.HideToolTip();
        }
    }
    #endregion
}