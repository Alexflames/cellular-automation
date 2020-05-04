Shader "Unlit/CellularAutomation"
{
    Properties
    {
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float _rule[512];

            float4 get(v2f_customrendertexture IN, int x, int y) : COLOR
            {
                return tex2D(_SelfTexture2D, IN.localTexcoord.xy + fixed2(x / _CustomRenderTextureWidth, y / _CustomRenderTextureHeight));
            }

            float getRule9(v2f_customrendertexture IN) : float
            {
                int accumulator = 0;
                for (int i = 2; i >= 0; i--) 
                {
                    for (int j = 0; j <= 2; j++) 
                    {
                        //accumulator += get(IN, i, j).a; // sometimes provides results similar to game of life. No?
                        int roundedAlpha = round(get(IN, i-1, j-1).a);
                        accumulator = (accumulator << 1) + roundedAlpha;
                    }
                }
                return _rule[accumulator];
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                return getRule9(IN);
            }
            ENDCG
        }
    }
}
