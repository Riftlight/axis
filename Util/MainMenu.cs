using Godot;
using Godot.Collections;

public partial class MainMenu : Control
{
	[Export] public string[] LevelScenes =
	{
		"res://Levels/Level1.tscn",
		"res://Levels/Level2.tscn",
		"res://Levels/Level3.tscn",
		"res://Levels/Level4.tscn",
		"res://Levels/Level5.tscn"
	};

	[Export] public string[] LevelDisplayNames =
	{
		"Level 1",
		"Level 2",
		"Level 3",
		"Level 4",
		"Level 5",
	};

	private const string SavePath = "user://level_times.dat";
	private const string EncryptionKey = "5f10577476a988495d411916ce9bba76ace12b058af4aabceaa1b934489f3a51";

	public override void _Ready()
	{
		BuildMenu(LoadTimes());
	}

	private Dictionary<string, float> LoadTimes()
	{
		if (!FileAccess.FileExists(SavePath))
			return new Dictionary<string, float>();
		
		using var file = FileAccess.OpenEncryptedWithPass(SavePath, FileAccess.ModeFlags.Read, EncryptionKey);
		if (file == null) return new Dictionary<string, float>();

		return (Dictionary<string, float>)file.GetVar();
	}

	private static string LevelId(string scenePath) => System.IO.Path.GetFileNameWithoutExtension(scenePath);

	private int NextPlayableIndex(Dictionary<string,float> times)
	{
		for (int i = 0; i < LevelScenes.Length; i++)
			if (!times.ContainsKey(LevelId(LevelScenes[i])))
				return i;

		return LevelScenes.Length - 1; // all complete, keep last level playable
	}
	
	private void BuildMenu(Dictionary<string, float> times)
	{
		int nextPlayable = NextPlayableIndex(times);

		VBoxContainer root = new();
		root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		root.AddThemeConstantOverride("separation", 0);
		AddChild(root);

		root.AddChild(Spacer(0, 80));

		var title = new Label
		{
			Text = "AXIS",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 38);
		root.AddChild(title);

		root.AddChild(Spacer(0, 48));

		for (int i = 0; i < LevelScenes.Length; i++)
		{
			bool completed = times.TryGetValue(LevelId(LevelScenes[i]), out float best);
			bool playable  = i <= nextPlayable;

			root.AddChild(LevelRow(i, completed, playable, best, LevelScenes[i]));
			root.AddChild(Spacer(0, 10));
		}
	}

	private HBoxContainer LevelRow(int i, bool completed, bool playable, float best, string scene)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 20);

		row.AddChild(ExpandSpacer());

		// Level name
		var nameLabel = new Label
		{
			Text = LevelDisplayNames[i],
			CustomMinimumSize = new Vector2(130, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 20);
		if (!playable) nameLabel.Modulate = new Color(0.35f, 0.35f, 0.35f);
		row.AddChild(nameLabel);

		// Best time or status string
		string timeText = completed ? FormatTime(best)
						: playable  ? "--:--.---"
									: "LOCKED";

		var timeLabel = new Label
		{
			Text = timeText,
			CustomMinimumSize = new Vector2(130, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		timeLabel.AddThemeFontSizeOverride("font_size", 20);
		timeLabel.Modulate = completed
			? new Color(0.5f, 1f, 0.5f)
			: new Color(0.45f, 0.45f, 0.45f);
		row.AddChild(timeLabel);

		// Play button, or blank placeholder to keep columns aligned
		if (playable)
		{
			var btn = new Button { Text = "PLAY", CustomMinimumSize = new Vector2(80, 0) };
			btn.AddThemeFontSizeOverride("font_size", 16);
			btn.Pressed += () => LevelManager.Instance.LoadLevel(i);
			row.AddChild(btn);
		}
		else
		{
			row.AddChild(new Control { CustomMinimumSize = new Vector2(80, 0) });
		}

		row.AddChild(ExpandSpacer());
		return row;
	}

	private static string FormatTime(float t)
	{
		int mins = Mathf.FloorToInt(t / 60);
		int secs = Mathf.FloorToInt(t % 60);
		int millis = Mathf.FloorToInt((t % 1) * 100);
		return mins == 0
			? $"{secs:D2}.{millis:D2}"
			: $"{mins:D2}:{secs:D2}.{millis:D2}";
	}

	private static Control Spacer(float w, float h) => new Control { CustomMinimumSize = new Vector2(w,h) };

	private static Control ExpandSpacer()
	{
		Control c = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		return c;
	}
}
