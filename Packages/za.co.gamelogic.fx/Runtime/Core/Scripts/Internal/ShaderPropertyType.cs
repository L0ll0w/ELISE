using System;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// Properties that can be set on a shader. 
	/// </summary>
	[Serializable]
	internal sealed class ShaderPropertyType
	{
		public const string Bool = nameof(Bool);
		public const string Int = nameof(Int);
		public const string Integer = nameof(Integer);
		public const string Float = nameof(Float);
		
		public const string Color = nameof(Color);
		public const string Vector = nameof(Vector);
		
		public const string IntegerArray = nameof(IntegerArray);
		public const string FloatArray = nameof(FloatArray);
		
		public const string MatrixArray = nameof(MatrixArray);
		public const string Texture = nameof(Texture);
		public const string FloatArrayCurve = nameof(FloatArrayCurve);
		public const string Keyword = nameof(Keyword);
		public const string KeywordSet = nameof(KeywordSet);
		public const string ScreenTextureSize = nameof(ScreenTextureSize);
		public const string ScreenAspectRatio = nameof(ScreenAspectRatio);

	}
}
