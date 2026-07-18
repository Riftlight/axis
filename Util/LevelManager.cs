// autoload, not tied to any Node
using Godot;

public partial class LevelManager : Node
{
	public static LevelManager Instance { get; private set; }

	private readonly string[] _levels =
	{
		"res://Levels/Level1.tscn",
		"res://Levels/Level2.tscn",
		"res://Levels/Level3.tscn",
		"res://Levels/Level4.tscn",
		"res://Levels/Level5.tscn"
	};

	private int _currentIndex = 0;

	public override void _Ready()
	{
		Instance = this;
		GetTree().ChangeSceneToFile("res://MainMenu.tscn");
	}

	public void LoadNextLevel()
	{
		_currentIndex++;
		_currentIndex %= _levels.Length;
		GetTree().ChangeSceneToFile(_levels[_currentIndex]);
	}

	public void RestartLevel()
	{
		GetTree().ChangeSceneToFile(_levels[_currentIndex]);
	}

	public void LoadLevel(int index)
	{
		_currentIndex = Mathf.Clamp(index, 0, _levels.Length - 1);
		GetTree().ChangeSceneToFile(_levels[_currentIndex]);
	}

	public int GetLevelIndex()
	{
		return _currentIndex;
	}
}
