using System;
using System.Threading;

namespace The_Outer_Worlds_Trainer
{
	internal class TOWTrainer
	{
		public bool ShouldAbort { get; set; }
		public bool ShouldNoclip { get; set; }
		public bool ShouldGod { get; set; }
		public bool ShouldAmmo { get; set; }
		public bool ShouldStore { get; set; }
		public bool ShouldTeleport { get; set; }
		public float SelectedGameSpeed { get; set; } = 1f;

		public float XPos { get; private set; }
		public float YPos { get; private set; }
		public float ZPos { get; private set; }
		public float Vel { get; private set; }

		private readonly TOWMemory mem;

		private readonly float[] storedPos = new float[5] { 0f, 0f, 0f, 0f, 0f };

		public TOWTrainer()
		{
			mem = new TOWMemory();
			Thread thread = new Thread(Update)
			{
				IsBackground = true
			};
			ShouldAbort = false;
			thread.Start();
		}

		private void Update()
		{
			do
			{
				if(mem.UpdateState())
				{
					UpdateUIValues();
					SetGameState();
				}
				Thread.Sleep(16);
			} while (!ShouldAbort);
		}

		private void SetGameState()
		{
			if(ShouldStore)
			{
				ShouldStore = false;
				StorePosition();
			}

			if(ShouldTeleport)
			{
				ShouldTeleport = false;
				Teleport();
			}

			if(!ShouldNoclip && (bool)mem.Watchers["godMode"].Current != ShouldGod)
			{
				SetGod(ShouldGod);
			}

			if(IsBitSet((byte)mem.Watchers["cheatFlying"].Current, 2) != ShouldNoclip)
			{
				SetNoclip(ShouldNoclip);
			}

			if(!(bool)mem.Watchers["ttdActive"].Current && (float)mem.Watchers["gameSpeed"].Current != SelectedGameSpeed)
			{
				SetGameSpeed(SelectedGameSpeed);
			}
		}

		private void UpdateUIValues()
		{
			XPos = (float)mem.Watchers["xPos"].Current;
			YPos = (float)mem.Watchers["yPos"].Current;
			ZPos = (float)mem.Watchers["zPos"].Current;
			float xVel = (float)mem.Watchers["xVel"].Current;
			float yVel = (float)mem.Watchers["yVel"].Current;
			double hVel = Math.Floor(Math.Sqrt((xVel * xVel) + (yVel * yVel)) + 0.5f) / 100;
			Vel = (float)hVel;
		}

		private void StorePosition()
		{
			storedPos[0] = (float)mem.Watchers["xPos"].Current;
			storedPos[1] = (float)mem.Watchers["yPos"].Current;
			storedPos[2] = (float)mem.Watchers["zPos"].Current;
			storedPos[3] = (float)mem.Watchers["vLook"].Current;
			storedPos[4] = (float)mem.Watchers["hLook"].Current;
		}

		private void Teleport()
		{
			mem.Write("xPos", storedPos[0]);
			mem.Write("yPos", storedPos[1]);
			mem.Write("zPos", storedPos[2]);
			mem.Write("vLook", storedPos[3]);
			mem.Write("hLook", storedPos[4]);
		}

		private void SetGod(bool b)
		{
			mem.Write("godMode", b);
			mem.Write("fallImmune", b);
		}

		private void SetGameSpeed(float newSpeed)
		{
			mem.Write("gameSpeed", newSpeed);
		}

		private void SetNoclip(bool b)
		{
			byte cheatFlying = (byte)mem.Watchers["cheatFlying"].Current;
			cheatFlying = SetBit(cheatFlying, 2, b);
			byte collisionEnabled = (byte)mem.Watchers["collisionEnabled"].Current;
			collisionEnabled = SetBit(collisionEnabled, 6, !b);
			byte movementMode;
			float flySpeed, acceleration;
			bool godMode;

			if(b)
			{
				movementMode = 5;
				flySpeed = 5000f;
				acceleration = 999999f;
				godMode = true;
			}
			else
			{
				movementMode = 1;
				flySpeed = 600f;
				acceleration = 2048f;
				godMode = ShouldGod;
			}
			mem.Write("cheatFlying", cheatFlying);
			mem.Write("movementMode", movementMode);
			mem.Write("flySpeed", flySpeed);
			mem.Write("acceleration", acceleration);
			mem.Write("collisionEnabled", collisionEnabled);
			mem.Write("jumpApex", float.MinValue);
			SetGod(godMode);
		}

		private bool IsBitSet(byte b, int n)
		{
			return (b & (1 << n)) != 0;
		}

		private byte SetBit(byte b, int i, bool v)
		{
			return v ? (byte)(b | (1 << i)) : (byte)(b & ~(1 << i));
		}
	}
}