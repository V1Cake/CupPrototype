using System.Collections.Generic;
using CupPrototype.Interaction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CupPrototype.Rendering
{
    // 合并所选物体的可见网格遮罩，只在遮罩外缘合成细线，不替换原始材质。
    public sealed class SelectionOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader outlineShader;
        [SerializeField, Range(1, 4)] private float thicknessPixels = 2f;
        [SerializeField] private Color outlineTint = new Color(.58f, .82f, .86f, 1);
        private Material material;
        private OutlinePass pass;
        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (outlineShader) material = CoreUtils.CreateEngineMaterial(outlineShader);
            pass = new OutlinePass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
        {
            if (!material || SelectionHighlight.Active.Count == 0 || data.cameraData.cameraType != CameraType.Game) return;
            material.SetFloat("_Thickness", thicknessPixels);
            material.SetColor("_OutlineTint", outlineTint);
            pass.material = material; renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); }

        private sealed class OutlinePass : ScriptableRenderPass
        {
            internal Material material;
            private sealed class MaskData { public Material material; public List<Renderer> renderers; }
            private sealed class CompositeData { public Material material; public TextureHandle mask; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var targets = new List<Renderer>();
                foreach (var selected in SelectionHighlight.Active)
                {
                    if (!selected || selected.State == SelectionHighlight.VisualState.Idle || selected.VisualRenderers == null) continue;
                    foreach (var r in selected.VisualRenderers)
                        if (r && r.enabled && r.gameObject.activeInHierarchy && (r is MeshRenderer || r is SkinnedMeshRenderer)) targets.Add(r);
                }
                if (targets.Count == 0) return;
                var resources = frame.Get<UniversalResourceData>();
                var desc = graph.GetTextureDesc(resources.activeDepthTexture);
                desc.name = "Interaction silhouette mask";
                desc.colorFormat = GraphicsFormat.R8_UNorm; desc.depthBufferBits = DepthBits.None;
                desc.clearBuffer = true; desc.clearColor = Color.clear; desc.filterMode = FilterMode.Point;
                var mask = graph.CreateTexture(desc);
                using (var builder = graph.AddRasterRenderPass<MaskData>("Selection silhouette mask", out var d))
                {
                    d.material = material; d.renderers = targets;
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                    builder.SetRenderFunc((MaskData data, RasterGraphContext ctx) =>
                    {
                        foreach (var r in data.renderers)
                        {
                            if (!r) continue;
                            var filter = r.GetComponent<MeshFilter>();
                            var skin = r as SkinnedMeshRenderer;
                            var mesh = skin ? skin.sharedMesh : filter ? filter.sharedMesh : null;
                            if (!mesh) continue;
                            for (int sub = 0; sub < mesh.subMeshCount; sub++) ctx.cmd.DrawRenderer(r, data.material, sub, 0);
                        }
                    });
                }
                using (var builder = graph.AddRasterRenderPass<CompositeData>("Selection silhouette outline", out var d))
                {
                    d.material = material; d.mask = mask;
                    builder.UseTexture(mask, AccessFlags.Read);
                    // 原帧只通过混合单元保留，不采样/复制整帧，也不改变相机配置。
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((CompositeData data, RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd, data.mask, new Vector4(1, 1, 0, 0), data.material, 1));
                }
            }
        }
    }
}

