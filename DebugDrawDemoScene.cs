using Godot;
using System;
using System.Linq;

[Tool]
public class DebugDrawDemoScene : Spatial
{
    [Export] bool ZylannExample = false;
    Vector3 start_pos;
    CSGBox box = null;

    public override void _Ready()
    {
        box = GetNodeOrNull<CSGBox>("LagTest");
        start_pos = box.Translation;
        ProcessPriority = 1;
    }

    public override void _Process(float delta)
    {
        // Zylann's example :D
        if (ZylannExample)
        {
            var time = OS.GetTicksMsec() / 1000f;
            var box_pos = new Vector3(0, Mathf.Sin(time * 4f), 0);
            var line_begin = new Vector3(-1, Mathf.Sin(time * 4f), 0);
            var line_end = new Vector3(1, Mathf.Cos(time * 4f), 0);

            DebugDraw.DrawBox(box_pos, new Vector3(1, 2, 1), new Color(0, 1, 0), 0, false); // Box need to be NOT centered
            DebugDraw.DrawLine3D(line_begin, line_end, new Color(1, 1, 0));
            DebugDraw.SetText("Time", time);
            DebugDraw.SetText("Frames drawn", Engine.GetFramesDrawn());
            DebugDraw.SetText("FPS", Engine.GetFramesPerSecond());
            DebugDraw.SetText("delta", delta);
            return;
        }


        // Enable FPSGraph
        DebugDraw.FPSGraphEnabled = true;
        DebugDraw.FPSGraphShowTextFlags = DebugDraw.FPSGraphTextFlags.Current | DebugDraw.FPSGraphTextFlags.Max | DebugDraw.FPSGraphTextFlags.Min;
        DebugDraw.FPSGraphSize = new Vector2(200, 32);

        // Debug for debug
        DebugDraw.Freeze3DRender = Input.IsActionPressed("ui_accept");
        DebugDraw.ForceUseCameraFromScene = Input.IsActionPressed("ui_up");
        DebugDraw.DebugEnabled = !Input.IsActionPressed("ui_down");

        // Zones
        var col = Colors.Black;
        foreach (Spatial z in GetNode<Spatial>("Zones").GetChildren())
            DebugDraw.DrawBox(z.GlobalTransform, col);

        // Spheres
        {
            DebugDraw.DrawSphere(GetNode<Spatial>("SphereTransform").GlobalTransform, Colors.Crimson);
            DebugDraw.DrawSphere(GetNode<Spatial>("SpherePosition").GlobalTransform.origin, 2, Colors.BlueViolet);
        }

        // Cylinders
        {
            DebugDraw.DrawCylinder(GetNode<Spatial>("Cylinder1").GlobalTransform, Colors.Crimson);
            DebugDraw.DrawCylinder(GetNode<Spatial>("Cylinder2").GlobalTransform.origin, 1, 2, Colors.Red);
        }

        // Boxes
        {
            DebugDraw.DrawBox(GetNode<Spatial>("Box1").GlobalTransform, Colors.Purple);
            DebugDraw.DrawBox(GetNode<Spatial>("Box2").GlobalTransform.origin, Vector3.One, Colors.RebeccaPurple);
            DebugDraw.DrawBox(GetNode<Spatial>("Box3").GlobalTransform.origin, new Quat(Vector3.Up, Mathf.Pi * 0.25f), Vector3.One * 2, Colors.RosyBrown);

            var r = GetNode<Spatial>("AABB");
            DebugDraw.DrawAABB(r.GetChild<Spatial>(0).GlobalTransform.origin, r.GetChild<Spatial>(1).GlobalTransform.origin, Colors.DeepPink);

            DebugDraw.DrawAABB(new AABB(GetNode<Spatial>("AABB_fixed").GlobalTransform.origin, new Vector3(2, 1, 2)), Colors.Aqua);
        }

        // Lines
        {
            var target = GetNode<Spatial>("Lines/Target");
            DebugDraw.DrawBillboardSquare(target.GlobalTransform.origin, 0.5f, Colors.Red);

            // Normal
            {
                DebugDraw.DrawLine3D(GetNode<Spatial>("Lines/1").GlobalTransform.origin, target.GlobalTransform.origin, Colors.Fuchsia);
                DebugDraw.DrawRay3D(GetNode<Spatial>("Lines/3").GlobalTransform.origin, (target.GlobalTransform.origin - GetNode<Spatial>("Lines/3").GlobalTransform.origin).Normalized(), 3f, Colors.Crimson);
            }

            // Arrow
            {
                DebugDraw.DrawArrowLine3D(GetNode<Spatial>("Lines/2").GlobalTransform.origin, target.GlobalTransform.origin, Colors.Blue);
                DebugDraw.DrawArrowRay3D(GetNode<Spatial>("Lines/4").GlobalTransform.origin, (target.GlobalTransform.origin - GetNode<Spatial>("Lines/4").GlobalTransform.origin).Normalized(), 8f, Colors.Lavender);
            }

            // Path
            {
                DebugDraw.DrawLinePath3D(GetNode<Spatial>("LinePath")?.GetChildren().ToArray<Spatial>().Select((o) => o.GlobalTransform.origin).ToArray(), Colors.Beige);
                DebugDraw.DrawArrowPath3D(GetNode<Spatial>("LinePath")?.GetChildren().ToArray<Spatial>().Select((o) => o.GlobalTransform.origin + Vector3.Down).ToArray(), Colors.Gold);
            }

            DebugDraw.DrawLine3DHit(GetNode<Spatial>("Lines/5").GlobalTransform.origin, target.GlobalTransform.origin, true, Mathf.Abs(Mathf.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds)), 0.25f, 0, Colors.Aqua);
        }

