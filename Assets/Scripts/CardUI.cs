//Handles Card effect logic

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button button;

    private CardType cardType;
    private BattleControl battleControl;
    private CardPanelManager cardManager;
    private QuizManager quizManager;

    public void Setup(CardType type, BattleControl battle, CardPanelManager manager, QuizManager quiz)
    {
        cardType = type;
        battleControl = battle;
        quizManager = quiz;

        nameText.text = type.cardName;
        costText.text = type.cost.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("Card clicked -> sending to BattleControl");

        if (battleControl.currentstate != BattleControl.BattleState.PlayerTurn)
        {
            Debug.Log("Not your turn");
            return;
        }

        Debug.Log("Played Card: " + cardType.cardName);
        Debug.Log("Target Type: " + cardType.targetType);

        if (cardType.quizCard)
        {
            QuizQuestion q = quizManager.GetRandomQuestions();
            quizManager.StartQuiz(q, cardType.damage, battleControl);

            Destroy(gameObject);
            return;
        }

            battleControl.HandleCardPlay(cardType, this);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
