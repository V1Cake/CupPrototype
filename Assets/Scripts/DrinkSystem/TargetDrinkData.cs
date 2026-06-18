using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 目标饮品资产 =====
    // 用于定义评分目标：容量、风味和必须出现的关键材料。
    [CreateAssetMenu(fileName = "NewTargetDrink", menuName = "Cup Prototype/Target Drink Data")]
    public class TargetDrinkData : ScriptableObject
    {
        // ===== 目标基础信息 =====
        public string drinkName;
        public float targetVolume = 100f;
        public float volumeTolerance = 10f;

        // ===== 目标风味 =====
        public FlavorProfile targetFlavor;
        public float flavorTolerance = 2f;

        // ===== 关键材料要求 =====
        // 第一版只检查是否包含，不检查比例。
        public List<IngredientData> requiredIngredients = new List<IngredientData>();
    }
}
