using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GDArray = Godot.Collections.Array;

/// <summary>
/// Single-file autoload for debug drawing and printing.
/// Draw and print on screen from anywhere in a single line of code.
/// 
/// You can use only this file by adding it to autoload.
/// Also you can use it in editor by enabling 'Debug Draw For Editor' plugin.
/// 
/// No need to remove any code associated with this class in the release build.
/// Canvas placed on layer 64.
/// All positions in global space.
/// Thread-safe (I hope).
/// "Game Camera Override" is not supports, because no one in the Godot Core Team 
/// exposes methods to support this (but you can just disable culling see <see cref="UseFrustumCulling"/>).
/// </summary>
public class DebugDraw : Node
{
    public enum BlockPosition
    {
        LeftTop,
        RightTop,
        LeftBottom,
        RightBottom,
    }

    [Flags]
    public enum FPSGraphTextFlags
    {
        None = 0,
        Current = 1 << 0,
        Avarage = 1 << 1,
        Max = 1 << 2,
        Min = 1 << 3,
        All = Current | Avarage | Max | Min
    }

    public struct RenderCountData
    {
        public int Instances;
        public int Wireframes;
        public int Total;
        public RenderCountData(int instances, int wireframes)
        {
            Instances = instances;
            Wireframes = wireframes;
            Total = instances + wireframes;
        }
    }

    // GENERAL

    /// <summary>
    /// Enable or disable all debug draw.
    /// </summary>
    public static bool DebugEnabled { get; set; } = true;

    /// <summary>
    /// Debug for debug...
    /// </summary>
    public static bool Freeze3DRender { get; set; } = false;

    /// <summary>
    /// Geometry culling based on camera frustum
    /// Change to false to disable it
    /// </summary>
    public static bool UseFrustumCulling { get; set; } = true;

    /// <summary>
    /// Force use camera placed on edited scene. Usable for editor.
    /// </summary>
    public static bool ForceUseCameraFromScene { get; set; } = false;

    // TEXT

    /// <summary>
    /// Position of text block
    /// </summary>
    public static BlockPosition TextBlockPosition { get; set; } = BlockPosition.LeftTop;

    /// <summary>
    /// Offset from the corner selected in <see cref="TextBlockPosition"/>
    /// </summary>
    public static Vector2 TextBlockOffset { get; set; } = new Vector2(8, 8);

    /// <summary>
    /// Text padding for each line
    /// </summary>
    public static Vector2 TextPadding { get; set; } = new Vector2(2, 1);

    /// <summary>
    /// How long HUD text lines remain shown after being invoked.
    /// </summary>
    public static TimeSpan TextDefaultDuration { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Color of the text drawn as HUD
    /// </summary>
    public static Color TextForegroundColor { get; set; } = new Color(1, 1, 1);

    /// <summary>
    /// Background color of the text drawn as HUD
    /// </summary>
    public static Color TextBackgroundColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    // FPS GRAPH

    /// <summary>
    /// Is FPSGraph enabled
    /// </summary>
    public static bool FPSGraphEnabled { get; set; } = false;

    /// <summary>
    /// Switch between frame time and FPS modes
    /// </summary>
    public static bool FPSGraphFrameTimeMode { get; set; } = true;

    /// <summary>
    /// Draw a graph line aligned vertically in the center
    /// </summary>
    public static bool FPSGraphCenteredGraphLine { get; set; } = true;

    /// <summary>
    /// Sets the text visibility
    /// </summary>
    public static FPSGraphTextFlags FPSGraphShowTextFlags { get; set; } = FPSGraphTextFlags.All;

    /// <summary>
    /// Size of the FPS Graph. The width is equal to the number of stored frames.
    /// </summary>
    public static Vector2 FPSGraphSize { get; set; } = new Vector2(256, 64);

    /// <summary>
    /// Offset from the corner selected in <see cref="FPSGraphPosition"/>
    /// </summary>
    public static Vector2 FPSGraphOffset { get; set; } = new Vector2(8, 8);

    /// <summary>
    /// FPS Graph position
    /// </summary>
    public static BlockPosition FPSGraphPosition { get; set; } = BlockPosition.RightTop;

    /// <summary>
    /// Graph line color
    /// </summary>
    public static Color FPSGraphLineColor { get; set; } = Colors.OrangeRed;

    /// <summary>
    /// Color of the info text
    /// </summary>
    public static Color FPSGraphTextColor { get; set; } = Colors.WhiteSmoke;

    /// <summary>
    /// Background color
    /// </summary>
    public static Color FPSGraphBackgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    /// <summary>
    /// Border color
    /// </summary>
    public static Color FPSGraphBorderColor { get; set; } = Colors.Black;

    // GEOMETRY

    public static RenderCountData RenderCount
    {
#if DEBUG
        get
        {
            if (internalInstance != null)
                return new RenderCountData(internalInstance.renderInstances, internalInstance.renderWireframes);
            else
                return default;
        }
#else
        get => default;
#endif
    }

    /// <summary>
    /// Color of line with hit
    /// </summary>
    public static Color LineHitColor { get; set; } = Colors.Red;

    /// <summary>
    /// Color of line after hit
    /// </summary>
    public static Color LineAfterHitColor { get; set; } = Colors.Green;

    // Misc

    /// <summary>
    /// Custom <see cref="Viewport"/> to use for frustum culling.
    /// Usually used in editor.
    /// </summary>
    public static Viewport CustomViewport { get; set; } = null;

    /// <summary>
    /// Custom <see cref="CanvasItem"/> to draw on it. Set to <see langword="null"/> to disable.
    /// </summary>
    public static CanvasItem CustomCanvas
    {
#if DEBUG
        get => internalInstance?.CustomCanvas;
        set { if (internalInstance != null) internalInstance.CustomCanvas = value; }
#else
        get; set;
#endif
    }

#if DEBUG

    static DebugDrawInternalFunctionality.DebugDrawImplementation internalInstance = null;
    static DebugDraw instance = null;

    /// <summary>
    /// Do not use it directly. This property will not be available without debug
    /// </summary>
    public static DebugDraw Instance
    {
        get => instance;
    }

#endif

    #region Node Functions

#if DEBUG

    public DebugDraw()
    {
        if (instance == null)
            instance = this;
        else
            throw new Exception("Only 1 instance of DebugDraw is allowed");

        Name = nameof(DebugDraw);
        internalInstance = new DebugDrawInternalFunctionality.DebugDrawImplementation(this);
    }

    public override void _EnterTree()
    {
        SetMeta(nameof(DebugDraw), true);

        // Specific for editor settings
        if (Engine.EditorHint)
        {
            TextBlockPosition = BlockPosition.LeftBottom;
            FPSGraphOffset = new Vector2(12, 72);
            FPSGraphPosition = BlockPosition.LeftTop;
        }
    }

    protected override void Dispose(bool disposing)
    {
        internalInstance?.Dispose();
        internalInstance = null;
        instance = null;

        if (NativeInstance != IntPtr.Zero && !IsQueuedForDeletion())
            QueueFree();
        base.Dispose(disposing);
    }

    public override void _ExitTree()
    {
        internalInstance?.Dispose();
        internalInstance = null;
    }

    public override void _Ready()
    {
        ProcessPriority = int.MaxValue;
        internalInstance.Ready();
    }

    public override void _Process(float delta)
    {
        internalInstance?.Update(delta);
    }

#endif

#pragma warning disable CA1822 // Mark members as static
    public void OnCanvaItemDraw(CanvasItem ci)
#pragma warning restore CA1822 // Mark members as static
    {
#if DEBUG
        internalInstance?.OnCanvaItemDraw(ci);
#endif
    }

    #endregion // Node Functions

    #region Static Draw Functions

    /// <summary>
    /// Clear all 3D objects
    /// </summary>
    public static void Clear3DObjects()
    {
#if DEBUG
        internalInstance?.Clear3DObjectsInternal();
#endif
    }

    /// <summary>
    /// Clear all 2D objects
    /// </summary>
    public static void Clear2DObjects()
    {
#if DEBUG
        internalInstance?.Clear2DObjectsInternal();
#endif
    }

    /// <summary>
    /// Clear all debug objects
    /// </summary>
    public static void ClearAll()
    {
#if DEBUG
        internalInstance?.ClearAllInternal();
#endif
    }

    #region 3D

    #region Spheres

    /// <summary>
    /// Draw sphere
    /// </summary>
    /// <param name="position">Position of the sphere center</param>
    /// <param name="radius">Sphere radius</param>
    /// <param name="color">Sphere color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawSphere(Vector3 position, float radius, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawSphereInternal(ref position, radius, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw sphere
    /// </summary>
    /// <param name="transform">Transform of the sphere</param>
    /// <param name="color">Sphere color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawSphere(Transform transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawSphereInternal(ref transform, ref color, duration);
#endif
    }

    #endregion // Spheres

    #region Cylinders