        // Misc
        {
            DebugDraw.DrawCameraFrustum(GetNode<Camera>("Camera"), Colors.DarkOrange);
            DebugDraw.DrawBillboardSquare(GetNode<Spatial>("Billboard").GlobalTransform.origin, 0.5f, Colors.Green);
            DebugDraw.DrawPosition3D(GetNode<Spatial>("Position").GlobalTransform, Colors.Brown);
        }

        // Text
        {
            DebugDraw.SetText("FPS", $"{Engine.GetFramesPerSecond():F2}", 0, Colors.Gold);

            DebugDraw.BeginTextGroup("-- First Group --", 2, Colors.ForestGreen);
            DebugDraw.SetText("Simple text");
            DebugDraw.SetText("Text", "Value", 0, Colors.Aquamarine);
            DebugDraw.SetText("Text out of order", null, -1, Colors.Silver);
            DebugDraw.BeginTextGroup("-- Second Group --", 1, Colors.Beige);
            DebugDraw.SetText("Rendered frames", $"{Engine.GetFramesDrawn()}");
            DebugDraw.EndTextGroup();

            DebugDraw.BeginTextGroup("-- Stats --", 3, Colors.Wheat);
            DebugDraw.SetText("Total rendered", DebugDraw.RenderCount.Total, 0);
            DebugDraw.SetText("Instances", DebugDraw.RenderCount.Instances, 1);
            DebugDraw.SetText("Wireframes", DebugDraw.RenderCount.Wireframes, 2);
            DebugDraw.EndTextGroup();
        }

        // Lag Test
        {
            if (!Engine.EditorHint && box != null)
            {
                box.Translation = start_pos + new Vector3(Mathf.Sin(OS.GetTicksMsec() / 100.0f) * 2.5f, 0, 0);
                DebugDraw.DrawBox(box.GlobalTransform.origin, Vector3.One * 2f);
            }
        }
    }

}

public static class Extensions
{
    public static T[] ToArray<T>(this Godot.Collections.Array array) where T : Godot.Node, new()
    {
        var res = new T[array.Count];
        for (int i = 0; i < array.Count; i++)
            res[i] = array[i] as T;
        return res;
    }
}