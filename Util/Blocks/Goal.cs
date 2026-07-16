using System.Threading;
using Godot;

public partial class Goal : Area2D
{
    [Export] public Vector2 Size = new Vector2(32, 32);
    [Export] public Color GoalColor = new Color(0.2f, 1f, 0.5f);

    public override void _Ready()
    {
        var coll = new CollisionShape2D();
        var shape = new RectangleShape2D { Size = Size };
        coll.Shape = shape;
        AddChild(coll);

        var visual = new ColorRect { Size = Size, Position = -Size / 2f, Color = GoalColor };
        AddChild(visual);

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.Visible = false;
            
            SlimeFinishEffect sfe = new();
            GetTree().CurrentScene.AddChild(sfe);

            sfe.GlobalPosition = player.GlobalPosition;
            sfe.Init(new Color(1.0f, 0.843f, 0f), player.spriteSize);
        }
    }
}