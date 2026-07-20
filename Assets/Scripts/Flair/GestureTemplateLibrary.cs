using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 管理资源模板和 Play 期间录制的运行时模板。
    public class GestureTemplateLibrary : MonoBehaviour
    {
        // Inspector 中配置的模板资产，Awake 时会逐项校验并加载。
        public List<GestureTemplateAsset> templateAssets = new List<GestureTemplateAsset>();

        private readonly List<GestureTemplateData> templates = new List<GestureTemplateData>();

        private void Awake()
        {
            LoadTemplateAssets();
            ValidateTemplates();
        }

        // 添加录制得到的运行时模板；无效或重复模板不会进入模板库。
        public void AddTemplate(GestureTemplateData template)
        {
            if (template == null)
            {
                Debug.LogWarning("[GestureTemplateLibrary] Cannot add null runtime template.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(template.templateId))
            {
                Debug.LogWarning("[GestureTemplateLibrary] Cannot add runtime template with empty templateId.", this);
                return;
            }

            if (template.normalizedPoints == null || template.normalizedPoints.Count == 0)
            {
                Debug.LogWarning("[GestureTemplateLibrary] Cannot add runtime template with no normalized points: " + template.templateId, this);
                return;
            }

            if (ContainsTemplateId(template.templateId))
            {
                Debug.LogWarning("[GestureTemplateLibrary] Duplicate templateId detected: " + template.templateId, this);
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

        // 逐项校验 Inspector 资产和运行时模板，只报告问题，不修改原始资产。
        public void ValidateTemplates()
        {
            if (templateAssets == null || templateAssets.Count == 0)
            {
                Debug.LogWarning("[GestureTemplateLibrary] No template assets assigned on " + gameObject.name, this);
            }
            else
            {
                HashSet<string> assetIds = new HashSet<string>();
                for (int i = 0; i < templateAssets.Count; i++)
                {
                    GestureTemplateAsset asset = templateAssets[i];
                    if (asset == null)
                    {
                        Debug.LogWarning("[GestureTemplateLibrary] Skipped empty template asset on " + gameObject.name, this);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(asset.templateId))
                    {
                        Debug.LogWarning("[GestureTemplateLibrary] Template asset has empty templateId on " + gameObject.name, this);
                    }
                    else if (!assetIds.Add(asset.templateId))
                    {
                        Debug.LogWarning("[GestureTemplateLibrary] Duplicate templateId detected: " + asset.templateId, this);
                    }

                    if (asset.normalizedPoints == null || asset.normalizedPoints.Count == 0)
                    {
                        Debug.LogWarning("[GestureTemplateLibrary] Template asset has no normalized points: " + asset.name, this);
                    }
                }
            }

        }

        // 将有效资产转换为运行时数据，空槽位必须明确报告后再跳过。
        private void LoadTemplateAssets()
        {
            if (templateAssets == null)
            {
                return;
            }

            for (int i = 0; i < templateAssets.Count; i++)
            {
                GestureTemplateAsset asset = templateAssets[i];
                if (asset == null)
                {
                    Debug.LogWarning("[GestureTemplateLibrary] Skipped empty template asset on " + gameObject.name, this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(asset.templateId) ||
                    asset.normalizedPoints == null ||
                    asset.normalizedPoints.Count == 0)
                {
                    continue;
                }

                templates.Add(asset.ToRuntimeData());
                Debug.Log($"[GestureTemplateLibrary] Loaded template asset: {asset.templateId}", this);
            }
        }

        // 资产和运行时模板共用同一 ID 命名空间。
        private bool ContainsTemplateId(string templateId)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                if (templates[i] != null && templates[i].templateId == templateId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
