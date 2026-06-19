using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 目标饮品资产 =====
    // 用于定义评分目标：容量、风味和必须出现的关键材料。
    [CreateAssetMenu(fileName = "NewTargetDrink", menuName = "Cup Prototype/Target Drink Data")]
    public class TargetDrinkData : ScriptableObject
    {
        // ===== 制作方式要求 =====
        // None：无要求；Built：直接在最终杯中调制；Shaken：需要经过 Shaker 并达到 Mixed；
        // Stirred：预留给未来 MixingGlass 搅拌杯系统。
        public enum PreparationMethod
        {
            None,
            Built,
            Shaken,
            Stirred
        }

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

        // ===== 目标制作方式 =====
        // 在 Inspector 中选择目标饮品期望的制作方式；当前阶段只用于提示，不参与评分。
        public PreparationMethod requiredPreparation = PreparationMethod.None;
    }
}
