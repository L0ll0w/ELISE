using Gamelogic.Extensions;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/*	Design note: Internal because probably belongs in Extensions, and needs a more thoughtful design that should be
		generalized to other vector types.
	*/
	[ReuseCandidate(MoveToWhere = nameof(Extensions))]
	internal class LockableVectorRangeAttribute : PropertyAttribute
	{
		public readonly float min;
		public readonly float max;

		public LockableVectorRangeAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}
}
