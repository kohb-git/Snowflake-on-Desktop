using System;
using System.IO;
using System.Reflection;

namespace WinterDesktop
{
	internal sealed class Preferences
	{
		internal int Snowfall = 1;
		internal int CordOffset = 3;
		internal int SagPercent = 100;
		internal System.Collections.Generic.Dictionary<string,int> Modes = new System.Collections.Generic.Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
		internal int Mode(string id) { int m; return Modes.TryGetValue(id,out m) ? m : 0; }
		internal bool SnowOn(string id) { int m=Mode(id); return m != 3 && m != 4; }
		internal bool LightsOn(string id) { int m=Mode(id); return m == 0 ? Garland : m == 1 || m == 3; }

		internal bool Interactive = true;

		internal bool Garland = true;

		internal int Intensity = -1;

		internal int Opacity = 100;

		internal int Performance;

		internal int Theme;

		internal int LampSize = 100;

		internal int Brightness = 100;

		internal int Twinkle = 100;

		internal int Sway = 100;

		internal bool Ground = true;

		internal bool Windows = true;

		internal bool Melt;

		internal bool Settle = true;

		internal bool AutoEconomy = true;

		internal static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietSnow", "settings.txt");

		internal static Preferences Load(string path)
		{
			Preferences preferences = new Preferences();
			try
			{
				string[] array = File.ReadAllLines(path);
				foreach (string text in array)
				{
					if (text.StartsWith("snowfall=") && int.TryParse(text.Substring(9), out var result) && result >= 0 && result <= 2)
					{
						preferences.Snowfall = result;
					}
					if (text == "interactive=false")
					{
						preferences.Interactive = false;
					}
					if (text == "garland=false")
					{
						preferences.Garland = false;
					}
					preferences.ReadExtra(text);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			if (preferences.Intensity < 0)
			{
				preferences.Intensity = ((preferences.Snowfall == 0) ? 50 : ((preferences.Snowfall == 2) ? 170 : 100));
			}
			return preferences;
		}

		internal bool Save(string path)
		{
			string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllText(text, "snowfall=" + Snowfall + Environment.NewLine + "interactive=" + (Interactive ? "true" : "false") + Environment.NewLine + "garland=" + (Garland ? "true" : "false") + ExtraText());
				if (File.Exists(path))
				{
					File.Replace(text, path, null);
				}
				else
				{
					File.Move(text, path);
				}
				return true;
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
			finally
			{
				try
				{
					if (File.Exists(text))
					{
						File.Delete(text);
					}
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}

		internal Preferences Copy()
		{
			var copy = (Preferences)MemberwiseClone();
			copy.Modes = new System.Collections.Generic.Dictionary<string,int>(Modes, Modes.Comparer);
			return copy;
		}

		internal void CopyFrom(Preferences other)
		{
			FieldInfo[] fields = typeof(Preferences).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				fieldInfo.SetValue(this, fieldInfo.GetValue(other));
			}
		}

		internal void ReadExtra(string line)
		{
			string[] array = line.Split('=');
			if (array.Length != 2)
			{
				return;
			}
			if (int.TryParse(array[1], out var result))
			{
				switch (array[0])
				{
				case "cordOffset": CordOffset = Math.Max(0, Math.Min(16,result)); break;
				case "sagPercent": SagPercent = Math.Max(0, Math.Min(100,result)); break;
				case "intensity":
					if (result >= 0 && result <= 200)
					{
						Intensity = result;
					}
					break;
				case "opacity":
					if (result >= 10 && result <= 100)
					{
						Opacity = result;
					}
					break;
				case "performance":
					if (result >= 0 && result <= 2)
					{
						Performance = result;
					}
					break;
				case "theme":
					if (result >= 0 && result <= 2)
					{
						Theme = result;
					}
					break;
				case "lampSize":
					if (result >= 60 && result <= 150)
					{
						LampSize = result;
					}
					break;
				case "brightness":
					if (result >= 10 && result <= 100)
					{
						Brightness = result;
					}
					break;
				case "twinkle":
					if (result >= 0 && result <= 200)
					{
						Twinkle = result;
					}
					break;
				case "sway":
					if (result >= 0 && result <= 200)
					{
						Sway = result;
					}
					break;
				}
			}
			if (array[0].StartsWith("screen.") && int.TryParse(array[1],out result) && result >= 1 && result <= 4) Modes[array[0].Substring(7)] = result;
			if (bool.TryParse(array[1], out var result2))
			{
				switch (array[0])
				{
				case "ground":
					Ground = result2;
					break;
				case "windows":
					Windows = result2;
					break;
				case "melt":
					Melt = result2;
					break;
				case "settle":
					Settle = result2;
					break;
				case "autoEconomy":
					AutoEconomy = result2;
					break;
				}
			}
		}

		internal string ExtraText()
		{
			var extra = new System.Text.StringBuilder();
			foreach (var pair in Modes) extra.Append("\nscreen.").Append(pair.Key).Append("=").Append(pair.Value);
			return extra.ToString() + "\ncordOffset=" + CordOffset + "\nsagPercent=" + SagPercent + "\nintensity=" + Intensity + "\nopacity=" + Opacity + "\nperformance=" + Performance + "\ntheme=" + Theme + "\nlampSize=" + LampSize + "\nbrightness=" + Brightness + "\ntwinkle=" + Twinkle + "\nsway=" + Sway + "\nground=" + Ground + "\nwindows=" + Windows + "\nmelt=" + Melt + "\nsettle=" + Settle + "\nautoEconomy=" + AutoEconomy;
		}
	}
}

