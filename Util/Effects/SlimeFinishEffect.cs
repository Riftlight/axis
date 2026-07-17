using Godot;
using System;

// yes a nearly identical class already exists but who cares about good code quality
public partial class SlimeFinishEffect : Node2D
{
	private const float FadeDuration = 0.3f;

	private struct Confetto
	{
		public Vector2 Position, Velocity, Size;
		public float Rotation, AngularVelocity;
		public Color Color;
	}

	private Vector2 _gravityDir = Vector2.Up;

	private Confetto[] _confetti;
	private float _elapsed;
	private bool _fadeStarted;
	private ColorRect _fadeRect;

	public void Init(Color color, float baseScale)
	{
		const int count = 1000;
		RandomNumberGenerator rng = new();
		rng.Randomize();

		_confetti = new Confetto[count];
		for (int i = 0; i < count; i++)
		{
			float angle = rng.RandfRange(Mathf.Pi*1.25f, Mathf.Tau*0.75f);
			float speed = rng.RandfRange(80f, 200f);

			Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
			vel -= _gravityDir * rng.RandfRange(0f, 140f);

			float w = rng.RandfRange(0.1f, 2f);
			float h = rng.RandfRange(0.1f, 2f);

			float hue = color.H;
			float sat = rng.RandfRange(0f, 1f);
			float val = Mathf.Clamp(color.V + rng.RandfRange(-.5f, .5f), 0f, 1f);
			_confetti[i] = new Confetto
			{
				Position = new Vector2(rng.RandfRange(-3f, 3f), rng.RandfRange(-3f, 3f)) * baseScale,
				Velocity = vel,
				Rotation = rng.RandfRange(Mathf.Pi, Mathf.Tau),
				AngularVelocity = rng.RandfRange(-8f, 8f),
				Size = new Vector2(w,h) * baseScale	,
				Color = Color.FromHsv(hue, sat, val)
			};
		}

		CanvasLayer layer = new CanvasLayer { Layer = 128 };
		GetTree().CurrentScene.AddChild(layer);

		_fadeRect = new ColorRect
		{
			Color = Colors.Black,
			Modulate = new Color(1f,1f,1f,0f),
			AnchorRight = 1f,
			AnchorBottom = 1f
		};
		layer.AddChild(_fadeRect);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_elapsed += dt;

		const float chunkGravity = 550f;
		for (int i = 0; i < _confetti.Length; i++)
		{
			_confetti[i].Velocity += _gravityDir * chunkGravity * dt;
			_confetti[i].Position += _confetti[i].Velocity * dt;
			_confetti[i].Rotation += _confetti[i].AngularVelocity * dt;
		}

		float lowestY = float.NegativeInfinity;

		foreach (Confetto c in _confetti)
		{
			lowestY = Mathf.Max(lowestY, ToGlobal(c.Position).Y);
		}

		QueueRedraw();

		if (lowestY < 0 && !_fadeStarted)
		{
			_fadeStarted = true;
			Tween tween = CreateTween();
			tween.TweenProperty(_fadeRect, "modulate:a", 1f, FadeDuration);
			tween.TweenInterval(0.05f);
			tween.TweenCallback(Callable.From(() => LevelManager.Instance.LoadNextLevel()));
		}
	}

	public override void _Draw()
	{
		foreach (Confetto c in _confetti)
		{
			DrawSetTransform(c.Position, c.Rotation);
			DrawRect(new Rect2(-c.Size / 2f, c.Size), c.Color);
		}
		DrawSetTransform(Vector2.Zero, 0f);
	}
}
