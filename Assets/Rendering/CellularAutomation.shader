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
                for (int i = -1; i <= 1; i++) 
                {
                    for (int j = -1; j <= 1; j++) 
                    {
                        //accumulator += get(IN, i, j).a; // sometimes provides results similar to game of life. No?
                        int roundedAlpha = round(get(IN, i, j).a);
                        accumulator += roundedAlpha << (3 - 2*i - j);
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
