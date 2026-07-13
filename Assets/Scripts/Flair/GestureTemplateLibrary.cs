using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // MVP 阶段的运行时模板库；退出 Play 后模板会丢失，后续可升级为 ScriptableObject 保存。
    public class GestureTemplateLibrary : MonoBehaviour
    {
        // 可保存到项目中的模板资产；Awake 时会加载到运行时模板库。
        public List<GestureTemplateAsset> templateAssets = new List<GestureTemplateAsset>();

        private readonly List<GestureTemplateData> templates = new List<GestureTemplateData>();

        private void Awake()
        {
            LoadTemplateAssets();
        }

        public void AddTemplate(GestureTemplateData template)
        {
            if (template == null || template.normalizedPoints == null || template.normalizedPoints.Count == 0)
            {
                Debug.LogWarning("[GestureTemplateLibrary] Cannot add empty template.", this);
                return;
            }

            templates.Add(template);
            Debug.Log($"[GestureTemplateLibrary] Added template: {template.templateId}, Type={template.gestureType}", this);
        }

        public IReadOnlyList<GestureTemplateData> GetTemplates()
        {
            return templates;
        }

        public void ClearTemplates()
        {
            templates.Clear();
        }

        private void LoadTemplateAssets()
        {
            for (int i = 0; i < templateAssets.Count; i++)
            {
                GestureTemplateAsset asset = templateAssets[i];
                if (asset == null || asset.normalizedPoints == null || asset.normalizedPoints.Count == 0)
                {
                    Debug.LogWarning("[GestureTemplateLibrary] Cannot load empty template asset.", this);
                    continue;
                }

                templates.Add(asset.ToRuntimeData());
                Debug.Log($"[GestureTemplateLibrary] Loaded template asset: {asset.templateId}", this);
            }
        }
    }
}
