using System.Linq;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.BuiltIn.PostProcessing
{
	/// <summary>
	/// Runs all post-processes found in children of a given root on the <see cref="Camera"/> this script is
	/// attached to, applying a procedurally computed mask (<see cref="MaskShape.HalfPlane"/> or
	/// <see cref="MaskShape.Ellipse"/>) instead of a texture mask.
	/// </summary>
	/// <remarks>
	/// All spatial parameters (<c>center</c>, <c>radii</c>, <c>softness</c>) are expressed
	/// in aspect-corrected normalised screen-height units, so a radius of 0.5 always reaches half the screen height
	/// regardless of the display's aspect ratio.
	/// <para/>
	/// For the texture-masked equivalent, see <see cref="MaskedPostProcessRunner"/>.
	/// </remarks>
	/// <inheritdoc cref="PostProcessRunner"/>
	[ExecuteInEditMode]
	public sealed class ProceduralMaskedPostProcessRunner : GLMonoBehaviour
	{
		/// <summary>Determines which procedural shape is used to compute the mask.</summary>
		public enum MaskShape
		{
			/// <summary>A half-plane: everything on one side of a line through <c>center</c> is masked.</summary>
			HalfPlane = 0,

			/// <summary>An ellipse centred at <c>center</c> with the given <c>radii</c>.</summary>
			Ellipse = 1,
		}

		private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
		private static readonly int OverlayTexID = Shader.PropertyToID("_OverlayTex");
		private static readonly int MaskTypeID = Shader.PropertyToID("_MaskType");
		private static readonly int CenterID = Shader.PropertyToID("_Center");
		private static readonly int AngleID = Shader.PropertyToID("_Angle");
		private static readonly int SoftnessID = Shader.PropertyToID("_Softness");
		private static readonly int InvertID = Shader.PropertyToID("_Invert");
		private static readonly int RadiiID = Shader.PropertyToID("_Radii");

		[ReadOnly]
		[SerializeField] private Shader maskShader;

		[Tooltip("The root transform whose children are scanned for enabled post-process components.")]
		[ValidateNotNull]
		[SerializeField] private Transform postProcessRoot = null;

		[Tooltip("Overall strength of the post-process effect. 0 = no effect, 1 = full effect.")]
		[Range(0f, 1f)]
		[SerializeField] private float effectStrength = 1.0f;

		[Tooltip("Which procedural shape to use as the mask.")]
		[SerializeField] private MaskShape maskShape = MaskShape.HalfPlane;

		[Tooltip("Centre of the mask shape in normalised screen coordinates (0,0 = bottom-left, 1,1 = top-right).")]
		[SerializeField] private Vector2 center = new Vector2(0.5f, 0.5f);

		[Tooltip("Rotation of the mask shape in degrees.")]
		[SerializeField] private float angle = 0f;

		[Tooltip("Width of the soft edge transition in aspect-corrected normalised screen-height units.")]
		[SerializeField] private float softness = 0.02f;

		[Tooltip("When enabled, the masked and unmasked regions are swapped.")]
		[SerializeField] private bool invert = false;

		[Tooltip("Semi-axes of the ellipse mask in aspect-corrected normalised screen-height units. Only used when Mask Shape is Ellipse.")]
		[SerializeField] private Vector2 radii = new Vector2(0.3f, 0.2f);

		private Material maskMaterial;

		private Material MaskMaterial
		{
			get
			{
				if (maskMaterial != null)
				{
					return maskMaterial;
				}

				if (maskShader == null)
				{
					Debug.LogError("No procedural mask shader set for " + name);
					return null;
				}

				maskMaterial = new Material(maskShader)
				{
					hideFlags = HideFlags.DontSave
				};

				return maskMaterial;
			}
		}

#if UNITY_EDITOR
		// Only for debugging
		// Not serialized in older versions of Unity.
		// ReSharper disable once Unity.RedundantSerializeFieldAttribute
		[SerializeField, ReadOnly]
#endif
		private IPostProcess[] postProcesses;

		public void OnRenderImage(RenderTexture sourceTexture, RenderTexture destinationTexture)
		{
			if (postProcessRoot == null || MaskMaterial == null)
			{
				Graphics.Blit(sourceTexture, destinationTexture);
				return;
			}

			postProcesses = postProcessRoot
				.GetComponentsInChildren<MonoBehaviour>(includeInactive: false)
				.Where(component => component.enabled)
				.Where(component => component is IPostProcess)
				.Cast<IPostProcess>()
				.ToArray();

			if (!postProcesses.Any())
			{
				Graphics.Blit(sourceTexture, destinationTexture);
				return;
			}

			var currentSource = sourceTexture;
			var descriptor = sourceTexture.descriptor;

			foreach (var postProcess in postProcesses)
			{
				var temporaryTexture = RenderTexture.GetTemporary(descriptor);
				postProcess.OnRenderImage(currentSource, temporaryTexture);

				if (currentSource != sourceTexture)
				{
					RenderTexture.ReleaseTemporary(currentSource);
				}

				currentSource = temporaryTexture;
			}

			SetMaskMaterialProperties(currentSource);
			Graphics.Blit(sourceTexture, destinationTexture, MaskMaterial);

			if (currentSource != sourceTexture)
			{
				RenderTexture.ReleaseTemporary(currentSource);
			}
		}

		public void OnEnable()
		{
			maskShader = ShaderNames.GetShader(Constants.ShaderNameRoot + ShaderNames.ProceduralMask);
		}

		public void OnDisable()
		{
			if (maskMaterial != null)
			{
				DestroyImmediate(maskMaterial);
			}
		}

		private void SetMaskMaterialProperties(RenderTexture overlay)
		{
			MaskMaterial.SetTexture(OverlayTexID, overlay);
			MaskMaterial.SetFloat(OpacityID, effectStrength);
			MaskMaterial.SetInt(MaskTypeID, (int)maskShape);
			MaskMaterial.SetVector(CenterID, center);
			MaskMaterial.SetFloat(AngleID, angle);
			MaskMaterial.SetFloat(SoftnessID, softness);
			MaskMaterial.SetFloat(InvertID, invert ? 1f : 0f);
			MaskMaterial.SetVector(RadiiID, radii);
		}
	}
}
