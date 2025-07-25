using UnityEngine;


public class UserData : MonoBehaviour
{
    [SerializeField]
    private string userName;
    [SerializeField]
    private int userId;
    [SerializeField]
    private int score;
    [SerializeField]
    private Color32 playerColor = Color.white;
    [SerializeField]
    private PlayerIcon[] playerIcons;
    [SerializeField]
    private int correctedAnswerNumber;
    [SerializeField]
    private float correctAnswerPercentage;
    [SerializeField]
    private int retry = 0;
    [SerializeField]
    private int numberOfRetry = 3;
    private float answerTime;

    public string UserName
    {
        get { return this.userName; }
        set { this.userName = value; }
    }

    public int UserId
    {
        get { return this.userId; }
        set { this.userId = value; }
    }

    public int RealUserId
    {
        get { return this.userId + 1; }
    }

    public int Score
    {
        get { return this.score; }
        set { this.score = value; }
    }

    public Color32 PlayerColor
    {
        get { return this.playerColor; }
        set { this.playerColor = value; }
    }

    public PlayerIcon[] PlayerIcons
    {
        get { return this.playerIcons; }
        set { this.playerIcons = value; }
    }

    public int CorrectedAnswerNumber
    {
        get { return this.correctedAnswerNumber; }
        set { this.correctedAnswerNumber = value; }
    }

    public float AnsweredPercentage(int totalQuestions = 0)
    {
        if (totalQuestions == 0) return 0;
        this.correctAnswerPercentage = ((float)this.CorrectedAnswerNumber / totalQuestions) * 100f;
        return this.correctAnswerPercentage;
    }

    public float AnswerTime
    {
        get { return this.answerTime; }
        set { this.answerTime = value; }
    }

    public int Retry
    {
        get { return this.retry; }
        set { this.retry = value; }
    }

    public int NumberOfRetry
    {
        get { return this.numberOfRetry; }
        set { this.numberOfRetry = value; }
    }
}
