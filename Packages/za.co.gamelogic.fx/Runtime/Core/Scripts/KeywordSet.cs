using System;
using System.Collections.Generic;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Represents a set of mutually exclusive keywords of a shader. 
	/// </summary>
	[Serializable]
	public sealed class KeywordSet
	{
		/// <summary>
		/// The set of keywords. At most one is assumed to be enabled.  
		/// </summary>
		public List<Keyword> keywords = new List<Keyword>();
	}
}
