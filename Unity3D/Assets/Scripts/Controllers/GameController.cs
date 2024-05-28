// X&O Duel - Game Script
// Version 0.1.1a
// Happy Dayz Games
// 27/05/2024
// https://github.com/Kearinl/

using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Text currentPlayerText;
    public Text winText;
    public Text playerXRoundText;
    public Text playerORoundText;
    public Text levelText;

    public Sprite xSprite;
    public Sprite oSprite;
    public Sprite defaultSprite;

    public Button restartButton;
    public Button exitButton;
    
    // Add this variable
public Button muteButton;

private bool isMuted = false;
	
	public AudioClip buttonClickSound; // Add this variable

    private AudioSource audioSource; // Add this variable

    private Button[,] grids;
    private string[,] gridValues;
    private string currentPlayer;
    private int playerXRound;
    private int playerORound;
    private int currentLevel = 1;
	private int lastLevel = 9;
    private int maxLevels = 10;
    private int maxDepth;

    private int roundsToWin = 2; // Best of 3

    void Start()
    {
        InitializeGame();
		
		// Initialize the AudioSource component
        audioSource = GetComponent<AudioSource>();
    }

    void InitializeGame()
{
    currentPlayer = (Random.Range(0, 2) == 0) ? "X" : "O"; // Randomly choose starting player
    currentPlayerText.text = "Current Player: " + currentPlayer;

    winText.text = "";
    playerXRoundText.text = "Player X Rounds: " + playerXRound;
    playerORoundText.text = "Player O Rounds: " + playerORound;
    levelText.text = "Level: " + currentLevel;

    // Set up event listener for the restart button
    restartButton.onClick.AddListener(RestartGame);
    
     // Set up event listener for the exit button
    exitButton.onClick.AddListener(ExitGame);
    
    // Set up event listener for the mute button
    muteButton.onClick.AddListener(ToggleMute);

    grids = new Button[3, 3];
    gridValues = new string[3, 3];

    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            grids[i, j] = GameObject.Find("Row" + i + "/Grid" + j).GetComponent<Button>();
            int row = i;
            int col = j;
            grids[i, j].onClick.AddListener(() => GridClick(row, col));
        }
    }

    // Initialize the AudioSource component
    audioSource = GetComponent<AudioSource>();

    // Call AIMove only if Player O starts
    if (currentPlayer == "O")
    {
        AIMove();
    }

    HideRestartButton();
}

public void ExitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}

