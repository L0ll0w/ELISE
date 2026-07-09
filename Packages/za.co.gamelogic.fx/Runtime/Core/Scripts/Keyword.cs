using System;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Represents a shader keyword, and whether it is present or not. 
	/// </summary>
	[Serializable]
	public sealed class Keyword
	{
		/// <summary>
		/// The keyword. 
		/// </summary>
		public string name;
		
		/// <summary>
		/// Whether the keyword is enabled in the shader or not.
		/// </summary>
		public bool enabled;
	}
}