    /// <summary>
    /// Draw vertical cylinder
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="radius">Cylinder radius</param>
    /// <param name="height">Cylinder height</param>
    /// <param name="color">Cylinder color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCylinder(Vector3 position, float radius, float height, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCylinderInternal(ref position, radius, height, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw vertical cylinder
    /// </summary>
    /// <param name="transform">Cylinder transform</param>
    /// <param name="color">Cylinder color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCylinder(Transform transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCylinderInternal(ref transform, ref color, duration);
#endif
    }

    #endregion // Cylinders

    #region Boxes

    /// <summary>
    /// Draw box
    /// </summary>
    /// <param name="position">Position of the box</param>
    /// <param name="size">Box size</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Vector3 position, Vector3 size, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref position, ref size, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw rotated box
    /// </summary>
    /// <param name="position">Position of the box</param>
    /// <param name="rotation">Box rotation</param>
    /// <param name="size">Box size</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Vector3 position, Quat rotation, Vector3 size, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref position, ref rotation, ref size, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw rotated box
    /// </summary>
    /// <param name="transform">Box transform</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Transform transform, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref transform, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw AABB from <paramref name="a"/> to <paramref name="b"/>
    /// </summary>
    /// <param name="a">Firts corner</param>
    /// <param name="b">Second corner</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawAABB(Vector3 a, Vector3 b, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawAABBInternal(ref a, ref b, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw AABB
    /// </summary>
    /// <param name="aabb">AABB</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawAABB(AABB aabb, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawAABBInternal(ref aabb, ref color, duration);
