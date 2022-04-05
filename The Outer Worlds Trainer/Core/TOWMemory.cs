using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using MemUtil;

namespace The_Outer_Worlds_Trainer
{
	internal class TOWMemory
	{
		public MemoryWatcherList Watchers { get; private set; }
		public bool IsInitialized { get; private set; } = false;

		private Process proc;

		public bool UpdateState()
		{
			if (!IsHooked() || !IsInitialized)
			{
				IsInitialized = false;
				Hook();
				return false;
			}

			try
			{
				Watchers.UpdateAll(proc);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			return true;
		}

		private bool IsHooked()
		{
			return proc != null && !proc.HasExited;
		}

		private void Hook()
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => Regex.IsMatch(x.ProcessName, "Indiana.*-Win64-Shipping"));
			if (processList.Count == 0)
			{
				proc = null;
				return;
			}
			proc = processList[0];

			if (IsHooked())
			{
				IsInitialized = Initialize();
			}
		}

		private bool Initialize()
		{

			IntPtr characterBase, worldBase;
			try
			{
				SignatureScanner scanner = new SignatureScanner(proc, proc.MainModule.BaseAddress, proc.MainModule.ModuleMemorySize);
				if (!GetPlayerCharacterBasePtr(scanner, out characterBase) || !GetWorldBasePtr(scanner, out worldBase))
				{
					return false;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			DeepPointer xPosPtr = new DeepPointer(characterBase, 0x170, 0x1C0);
			DeepPointer yPosPtr = new DeepPointer(characterBase, 0x170, 0x1C4);
			DeepPointer zPosPtr = new DeepPointer(characterBase, 0x170, 0x1C8);
			DeepPointer xVelPtr = new DeepPointer(characterBase, 0x170, 0x210);
			DeepPointer yVelPtr = new DeepPointer(characterBase, 0x170, 0x214);
			DeepPointer zVelPtr = new DeepPointer(characterBase, 0x170, 0x218);
			DeepPointer godModePtr = new DeepPointer(characterBase, 0xBE8, 0x19C);
			DeepPointer fallImmunePtr = new DeepPointer(characterBase, 0xBE8, 0x19E);
			DeepPointer movementModePtr = new DeepPointer(characterBase, 0x3F8, 0x1E8);
			DeepPointer flySpeedPtr = new DeepPointer(characterBase, 0x3F8, 0x218);
			DeepPointer accelerationPtr = new DeepPointer(characterBase, 0x3F8, 0x220);
			DeepPointer cheatFlyingPtr = new DeepPointer(characterBase, 0x3F8, 0x3F4);
			DeepPointer jumpApexPtr = new DeepPointer(characterBase, 0x3F8, 0x898);
			DeepPointer collisionEnabledPtr = new DeepPointer(characterBase, 0x9C);
			DeepPointer vLookPtr = new DeepPointer(characterBase, 0x3C0, 0x3F0);
			DeepPointer hLookPtr = new DeepPointer(characterBase, 0x3C0, 0x3F4);
			DeepPointer ttdActivePtr = new DeepPointer(characterBase, 0x1470, 0x1A0);

			DeepPointer gameSpeedPtr = new DeepPointer(worldBase, 0x38, 0x270, 0x518);

			Watchers = new MemoryWatcherList() {
				new MemoryWatcher<float>(xPosPtr) { Name = "xPos" },
				new MemoryWatcher<float>(yPosPtr) { Name = "yPos" },
				new MemoryWatcher<float>(zPosPtr) { Name = "zPos" },
				new MemoryWatcher<float>(xVelPtr) { Name = "xVel" },
				new MemoryWatcher<float>(yVelPtr) { Name = "yVel" },
				new MemoryWatcher<float>(zVelPtr) { Name = "zVel" },
				new MemoryWatcher<bool>(godModePtr) { Name = "godMode" },
				new MemoryWatcher<bool>(fallImmunePtr) { Name = "fallImmune" },
				new MemoryWatcher<byte>(movementModePtr) { Name = "movementMode" },
				new MemoryWatcher<float>(flySpeedPtr) { Name = "flySpeed" },
				new MemoryWatcher<float>(accelerationPtr) { Name = "acceleration" },
				new MemoryWatcher<byte>(cheatFlyingPtr) { Name = "cheatFlying" },
				new MemoryWatcher<byte>(collisionEnabledPtr) { Name = "collisionEnabled" },
				new MemoryWatcher<float>(jumpApexPtr) { Name = "jumpApex" },
				new MemoryWatcher<float>(vLookPtr) { Name = "vLook" },
				new MemoryWatcher<float>(hLookPtr) { Name = "hLook" },
				new MemoryWatcher<bool>(ttdActivePtr) { Name = "ttdActive" },
				new MemoryWatcher<float>(gameSpeedPtr) { Name = "gameSpeed" }
			};

			try
			{
				Watchers.UpdateAll(proc);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			return true;
		}

		public void Write(string name, byte[] bytes)
		{
			if (!IsHooked() || !IsInitialized || !Watchers[name].DeepPtr.DerefOffsets(proc, out IntPtr addr))
			{
				return;
			}

			try
			{
				_ = proc.WriteBytes(addr, bytes);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public void Write(string name, float fValue)
		{
			Write(name, BitConverter.GetBytes(fValue));
		}

		public void Write(string name, bool boolValue)
		{
			Write(name, BitConverter.GetBytes(boolValue));
		}

		public void Write(string name, byte bValue)
		{
			Write(name, new byte[] { bValue });
		}

		private bool GetPlayerCharacterBasePtr(SignatureScanner scanner, out IntPtr result)
		{
			/*
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EB1 - FF 90 48010000        - call qword ptr [rax+00000148]
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EB7 - 48 8B C8              - mov rcx,rax
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EBA - E8 C18B7201           - call IndianaEpicGameStore-Win64-Shipping.exe+1E0DA80
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EBF - 84 C0                 - test al,al
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EC1 - 74 15                 - je IndianaEpicGameStore-Win64-Shipping.exe+6E4ED8
				IndianaEpicGameStore-Win64-Shipping.exe+6E4EC3 - 48 8B 05 FE4C6B03     - mov rax,[IndianaEpicGameStore-Win64-Shipping.exe+3D99BC8] <---
				IndianaEpicGameStore-Win64-Shipping.exe+6E4ECA - 48 85 C0              - test rax,rax
				IndianaEpicGameStore-Win64-Shipping.exe+6E4ECD - 48 0F44 C3            - cmove rax,rbx

				AOB:

				FF 90 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 84 C0 74 15 48 8B 05 ?? ?? ?? ?? 48 85 C0 48 0F 44 C3 
			 */

			SigScanTarget pattern = new SigScanTarget("FF 90 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 84 C0 74 15 48 8B 05 ?? ?? ?? ?? 48 85 C0 48 0F 44 C3");
			IntPtr codeLocation = scanner.Scan(pattern);
			if(codeLocation == IntPtr.Zero)
			{
				result = codeLocation;
				return false;
			}
			int offset = proc.ReadValue<int>(codeLocation + 0x15);
			result = codeLocation + 0x19 + offset;
			return true;
		}

		private bool GetWorldBasePtr(SignatureScanner scanner, out IntPtr result)
		{
			SigScanTarget pattern = new SigScanTarget("0F 2E ?? 74 ?? 48 8B 1D ?? ?? ?? ?? 48 85 DB 74");
			IntPtr codeLocation = scanner.Scan(pattern);
			if (codeLocation == IntPtr.Zero)
			{
				result = codeLocation;
				return false;
			}
			int offset = proc.ReadValue<int>(codeLocation + 0x8);
			result = codeLocation + 0xC + offset;
			return true;
		}
	}
}