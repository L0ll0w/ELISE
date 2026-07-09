using System;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.PostProcessing
{
	/// <summary>
	/// Base class for shader property sets used by separable post-processing effects.
	/// </summary>
	/// <remarks>
	/// A separable effect is one that can be evaluated in two one-dimensional passes
	/// (typically horizontal and vertical) instead of a full two-dimensional kernel.
	/// This significantly reduces the computational cost for larger kernels.
	/// <para/>
	/// <see cref="SeparableShaderProperties"/> extends <see cref="ShaderProperties"/> by
	/// providing a shared <see cref="KernelInfo"/> definition that controls how the
	/// one-dimensional kernel is sampled.
	/// </remarks>
	[Serializable]
	public abstract class SeparableShaderProperties : ShaderProperties
	{
		/// <summary>
		/// Kernel configuration used for separable sampling.
		/// </summary>
		/// <remarks>
		/// The kernel defines the number of samples, their relative offset, and the step size
		/// between samples. These values are typically interpreted by the corresponding
		/// separable post-process renderer to perform horizontal and vertical passes.
		/// <para/>
		/// The <see cref="CenterKernelAttribute"/> ensures that the kernel is centered
		/// appropriately when edited in the inspector.
		/// </remarks>
		[CenterKernel]
		[SerializeField] private KernelInfo kernel 
			= new KernelInfo
			{
				offset = -1,
				size = 3,
				jumpSize = 1
			};

		/// <summary>
		/// Gets the kernel configuration used by this separable shader.
		/// </summary>
		/// <remarks>
		/// This property is internal because kernel application is handled by the
		/// separable post-process infrastructure rather than user code.
		/// </remarks>
		/* TODO: We should expose this.
			Options:
				1. Make KernelInfo public.
				2. Expose individual properties for offset, size, jump size.
				3. Expose through a data-only type.  
		*/
		internal KernelInfo Kernel => kernel; 
	}
}