public void ToggleMute()
{
    isMuted = !isMuted;

    // Mute or unmute all audio sources in the scene
    AudioListener.volume = isMuted ? 0 : 1;

    // Optionally, update UI or perform other actions based on mute state
    // For example, change the color or visibility of the image
    // muteButton.GetComponent<Image>().color = isMuted ? Color.red : Color.green;

    // Unpause the game by setting Time.timeScale back to 1
    Time.timeScale = isMuted ? 0 : 1;
}

    void GridClick(int row, int col)
    {
        if (gridValues[row, col] == null)
        {
            gridValues[row, col] = currentPlayer;
            grids[row, col].interactable = false;

            Image buttonImage = grids[row, col].GetComponent<Image>();
            Sprite symbolSprite = (currentPlayer == "X") ? xSprite : oSprite;
            buttonImage.sprite = symbolSprite;

            if (CheckForWin(row, col))
            {
                if (currentPlayer == "X")
                    playerXRound++;
                else
                    playerORound++;

                playerXRoundText.text = "Player X Rounds: " + playerXRound;
                playerORoundText.text = "Player O Rounds: " + playerORound;
				
				if (playerXRound == roundsToWin && currentLevel == maxLevels)
                {
					winText.text = "Player " + currentPlayer + " wins the game!";
                    DisableGrid();
                    ShowRestartButton();
                }
			    else if (playerXRound == roundsToWin && currentLevel <= lastLevel)
                {
                currentLevel++;
                winText.text = "Player X wins the best out of 3! Progressing to Level " + currentLevel;
                Invoke("RestartRound", 2f);
				playerXRound = 0;
                playerORound = 0;
                }
				
                if (playerORound == roundsToWin)
                {
                    winText.text = "Player " + currentPlayer + " wins the game!";
                    DisableGrid();
                    ShowRestartButton();
                }
                else if (playerORound <= roundsToWin)
                {
                    winText.text = "Player " + currentPlayer + " wins the round!";
                    Invoke("RestartRound", 2f);
                }
            }
            else if (CheckForDraw())
            {
                winText.text = "It's a draw!";
                Invoke("RestartRound", 2f);
            }
            else
            {
                currentPlayer = (currentPlayer == "X") ? "O" : "X";
                currentPlayerText.text = "Current Player: " + currentPlayer;

                if (currentPlayer == "O")
                    AIMove();
            }
			
			 // Play the button click sound
            if (buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }
    }

    void AIMove()
    {
        maxDepth = Mathf.Clamp(currentLevel, 1, maxLevels);

        int[] bestMove = Minimax(gridValues, "O", maxDepth);

        GridClick(bestMove[0], bestMove[1]);
    }

    int[] Minimax(string[,] board, string player, int depth)
    {
        int[] bestMove = new int[] { -1, -1 };
        int bestScore = (player == "O") ? int.MinValue : int.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == null)
                {
                    board[i, j] = player;
                    int score = MinimaxScore(board, 0, false, depth);
                    board[i, j] = null;

                    if ((player == "O" && score > bestScore) || (player == "X" && score < bestScore))
                    {
                        bestScore = score;
                        bestMove[0] = i;
                        bestMove[1] = j;
                    }
                }
            }
        }

        return bestMove;
    }

    int MinimaxScore(string[,] board, int depth, bool isMaximizing, int maxDepth)
{
    if (CheckForWin(0, 0) || CheckForDraw())
    {
        return EvaluateBoard(board);
    }

    if (isMaximizing)
    {
        int maxScore = int.MinValue;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == null)
                {
                    board[i, j] = "O";
                    int score = MinimaxScore(board, depth + 1, false, maxDepth);
                    board[i, j] = null;
                    maxScore = Mathf.Max(maxScore, score);
                }
            }
        }

        return maxScore;
    }
    else
    {
        int minScore = int.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == null)
                {
                    board[i, j] = "X";
                    int score = MinimaxScore(board, depth + 1, true, maxDepth);
                    board[i, j] = null;
                    minScore = Mathf.Min(minScore, score);
                }
            }
        }

        return minScore;
    }
}

    int EvaluateBoard(string[,] board)
{
    int score = 0;

    for (int i = 0; i < 3; i++)
    {
        // Check rows for winning or blocking moves
        score += EvaluateLine(board[i, 0], board[i, 1], board[i, 2]);

        // Check columns for winning or blocking moves
        score += EvaluateLine(board[0, i], board[1, i], board[2, i]);
    }

    // Check diagonals for winning or blocking moves
    score += EvaluateLine(board[0, 0], board[1, 1], board[2, 2]);
    score += EvaluateLine(board[0, 2], board[1, 1], board[2, 0]);

    return score;
}

int EvaluateLine(string cell1, string cell2, string cell3)
{
    int score = 0;

    // Offensive move: AI trying to win
    if (cell1 == "O" && cell2 == "O" && cell3 == "O")
        score += 10;

    // Defensive move: Blocking the opponent
    else if (cell1 == "X" && cell2 == "X" && cell3 == null)
        score += 1;
    else if (cell1 == "X" && cell2 == null && cell3 == "X")
        score += 1;
    else if (cell1 == null && cell2 == "X" && cell3 == "X")
        score += 1;

    return score;
}

    void RestartRound()
{
    if (currentLevel < maxLevels || (currentLevel == maxLevels && playerXRound < roundsToWin))
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Image buttonImage = grids[i, j].GetComponent<Image>();
                buttonImage.sprite = defaultSprite;
                grids[i, j].interactable = true;
            }
        }

        InitializeGame();
    }
    else
    {
        // Do something to handle the end of the game at level 10
        // You can display a message, show scores, or take any appropriate action.
        // For example:
        winText.text = "Game Over! Player " + currentPlayer + " wins the series!";
        DisableGrid();
        ShowRestartButton();
    }
}

    void DisableGrid()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                grids[i, j].interactable = false;
            }
        }
    }

    void ShowRestartButton()
    {
        restartButton.gameObject.SetActive(true);
    }

    void HideRestartButton()
    {
        restartButton.gameObject.SetActive(false);
    }

  public void RestartGame()
    {
		currentLevel = 1;
		playerXRound = 0;
		playerORound = 0;
        HideRestartButton();
        RestartRound();
        InitializeGame();
    }

    bool CheckForWin(int row, int col)
    {
        if (gridValues[row, 0] == currentPlayer && gridValues[row, 1] == currentPlayer && gridValues[row, 2] == currentPlayer)
            return true;

        if (gridValues[0, col] == currentPlayer && gridValues[1, col] == currentPlayer && gridValues[2, col] == currentPlayer)
            return true;

        if ((row == col || row + col == 2) &&
            ((gridValues[0, 0] == currentPlayer && gridValues[1, 1] == currentPlayer && gridValues[2, 2] == currentPlayer) ||
             (gridValues[0, 2] == currentPlayer && gridValues[1, 1] == currentPlayer && gridValues[2, 0] == currentPlayer)))
            return true;

        return false;
    }

    bool CheckForDraw()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (gridValues[i, j] == null)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
