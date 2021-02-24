using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager instance = null;
    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        //DontDestroyOnLoad(this.gameObject);
    }
    #endregion

    public int points;
    public int difficultyLevel;
    [HideInInspector] public int timesVisited;
    [HideInInspector] public int messValue;
    [HideInInspector] public int peopleInjured;
    int injuredLimit = 1;
    int messLimit = 1;
    int visitLimit = 2;
    public bool janitorActive;
    public bool nurseActive;
    public bool principalActive;

    public TextMeshProUGUI score;
    public GameObject gameOverScreen;
    public GameObject janitorPF;
    public GameObject nursePF;
    public GameObject principalPF;
    public Transform enemySpawnLocation;

    private void Start()
    {
        score.text = points.ToString();
    }

    public void UpdatePointValue(int pointValue)
    {
        points += pointValue;
        score.text = points.ToString();
    }

    public void UpdateMessValue(int value)
    {
        messValue += value;
        if(messValue >= messLimit)
        {
            if (!janitorActive)
                ActivateJanitor();
        }
    }

    public void UpdateInjuredCount(int value)
    {
        peopleInjured += value;
        if (peopleInjured >= injuredLimit)
        {
            if (!nurseActive)
                ActivateNurse();
        }
    }

    public void UpdateTimesVisited()
    {
        timesVisited++;
        if(timesVisited >= visitLimit)
        {
            if (!principalActive)
                ActivatePrincipal();
        }
    }

    public void GameOver()
    {
        gameOverScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        UpdatePointValue(-points);

        gameOverScreen.SetActive(false);
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
    }

    void ActivateJanitor()
    {
        GameObject newJanitor = Instantiate(janitorPF, enemySpawnLocation.position, Quaternion.identity);
        difficultyLevel++;
        janitorActive = true;
    }

    void ActivateNurse()
    {
        GameObject newNurse = Instantiate(nursePF, enemySpawnLocation.position, Quaternion.identity);
        difficultyLevel++;
        nurseActive = true;
    }

    void ActivatePrincipal()
    {
        GameObject newPrincipal = Instantiate(principalPF, enemySpawnLocation.position, Quaternion.identity);
        principalActive = true;
    }
}
