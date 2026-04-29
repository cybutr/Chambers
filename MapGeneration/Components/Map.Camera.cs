public partial class Map
{
    private void InitializeCamera()
    {
        int cw = Math.Max(1, Console.WindowWidth  / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding);
        int ch = Math.Max(1, Console.WindowHeight - GUIConfig.BottomPadding   - GUIConfig.TopPadding);
        camera = new Camera(Math.Min(cw, width), Math.Min(ch, height));
    }

    private void InitializeFramebuffer() { }

    public void InvalidateFramebuffer() { _fb.Invalidate(); _guiBuf.Invalidate(); }
}
