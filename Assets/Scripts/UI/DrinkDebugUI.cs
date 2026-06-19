using CupPrototype.DrinkSystem;
using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    // ===== 饮品调试 UI =====
    // 原型阶段用来把当前材料、容量、试味和评分结果显示在 Game 画面上。
    public class DrinkDebugUI : MonoBehaviour
    {
        // ===== Inspector 绑定文本 =====
        // 这些 TextMeshProUGUI 需要在 Canvas 中手动拖拽绑定。
        [SerializeField] private TextMeshProUGUI selectedIngredientText;
        [SerializeField] private TextMeshProUGUI volumeText;
        [SerializeField] private TextMeshProUGUI tasteFeedbackText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI transferSourceText;

        // ===== 生命周期：初始化显示 =====
        private void Awake()
        {
            ClearAll();
        }

        // ===== 当前选中材料显示 =====
        public void SetSelectedIngredient(string ingredientName)
        {
            if (selectedIngredientText == null)
            {
                Debug.LogWarning("[DrinkDebugUI] selectedIngredientText is null", this);
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(ingredientName)
                ? "None"
                : ingredientName;

            selectedIngredientText.text = $"Ingredient: {displayName}";
        }

        // ===== 当前容量显示 =====
        public void SetVolume(float currentVolume, float maxVolume)
        {
            SetVolume(string.Empty, currentVolume, maxVolume);
        }

        public void SetVolume(string containerName, float currentVolume, float maxVolume)
        {
            if (volumeText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(containerName)
                ? string.Empty
                : $"{containerName} ";

            volumeText.text = $"Volume: {displayName}{currentVolume:0.##} / {maxVolume:0.##}";
        }

        // ===== 试味反馈显示 =====
        public void SetTasteFeedback(string feedback)
        {
            if (tasteFeedbackText == null)
            {
                return;
            }

            string displayFeedback = string.IsNullOrWhiteSpace(feedback)
                ? "None"
                : feedback;

            tasteFeedbackText.text = $"Taste: {displayFeedback}";
        }

        // ===== 评分结果显示 =====
        // result 为 null 时视为清空评分显示。
        public void SetScoreResult(DrinkScoreResult result)
        {
            if (scoreText == null)
            {
                return;
            }

            if (result == null)
            {
                scoreText.text = "Score: None";
                return;
            }

            string missingIngredients = result.missingRequiredIngredients != null && result.missingRequiredIngredients.Count > 0
                ? string.Join(", ", result.missingRequiredIngredients)
                : "None";

            scoreText.text =
                $"Score: {result.totalScore:0.0}/100\n" +
                $"Flavor: {result.flavorScore:0.0}/60\n" +
                $"Volume: {result.volumeScore:0.0}/20\n" +
                $"Ingredient: {result.ingredientScore:0.0}/20\n" +
                $"Missing: {missingIngredients}\n" +
                $"Prep: {result.preparationFeedback}\n" +
                $"Feedback: {result.feedbackText}";
        }

        // ===== 转移源容器显示 =====
        public void SetTransferSource(string sourceName)
        {
            if (transferSourceText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(sourceName)
                ? "None"
                : sourceName;

            transferSourceText.text = $"Transfer Source: {displayName}";
        }

        // ===== 清空全部调试显示 =====
        public void ClearAll()
        {
            SetSelectedIngredient("None");
            SetVolume(0f, 0f);
            SetTasteFeedback("None");
            SetScoreResult(null);
            SetTransferSource("None");
        }
    }
}
