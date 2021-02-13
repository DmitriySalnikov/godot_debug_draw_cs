#if TOOLS
using Godot;
using System;

[Tool]
public class DebugDrawEditor : EditorPlugin
{
    public static string PluginDir = "res://addons/debug_draw_cs/";

    ViewportContainer spatial_viewport = null;

    public override void _EnterTree()
    {
        CreateAutoFind();

        if (!IsConnected("scene_changed", this, nameof(OnSceneChanged)))
            Connect("scene_changed", this, nameof(OnSceneChanged));
    }

    public override void _ExitTree()
    {
        RemovePrevNode();

        if (IsConnected("scene_changed", this, nameof(OnSceneChanged)))
            Disconnect("scene_changed", this, nameof(OnSceneChanged));
    }

    public override void DisablePlugin()
    {
        RemovePrevNode();
    }

    public override void _Process(float delta)
    {
        // Dirty workaround for reloading of DebugDraw after project rebuild
        CreateAutoFind();
    }

    void OnSceneChanged(Node node)
    {
        if (node == null) return;

        CreateNewNode(node);
    }

    #region Utilities

    // HACK for finding canvas and drawing on it
    // Hardcoded for 3.2.4
    void FindViewportControl()
    {
        // Create temp control to get spatial viewport
        Control ctrl = new Control();
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu, ctrl);

        // Try to get main viewport node. Must be `SpatialEditor`
        Control spatial_editor = ctrl.GetParent().GetParent<Control>();

        // Remove and destroy temp control
        RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, ctrl);
        ctrl.QueueFree();

        spatial_viewport = null;
        if (spatial_editor.GetClass() == "SpatialEditor")
        {
            // Try to recursively find `SpatialEditorViewport`
            Func<Control, int, Control> get = null;
            get = (c, level) =>
            {
                if (c.GetClass() == "SpatialEditorViewport")
                    return c;

                // 4 Levels must be enough for 3.2.4
                if (level < 4)
                {
                    foreach (var o in c.GetChildren())
                    {
                        if (o is Control ch)
                        {
                            var res = get(ch, level + 1);
                            if (res != null)
                                return res;
                        }
                    }
                }

                return null;
            };

            spatial_viewport = get(spatial_editor, 0)?.GetChild<ViewportContainer>(0);
        }

        if (spatial_viewport != null)
        {
            spatial_viewport.SetMeta("UseParentSize", true);
            spatial_viewport.Update();
        }
    }

    void RemovePrevNode()
    {
        DebugDraw.Instance?.QueueFree();
        spatial_viewport?.Update();

        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        if (tree != null)
        {
            var root = tree.EditedSceneRoot;
            if (root != null)
            {
                var nodes = root.GetChildren();
                foreach (Node n in nodes)
                {
                    if (n.Owner == null && n.HasMeta(nameof(DebugDraw)) && !n.IsQueuedForDeletion())
                    {
                        n.QueueFree();
                    }
                }
            }
        }
    }

    void CreateNewNode(Node parent)
    {
        RemovePrevNode();
        if (DebugDraw.Instance == null)
        {
            FindViewportControl();

            var d = new DebugDraw();
            parent.AddChild(d);

            DebugDraw.CustomViewport = spatial_viewport.GetChild<Viewport>(0);
            DebugDraw.CustomCanvas = spatial_viewport;
        }
    }

    void CreateAutoFind()
    {
        if (DebugDraw.Instance == null)
        {
            Node node = (Engine.GetMainLoop() as SceneTree)?.EditedSceneRoot;
            if (node != null)
                CreateNewNode(node);
        }
    }

    #endregion
}
#endif