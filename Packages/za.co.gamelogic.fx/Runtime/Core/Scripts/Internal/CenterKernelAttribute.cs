using System;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// Mark a field of type <see cref="KernelInfo"/> to indicate that the offset should be calculated to center the
	/// kernel. See <see cref="KernelInfo.CenterKernel"/>.  
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class CenterKernelAttribute : Attribute
	{
	}
}
