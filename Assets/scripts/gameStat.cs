using UnityEngine;
using TMPro; // Required namespace
using UnityEngine.InputSystem; // Required for Input System
using System.Collections;
using UnityEngine.SceneManagement;


public class gameStat : MonoBehaviour
{
    public static gameStat Instance;
    public int hp=5;

    [Header("World Space UI")]
    // Use TextMeshPro for objects in the 3D scene
    public TextMeshPro statusText; 
    public TextMeshPro winText; 
    
    public GameObject winPanel;
    public bool finishedSpawning = false;
    private bool isWin = false;
    
    [Header("Stats")]
    public int totalEnemyCount = 50;
    public int score = 0;
    public int enemyCount = 0;
    public bool isDead = false;
    public void RestartGame()
    {
        // 必须恢复时间流速，否则新场景也是静止的
        Time.timeScale = 1f;

        // 重新加载当前处于激活状态的场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void Awake()
    {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        StartCoroutine(StartSequence());
    }

    void Update()
    {
        // Update spawning status
        if (enemyCount >= totalEnemyCount) {
            finishedSpawning = true;
        }
        if(hp<=0){
            isDead=true;
            isPaused=true;
            //PlayerController.Instance.StartDeathAnimation();
            winPanel.SetActive(true);
            winText.text = $"你已经陨落\n按R键重生";
            PlayerController.Instance.Clear();
        }

        // Win logic: Check if we are done spawning AND all enemies are gone
        if (!isWin && finishedSpawning && enemyCount <= 0) {
            
            StartCoroutine(WinSequence());
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
        if(Keyboard.current.rKey.wasPressedThisFrame){
            if(!isDead){
                return;
            }
            RestartGame();
        }

        if(gameStat.Instance.isPaused && Keyboard.current.qKey.wasPressedThisFrame){
            gameStat.Instance.QuitGame();
        }

        UpdateTextDisplay();
    }
    public void QuitGame(){
        Application.Quit();
    }
    void Restart(){
        isDead=false;
        isPaused=false;
        winPanel.SetActive(false);
        hp=5;
        finishedSpawning=false;
        isWin=false;
        PlayerController.Instance.Reset();
    }
    void nextRound(){
        totalEnemyCount=4*totalEnemyCount;
        hp=5;
        finishedSpawning=false;
        isWin=false;
    }
    IEnumerator StartSequence(){
        winPanel.SetActive(true);
        winText.text = $"制作:黄雨辰\n按WSAD移动\n按鼠标左右键开枪\n按ESC暂停";
        yield return new WaitForSeconds(5f);
        winPanel.SetActive(false);
        
    }
    IEnumerator WinSequence()
    {
        isWin = true;

        // 1. 显示胜利面板
        if (winPanel != null) winPanel.SetActive(true);

        // 2. 等待一段时间（例如 5 秒）
        // 你也可以把这个 5f 改成一个变量，比如 public float winPanelDuration = 5f;
        winText.text = $"你打爆了一堆绿豆";
        yield return new WaitForSeconds(3f);

        winText.text = $"下一波绿豆在7秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在6秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在5秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在4秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在3秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在2秒之后到达";
        yield return new WaitForSeconds(1f);

        winText.text = $"下一波绿豆在1秒之后到达";
        yield return new WaitForSeconds(1f);

        // 3. 关掉面板
        if (winPanel != null) winPanel.SetActive(false);
        nextRound();
        // 选做：这里可以加入加载下一关或者返回菜单的逻辑
        // SceneManager.LoadScene("MainMenu");
    }

    void UpdateTextDisplay()
    {
        if (statusText != null)
        {
            // \n creates a new line in the 3D text
            statusText.text = $"击杀绿豆: {score}\n 剩余: {enemyCount}";
        }

    }

    public GameObject pauseMenuUI;
    public bool isPaused = false;
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseMenuUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            pauseMenuUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}