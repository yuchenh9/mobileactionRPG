using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance{get;private set;}
    public Transform playerBody;
    public Vector3 initialPosition;
    public float volumn = 0.5f;
    void Awake(){
        if(Instance==null){
            Instance=this;
        }else{
            Destroy(gameObject);
        }
    }
    [Header("Movement")]
    public float moveSpeed = 5f;
    public CharacterController controller; // 拖入 CharacterController 组件
    [Header("Jump & Gravity")]
    public float gravity = -20.81f;    // 重力加速度
    public float jumpHeight = 1.5f;   // 跳跃高度
    private Vector3 velocity;         // 当前的垂直速度
    private bool isGrounded;          // 是否在地面上
    //public Transform gun;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public GameObject uis;
    //public Transform deathCamera;

    private float xRotation = 0f;
    

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        initialPosition=playerBody.position;
        // 自动获取组件（如果没拖的话）
        if (controller == null) controller = GetComponentInParent<CharacterController>();
    }
    public void SetUIS(bool setActive){
        uis.SetActive(setActive);
    }
    public void Reset()
    {
        // 1. 停止所有正在运行的协程（非常重要！防止死亡倒地动画继续执行）
        StopAllCoroutines();

        // 2. 必须先禁用 CharacterController 才能强制修改坐标
        if (controller != null) controller.enabled = false;

        // 3. 重置时间缩放
        Time.timeScale = 1f;

        // 4. 重置位置和旋转
        // 注意：我们将父物体移动到初始位置，将子物体(playerBody)本地旋转归零
        transform.position = initialPosition;
        transform.rotation = Quaternion.identity; // 身体朝向前方
        
        if (playerBody != null)
        {
            playerBody.localRotation = Quaternion.identity;
            playerBody.localPosition = Vector3.zero;
        }

        // 5. 【关键】重置鼠标旋转逻辑变量
        xRotation = 0f;          // 视角水平
        velocity = Vector3.zero; // 垂直速度归零，防止复活后被重力瞬间拍在地上

        // 6. 重新激活 UI 和控制
        uis.SetActive(true);
        
        // 7. 最后重新开启 CharacterController
        if (controller != null) controller.enabled = true;

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        
        Debug.Log("玩家已重置，当前位置：" + transform.position);
    }
    public void Clear(){
        uis.SetActive(false);
        controller.enabled = false;
        //StartDeathAnimation();
    }

    void Update()
    {
        if(!Mouse.current.leftButton.wasPressedThisFrame&&!gameStat.Instance.isPaused){
            HandleMouseLook();
        }
        HandleMovement();
    }
    private Vector2 moveInput;
    private Vector3 toMove=new Vector3(0f,0f,0f);

    // This is the function you select in the Unity Event dropdown
    public void OnMove(InputAction.CallbackContext context)
    {
        // Reads the Vector2 value (X and Y) from the On-Screen Stick
        moveInput = context.ReadValue<Vector2>();
        Debug.Log("moveInput: "+moveInput);
        toMove.x=moveInput.x;
        toMove.z=moveInput.y;
        //controller.Move(toMove * moveSpeed);
    }
public Transform camTransform;
private Vector3 moveDirection;
private Transform handTransform;

private Vector3 camForward ;
private Vector3 camRight ;
    // 获取当前有效的移动输入：优先使用 DynamicMoveJoystick（手机触摸），否则用 OnMove（固定摇杆/键盘）
    Vector2 GetMoveInput()
    {
        if (DynamicMoveJoystick.IsActive)
            return DynamicMoveJoystick.Output;
        return moveInput;
    }

    void HandleMovement()
    {
        // 1. 地面检测
        // CharacterController 有个 isGrounded 属性，但在移动前检测更准确
        isGrounded = controller.isGrounded;
        
        // 如果在地面上且速度在往下掉，重置速度（设置一个小的负值如 -2f 以确保能贴着地走）
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 currentMoveInput = GetMoveInput();
        toMove.x = currentMoveInput.x;
        toMove.z = currentMoveInput.y;
        camForward = camTransform.forward;
        camRight = camTransform.right;
        moveDirection=(camForward*currentMoveInput.y)+(camRight*currentMoveInput.x);
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        // 4. 跳跃逻辑
        // 使用公式: v = sqrt(h * -2 * g)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. 应用重力
        // 速度随时间增加 (Δv = g * Δt)
        velocity.y += gravity * Time.deltaTime;

        // 6. 执行垂直移动 (Δy = v * Δt)
        controller.Move(velocity * Time.deltaTime);
    }
    private void Move(){}
    [Header("Gun Movement")]
    public float horizontalMovement=0f;
    public float horizontalThreshhold=1f;
    public bool lastDirection=true;
    public float gunMoveScale=0.1f;
    public float lookSpeed=5f;
    void HandleMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(-look.y*lookSpeed*5, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * look.x*lookSpeed);
        }
    }
    private Vector2 look;
    public void OnLook(InputAction.CallbackContext context)
    {
        look = context.ReadValue<Vector2>();
    }
}