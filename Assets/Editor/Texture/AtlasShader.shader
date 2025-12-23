Shader "Custom/AtlasShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _UVOffset ("UV Offset", Vector) = (0,0,0,0)
        _UVScale ("UV Scale", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc" // 조명 관련 내장 변수를 사용하기 위해 포함

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // 노멀 데이터 추가
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1; // 월드 공간 노멀 추가
            };

            float4 _Color;
            sampler2D _MainTex;
            float2 _UVOffset;
            float2 _UVScale;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // 기본 UV(보통 0~1)를 _UVScale, _UVOffset으로 조정
                o.uv = v.uv * _UVScale + _UVOffset;
                // 월드 공간으로 노멀 변환
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 월드 공간 노멀을 정규화
                fixed3 normal = normalize(i.worldNormal);
                // 주 조명 방향 (Directional Light의 경우)
                fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // 빛과 노멀의 내적 계산 (Lambertian diffuse)
                fixed diffuse = max(0, dot(normal, lightDir));
                // 텍스처 색상에 확산(diffuse) 색상 곱하기
                fixed4 finalColor = texColor * _Color * diffuse;

                // 엠비언트(ambient) 조명 추가
                fixed4 ambientColor = UNITY_LIGHTMODEL_AMBIENT;
                finalColor.rgb += ambientColor.rgb * texColor.rgb;

                return finalColor;
            }
            ENDCG
        }
    }
}
