using CupPrototype.DrinkSystem;
using CupPrototype.UI;
using TMPro;
using UnityEngine;

namespace CupPrototype.Game
{
    public enum DemoRoundState
    {
        Ready,
        Mixing,
        Submitted
    }

    // 最小 Demo 回合状态机：只负责订单和状态显示，不阻止原有调饮输入。
    public class DemoRoundManager : MonoBehaviour
    {
        public TargetDrinkData currentTarget;
        public OrderDisplayController orderDisplay;
        public TextMeshProUGUI roundStatusText;

        public DemoRoundState State { get; private set; } = DemoRoundState.Ready;

        private void Start()
        {
            StartRound();
        }

        public void StartRound()
        {
            State = DemoRoundState.Mixing;
            if (orderDisplay != null)
            {
                orderDisplay.ShowTargetDrink(currentTarget);
            }

            SetStatus("Round: Mixing");
        }

        public void MarkMixing()
        {
            if (State != DemoRoundState.Submitted)
            {
                State = DemoRoundState.Mixing;
                SetStatus("Round: Mixing");
            }
        }

        public void SubmitRound()
        {
            State = DemoRoundState.Submitted;
            SetStatus("Round: Submitted\nPress R to reset");
        }

        public void ResetRound()
        {
            StartRound();
        }

        public void ShowScoreSummary(string summary)
        {
            SubmitRound();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                SetStatus($"Round: Submitted\n{summary}\nPress R to reset");
            }
        }

        private void SetStatus(string text)
        {
            if (roundStatusText != null)
            {
                roundStatusText.text = text;
            }

            Debug.Log($"[DemoRoundManager] {text}", this);
        }
    }
}
