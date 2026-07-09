using System;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// Represents the parameters of a sampling kernel used by image-processing effects.
	/// A kernel defines how many samples are taken, how far apart they are, 
	/// and where sampling begins relative to the current pixel.
	/// </summary>
	/* Design note: This class refers to specific property names, and is therefore not suitable for public.
	*/
	[Serializable]
	internal struct KernelInfo
	{
		/* These internal field names are meant for property drawers.
		*/
		/// <summary>
		/// The serialized field name of <see cref="offset"/>.
		/// </summary>
		internal static string OffsetFieldName = nameof(offset);

		/// <summary>
		/// The serialized field name of <see cref="size"/>.
		/// </summary>
		internal static string SizeFieldName = nameof(size);

		/// <summary>
		/// The serialized field name of <see cref="jumpSize"/>.
		/// </summary>
		internal static string JumpSizeFieldName = nameof(jumpSize);
		
		/// <summary>
		/// The shader property ID for the kernel size.
		/// </summary>
		public static int SizeID = Shader.PropertyToID("_KernelSize");

		/// <summary>
		/// The shader property ID for the kernel offset.
		/// </summary>
		public static int OffsetID = Shader.PropertyToID("_KernelOffset");

		/// <summary>
		/// The shader property ID for the kernel jump size.
		/// </summary>
		public static int JumpSizeID = Shader.PropertyToID("_KernelJumpSize");
		
		/// <summary>
		/// The starting offset of the kernel relative to the current sample position.
		/// Controls where the first sample is taken.
		/// </summary>
		public float offset;

		/// <summary>
		/// The number of samples in the kernel. Must be at least 1.
		/// </summary>
		public int size;

		/// <summary>
		/// The spacing between consecutive samples.
		/// A value of 1 samples adjacent texels; larger values skip texels.
		/// </summary>
		public float jumpSize;
		
		/// <summary>
		/// Ensures that the kernel parameters contain valid values.
		/// <see cref="size"/> is clamped to at least 1, and <see cref="jumpSize"/> to at least 1.
		/// </summary>
		public void ForceDataValid()
		{
			size = size < 1 ? 1 : size;
			jumpSize = jumpSize < 1 ? 1 : jumpSize;
		}

		/// <summary>
		/// Sets <see cref="offset"/> so that the kernel is centered around the current pixel.
		/// The centered offset is computed as <c>-(size - 1) / 2</c>.
		/// </summary>
		public void CenterKernel()
		{
			offset = -((size - 1) / 2);
		}
	}
}
