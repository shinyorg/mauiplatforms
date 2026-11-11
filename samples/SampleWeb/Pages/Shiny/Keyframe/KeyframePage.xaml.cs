using Shiny.Controls.Keyframe;
using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;

namespace Sample.Features.Keyframe;

public partial class KeyframePage : ContentPage
{
    public KeyframePage()
    {
        InitializeComponent();
        SceneView.Scene = BuildScene();

        // The scene is deliberately finite so the scrubber has an end to scrub toward; replay it
        // when it finishes so the demo keeps moving.
        SceneView.Loaded += (_, _) =>
        {
            if (SceneView.Player is { } player)
                player.Finished += (_, _) => Dispatcher.Dispatch(player.Play);
        };
    }

    /// <summary>
    /// The drawn counterpart to the XAML animations above: same timeline model, same easing
    /// curves, but the targets are scene layers on a canvas rather than views in the visual tree.
    /// </summary>
    static KeyframeScene BuildScene()
    {
        var scene = new KeyframeScene(400, 200) { Background = Colors.Transparent };

        var track = scene.Add(new RectangleLayer
        {
            Id = "track",
            Size = new SizeF(320, 6),
            Position = new PointF(40, 97),
            CornerRadius = 3,
            Fill = Color.FromArgb("#E5E7EB")
        });

        var dots = new List<EllipseLayer>();

        for (var i = 0; i < 5; i++)
        {
            dots.Add(scene.Add(new EllipseLayer
            {
                Id = $"dot{i}",
                Size = new SizeF(28, 28),
                Position = new PointF(40 + i * 76, 86),
                Fill = Color.FromArgb("#2563EB")
            }));
        }

        // Each dot gets its own timeline; the storyboard staggers their starts. Composing at the
        // storyboard level rather than baking offsets into the keyframes means the same per-dot
        // animation can be reused with a different rhythm.
        var storyboard = new Storyboard();

        storyboard.Add(TimelineBuilder
            .Create(TimeSpan.FromSeconds(2))
            .Fill(FillMode.Both)
            .AnimateFill(track, k => k
                .From(Color.FromArgb("#E5E7EB"))
                .To(Color.FromArgb("#BFDBFE")))
            .Build());

        storyboard.Stagger(
            dots.Select(dot => (IAnimationNode)TimelineBuilder
                .Create(TimeSpan.FromSeconds(1.4))
                .Fill(FillMode.Both)
                .AnimatePosition(dot, k => k
                    .From(new PointF(dot.Position.X, 86))
                    .Key(0.5, new PointF(dot.Position.X, 30), Easings.CubicOut)
                    .To(new PointF(dot.Position.X, 86)))
                .AnimateScale(dot, k => k
                    .From(new SizeF(1f, 1f))
                    .Key(0.5, new SizeF(1.4f, 1.4f), Easings.CubicInOut)
                    .To(new SizeF(1f, 1f)))
                .AnimateFill(dot, k => k
                    .From(Color.FromArgb("#2563EB"))
                    .Key(0.5, Color.FromArgb("#EC4899"))
                    .To(Color.FromArgb("#2563EB")))
                .Build()),
            interval: TimeSpan.FromMilliseconds(120));

        scene.Animation = storyboard;
        return scene;
    }

    void OnPlay(object? sender, EventArgs e)
    {
        SceneView.Speed = Math.Abs(SceneView.Speed);
        SceneView.IsPlaying = true;
    }

    void OnPause(object? sender, EventArgs e) => SceneView.IsPlaying = false;

    void OnReverse(object? sender, EventArgs e)
    {
        // Reverse is just a negative rate. It works from any position, mid-flight, because
        // evaluation is a pure function of time rather than an accumulation of frames.
        SceneView.Speed = -Math.Abs(SceneView.Speed);
        SceneView.IsPlaying = true;
    }
}
