Shader "CupPrototype/Interaction/SelectionOutline"
{
    Properties
    {
        _OutlineTint("Outline",Color)=(0.58,0.82,0.86,1)
        _Thickness("Thickness pixels",Float)=2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SilhouetteMask"
            Cull Off ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            Varyings MaskVertex(Attributes input)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 MaskFragment(Varyings i):SV_Target { return 1; }
            ENDHLSL
        }
        Pass
        {
            Name "OutlineComposite"
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment OutlineFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _OutlineTint;
            float _Thickness;
            float Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv).r;
            }
            half4 OutlineFragment(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float center=Mask(i.texcoord);
                // 遮罩内完全透明：玻璃、液体和金属高光保持原样。
                if(center>0.5)return 0;
                float inner=0,outer=0;
                const float2 dirs[8]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1),
                    float2(.707,.707),float2(-.707,.707),float2(.707,-.707),float2(-.707,-.707)};
                float2 pixel=rcp(_ScreenParams.xy);
                [unroll]for(int j=0;j<8;j++)
                {
                    inner=max(inner,Mask(i.texcoord+dirs[j]*pixel*_Thickness));
                    outer=max(outer,Mask(i.texcoord+dirs[j]*pixel*(_Thickness+1)));
                }
                // 细暗边仅在轮廓外围多一像素，让浅色台面上也能辨认，不参与 Bloom。
                return inner>0.5?half4(_OutlineTint.rgb,.9):half4(.012,.026,.031,outer*.75);
            }
            ENDHLSL
        }
    }
}
