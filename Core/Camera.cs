public class Camera
{
    private volatile int _x;
    private volatile int _y;
    public int X { get => _x; set => _x = value; }
    public int Y { get => _y; set => _y = value; }
    public int Width  { get; private set; }
    public int Height { get; private set; }

    public Camera(int width, int height) { Width = width; Height = height; }

    public void Resize(int w, int h) { Width = w; Height = h; }

    public (int wx, int wy) ToWorld(int sx, int sy) => (X + sx, Y + sy);
    public bool IsVisible(int wx, int wy) =>
        wx >= X && wx < X + Width && wy >= Y && wy < Y + Height;
}
