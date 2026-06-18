using System.Collections.Generic;

namespace CupPrototype.DrinkSystem
{
    // ===== 饮品评分结果 =====
    // DrinkScoreSystem 计算后把各项分数和文字反馈集中放在这里。
    public class DrinkScoreResult
    {
        // ===== 分数拆分 =====
        public float totalScore;
        public float flavorScore;
        public float volumeScore;
        public float ingredientScore;

        // ===== 缺失材料和反馈 =====
        public List<string> missingRequiredIngredients = new List<string>();
        public string feedbackText;
    }
}
