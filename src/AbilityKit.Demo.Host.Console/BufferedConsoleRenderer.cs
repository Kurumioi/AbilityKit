using System.Text;

namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Buffered text renderer for console demo presentations.
/// </summary>
public sealed class BufferedConsoleRenderer : IConsoleRenderer
{
    private readonly IConsoleOutput _output;
    private readonly char[,] _buffer;
    private readonly float _worldMinX;
    private readonly float _worldMaxX;
    private readonly float _worldMinY;
    private readonly float _worldMaxY;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedConsoleRenderer"/> class.
    /// </summary>
    public BufferedConsoleRenderer(
        IConsoleOutput output,
        int width = 80,
        int height = 40,
        float worldMinX = -50f,
        float worldMaxX = 50f,
        float worldMinY = -50f,
        float worldMaxY = 50f)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        Width = Math.Max(8, width);
        Height = Math.Max(4, height);
        _worldMinX = worldMinX;
        _worldMaxX = worldMaxX;
        _worldMinY = worldMinY;
        _worldMaxY = worldMaxY;
        _buffer = new char[Height, Width];
        ClearBuffer();
    }

    /// <inheritdoc />
    public int Width { get; }

    /// <inheritdoc />
    public int Height { get; }

    /// <inheritdoc />
    public void Clear()
    {
        ClearBuffer();
        _output.Clear();
    }

    /// <inheritdoc />
    public void DrawText(int x, int y, string text)
    {
        if (string.IsNullOrEmpty(text) || y < 0 || y >= Height || x >= Width)
        {
            return;
        }

        var start = Math.Max(0, x);
        var sourceOffset = Math.Max(0, -x);
        for (var i = sourceOffset; i < text.Length && start + i - sourceOffset < Width; i++)
        {
            _buffer[y, start + i - sourceOffset] = text[i];
        }
    }

    /// <inheritdoc />
    public void DrawRect(int x, int y, int width, int height)
    {
        if (width <= 1 || height <= 1)
        {
            return;
        }

        for (var ix = x; ix < x + width; ix++)
        {
            DrawPoint(ix, y, '-');
            DrawPoint(ix, y + height - 1, '-');
        }

        for (var iy = y; iy < y + height; iy++)
        {
            DrawPoint(x, iy, '|');
            DrawPoint(x + width - 1, iy, '|');
        }

        DrawPoint(x, y, '+');
        DrawPoint(x + width - 1, y, '+');
        DrawPoint(x, y + height - 1, '+');
        DrawPoint(x + width - 1, y + height - 1, '+');
    }

    /// <inheritdoc />
    public void DrawLine(int x1, int y1, int x2, int y2)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);
        var sx = x1 < x2 ? 1 : -1;
        var sy = y1 < y2 ? 1 : -1;
        var error = dx - dy;

        while (true)
        {
            DrawPoint(x1, y1, '*');
            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var doubled = error * 2;
            if (doubled > -dy)
            {
                error -= dy;
                x1 += sx;
            }

            if (doubled < dx)
            {
                error += dx;
                y1 += sy;
            }
        }
    }

    /// <inheritdoc />
    public void Present()
    {
        var builder = new StringBuilder();
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                builder.Append(_buffer[y, x]);
            }

            builder.AppendLine();
        }

        _output.Write(ConsoleOutputChannel.View, builder.ToString().TrimEnd());
    }

    /// <inheritdoc />
    public (int px, int py) WorldToScreen(float worldX, float worldY)
    {
        var normalizedX = (worldX - _worldMinX) / (_worldMaxX - _worldMinX);
        var normalizedY = (worldY - _worldMinY) / (_worldMaxY - _worldMinY);
        var px = Math.Clamp((int)MathF.Round(normalizedX * (Width - 1)), 0, Width - 1);
        var py = Math.Clamp(Height - 1 - (int)MathF.Round(normalizedY * (Height - 1)), 0, Height - 1);
        return (px, py);
    }

    private void DrawPoint(int x, int y, char value)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return;
        }

        _buffer[y, x] = value;
    }

    private void ClearBuffer()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _buffer[y, x] = '.';
            }
        }
    }
}
