using System;

namespace Gamelogic.Fx
{
	/// <summary>
	/// Used to flag components that need Mixbox to work. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class RequiresMixboxAttribute : Attribute
	{
	}
}
