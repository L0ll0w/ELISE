using System.Collections;
using System.Linq;
using Gamelogic.Extensions.Algorithms;
using Gamelogic.Extensions.Internal;
using UnityEngine;
using UnityEngine.UI;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Component for displaying the median frame time, maximum frame time, and frame rate.
	/// </summary>
	/// <remarks>
	/// This is used in some examples, and is not intended for production use.
	/// </remarks>
	[Experimental]
	[Version(4, 0, 0)]
	public class FpsCounter : MonoBehaviour
	{
		private static class ToolTip
		{
			internal const string BufferLength = "How many samples to use to calculate the median frame time.";
			internal const string TextUpdateInterval = "How often to update the text in seconds.";
			internal const string MedianFrameTimeLabel = "The label for the median frame time.";
			internal const string MaxFrameTimeLabel = "The label for the maximum frame time.";
			internal const string FpsLabel = "The label for the frames per second.";
			internal const string MedianFrameTimeText = "The text for the median frame time.";
			internal const string MaxFrameTimeText = "The text for the maximum frame time.";
			internal const string FpsText = "The text for the frames per second.";
		}
		
		private const string MedianFrameTimeLabelText = "MED (MS)";
		private const string MaxFrameTimeLabelText = "MAX (MS)";
		private const string FpsLabelText = "FPS";

		private const int DefaultBufferLength = 60;
		
		private WaitForSeconds wait;
		private IBuffer<float> frameTimes;
		private float[] frameTimesCopy;  
		
		[Header("Operation")]
		[Tooltip(ToolTip.BufferLength)]
		[ValidatePositive]
		[SerializeField] private int bufferLength = DefaultBufferLength;
		
		[Tooltip(ToolTip.TextUpdateInterval)]
		[Min(0.1f)]
		[SerializeField] private float textUpdateInterval = 1.0f;

		[Header("UI")]
		[Tooltip(ToolTip.MedianFrameTimeLabel), ValidateNotNull]
		[SerializeField] private Text medianFrameTimeLabel = null;
		
		[Tooltip(ToolTip.MaxFrameTimeLabel), ValidateNotNull]
		[SerializeField] private Text maxFrameTimeLabel = null;
		
		[Tooltip(ToolTip.FpsLabel), ValidateNotNull]
		[SerializeField] private Text fpsLabel = null;
		
		[Tooltip(ToolTip.MedianFrameTimeText), ValidateNotNull]
		[SerializeField] private Text medianFrameTimeText = null;
		
		[Tooltip(ToolTip.MaxFrameTimeText), ValidateNotNull]
		[SerializeField] private Text maxFrameTimeText = null;
		
		[Tooltip(ToolTip.FpsText), ValidateNotNull]
		[SerializeField] private Text fpsText = null;

		private void Start()
		{
			medianFrameTimeLabel.text = MedianFrameTimeLabelText;
			maxFrameTimeLabel.text = MaxFrameTimeLabelText;
			fpsLabel.text = FpsLabelText;
			
			InitializeFpsCounter();
		}
		private void InitializeFpsCounter()
		{
			frameTimes = new RingBuffer<float>(bufferLength);
			wait = new WaitForSeconds(textUpdateInterval);
			frameTimesCopy = new float[bufferLength];
			StartCoroutine(UpdateText());
		}

		/// <summary>Records the current frame time.</summary>
		public void Update()
		{
			frameTimes.Insert(Time.deltaTime);
		}

		private IEnumerator UpdateText()
		{
			while (true)
			{
				UpdateCopy();

				float median = 1000 * frameTimesCopy.MedianPartition();
				float max = 1000 * frameTimesCopy.Max();
				float fps = 1000 / median;

				medianFrameTimeText.text = $"{median:0.0}";
				maxFrameTimeText.text = $"{max:0.0}";
				fpsText.text = $"{fps:0.00}";

				yield return wait; 
			}
			// ReSharper disable once IteratorNeverReturns
		}
		
		private void UpdateCopy()
		{
			foreach ((float item, int index) in frameTimes.WithIndices())
			{
				frameTimesCopy[index] = item;
			}
		}
	}
}
