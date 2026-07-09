using Gamelogic.Fx.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Fx.Editor.Internal
{
	/// <summary>
	/// Property drawer for <see cref="KernelInfo"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(KernelInfo))]
	internal sealed class KernelInfoDrawer : PropertyDrawer
	{
		/// <inheritdoc/>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 3;
		}

		/// <inheritdoc/>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var startProp = FindOffset();
			var sizeProp = FindSize();
			var jumpProp = FindJumpSize();

			EditorGUI.BeginProperty(position, label, property);
			bool centered = fieldInfo.GetCustomAttributes(typeof(CenterKernelAttribute), false).Length > 0;
			var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			EditorGUI.PropertyField(rect, sizeProp);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			
			EditorGUI.PropertyField(rect, jumpProp);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			ForceValid();
			
			EditorGUI.BeginDisabledGroup(centered);
			EditorGUI.PropertyField(rect, startProp);
			EditorGUI.EndDisabledGroup();
			EditorGUI.EndProperty();

			return;

			void ForceValid()
			{
				var kernel = GetKernel();
				kernel.ForceDataValid();

				if(centered)
				{
					kernel.CenterKernel();
				}

				SetKernel(kernel);
			}
			
			KernelInfo GetKernel()
				=> new KernelInfo()
				{
					offset = FindOffset().floatValue,
					size = FindSize().intValue,
					jumpSize = FindJumpSize().floatValue
				};

			void SetKernel(KernelInfo kernel)
			{
				FindOffset().floatValue = kernel.offset;
				FindSize().intValue = kernel.size;
				FindJumpSize().floatValue = kernel.jumpSize;
			}
			
			SerializedProperty FindOffset() => property.FindPropertyRelative(KernelInfo.OffsetFieldName);
			SerializedProperty FindSize() => property.FindPropertyRelative(KernelInfo.SizeFieldName);
			SerializedProperty FindJumpSize() => property.FindPropertyRelative(KernelInfo.JumpSizeFieldName);
		}
	}
}
