using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    [Header("References")]
    public FlappyView flappyView;
    public GameObject panelMenu;
    public GameObject panelGameOver;

    private bool isGameRunning = false;

    void Start()
    {
        ShowMenu();
        flappyView.StartFlappy();
    }

    void Update()
    {
        if (!isGameRunning) return;

        // Met à jour la logique de jeu via FlappyView
        flappyView.UpdateFlappy();

        // Si le jeu est terminé selon la DLL
        if (flappyView.GameOver())
        {
            GameOver();
        }
    }

    // --- ÉTATS DE JEU ---
    public void ShowMenu()
    {
        isGameRunning = false;
        panelMenu.SetActive(true);
        panelGameOver.SetActive(false);
    }

    public void StartGame()
    {
        flappyView.Reset();
        isGameRunning = true;
        panelMenu.SetActive(false);
        panelGameOver.SetActive(false);
    }

    public void GameOver()
    {
        isGameRunning = false;
        panelMenu.SetActive(false);
        panelGameOver.SetActive(true);
    }

    public void Replay()
    {
        flappyView.Reset();  // Réinitialise la logique interne
        StartGame();         // Redémarre une partie
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReturnToMenu()
    {
        ShowMenu();
    }
}
