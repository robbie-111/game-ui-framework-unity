using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Manager
{
    public static class ObjectMgr
    {
        private static Dictionary<string, GameObject> _uiCache = new Dictionary<string, GameObject>();
    
        private static void Awake()
        {
            _uiCache.Clear();
        }

        /// <summary>
        /// Resources 폴더내에 있는 프리펩을 로드합니다.(동시에 캐싱)
        /// </summary>
        /// <param name="type">디렉토리 타입.</param>
        /// <param name="prefabName">프리펩 이름.</param>
        public static GameObject GetUIPrefab(string type, string prefabName)
        {
            GameObject go;
            if (_uiCache.TryGetValue(prefabName, out go))
                return go;

            go = (GameObject)Resources.Load("Prefabs/UI/" + type + "/" + prefabName);
            if (go != null)
            {
                _uiCache.Add(prefabName, go);
            }
            else
            {
                Debug.LogError("ui prefab " + prefabName + " doesn't exist");
            }

            return go;
        }

        public static GameObject GetUIPrefab(string prefabName)
        {
            GameObject go;
            if (_uiCache.TryGetValue(prefabName, out go))
                return go;

            go = (GameObject)Resources.Load($"Prefabs/{prefabName}");
            if (go != null)
            {
                _uiCache.Add(prefabName, go);
            }
            else
            {
                Debug.LogError("ui prefab " + prefabName + " doesn't exist");
            }

            return go;
        }
    }
}
