﻿using System;

namespace BlocksWorld
{
	public sealed class FlipOverBehaviour : Behaviour
	{
		private Interaction interaction;
		private Slot flip;

		public FlipOverBehaviour()
		{
			this.flip = this.CreateSlot("flip", this.Obj_InterationTriggered);

			this.Attached += FlipOverBehaviour_Attached;
			this.Detached += FlipOverBehaviour_Detached;
		}

		private void FlipOverBehaviour_Attached(object sender, DetailEventArgs e)
		{
			this.interaction = new Interaction("Flip Over", Obj_InterationTriggered);
			this.Detail.Interactions.Add(this.interaction);
		}

		private void FlipOverBehaviour_Detached(object sender, DetailEventArgs e)
		{
			this.Detail.Interactions.Remove(this.interaction);
		}

		private void Obj_InterationTriggered(object sender, EventArgs e)
		{
			var rot = this.Detail.Rotation;
			rot.X += this.Rotation;
			this.Detail.Rotation = rot;
		}

		public float Rotation { get; set; } = (float)Math.PI;
	}
}