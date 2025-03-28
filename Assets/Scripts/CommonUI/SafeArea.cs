using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform _safeAreaRect;

    private void Awake()
    {
        _safeAreaRect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _safeAreaRect.anchorMin = anchorMin;
        _safeAreaRect.anchorMax = anchorMax;
    }
}
