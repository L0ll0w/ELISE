using System;
using Gamelogic.Extensions.Algorithms;
using UnityEngine;

namespace Gamelogic.Extensions.Samples
{
	/// <summary>
	/// A dictionary that maps string IDs to Colors.
	/// </summary>
	/// <remarks>
	/// This example shows how to use a <see cref="FixedKeyDictionary{TKey,TValue}"/>.
	///
	/// You need to extend FixedKeyDictionary to create a concrete (non-generic)
	/// dictionary type, as done here. To set the fixed keys, call the 
	/// </remarks>
	[Serializable]
	public class IdColorDictionary : FixedKeyDictionary<string, Color>
	{
	}
}
