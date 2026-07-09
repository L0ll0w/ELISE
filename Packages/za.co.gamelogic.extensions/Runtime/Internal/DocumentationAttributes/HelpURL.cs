// Copyright Gamelogic (c) http://www.gamelogic.co.za

#if !(UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER)
using System;

namespace Gamelogic.Extensions.Internal
{
	/// <summary>
	/// A dummy attribute meant to mimic Unity 5's HelpURLAttribute so that scripts for Unity 5 can also compile
	/// under Unity 4.
	/// </summary>
	[Version(2, 4, 0)]
	[AttributeUsage(AttributeTargets.Class)]
	public class HelpURLAttribute : Attribute
	{
		/// <summary>Initializes a new instance of <see cref="HelpURLAttribute"/>.</summary>
		/// <param name="help">The help URL (unused; provided for API compatibility).</param>
		public HelpURLAttribute(string help)
		{
			//Do nothing
		}
	}
}
#endif
