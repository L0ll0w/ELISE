// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// The base class for the generic InspectorList. This class exists so that 
	/// a single property drawer can be used for all subclasses.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class InspectorList
	{
		internal const string ObsoleteMessage =
			"Use ordinary lists instead, they provide similar enhancements since Unity 2020.1.";
	}

	/// <summary>
	/// Exactly the same as generic <c>List</c>, but has a custom property drawer 
	/// that draws a re-orderable list in the inspector.
	/// </summary>
	/// <typeparam name="T">The type of the contents of this list.</typeparam>
	/// <remarks>This class should not be used directly (otherwise, it will not appear in the inspector).
	/// Instead, use either one of the provided subclasses, or define a new custom non-generic subclass
	/// and use that.</remarks>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class InspectorList<T> : InspectorList, IList<T>
	{
		[Tooltip("The elements of this list.")]
		[SerializeField]
		private List<T> values;

		/// <summary>Initializes a new empty instance of <see cref="InspectorList{T}"/>.</summary>
		public InspectorList()
		{
			values = new List<T>();
		}

		/// <summary>Initializes a new instance of <see cref="InspectorList{T}"/> with the given initial values.</summary>
		/// <param name="initialValues">The values to populate the list with.</param>
		public InspectorList(IEnumerable<T> initialValues)
		{
			values = initialValues.ToList();
		}

		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator()
		{
			return values.GetEnumerator();
		}

		[Support.MustDisposeResource] // ReSharper disable once NotDisposedResourceIsReturned
		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();

		/// <inheritdoc />
		public void Add(T item)
		{
			values.Add(item);
		}

		/// <summary>Adds the elements of the specified collection to the end of this list.</summary>
		/// <param name="item">The collection to add.</param>
		public void AddRange(IEnumerable<T> item)
		{
			values.AddRange(item);
		}

		/// <inheritdoc />
		public void Clear()
		{
			values.Clear();
		}

		/// <inheritdoc />
		public bool Contains(T item)
		{
			return values.Contains(item);
		}

		/// <inheritdoc />
		public void CopyTo(T[] array, int arrayIndex)
		{
			values.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc />
		public bool Remove(T item)
		{
			return values.Remove(item);
		}

		/// <inheritdoc />
		public int Count => values.Count;

		/// <inheritdoc />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public int IndexOf(T item)
		{
			return values.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, T item)
		{
			values.Insert(index, item);
		}

		/// <inheritdoc />
		public void RemoveAt(int index)
		{
			values.RemoveAt(index);
		}

		/// <inheritdoc />
		public T this[int index]
		{
			get => values[index];
			set => values[index] = value;
		}
	}

	/// <summary>
	/// An <c>InspectorList</c> of type <c>int</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class IntList : InspectorList<int> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>float</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class FloatList : InspectorList<float> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>string</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class StringList : InspectorList<string> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>Object</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class ObjectList : InspectorList<UnityEngine.Object> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>MonoBehaviour</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class MonoBehaviourList : InspectorList<MonoBehaviour> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>Color</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class ColorList : InspectorList<Color>
	{
		/// <summary>Initializes a new instance of <see cref="ColorList"/> with the default colors.</summary>
		public ColorList() : base(Utils.DefaultColors)
		{ }

		/// <summary>Initializes a new instance of <see cref="ColorList"/> with the specified default colors.</summary>
		/// <param name="defaultColors">The colors to populate the list with.</param>
		public ColorList(IEnumerable<Color> defaultColors) : base(defaultColors)
		{ }
	}

	/// <summary>
	/// An <c>InspectorList</c> of type <c>Vector2</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class Vector2List : InspectorList<Vector2> { }

	/// <summary>
	/// An <c>InspectorList</c> of type <c>Vector3</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class Vector3List : InspectorList<Vector3> { }


	/// <summary>
	/// An <c>InspectorList</c> of type <c>Vector4</c>.
	/// </summary>
#if UNITY_2020_1_OR_NEWER
	[Obsolete(ObsoleteMessage)]
#endif
	[Version(2, 5, 0)]
	[Serializable]
	public class Vector4List : InspectorList<Vector4> { }
}
