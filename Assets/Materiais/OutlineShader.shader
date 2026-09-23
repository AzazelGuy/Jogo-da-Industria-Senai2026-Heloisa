Shader "Custom/OutlineShader"
{
    Properties
    {
        _OutlineColor ("Cor do Contorno", Color) = (1, 0.92, 0.016, 1)
        _OutlineWidth ("Espessura", Range(0.001, 0.2)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }

        Pass
        {
            Name "OUTLINE"
            
            // Renderiza apenas as faces de trás
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Expande o vértice ao longo da sua Normal no próprio espaço local do objeto
                // Isso faz o contorno crescer JUNTO com o objeto na animação sem achatar
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * _OutlineWidth;

                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Off"
}