#endif
    }

    #endregion // Boxes

    #region Lines

    /// <summary>
    /// Draw line separated by hit point (billboard square) or not separated if <paramref name="is_hit"/> = <see langword="false"/>
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="is_hit">Is hit</param>
    /// <param name="unitOffsetOfHit">Unit offset on the line where the hit occurs</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="hitColor">Color of the hit point and line before hit</param>
    /// <param name="afterHitColor">Color of line after hit position</param>
    public static void DrawLine3DHit(Vector3 a, Vector3 b, bool is_hit, float unitOffsetOfHit = 0.5f, float hitSize = 0.25f, float duration = 0f, Color? hitColor = null, Color? afterHitColor = null)
    {
#if DEBUG
        internalInstance?.DrawLine3DHitInternal(ref a, ref b, is_hit, unitOffsetOfHit, hitSize, duration, ref hitColor, ref afterHitColor);
#endif
    }

    #region Normal

    /// <summary>
    /// Draw line
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="color">Line color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawLine3D(Vector3 a, Vector3 b, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawLine3DInternal(ref a, ref b, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw ray
    /// </summary>
    /// <param name="origin">Origin</param>
    /// <param name="direction">Direction</param>
    /// <param name="length">Length</param>
    /// <param name="color">Ray color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawRay3D(Vector3 origin, Vector3 direction, float length, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawRay3DInternal(origin, direction, length, color, duration);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines
    /// </summary>
    /// <param name="path">Sequence of points</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawLinePath3D(IList<Vector3> path, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawLinePath3DInternal(path, color, duration);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines
    /// </summary>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="path">Sequence of points</param>
    public static void DrawLinePath3D(Color? color = null, float duration = 0f, params Vector3[] path)
    {
#if DEBUG
        internalInstance?.DrawLinePath3DInternal(color, duration, path);
#endif
    }

    #endregion // Normal

    #region Arrows

    /// <summary>
    /// Draw line with arrow
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="color">Line color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowLine3D(Vector3 a, Vector3 b, Color? color = null, float duration = 0f, float arrowSize = 0.15f, bool absoluteSize = false)
    {
#if DEBUG
        internalInstance?.DrawArrowLine3DInternal(a, b, color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw ray with arrow
    /// </summary>
    /// <param name="origin">Origin</param>
    /// <param name="direction">Direction</param>
    /// <param name="length">Length</param>
    /// <param name="color">Ray color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowRay3D(Vector3 origin, Vector3 direction, float length, Color? color = null, float duration = 0f, float arrowSize = 0.15f, bool absoluteSize = false)
    {
#if DEBUG
        internalInstance?.DrawArrowRay3DInternal(origin, direction, length, color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines with arrows
    /// </summary>
    /// <param name="path">Sequence of points</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowPath3D(IList<Vector3> path, Color? color = null, float duration = 0f, float arrowSize = 0.75f, bool absoluteSize = true)
    {
#if DEBUG
        internalInstance?.DrawArrowPath3DInternal(path, ref color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines with arrows
    /// </summary>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="path">Sequence of points</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowPath3D(Color? color = null, float duration = 0f, float arrowSize = 0.75f, bool absoluteSize = true, params Vector3[] path)
    {
#if DEBUG
        internalInstance?.DrawArrowPath3DInternal(ref color, duration, arrowSize, absoluteSize, path);
#endif
    }

    #endregion // Arrows
    #endregion // Lines

    #region Misc

    /// <summary>
    /// Draw a square that will always be turned towards the camera
    /// </summary>
    /// <param name="position">Center position of square</param>
    /// <param name="color">Color</param>
    /// <param name="size">Unit size</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawBillboardSquare(Vector3 position, float size = 0.2f, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawBillboardSquareInternal(ref position, size, ref color, duration);
#endif
    }

    #region Camera Frustum

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="camera">Camera node</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCameraFrustum(Camera camera, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCameraFrustumInternal(ref camera, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="cameraFrustum">Array of frustum planes</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCameraFrustum(GDArray cameraFrustum, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCameraFrustumInternal(ref cameraFrustum, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="planes">Array of frustum planes</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCameraFrustum(Plane[] planes, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCameraFrustumInternal(ref planes, ref color, duration);
#endif
    }

    #endregion // Camera Frustum

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="transform">Transform</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Transform transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref transform, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="scale">Scale</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Vector3 position, Quat rotation, Vector3 scale, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref position, ref rotation, ref scale, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="color">Color</param>
    /// <param name="scale">Uniform scale</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Vector3 position, Color? color = null, float scale = 0.25f, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref position, ref color, scale, duration);
#endif
    }

    #endregion // Misc
    #endregion // 3D

    #region 2D

    /// <summary>
    /// Begin text group
    /// </summary>
    /// <param name="groupTitle">Group title and ID</param>
    /// <param name="groupPriority">Group priority</param>
    /// <param name="showTitle">Whether to show the title</param>
    public static void BeginTextGroup(string groupTitle, int groupPriority = 0, Color? groupColor = null, bool showTitle = true)
    {
#if DEBUG
        internalInstance?.BeginTextGroupInternal(groupTitle, groupPriority, ref groupColor, showTitle);
#endif
    }

    /// <summary>
    /// End text group. Should be called after <see cref="BeginTextGroup(string, int, bool)"/> if you don't need more than one group.
    /// If you need to create 2+ groups just call again <see cref="BeginTextGroup(string, int, bool)"/>
    /// and this function in the end.
    /// </summary>
    /// <param name="groupTitle">Group title and ID</param>
    /// <param name="groupPriority">Group priority</param>
    /// <param name="showTitle">Whether to show the title</param>
    public static void EndTextGroup()
    {
#if DEBUG
        internalInstance?.EndTextGroupInternal();
#endif
    }

    /// <summary>
    /// Add or update text in overlay
    /// </summary>
    /// <param name="key">Name of field if <paramref name="value"/> exists, otherwise whole line will equal <paramref name="key"/>.</param>
    /// <param name="value">Value of field</param>
    /// <param name="priority">Priority of this line. Lower value is higher position.</param>
    /// <param name="duration">Expiration time</param>
    public static void SetText(string key, object value = null, int priority = 0, Color? colorOfValue = null, float duration = -1f)
    {
#if DEBUG
        internalInstance?.SetTextIntenal(ref key, ref value, priority, ref colorOfValue, duration);
#endif
    }

    #endregion // 2D

    #endregion
}

#if DEBUG

namespace DebugDrawInternalFunctionality
{
    #region Renderable Primitives

    public class SphereBounds
    {
        public Vector3 Position;
        public float Radius;
    }

    class TextGroup
    {
        public string Title;
        public int GroupPriority;
        public Color GroupColor;
        public bool ShowTitle;
        public Dictionary<string, DelayedText> Texts = new Dictionary<string, DelayedText>();

        public TextGroup(string title, int priority, bool showTitle, Color groupColor)
        {
            Title = title;
            GroupPriority = priority;
            ShowTitle = showTitle;
            GroupColor = groupColor;
        }

        public void CleanTexts(Action update)
        {
            var keysToRemove = Texts
                .Where(p => p.Value.IsExpired())
                .Select(p => p.Key).ToArray();

            foreach (var k in keysToRemove)
                Texts.Remove(k);

            if (keysToRemove.Length > 0)
                update?.Invoke();

        }
    }

    class DelayedText
    {
        public DateTime ExpirationTime;
        public string Text;
        public int Priority;
        public Color? ValueColor = null;
        public DelayedText(DateTime expirationTime, string text, int priority, Color? color)
        {
            ExpirationTime = expirationTime;
            Text = text;
            Priority = priority;
            ValueColor = color;
        }

        public bool IsExpired()
        {
            return !DebugDraw.DebugEnabled || (DateTime.Now - ExpirationTime).TotalMilliseconds > 0;
        }
    }

    class DelayedRenderer : IPoolable
    {
        public DateTime ExpirationTime;
        public bool IsUsedOneTime = false;
        public bool IsVisible = true;

        public bool IsExpired()
        {
            return !DebugDraw.DebugEnabled || ((DateTime.Now - ExpirationTime).TotalMilliseconds > 0 && IsUsedOneTime);
        }

        public void Returned()
        {
            IsUsedOneTime = false;
            IsVisible = true;
        }
    }

    class DelayedRendererInstance : DelayedRenderer
    {
        public Transform InstanceTransform;
        public Color InstanceColor;
        public SphereBounds Bounds = new SphereBounds();
    }

    class DelayedRendererLine : DelayedRenderer
    {
        public AABB Bounds { get; set; }
        public Color LinesColor;
        protected Vector3[] _lines = Array.Empty<Vector3>();
        public virtual Vector3[] Lines
        {
            get => _lines;
            set
            {
                _lines = value;
                Bounds = CalculateBoundsBasedOnLines(ref _lines);
            }
        }

        protected AABB CalculateBoundsBasedOnLines(ref Vector3[] lines)
        {
            if (lines.Length > 0)
            {
                var b = new AABB(lines[0], Vector3.Zero);
                foreach (var v in lines)
                    b = b.Expand(v);

                return b;
            }
            else
            {
                return new AABB();
            }
        }
    }

    #endregion // Renderable Primitives

    class FPSGraph
    {
        float[] frameTimes = new float[1];
        int position = 0;
        int filled = 0;

        public void Update(float delta)
        {
            if (delta == 0)
                return;

            var length = Mathf.Clamp((int)DebugDraw.FPSGraphSize.x, 150, int.MaxValue);
            if (frameTimes.Length != length)
            {
                frameTimes = new float[length];
                frameTimes[0] = delta;
                // loop array
                frameTimes[length - 1] = delta;
                position = 1;
                filled = 1;
            }
            else
            {
                frameTimes[position] = delta;
                position = Mathf.PosMod(position + 1, frameTimes.Length);
                filled = Mathf.Clamp(filled + 1, 0, frameTimes.Length);
            }
        }

        public void Draw(CanvasItem ci, Font font, Vector2 viewportSize)
        {
            var notZero = frameTimes.Where((f) => f > 0f).Select((f) => DebugDraw.FPSGraphFrameTimeMode ? f * 1000 : 1f / f).ToArray();

            // No elements. Leave
            if (notZero.Length == 0)
                return;

            var max = notZero.Max();
            var min = notZero.Min();
            var avg = notZero.Average();

            // Truncate for pixel perfect render
            var graphSize = new Vector2(frameTimes.Length, (int)DebugDraw.FPSGraphSize.y);
            var graphOffset = new Vector2((int)DebugDraw.FPSGraphOffset.x, (int)DebugDraw.FPSGraphOffset.y);
            var pos = graphOffset;

            switch (DebugDraw.FPSGraphPosition)
            {
                case DebugDraw.BlockPosition.LeftTop:
                    break;
                case DebugDraw.BlockPosition.RightTop:
                    pos = new Vector2(viewportSize.x - graphSize.x - graphOffset.x, graphOffset.y);
                    break;
                case DebugDraw.BlockPosition.LeftBottom:
                    pos = new Vector2(graphOffset.x, viewportSize.y - graphSize.y - graphOffset.y);
                    break;
                case DebugDraw.BlockPosition.RightBottom:
                    pos = new Vector2(viewportSize.x - graphSize.x - graphOffset.x, viewportSize.y - graphSize.y - graphOffset.y);
                    break;
            }

            var height_multiplier = graphSize.y / max;
            var center_offset = DebugDraw.FPSGraphCenteredGraphLine ? (graphSize.y - height_multiplier * (max - min)) * 0.5f : 0;
            float get_warped(int idx) => notZero[Mathf.PosMod(idx, notZero.Length)];
            float get_y_pos(int idx) => graphSize.y - get_warped(idx) * height_multiplier + center_offset;

            var start = position - filled;
            var prev = new Vector2(0, get_y_pos(start)) + pos;
            var border_size = new Rect2(pos + Vector2.Up, graphSize + Vector2.Down);

            // Draw background
            ci.DrawRect(border_size, DebugDraw.FPSGraphBackgroundColor, true);

            // Draw framerate graph
            for (int i = 1; i < filled; i++)
            {
                var idx = Mathf.PosMod(start + i, notZero.Length);
                var v = pos + new Vector2(i, (int)get_y_pos(idx));
                ci.DrawLine(v, prev, DebugDraw.FPSGraphLineColor);
                prev = v;
            }

            // Draw border
            ci.DrawRect(border_size, DebugDraw.FPSGraphBorderColor, false);

            // Draw text
            var suffix = (DebugDraw.FPSGraphFrameTimeMode ? "ms" : "fps");

            var min_text = $"min: {min:F1} {suffix}";

            var max_text = $"max: {max:F1} {suffix}";
            var max_height = font.GetHeight();

            var avg_text = $"avg: {avg:F1} {suffix}";
            var avg_height = font.GetHeight();

            // `space` at the end of line for offset from border
            var cur_text = $"{get_warped(position - 1):F1} {suffix} ";
            var cur_size = font.GetStringSize(cur_text);

            if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Max) == DebugDraw.FPSGraphTextFlags.Max)
                ci.DrawString(font, pos + new Vector2(4, max_height - 1),
                    max_text, DebugDraw.FPSGraphTextColor);

            if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Avarage) == DebugDraw.FPSGraphTextFlags.Avarage)
                ci.DrawString(font, pos + new Vector2(4, graphSize.y * 0.5f + avg_height * 0.5f - 2),
                    avg_text, DebugDraw.FPSGraphTextColor);

            if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Min) == DebugDraw.FPSGraphTextFlags.Min)
                ci.DrawString(font, pos + new Vector2(4, graphSize.y - 3),
                    min_text, DebugDraw.FPSGraphTextColor);

            if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Current) == DebugDraw.FPSGraphTextFlags.Current)
                ci.DrawString(font, pos + new Vector2(graphSize.x - cur_size.x, graphSize.y * 0.5f + cur_size.y * 0.5f - 2),
                    cur_text, DebugDraw.FPSGraphTextColor);
        }
    }

    class MultiMeshContainer
    {
        readonly Action<int> addRenderedObjects = null;

        readonly MultiMeshInstance _mmi_cubes = null;
        readonly MultiMeshInstance _mmi_cubes_centered = null;
        readonly MultiMeshInstance _mmi_arrowheads = null;
        readonly MultiMeshInstance _mmi_billboard_squares = null;
        readonly MultiMeshInstance _mmi_positions = null;
        readonly MultiMeshInstance _mmi_spheres = null;
        readonly MultiMeshInstance _mmi_cylinders = null;

        public HashSet<DelayedRendererInstance> Cubes { get => all_mmi_with_values[_mmi_cubes]; }
        public HashSet<DelayedRendererInstance> CubesCentered { get => all_mmi_with_values[_mmi_cubes_centered]; }
        public HashSet<DelayedRendererInstance> Arrowheads { get => all_mmi_with_values[_mmi_arrowheads]; }
        public HashSet<DelayedRendererInstance> BillboardSquares { get => all_mmi_with_values[_mmi_billboard_squares]; }
        public HashSet<DelayedRendererInstance> Positions { get => all_mmi_with_values[_mmi_positions]; }
        public HashSet<DelayedRendererInstance> Spheres { get => all_mmi_with_values[_mmi_spheres]; }
        public HashSet<DelayedRendererInstance> Cylinders { get => all_mmi_with_values[_mmi_cylinders]; }

        readonly Dictionary<MultiMeshInstance, HashSet<DelayedRendererInstance>> all_mmi_with_values =
            new Dictionary<MultiMeshInstance, HashSet<DelayedRendererInstance>>();

        public MultiMeshContainer(Node root, Action<int> onObjectRendered)
        {
            addRenderedObjects = onObjectRendered;

            // Create node with material and MultiMesh. Add to tree. Create array of instances
            _mmi_cubes = CreateMMI(root, nameof(_mmi_cubes));
            _mmi_cubes_centered = CreateMMI(root, nameof(_mmi_cubes_centered));
            _mmi_arrowheads = CreateMMI(root, nameof(_mmi_arrowheads));
            _mmi_billboard_squares = CreateMMI(root, nameof(_mmi_billboard_squares));
            _mmi_positions = CreateMMI(root, nameof(_mmi_positions));
            _mmi_spheres = CreateMMI(root, nameof(_mmi_spheres));
            _mmi_cylinders = CreateMMI(root, nameof(_mmi_cylinders));

            // Customize parameters
            (_mmi_billboard_squares.MaterialOverride as SpatialMaterial).ParamsBillboardMode = SpatialMaterial.BillboardMode.Enabled;
            (_mmi_billboard_squares.MaterialOverride as SpatialMaterial).ParamsBillboardKeepScale = true;

            // Create Meshes
            _mmi_cubes.Multimesh.Mesh = CreateMesh(
                Mesh.PrimitiveType.Lines, DebugDrawImplementation.CubeVertices, DebugDrawImplementation.CubeIndices);

            _mmi_cubes_centered.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
                DebugDrawImplementation.CenteredCubeVertices, DebugDrawImplementation.CubeIndices);

            _mmi_arrowheads.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
                DebugDrawImplementation.ArrowheadVertices, DebugDrawImplementation.ArrowheadIndices);

            _mmi_billboard_squares.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Triangles,
                DebugDrawImplementation.CenteredSquareVertices, DebugDrawImplementation.SquareIndices);

            _mmi_positions.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
                DebugDrawImplementation.PositionVertices, DebugDrawImplementation.PositionIndices);

            _mmi_spheres.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
                DebugDrawImplementation.CreateSphereLines(6, 6, 0.5f, Vector3.Zero));

            _mmi_cylinders.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
                DebugDrawImplementation.CreateCylinderLines(52, 0.5f, 1, Vector3.Zero, 4));
        }

        MultiMeshInstance CreateMMI(Node root, string name)
        {
            var mmi = new MultiMeshInstance()
            {
                Name = name,
                CastShadow = GeometryInstance.ShadowCastingSetting.Off,
                UseInBakedLight = false,

                MaterialOverride = new SpatialMaterial()
                {
                    FlagsUnshaded = true,
                    VertexColorUseAsAlbedo = true
                }
            };
            mmi.Multimesh = new MultiMesh()
            {
                ColorFormat = MultiMesh.ColorFormatEnum.Float,
                CustomDataFormat = MultiMesh.CustomDataFormatEnum.None,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3d,
            };

            root.AddChild(mmi);
            all_mmi_with_values.Add(mmi, new HashSet<DelayedRendererInstance>());
            return mmi;
        }

        ArrayMesh CreateMesh(Mesh.PrimitiveType type, Vector3[] vertices, int[] indices = null, Color[] colors = null)
        {
            var mesh = new ArrayMesh();
            var a = new GDArray();
            a.Resize((int)ArrayMesh.ArrayType.Max);

            a[(int)ArrayMesh.ArrayType.Vertex] = vertices;
            if (indices != null)
                a[(int)ArrayMesh.ArrayType.Index] = indices;
            if (colors != null)
                a[(int)ArrayMesh.ArrayType.Index] = colors;

            mesh.AddSurfaceFromArrays(type, a);

            return mesh;
        }

        public void Deinit()
        {
            all_mmi_with_values.Clear();

            foreach (var p in all_mmi_with_values)
                p.Key?.QueueFree();
        }

        public void ClearInstances()
        {
            foreach (var item in all_mmi_with_values)
                item.Value.Clear();
        }

        public void RemoveExpired(Action<DelayedRendererInstance> returnFunc)
        {
            foreach (var item in all_mmi_with_values)
            {
                item.Value.RemoveWhere((o) =>
                {
                    if (o == null || o.IsExpired())
                    {
                        returnFunc(o);
                        return true;
                    }
                    return false;
                });
            }
        }

        public void UpdateVisibility(Plane[] frustum)
        {
            Parallel.ForEach(all_mmi_with_values, (item) => UpdateVisibilityInternal(item.Value, frustum));
        }

        public void UpdateInstances()
        {
            foreach (var item in all_mmi_with_values)
                UpdateInstancesInternal(item.Key, item.Value);
        }

        public void HideAll()
        {
            foreach (var item in all_mmi_with_values)
                item.Key.Multimesh.VisibleInstanceCount = 0;
        }

        void UpdateInstancesInternal(MultiMeshInstance mmi, HashSet<DelayedRendererInstance> instances)
        {
            if (instances.Count > 0)
            {
                if (mmi.Multimesh.InstanceCount < instances.Count)
                    mmi.Multimesh.InstanceCount = instances.Count;
                mmi.Multimesh.VisibleInstanceCount = instances.Sum((inst) => inst.IsVisible ? 1 : 0);
                addRenderedObjects?.Invoke(mmi.Multimesh.VisibleInstanceCount);

                int i = 0;
                foreach (var d in instances)
                {
                    d.IsUsedOneTime = true;
                    if (d.IsVisible)
                    {
                        mmi.Multimesh.SetInstanceTransform(i, d.InstanceTransform);
                        mmi.Multimesh.SetInstanceColor(i, d.InstanceColor);
                        i++;
                    }
                }
            }
            else
                mmi.Multimesh.VisibleInstanceCount = 0;
        }

        void UpdateVisibilityInternal(HashSet<DelayedRendererInstance> instances, Plane[] frustum)
        {
            foreach (var _mesh in instances)
                _mesh.IsVisible = DebugDrawImplementation.BoundsPartiallyInsideConvexShape(_mesh.Bounds, frustum);
        }
    }

    // https://docs.microsoft.com/en-gb/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
    class ObjectPool<T> where T : class, IPoolable, new()
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
        }

        public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();

        public void Return(T item)
        {
            _objects.Add(item);
            item.Returned();
        }
    }

    interface IPoolable
    {
        void Returned();
    }

    class DebugDrawImplementation : IDisposable
    {
        // 2D

        public Node2D CanvasItemInternal { get; private set; } = null;
        CanvasLayer _canvasLayer = null;
        bool _canvasNeedUpdate = true;
        Font _font = null;

        // fps
        readonly FPSGraph fpsGraph = new FPSGraph();

        // Text
        readonly HashSet<TextGroup> _textGroups = new HashSet<TextGroup>();
        TextGroup _currentTextGroup = null;
        readonly TextGroup _defaultTextGroup = new TextGroup(null, 0, false, DebugDraw.TextForegroundColor);

        // 3D

        #region Predefined Geometry Parts

        public static float CubeDiaganolLengthForSphere = (Vector3.One * 0.5f).Length();

        public static Vector3[] CenteredCubeVertices = new Vector3[]{
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };
        public static Vector3[] CubeVertices = new Vector3[]{
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1)
        };
        public static int[] CubeIndices = new int[] {
            0, 1,
            1, 2,
            2, 3,
            3, 0,

            4, 5,
            5, 6,
            6, 7,
            7, 4,

            0, 4,
            1, 5,
            2, 6,
            3, 7,
        };
        public static int[] CubeWithDiagonalsIndices = new int[] {
            0, 1,
            1, 2,
            2, 3,
            3, 0,

            4, 5,
            5, 6,
            6, 7,
            7, 4,

            0, 4,
            1, 5,
            2, 6,
            3, 7,

            // Diagonals

            // Top Bottom
            1, 3,
            //0, 2,
            4, 6,
            //5, 7,

            // Front Back
            1, 4,
            //0, 5,
            3, 6,
            //2, 7,

            // Left Right
            3, 4,
            //0, 7,
            1, 6,
            //2, 5,
        };
        public static Vector3[] ArrowheadVertices = new Vector3[]
        {
            new Vector3(0, 0, -1),
            new Vector3(0, 0.25f, 0),
            new Vector3(0, -0.25f, 0),
            new Vector3(0.25f, 0, 0),
            new Vector3(-0.25f, 0, 0),
            // Cross to center
            new Vector3(0, 0, -0.2f),
        };
        public static int[] ArrowheadIndices = new int[]
        {
            0, 1,
            0, 2,
            0, 3,
            0, 4,
            // Cross
            //1, 2,
            //3, 4,
            // Or Cross to center
            5, 1,
            5, 2,
            5, 3,
            5, 4,
        };
        public static Vector3[] CenteredSquareVertices = new Vector3[]
        {
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
        };
        public static int[] SquareIndices = new int[]
        {
            0, 1, 2,
            2, 3, 0,
        };
        public static Vector3[] PositionVertices = new Vector3[]
        {
            new Vector3(0.5f, 0, 0),
            new Vector3(-0.5f, 0, 0),
            new Vector3(0, 0.5f, 0),
            new Vector3(0, -0.5f, 0),
            new Vector3(0, 0, 0.5f),
            new Vector3(0, 0, -0.5f),
        };
        public static int[] PositionIndices = new int[]
        {
            0, 1,
            2, 3,
            4, 5,
        };

        #endregion

        ImmediateGeometry _immediateGeometry = null;
        MultiMeshContainer _mmc = null;
        readonly HashSet<DelayedRendererLine> _wireMeshes = new HashSet<DelayedRendererLine>();
        readonly ObjectPool<DelayedRendererLine> _poolWiredRenderers = null;
        readonly ObjectPool<DelayedRendererInstance> _poolInstanceRenderers = null;
        public int renderInstances = 0;
        public int renderWireframes = 0;

        // Misc

        readonly object dataLock = new object();
        readonly DebugDraw debugDraw = null;
        bool isReady = false;

        CanvasItem _customCanvas = null;
        public CanvasItem CustomCanvas
        {
            get => _customCanvas;
            set
            {
                var connected_internal = CanvasItemInternal.IsConnected("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));
                var connected_custom = _customCanvas != null && _customCanvas.IsConnected("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));

                if (value == null)
                {
                    if (!connected_internal)
                        CanvasItemInternal.Connect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw), new GDArray { CanvasItemInternal });
                    if (connected_custom)
                        _customCanvas?.Disconnect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));
                }
                else

                {
                    if (connected_internal)
                        CanvasItemInternal.Disconnect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));
                    if (!connected_custom)
                        value.Connect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw), new GDArray { value });
                }
                _customCanvas = value;
            }
        }

        public DebugDrawImplementation(DebugDraw dd)
        {
            debugDraw = dd;

            _poolWiredRenderers = new ObjectPool<DelayedRendererLine>(() => new DelayedRendererLine());
            _poolInstanceRenderers = new ObjectPool<DelayedRendererInstance>(() => new DelayedRendererInstance());
        }

        /// <summary>
        /// Must be called only once be DebugDraw class
        /// </summary>
        public void Ready()
        {
            if (!isReady)
                isReady = true;

            // Funny hack to get default font
            var c = new Control();
            debugDraw.AddChild(c);
            _font = c.GetFont("font");
            c.QueueFree();

            // Setup default text group
            EndTextGroupInternal();

            // Create wireframe mesh drawer
            _immediateGeometry = new ImmediateGeometry()
            {
                Name = nameof(_immediateGeometry),
                CastShadow = GeometryInstance.ShadowCastingSetting.Off,
                UseInBakedLight = false,

                MaterialOverride = new SpatialMaterial()
                {
                    FlagsUnshaded = true,
                    VertexColorUseAsAlbedo = true
                }
            };
            debugDraw.AddChild(_immediateGeometry);
            // Create MultiMeshInstance instances..
            _mmc = new MultiMeshContainer(debugDraw, (i) => renderInstances += i);

            // Create canvas item and canvas layer
            _canvasLayer = new CanvasLayer() { Layer = 64 };
            CanvasItemInternal = new Node2D();

            if (CustomCanvas == null)
                CanvasItemInternal.Connect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw), new GDArray { CanvasItemInternal });

            debugDraw.AddChild(_canvasLayer);
            _canvasLayer.AddChild(CanvasItemInternal);
        }

        public void Dispose()
        {
            FinalizedClearAll();
        }

        void FinalizedClearAll()
        {
            lock (dataLock)
            {
                _textGroups.Clear();
                _wireMeshes.Clear();
                _mmc?.Deinit();
                _mmc = null;
            }

            _font?.Dispose();
            _font = null;

            if (CanvasItemInternal != null && CanvasItemInternal.IsConnected("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw)))
                CanvasItemInternal.Disconnect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));
            if (_customCanvas != null && _customCanvas.IsConnected("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw)))
                _customCanvas.Disconnect("draw", debugDraw, nameof(DebugDraw.OnCanvaItemDraw));

            CanvasItemInternal?.QueueFree();
            CanvasItemInternal = null;

            _canvasLayer?.QueueFree();
            _canvasLayer = null;

            _immediateGeometry?.QueueFree();
            _immediateGeometry = null;

            // Clear editor canvas
            CustomCanvas?.Update();
        }

        public void Update(float delta)
        {
            lock (dataLock)
            {
                // Clean texts
                _textGroups.RemoveWhere((g) => g.Texts.Count == 0);
                foreach (var g in _textGroups) g.CleanTexts(() => UpdateCanvas());

                // Clean lines
                _wireMeshes.RemoveWhere((o) =>
                {
                    if (o == null || o.IsExpired())
                    {
                        _poolWiredRenderers.Return(o);
                        return true;
                    }
                    return false;
                });

                // Clean instances
                _mmc.RemoveExpired((o) => _poolInstanceRenderers.Return(o));
            }

            // FPS Graph
            fpsGraph.Update(delta);

            // Update overlay
            if (_canvasNeedUpdate || DebugDraw.FPSGraphEnabled)
            {
                if (CustomCanvas == null)
                    CanvasItemInternal.Update();
                else
                    CustomCanvas.Update();

                // reset some values
                _canvasNeedUpdate = false;
                EndTextGroupInternal();
            }

            // Update 3D debug
            UpdateDebugGeometry();
        }

        void UpdateDebugGeometry()
        {
            // Don't clear geometry for debug this debug class
            if (DebugDraw.Freeze3DRender)
                return;

            // Clear first and then leave
            _immediateGeometry.Clear();

            renderInstances = 0;
            renderWireframes = 0;

            // Return if nothing to do
            if (!DebugDraw.DebugEnabled)
            {
                lock (dataLock)
                    _mmc?.HideAll();
                return;
            }

            // Get camera frustum
            var frustum_array = DebugDraw.CustomViewport == null || DebugDraw.ForceUseCameraFromScene ?
                debugDraw.GetViewport().GetCamera()?.GetFrustum() :
                DebugDraw.CustomViewport.GetCamera().GetFrustum();

            // Convert frustum to C# array
            Plane[] f = null;
            if (frustum_array != null)
            {
                f = new Plane[frustum_array.Count];
                for (int i = 0; i < frustum_array.Count; i++)
                    f[i] = ((Plane)frustum_array[i]);
            }

            // Check visibility of all objects

            lock (dataLock)
            {
                // Update visibility
                if (DebugDraw.UseFrustumCulling && f != null)
                {
                    // Update immediate geometry
                    foreach (var _lines in _wireMeshes)
                        _lines.IsVisible = BoundsPartiallyInsideConvexShape(_lines.Bounds, f);
                    // Update meshes
                    _mmc.UpdateVisibility(f);
                }

                _immediateGeometry.Begin(Mesh.PrimitiveType.Lines);
                // Line drawing much faster with only one Begin/End call
                foreach (var m in _wireMeshes)
                {
                    m.IsUsedOneTime = true;

                    if (m.IsVisible)
                    {
                        renderWireframes++;
                        _immediateGeometry.SetColor(m.LinesColor);
                        foreach (var l in m.Lines)
                        {
                            _immediateGeometry.AddVertex(l);
                        };
                    }
                }

                _immediateGeometry.End();

                {   // Debug bounds
                    //_immediateGeometry.Begin(Mesh.PrimitiveType.Lines); foreach (var l in _wire_meshes) __DrawDebugBoundsForDebugLinePrimitives(l); _immediateGeometry.End();
                    //foreach (var l in _mmc.Cubes.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.CubesCentered.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.BillboardSquares.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.Arrowheads.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.Positions.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.Spheres.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                    //foreach (var l in _mmc.Cylinders.ToArray()) DrawDebugBoundsForDebugInstancePrimitives(l);
                }

                // Update MultiMeshInstances
                _mmc.UpdateInstances();
            }
        }

        public void OnCanvaItemDraw(CanvasItem ci)
        {
            if (!DebugDraw.DebugEnabled)
                return;

            var time = DateTime.Now;
            Vector2 vp_size = ci.HasMeta("UseParentSize") ? ci.GetParent<Control>().RectSize : ci.GetViewportRect().Size;

            lock (dataLock)
            { // Text drawing
                var count = _textGroups.Sum((g) => g.Texts.Count + (g.ShowTitle ? 1 : 0));

                const string separator = " : ";

                Vector2 ascent = new Vector2(0, _font.GetAscent());
                Vector2 font_offset = ascent + DebugDraw.TextPadding;
                float line_height = _font.GetHeight() + DebugDraw.TextPadding.y * 2;
                Vector2 pos = Vector2.Zero;
                float size_mul = 0;

                switch (DebugDraw.TextBlockPosition)
                {
                    case DebugDraw.BlockPosition.LeftTop:
                        pos = DebugDraw.TextBlockOffset;
                        size_mul = 0;
                        break;
                    case DebugDraw.BlockPosition.RightTop:
                        pos = new Vector2(
                            vp_size.x - DebugDraw.TextBlockOffset.x,
                            DebugDraw.TextBlockOffset.y);
                        size_mul = -1;
                        break;
                    case DebugDraw.BlockPosition.LeftBottom:
                        pos = new Vector2(
                            DebugDraw.TextBlockOffset.x,
                            vp_size.y - DebugDraw.TextBlockOffset.y - line_height * count);
                        size_mul = 0;
                        break;
                    case DebugDraw.BlockPosition.RightBottom:
                        pos = new Vector2(
                            vp_size.x - DebugDraw.TextBlockOffset.x,
                            vp_size.y - DebugDraw.TextBlockOffset.y - line_height * count);
                        size_mul = -1;
                        break;
                }

                foreach (var g in _textGroups.OrderBy(g => g.GroupPriority))
                {
                    var a = g.Texts.OrderBy(t => t.Value.Priority).ThenBy(t => t.Key);

                    foreach (var t in g.ShowTitle ? a.Prepend(new KeyValuePair<string, DelayedText>(g.Title ?? "", null)) : a)
                    {
                        var keyText = t.Key ?? "";
                        var text = t.Value?.Text == null ? keyText : $"{keyText}{separator}{t.Value.Text}";
                        var size = _font.GetStringSize(text);
                        float size_right_revert = (size.x + DebugDraw.TextPadding.x * 2) * size_mul;
                        ci.DrawRect(
                            new Rect2(new Vector2(pos.x + size_right_revert, pos.y),
                            new Vector2(size.x + DebugDraw.TextPadding.x * 2, line_height)),
                            DebugDraw.TextBackgroundColor);

                        // Draw colored string
                        if (t.Value == null || t.Value.ValueColor == null || t.Value.Text == null)
                        {
                            ci.DrawString(_font, new Vector2(pos.x + font_offset.x + size_right_revert, pos.y + font_offset.y), text, g.GroupColor);
                        }
                        else
                        {
                            var textSep = $"{keyText}{separator}";
                            var _keyLength = textSep.Length;
                            ci.DrawString(_font,
                                new Vector2(pos.x + font_offset.x + size_right_revert, pos.y + font_offset.y),
                                text.Substring(0, _keyLength), g.GroupColor);
                            ci.DrawString(_font,
                                new Vector2(pos.x + font_offset.x + size_right_revert + _font.GetStringSize(textSep).x, pos.y + font_offset.y),
                                text.Substring(_keyLength), t.Value.ValueColor);
                        }
                        pos.y += line_height;
                    }
                }
            }

            if (DebugDraw.FPSGraphEnabled)
                fpsGraph.Draw(ci, _font, vp_size);
        }

        void UpdateCanvas()
        {
            _canvasNeedUpdate = true;
        }

        #region Local Draw Functions

        public void Clear3DObjectsInternal()
        {
            lock (dataLock)
            {
                _wireMeshes.Clear();
                _mmc?.ClearInstances();
            }
        }

        public void Clear2DObjectsInternal()
        {
            lock (dataLock)
            {
                _textGroups.Clear();
                UpdateCanvas();
            }
        }

        public void ClearAllInternal()
        {
            Clear2DObjectsInternal();
            Clear3DObjectsInternal();
        }

        #region 3D

        #region Spheres

        public void DrawSphereInternal(ref Vector3 position, float radius, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var t = Transform.Identity;
            t.origin = position;
            t.basis.Scale = Vector3.One * (radius * 2);

            DrawSphereInternal(ref t, ref color, duration);
        }

        public void DrawSphereInternal(ref Transform transform, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = transform;
                inst.InstanceColor = color ?? Colors.Chartreuse;
                inst.Bounds.Position = transform.origin; inst.Bounds.Radius = transform.basis.Scale.Length() * 0.5f;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _mmc?.Spheres.Add(inst);
            }
        }

        #endregion // Spheres

        #region Cylinders

        public void DrawCylinderInternal(ref Vector3 position, float radius, float height, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var t = Transform.Identity;
            t.origin = position;
            t.basis.Scale = new Vector3(radius * 2, height, radius * 2);

            DrawCylinderInternal(ref t, ref color, duration);
        }

        public void DrawCylinderInternal(ref Transform transform, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = transform;
                inst.InstanceColor = color ?? Colors.Yellow;
                inst.Bounds.Position = transform.origin; inst.Bounds.Radius = transform.basis.Scale.Length() * 0.5f;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _mmc?.Cylinders.Add(inst);
            }
        }

        #endregion // Cylinders

        #region Boxes

        public void DrawBoxInternal(ref Vector3 position, ref Vector3 size, ref Color? color, float duration, bool isBoxCentered)
        {
            if (!DebugDraw.DebugEnabled) return;

            var q = Quat.Identity;
            DrawBoxInternal(ref position, ref q, ref size, ref color, duration, isBoxCentered);
        }

        public void DrawBoxInternal(ref Vector3 position, ref Quat rotation, ref Vector3 size, ref Color? color, float duration, bool isBoxCentered)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var t = new Transform(rotation, position);
                t.basis.Scale = size;
                var radius = size.Length() * 0.5f;

                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = t;
                inst.InstanceColor = color ?? Colors.ForestGreen;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
                inst.Bounds.Radius = radius;

                if (isBoxCentered)
                    inst.Bounds.Position = t.origin;
                else
                    inst.Bounds.Position = t.origin + size * 0.5f;

                if (isBoxCentered)
                    _mmc?.CubesCentered.Add(inst);
                else
                    _mmc?.Cubes.Add(inst);
            }
        }

        public void DrawBoxInternal(ref Transform transform, ref Color? color, float duration, bool isBoxCentered)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var radius = transform.basis.Scale.Length() * 0.5f;

                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = transform;
                inst.InstanceColor = color ?? Colors.ForestGreen;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
                inst.Bounds.Radius = radius;

                if (isBoxCentered)
                    inst.Bounds.Position = transform.origin;
                else
                    inst.Bounds.Position = transform.origin + transform.basis.Scale * 0.5f;

                if (isBoxCentered)
                    _mmc?.CubesCentered.Add(inst);
                else
                    _mmc?.Cubes.Add(inst);
            }
        }

        public void DrawAABBInternal(ref AABB box, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;
            GetDiagonalVectors(box.Position, box.End, out Vector3 bottom, out _, out Vector3 diag);
            DrawBoxInternal(ref bottom, ref diag, ref color, duration, false);
        }

        public void DrawAABBInternal(ref Vector3 a, ref Vector3 b, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;
            GetDiagonalVectors(a, b, out Vector3 bottom, out _, out Vector3 diag);
            DrawBoxInternal(ref bottom, ref diag, ref color, duration, false);
        }

        #endregion // Boxes

        #region Lines

        public void DrawLine3DHitInternal(ref Vector3 a, ref Vector3 b, bool isHit, float unitOffsetOfHit, float hitSize, float duration, ref Color? hitColor, ref Color? afterHitColor)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                if (isHit && unitOffsetOfHit >= 0 && unitOffsetOfHit <= 1.0f)
                {
                    var time = DateTime.Now + TimeSpan.FromSeconds(duration);
                    var hit_pos = (b - a).Normalized() * a.DistanceTo(b) * unitOffsetOfHit + a;

                    // Get lines from pool and setup
                    var line_a = _poolWiredRenderers.Get();
                    var line_b = _poolWiredRenderers.Get();

                    line_a.Lines = new Vector3[] { a, hit_pos };
                    line_a.LinesColor = hitColor ?? DebugDraw.LineHitColor;
                    line_a.ExpirationTime = time;

                    line_b.Lines = new Vector3[] { hit_pos, b };
                    line_b.LinesColor = afterHitColor ?? DebugDraw.LineAfterHitColor;
                    line_b.ExpirationTime = time;

                    _wireMeshes.Add(line_a);
                    _wireMeshes.Add(line_b);

                    // Get instance from pool and setup
                    var t = new Transform(Basis.Identity, hit_pos);
                    t.basis.Scale = Vector3.One * hitSize;

                    var inst = _poolInstanceRenderers.Get();
                    inst.InstanceTransform = t;
                    inst.InstanceColor = hitColor ?? DebugDraw.LineHitColor;
                    inst.Bounds.Position = t.origin; inst.Bounds.Radius = CubeDiaganolLengthForSphere * hitSize;
                    inst.ExpirationTime = time;

                    _mmc?.BillboardSquares.Add(inst);
                }
                else
                {
                    var line = _poolWiredRenderers.Get();

                    line.Lines = new Vector3[] { a, b };
                    line.LinesColor = hitColor ?? DebugDraw.LineHitColor;
                    line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                    _wireMeshes.Add(line);
                }
            }
        }

        #region Normal

        public void DrawLine3DInternal(ref Vector3 a, ref Vector3 b, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var line = _poolWiredRenderers.Get();

                line.Lines = new Vector3[] { a, b };
                line.LinesColor = color ?? Colors.LightGreen;
                line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _wireMeshes.Add(line);
            }
        }

        public void DrawRay3DInternal(Vector3 origin, Vector3 direction, float length, Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var end = origin + direction * length;
            DrawLine3DInternal(ref origin, ref end, ref color, duration);
        }

        public void DrawLinePath3DInternal(IList<Vector3> path, Color? color, float duration = 0f)
        {
            if (!DebugDraw.DebugEnabled) return;

            if (path == null || path.Count <= 2) return;

            lock (dataLock)
            {
                var line = _poolWiredRenderers.Get();

                line.Lines = CreateLinesFromPath(path);
                line.LinesColor = color ?? Colors.LightGreen;
                line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _wireMeshes.Add(line);
            }
        }

        public void DrawLinePath3DInternal(Color? color, float duration, params Vector3[] path)
        {
            if (!DebugDraw.DebugEnabled) return;

            DrawLinePath3DInternal(path, color, duration);
        }

        #endregion // Normal

        #region Arrows

        public void DrawArrowLine3DInternal(Vector3 a, Vector3 b, Color? color, float duration, float arrowSize, bool absoluteSize)
        {
            if (!DebugDraw.DebugEnabled) return;

            var line = _poolWiredRenderers.Get();

            line.Lines = new Vector3[] { a, b };
            line.LinesColor = color ?? Colors.LightGreen;
            line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

            _wireMeshes.Add(line);

            GenerateArrowheadInstance(ref a, ref b, ref color, ref duration, ref arrowSize, ref absoluteSize);
        }

        public void DrawArrowRay3DInternal(Vector3 origin, Vector3 direction, float length, Color? color, float duration, float arrowSize, bool absoluteSize)
        {
            if (!DebugDraw.DebugEnabled) return;

            DrawArrowLine3DInternal(origin, origin + direction * length, color, duration, arrowSize, absoluteSize);
        }

        public void DrawArrowPath3DInternal(IList<Vector3> path, ref Color? color, float duration, float arrowSize, bool absoluteSize)
        {
            if (!DebugDraw.DebugEnabled) return;

            if (path == null || path.Count < 2) return;

            var line = _poolWiredRenderers.Get();
            line.Lines = CreateLinesFromPath(path);
            line.LinesColor = color ?? Colors.LightGreen;
            line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
            _wireMeshes.Add(line);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i], b = path[i + 1];
                GenerateArrowheadInstance(ref a, ref b, ref color, ref duration, ref arrowSize, ref absoluteSize);
            }
        }

        public void DrawArrowPath3DInternal(ref Color? color, float duration, float arrowSize, bool absoluteSize, params Vector3[] path)
        {
            if (!DebugDraw.DebugEnabled) return;

            DrawArrowPath3DInternal(path, ref color, duration, arrowSize, absoluteSize);
        }

        #endregion // Arrows
        #endregion // Lines

        #region Misc

        public void DrawBillboardSquareInternal(ref Vector3 position, float size, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var t = Transform.Identity;
                t.origin = position;
                t.basis.Scale = Vector3.One * size;

                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = t;
                inst.InstanceColor = color ?? Colors.Red;
                inst.Bounds.Position = t.origin; inst.Bounds.Radius = CubeDiaganolLengthForSphere * size;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _mmc?.BillboardSquares.Add(inst);
            }
        }

        #region Camera Frustum

        public void DrawCameraFrustumInternal(ref Camera camera, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;
            if (camera == null) return;

            var f = camera.GetFrustum();
            DrawCameraFrustumInternal(ref f, ref color, duration);
        }

        public void DrawCameraFrustumInternal(ref GDArray cameraFrustum, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;
            if (cameraFrustum.Count != 6) return;

            Plane[] f = new Plane[cameraFrustum.Count];
            for (int i = 0; i < cameraFrustum.Count; i++)
                f[i] = ((Plane)cameraFrustum[i]);

            DrawCameraFrustumInternal(ref f, ref color, duration);
        }

        public void DrawCameraFrustumInternal(ref Plane[] planes, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;
            if (planes.Length != 6) return;

            lock (dataLock)
            {
                var line = _poolWiredRenderers.Get();

                line.Lines = CreateCameraFrustumLines(planes);
                line.LinesColor = color ?? Colors.DarkSalmon;
                line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _wireMeshes.Add(line);
            }
        }

        #endregion // Camera frustum

        public void DrawPosition3DInternal(ref Transform transform, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            lock (dataLock)
            {
                var s = transform.basis.Scale;

                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = transform;
                inst.InstanceColor = color ?? Colors.Crimson;
                inst.Bounds.Position = transform.origin; inst.Bounds.Radius = GetMaxValue(ref s) * 0.5f;
                inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

                _mmc?.Positions.Add(inst);
            }
        }

        public void DrawPosition3DInternal(ref Vector3 position, ref Quat rotation, ref Vector3 scale, ref Color? color, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var t = new Transform(new Basis(rotation), position);
            t.basis.Scale = scale;

            DrawPosition3DInternal(ref t, ref color, duration);
        }

        public void DrawPosition3DInternal(ref Vector3 position, ref Color? color, float scale, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var t = new Transform(Basis.Identity, position);
            t.basis.Scale = Vector3.One * scale;

            DrawPosition3DInternal(ref t, ref color, duration);
        }

        #endregion // Misc
        #endregion // 3D

        #region 2D

        public void BeginTextGroupInternal(string groupTitle, int groupPriority, ref Color? groupColor, bool showTitle)
        {
            lock (dataLock)
            {
                var newGroup = _textGroups.FirstOrDefault(g => g.Title == groupTitle);
                if (newGroup != null)
                {
                    newGroup.ShowTitle = showTitle;
                    newGroup.GroupPriority = groupPriority;
                    newGroup.GroupColor = groupColor ?? DebugDraw.TextForegroundColor;
                }
                else
                {
                    newGroup = new TextGroup(groupTitle, groupPriority, showTitle, groupColor ?? DebugDraw.TextForegroundColor);
                    _textGroups.Add(newGroup);
                }
                _currentTextGroup = newGroup;
            }
        }

        public void EndTextGroupInternal()
        {
            lock (dataLock)
            {
                if (!_textGroups.Contains(_defaultTextGroup))
                    _textGroups.Add(_defaultTextGroup);
                _currentTextGroup = _defaultTextGroup;

                // Update color 
                _defaultTextGroup.GroupColor = DebugDraw.TextForegroundColor;
            }
        }

        public void SetTextIntenal(ref string key, ref object value, int priority, ref Color? colorOfValue, float duration)
        {
            if (!DebugDraw.DebugEnabled) return;

            var _newTime = DateTime.Now + (duration < 0 ? DebugDraw.TextDefaultDuration : TimeSpan.FromSeconds(duration));
            var _strVal = value?.ToString();

            lock (dataLock)
            {
                if (_currentTextGroup.Texts.ContainsKey(key))
                {
                    var t = _currentTextGroup.Texts[key];
                    if (_strVal != t.Text)
                        UpdateCanvas();
                    t.Text = _strVal;
                    t.Priority = priority;
                    t.ExpirationTime = _newTime;
                    t.ValueColor = colorOfValue;
                }
                else
                {
                    _currentTextGroup.Texts[key] = new DelayedText(_newTime, _strVal, priority, colorOfValue);
                    UpdateCanvas();
                }
            }
        }

        #endregion // 2D
        #endregion

        #region Utilities

        void DrawDebugBoundsForDebugLinePrimitives(DelayedRendererLine dr)
        {
            if (!dr.IsVisible)
                return;

            var _lines = CreateCubeLines(dr.Bounds.Position, Quat.Identity, dr.Bounds.Size, false, true);

            renderWireframes++;
            _immediateGeometry.SetColor(Colors.Orange);
            foreach (var l in _lines)
            {
                _immediateGeometry.AddVertex(l);
            };
        }

        void DrawDebugBoundsForDebugInstancePrimitives(DelayedRendererInstance dr)
        {
            if (!dr.IsVisible)
                return;

            renderInstances++;
            var p = dr.Bounds.Position;
            var r = dr.Bounds.Radius;
            Color? c = Colors.DarkOrange;
            DrawSphereInternal(ref p, r, ref c, 0);
        }

        void GenerateArrowheadInstance(ref Vector3 a, ref Vector3 b, ref Color? color, ref float duration, ref float arrowSize, ref bool absoluteSize)
        {
            lock (dataLock)
            {
                var offset = (b - a);
                var length = (absoluteSize ? arrowSize : offset.Length() * arrowSize);

                var t = new Transform(Basis.Identity, b - offset.Normalized() * length).LookingAt(b, Vector3.Up);
                t.basis.Scale = Vector3.One * length;
                var time = DateTime.Now + TimeSpan.FromSeconds(duration);

                var inst = _poolInstanceRenderers.Get();
                inst.InstanceTransform = t;
                inst.InstanceColor = color ?? Colors.LightGreen;
                inst.Bounds.Position = t.origin - t.basis.z * 0.5f; inst.Bounds.Radius = CubeDiaganolLengthForSphere * length;
                inst.ExpirationTime = time;

                _mmc?.Arrowheads.Add(inst);
            }
        }

        // Broken converter from Transform and Color to raw float[]
        static float[] GetRawMultiMeshTransforms(ISet<DelayedRendererInstance> instances)
        {
            float[] res = new float[instances.Count * 16];
            int index = 0;

            foreach (var i in instances)
            {
                i.IsUsedOneTime = true; // needed for proper clear
                int idx = index;
                index += 16;

                res[idx + 0] = i.InstanceTransform.basis.Row0.x; res[idx + 1] = i.InstanceTransform.basis.Row0.y;
                res[idx + 2] = i.InstanceTransform.basis.Row0.z; res[idx + 3] = i.InstanceTransform.basis.Row1.x;
                res[idx + 4] = i.InstanceTransform.basis.Row1.y; res[idx + 5] = i.InstanceTransform.basis.Row1.z;
                res[idx + 6] = i.InstanceTransform.basis.Row2.x; res[idx + 7] = i.InstanceTransform.basis.Row2.y;
                res[idx + 8] = i.InstanceTransform.basis.Row2.z; res[idx + 9] = i.InstanceTransform.origin.x;
                res[idx + 10] = i.InstanceTransform.origin.y; res[idx + 11] = i.InstanceTransform.origin.z;
                res[idx + 12] = i.InstanceColor.r; res[idx + 13] = i.InstanceColor.g;
                res[idx + 14] = i.InstanceColor.b; res[idx + 15] = i.InstanceColor.a;
            }

            return res;
        }

        #region Geometry Generation

        public static Vector3[] CreateCameraFrustumLines(Plane[] frustum)
        {
            if (frustum.Length != 6)
                return Array.Empty<Vector3>();

            Vector3[] res = new Vector3[CubeIndices.Length];

            //  near, far, left, top, right, bottom
            //  0,    1,   2,    3,   4,     5
            var cube = new Vector3[]{
            frustum[0].Intersect3(frustum[3], frustum[2]).Value,
            frustum[0].Intersect3(frustum[3], frustum[4]).Value,
            frustum[0].Intersect3(frustum[5], frustum[4]).Value,
            frustum[0].Intersect3(frustum[5], frustum[2]).Value,

            frustum[1].Intersect3(frustum[3], frustum[2]).Value,
            frustum[1].Intersect3(frustum[3], frustum[4]).Value,
            frustum[1].Intersect3(frustum[5], frustum[4]).Value,
            frustum[1].Intersect3(frustum[5], frustum[2]).Value,
        };

            for (int i = 0; i < res.Length; i++) res[i] = cube[CubeIndices[i]];

            return res;
        }

        public static Vector3[] CreateCubeLines(Vector3 position, Quat rotation, Vector3 size, bool centeredBox = true, bool withDiagonals = false)
        {
            Vector3[] scaled = new Vector3[8];
            Vector3[] res = new Vector3[withDiagonals ? CubeWithDiagonalsIndices.Length : CubeIndices.Length];

            bool dont_rot = rotation == Quat.Identity;

            Func<int, Vector3> get;
            if (centeredBox)
            {
                if (dont_rot)
                    get = (idx) => CenteredCubeVertices[idx] * size + position;
                else
                    get = (idx) => rotation.Xform(CenteredCubeVertices[idx] * size) + position;
            }
            else
            {
                if (dont_rot)
                    get = (idx) => CubeVertices[idx] * size + position;
                else
                    get = (idx) => rotation.Xform(CubeVertices[idx] * size) + position;
            }

            for (int i = 0; i < 8; i++)
                scaled[i] = get(i);

            if (withDiagonals)
                for (int i = 0; i < res.Length; i++) res[i] = scaled[CubeWithDiagonalsIndices[i]];
            else
                for (int i = 0; i < res.Length; i++) res[i] = scaled[CubeIndices[i]];

            return res;
        }

        public static Vector3[] CreateSphereLines(int lats, int lons, float radius, Vector3 position)
        {
            if (lats < 2)
                lats = 2;
            if (lons < 4)
                lons = 4;

            Vector3[] res = new Vector3[lats * lons * 6];
            int total = 0;
            for (int i = 1; i <= lats; i++)
            {
                float lat0 = Mathf.Pi * (-0.5f + (float)(i - 1) / lats);
                float z0 = Mathf.Sin(lat0);
                float zr0 = Mathf.Cos(lat0);

                float lat1 = Mathf.Pi * (-0.5f + (float)i / lats);
                float z1 = Mathf.Sin(lat1);
                float zr1 = Mathf.Cos(lat1);

                for (int j = lons; j >= 1; j--)
                {
                    float lng0 = 2 * Mathf.Pi * (j - 1) / lons;
                    float x0 = Mathf.Cos(lng0);
                    float y0 = Mathf.Sin(lng0);

                    float lng1 = 2 * Mathf.Pi * j / lons;
                    float x1 = Mathf.Cos(lng1);
                    float y1 = Mathf.Sin(lng1);

                    Vector3[] v = new Vector3[]{
                    new Vector3(x1 * zr0, z0, y1 * zr0) * radius + position,
                    new Vector3(x1 * zr1, z1, y1 * zr1) * radius + position,
                    new Vector3(x0 * zr1, z1, y0 * zr1) * radius + position,
                    new Vector3(x0 * zr0, z0, y0 * zr0) * radius + position
                };

                    res[total++] = v[0];
                    res[total++] = v[1];
                    res[total++] = v[2];

                    res[total++] = v[2];
                    res[total++] = v[3];
                    res[total++] = v[0];
                }
            }
            return res;
        }

        public static Vector3[] CreateCylinderLines(int edges, float radius, float height, Vector3 position, int drawEdgeEachNStep = 1)
        {
            var angle = 360f / edges;

            List<Vector3> points = new List<Vector3>();

            Vector3 d = new Vector3(0, height * 0.5f, 0);
            for (int i = 0; i < edges; i++)
            {
                float ra = Mathf.Deg2Rad(i * angle);
                float rb = Mathf.Deg2Rad((i + 1) * angle);
                Vector3 a = new Vector3(Mathf.Sin(ra), 0, Mathf.Cos(ra)) * radius + position;
                Vector3 b = new Vector3(Mathf.Sin(rb), 0, Mathf.Cos(rb)) * radius + position;

                // Top
                points.Add(a + d);
                points.Add(b + d);

                // Bottom
                points.Add(a - d);
                points.Add(b - d);

                // Edge
                if (i % drawEdgeEachNStep == 0)
                {
                    points.Add(a + d);
                    points.Add(a - d);
                }
            }

            return points.ToArray();
        }

        public static Vector3[] CreateLinesFromPath(IList<Vector3> path)
        {
            var res = new Vector3[(path.Count - 1) * 2];

            for (int i = 1; i < path.Count - 1; i++)
            {
                res[i * 2] = path[i];
                res[i * 2 + 1] = path[i + 1];
            }
            return res;
        }

        #endregion // Geometry Generation

        public static void GetDiagonalVectors(Vector3 a, Vector3 b, out Vector3 bottom, out Vector3 top, out Vector3 diag)
        {
            bottom = Vector3.Zero;
            top = Vector3.Zero;

            if (a.x > b.x)
            {
                top.x = a.x;
                bottom.x = b.x;
            }
            else
            {
                top.x = b.x;
                bottom.x = a.x;
            }

            if (a.y > b.y)
            {
                top.y = a.y;
                bottom.y = b.y;
            }
            else
            {
                top.y = b.y;
                bottom.y = a.y;
            }

            if (a.z > b.z)
            {
                top.z = a.z;
                bottom.z = b.z;
            }
            else
            {
                top.z = b.z;
                bottom.z = a.z;
            }

            diag = top - bottom;
        }

        public static bool BoundsPartiallyInsideConvexShape(AABB bounds, IList<Plane> planes)
        {
            var extent = bounds.Size * 0.5f;
            var center = bounds.Position + extent;
            foreach (var p in planes)
                //if ((center - extent * p.Normal.Sign()).Dot(p.Normal) > p.D) //little slower i think
                if (new Vector3(
                        center.x - extent.x * Math.Sign(p.Normal.x),
                        center.y - extent.y * Math.Sign(p.Normal.y),
                        center.z - extent.z * Math.Sign(p.Normal.z)
                        ).Dot(p.Normal) > p.D)
                    return false;

            return true;
        }

        public static bool BoundsPartiallyInsideConvexShape(SphereBounds sphere, IList<Plane> planes)
        {
            foreach (var p in planes)
                if (p.DistanceTo(sphere.Position) >= sphere.Radius)
                    return false;

            return true;
        }

        static float GetMaxValue(ref Vector3 value)
        {
            return Math.Max(Math.Abs(value.x), Math.Max(Math.Abs(value.y), Math.Abs(value.z)));
        }

        #endregion // Utilities

    }
}
#endif // DebugDrawImplementation
