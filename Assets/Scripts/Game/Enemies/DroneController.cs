﻿namespace ShootAR.Enemies
{
	public class DroneController : PyoopyooController
	{
		private const float ShootDelay = 5f;

		protected void Update()
		{
			if (lastBullet == null)
			{
				Invoke(nameof(Shoot), ShootDelay);
			}
		}

		protected void FixedUpdate()
		{
			//OrbitAround();
		}
	}
}