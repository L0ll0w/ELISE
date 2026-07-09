using System;
using Gamelogic.Extensions;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/*	Design note: Internal because probably belongs in Extensions, and needs a more thoughtful design that should be
		generalized to other vector types.
	*/
	[ReuseCandidate(MoveToWhere = nameof(Extensions))]
	[Serializable]
	internal struct LockableVector3
	{
		public Vector3 vector;
		public bool locked;
	}
	
	/*	Design note: Internal because probably belongs in Extensions, and needs a more thoughtful design that should be
		generalized to other vector types.
	*/
	[ReuseCandidate(MoveToWhere = nameof(Extensions))]
	[Serializable]
	internal struct LockableVector2
	{
		public Vector2 vector;
		public bool locked;
	}
}
