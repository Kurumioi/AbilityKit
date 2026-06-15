namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Draws text and simple primitives for demo console presentations.
/// </summary>
public interface IConsoleRenderer
{
    /// <summary>Gets renderer width.</summary>
    int Width { get; }

    /// <summary>Gets renderer height.</summary>
    int Height { get; }

    /// <summary>Clears the renderer buffer or output surface.</summary>
    void Clear();

    /// <summary>Draws text into the renderer buffer.</summary>
    void DrawText(int x, int y, string text);

    /// <summary>Draws a rectangle border.</summary>
    void DrawRect(int x, int y, int width, int height);

    /// <summary>Draws a line.</summary>
    void DrawLine(int x1, int y1, int x2, int y2);

    /// <summary>Presents buffered content.</summary>
    void Present();

    /// <summary>Maps world coordinates to screen coordinates.</summary>
    (int px, int py) WorldToScreen(float worldX, float worldY);
}
