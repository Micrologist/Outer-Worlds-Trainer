using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace The_Outer_Worlds_Trainer
{
	public partial class MainWindow : Window
	{
		private readonly TOWTrainer trainer;
		private readonly GlobalKeyboardHook kbHook;
		private readonly float[] gameSpeeds = new float[4] { 1f, 2f, 4f, 0.5f };

		private readonly List<Keys> hookedKeys = new List<Keys> { Keys.F1, Keys.F2, Keys.F3, Keys.F6, Keys.F7 };
		public MainWindow()
		{
			InitializeComponent();
			kbHook = new GlobalKeyboardHook();
			InitializeKeyboardHook();
			trainer = new TOWTrainer();
			DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = new TimeSpan(10 * 10000) };
			timer.Tick += UIUpdateTick;
			timer.Start();
		}

		private void InitializeKeyboardHook()
		{
			kbHook.KeyDown += HandleKeyDown;
			foreach (Keys key in hookedKeys)
			{
				kbHook.HookedKeys.Add(key);
			}
		}

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.F1:
					godBtn_Click(sender, null);
					break;
				case Keys.F2:
					noclipBtn_Click(sender, null);
					break;
				case Keys.F3:
					gameSpeedBtn_Click(sender, null);
					break;
				case Keys.F6:
					saveBtn_Click(sender, null);
					break;
				case Keys.F7:
					teleBtn_Click(sender, null);
					break;
				default:
					break;
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			trainer.ShouldAbort = true;
			kbHook.unhook();
		}

		private void UIUpdateTick(object sender, EventArgs e)
		{
			positionBlock.Text =
				  (trainer.XPos / 100).ToString("0.00") + "\n"
				+ (trainer.YPos / 100).ToString("0.00") + "\n"
				+ (trainer.ZPos / 100).ToString("0.00");
			speedBlock.Text = trainer.Vel.ToString("0.00") + " m/s";
			SetLabel(trainer.ShouldGod, godLabel);
			SetLabel(trainer.ShouldNoclip, noclipLabel);
			SetLabel(trainer.ShouldAmmo, ammoLabel);
			gameSpeedLabel.Content = trainer.SelectedGameSpeed.ToString("0.0") + "x";
		}

		private void SetLabel(bool state, System.Windows.Controls.Label label)
		{
			label.Content = state ? "ON" : "OFF";
			label.Foreground = state ? Brushes.Green : Brushes.Red;
		}

		private void teleBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldTeleport = true;
		}

		private void saveBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldStore = true;
		}

		private void noclipBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldNoclip = !trainer.ShouldNoclip;
		}

		private void godBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldGod = !trainer.ShouldGod;
		}

		private void ammoBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldAmmo = !trainer.ShouldAmmo;
		}

		private void gameSpeedBtn_Click(object sender, RoutedEventArgs e)
		{
			float old = trainer.SelectedGameSpeed;
			trainer.SelectedGameSpeed = gameSpeeds[(Array.IndexOf(gameSpeeds, old) + 1) % gameSpeeds.Length];
		}
	}
}
