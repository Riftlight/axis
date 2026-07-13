using Godot;

public partial class SwitchUi : CanvasLayer
{
	[Export] public bool SwitchesAreLimited = true;
	private Label label;
	private Sprite2D sprite;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (SwitchesAreLimited) {
			label = GetNode<Label>("Label");
			sprite = GetNode<Sprite2D>("Sprite2D");
		}
	}

	public void UpdateSwitches(int remaining)
	{
		if (!SwitchesAreLimited) return;

		label.Text = ""+remaining;
		if (remaining == 0) {
			sprite.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile("res://Textures/switchrestart.png"));
			label.Visible = false;
		} else
		{
			sprite.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile("res://Textures/switchcounter.png"));
			label.Visible = true;
		}
	}
}
