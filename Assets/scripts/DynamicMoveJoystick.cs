using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 手机RPG风格：点击屏幕左半部分时，在手指位置生成摇杆，松手后消失
/// </summary>
public class DynamicMoveJoystick : MonoBehaviour
{
    public static DynamicMoveJoystick Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("摇杆根节点（背景+手柄的父物体），用于显示/隐藏和定位")]
    public RectTransform joystickRoot;
    [Tooltip("摇杆背景图")]
    public RectTransform joystickBackground;
    [Tooltip("摇杆手柄（会跟随手指移动）")]
    public RectTransform joystickHandle;
    
    [Header("Settings")]
    [Tooltip("屏幕左半部分比例，0.5 = 左半边")]
    [Range(0.3f, 0.6f)]
    public float leftHalfRatio = 0.5f;
    [Tooltip("手柄最大偏移半径（像素）")]
    public float stickRadius = 80f;
    [Tooltip("摇杆输出的灵敏度")]
    public float outputScale = 1f;

    // 当前输出的移动向量，PlayerController 会读取
    public static Vector2 Output { get; private set; }
    /// <summary>摇杆是否正在显示/使用中</summary>
    public static bool IsActive => Instance != null && Instance.isActive;
    
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private bool isActive;
    private int trackedTouchId = -1;
    private Vector2 joystickStartPos;      // 摇杆中心在屏幕上的位置
    private Vector2 touchStartPos;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvasRect = parentCanvas.GetComponent<RectTransform>();

        HideJoystick();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // 优先使用 Touchscreen（手机）
        if (Touchscreen.current != null)
            ProcessTouchInput();
        else
            ProcessMouseInput();  // 编辑器中用鼠标测试
    }

    void ProcessTouchInput()
    {
        var touches = Touchscreen.current.touches;

        foreach (var touch in touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 pos = touch.position.ReadValue();
                if (IsInLeftHalf(pos))
                {
                    trackedTouchId = touch.touchId.ReadValue();
                    ShowJoystickAt(pos);
                    return;
                }
            }
        }

        if (isActive && trackedTouchId >= 0)
        {
            foreach (var touch in touches)
            {
                if (touch.touchId.ReadValue() == trackedTouchId)
                {
                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        HideJoystick();
                        trackedTouchId = -1;
                        return;
                    }
                    UpdateJoystick(touch.position.ReadValue());
                    return;
                }
            }
            //  touch 丢失
            HideJoystick();
            trackedTouchId = -1;
        }
    }

    void ProcessMouseInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            if (IsInLeftHalf(pos))
            {
                ShowJoystickAt(pos);
                return;
            }
        }

        if (isActive)
        {
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                HideJoystick();
                return;
            }
            UpdateJoystick(Mouse.current.position.ReadValue());
        }
    }

    bool IsInLeftHalf(Vector2 screenPos)
    {
        float width = Screen.width;
        return screenPos.x < width * leftHalfRatio;
    }

    void ShowJoystickAt(Vector2 screenPos)
    {
        if (joystickRoot == null) return;
        isActive = true;
        joystickStartPos = screenPos;
        touchStartPos = screenPos;

        joystickRoot.gameObject.SetActive(true);
        SetJoystickPosition(screenPos);
        if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
        Output = Vector2.zero;
    }

    void HideJoystick()
    {
        isActive = false;
        Output = Vector2.zero;
        if (joystickRoot != null)
            joystickRoot.gameObject.SetActive(false);
    }

    void SetJoystickPosition(Vector2 screenPos)
    {
        if (joystickRoot == null) return;

        // 转换到 joystickRoot 父物体的本地空间，这样 anchoredPosition 才能正确对应触摸点
        RectTransform parentRect = joystickRoot.parent as RectTransform;
        if (parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPos, parentCanvas?.worldCamera, out Vector2 localPos);
        joystickRoot.anchoredPosition = localPos;
    }

    void UpdateJoystick(Vector2 currentScreenPos)
    {
        Vector2 delta = currentScreenPos - joystickStartPos;
        float dist = delta.magnitude;

        // 限制在半径内
        Vector2 dir = dist > 0.01f ? delta.normalized : Vector2.zero;
        float normalizedDist = Mathf.Clamp01(dist / stickRadius);

        Output = dir * normalizedDist * outputScale;

        // 更新手柄视觉位置（注意屏幕Y向上，Canvas可能不同）
        if (joystickHandle != null)
        {
            Vector2 handleOffset = dir * (stickRadius * normalizedDist);
            joystickHandle.anchoredPosition = handleOffset;
        }
    }
}
