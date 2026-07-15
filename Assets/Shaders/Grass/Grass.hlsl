 // This describes a vertex on the generated mesh
struct DrawVertex
{
   float3 positionWS; // The position in world space
   float2 uv;
}; 
    // A triangle on the generated mesh
struct DrawTriangle
{
   float3 normalOS; 
   float3 diffuseColor;
   DrawVertex vertices[3]; // The three points on the triangle
};
    
// A buffer containing the generated mesh
StructuredBuffer<DrawTriangle> _DrawTriangles;
float _OrthographicCamSizeTerrain;
float3 _OrthographicCamPosTerrain;

// Global Day/Night split properties
uniform float3 _DayNightSplitDirection;
uniform float3 _DayNightWorldCenter;
uniform float _DayNightBaseOffset;
uniform float _DayNightPositionSensitivity;
uniform float _DayNightTransitionWidth;
uniform float4 _DayNightGrassNightTint;

//get the data from the compute shader
void GetComputeData_float(float vertexID, out float3 worldPos, out float3 normal, out float2 uv, out float3 col)
{
      DrawTriangle tri = _DrawTriangles[vertexID / 3];
      DrawVertex input = tri.vertices[vertexID % 3];
      worldPos = input.positionWS;
      normal =  tri.normalOS;   
      uv = input.uv;      
      col = tri.diffuseColor;

      // Apply dynamic Day/Night tint safely based on the world position of the vertex
      if (length(_DayNightSplitDirection) > 0.001)
      {
            float3 toGrass = worldPos - _DayNightWorldCenter;
            float posOffset = dot(toGrass, normalize(_DayNightSplitDirection)) * _DayNightPositionSensitivity;
            float splitVal = _DayNightBaseOffset + posOffset;
            float localTransition = smoothstep(-_DayNightTransitionWidth * 0.5, _DayNightTransitionWidth * 0.5, splitVal);
            
            // Fallback to a nice dark purple if the night tint is not set (is zero/black)
            float3 nightTint = _DayNightGrassNightTint.rgb;
            if (length(nightTint) < 0.001)
            {
                nightTint = float3(0.25, 0.15, 0.5);
            }

            float3 grassTint = lerp(nightTint, float3(1.0, 1.0, 1.0), localTransition);
            col *= grassTint;
      }
}

// world space uv for blending
void GetWorldUV_float(float3 worldPos, out float2 worldUV)
{
      float2 uv =worldPos.xz - _OrthographicCamPosTerrain.xz;
      uv = uv / (_OrthographicCamSizeTerrain * 2);
      uv += 0.5;
      worldUV = uv;
}