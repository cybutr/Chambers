using System;

namespace Internal
{
    public static class GUI
    {
        private static readonly object _consoleLock = new();
        internal static object ConsoleLock => _consoleLock;
        private static readonly bool _isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

        public static string SetBackgroundColor(int r, int g, int b) => $"\u001b[48;2;{r};{g};{b}m";

        public static string SetForegroundColor(int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m";

        public static string ResetColor() => "\u001b[0m";

        public static void SetCursorPosition(int x, int y)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(x, y);
            }
        }

        public static void SetCursorVisible(bool visible)
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.CursorVisible = visible;
                }
                catch
                {
                    // Ignore errors on platforms that don't support this
                }
            }
        }

        public static void Write(string text)
        {
            lock (_consoleLock)
            {
                Console.Write(text);
            }
        }

        public static void WriteLine(string text)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(text);
            }
        }

        public static void WriteLine(object value)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(value);
            }
        }

        public static void WriteLine()
        {
            lock (_consoleLock)
            {
                Console.WriteLine();
            }
        }

        public static void Write(string format, params object[] args)
        {
            lock (_consoleLock)
            {
                Console.Write(format, args);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(format, args);
            }
        }

        public static void Write(object value)
        {
            lock (_consoleLock)
            {
                Console.Write(value);
            }
        }

        public static void WriteAt(int x, int y, string text)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(x, y);
                Console.Write(text);
            }
        }

        public static void DrawPixel(int x, int y, int r, int g, int b, string symbol = "  ")
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(x, y);
                Console.Write(SetBackgroundColor(r, g, b) + symbol + ResetColor());
            }
        }

        public static void Clear()
        {
            lock (_consoleLock)
            {
                Console.Clear();
            }
        }

        public static void DrawGrid(int width, int height, int leftPadding, int topPadding, Func<int, int, (int r, int g, int b, string symbol)> getPixel)
        {
            lock (_consoleLock)
            {
                for (int y = 0; y < height; y++)
                {
                    Console.SetCursorPosition(leftPadding, y + topPadding);
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = getPixel(x, y);
                        Console.Write(SetBackgroundColor(pixel.r, pixel.g, pixel.b) + pixel.symbol + ResetColor());
                    }
                }
            }
        }

        public static void DrawGrid(int width, int height, int leftPadding, int topPadding, Func<int, int, (int r, int g, int b)> getColor, string defaultSymbol = "  ")
        {
            lock (_consoleLock)
            {
                for (int y = 0; y < height; y++)
                {
                    Console.SetCursorPosition(leftPadding, y + topPadding);
                    for (int x = 0; x < width; x++)
                    {
                        var color = getColor(x, y);
                        Console.Write(SetBackgroundColor(color.r, color.g, color.b) + defaultSymbol + ResetColor());
                    }
                }
            }
        }

        public static void DrawBox(int x, int y, int width, int height, string title, string titleLeftDecor = "{", string titleRightDecor = "}")
        {
            bool isLinux = _isLinux;
            
            // Define box drawing characters
            string topLeft = "╔";
            string topRight = "╗";
            string bottomLeft = "╚";
            string bottomRight = "╝";
            string doubleHorizontal = "═";
            string doubleVertical = "║";
            string horizontal = "-";
            string vertical = "|";
            string corner = "+";

            // Draw top border with double lines
            SetCursorPosition(x, y);
            if (!isLinux) Write(topLeft + new string(doubleHorizontal[0], width - 2) + topRight);
            else Write(corner + new string(horizontal[0], width - 2) + corner);

            // Draw sides and content area
            for (int i = 1; i < height - 1; i++)
            {
                SetCursorPosition(x, y + i);
                if (!isLinux) Write(doubleVertical + new string(' ', width - 2) + doubleVertical);
                else Write(vertical + new string(' ', width - 2) + vertical);
            }

            // Draw bottom border with double lines
            SetCursorPosition(x, y + height - 1);
            if (!isLinux) Write(bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight);
            else Write(corner + new string(horizontal[0], width - 2) + corner);

            // Write the decorated title in the middle of the top of the box if not empty or whitespace
            if (!string.IsNullOrWhiteSpace(title))
            {
                string decoratedTitle = $"{titleLeftDecor} {title} {titleRightDecor}";
                int titleLength = decoratedTitle.Length;
                int padding = (width - 2 - titleLength) / 2;
                int cursorX = x + 1 + padding;
                int cursorY = y;

                if (cursorX + titleLength < Console.WindowWidth && cursorY < Console.WindowHeight)
                {
                    SetCursorPosition(cursorX, cursorY);
                    // Assuming ColorSpectrum.DARK_GREEN is (0, 100, 0) or similar. Hardcoding a green for now as ColorSpectrum is not available here.
                    // Ideally, pass color as argument or use a default.
                    Write(SetForegroundColor(0, 100, 0) + decoratedTitle + ResetColor());
                }
            }

            // Add decorative corners
            if (!isLinux)
            {
                SetCursorPosition(x, y);
                Write(topLeft);
                SetCursorPosition(x + width - 1, y);
                Write(topRight);
                SetCursorPosition(x, y + height - 1);
                Write(bottomLeft);
                SetCursorPosition(x + width - 1, y + height - 1);
                Write(bottomRight);
            }
            else 
            {
                SetCursorPosition(x, y);
                Write(corner);
                SetCursorPosition(x + width - 1, y);
                Write(corner);
                SetCursorPosition(x, y + height - 1);
                Write(corner);
                SetCursorPosition(x + width - 1, y + height - 1);
                Write(corner);
            }
        }

        public static void DrawColoredBox(int x, int y, int width, int height, string title, (int r, int g, int b) color, string titleLeftDecor = "{", string titleRightDecor = "}")
        {
            bool isLinux = _isLinux;

            // Safety check - ensure box fits within console bounds
            if (x < 0 || y < 0 || x + width > Console.WindowWidth || y + height > Console.WindowHeight) return; // Don't draw if box would be outside console bounds
            
            // Define box drawing characters
            string topLeft = "╔";
            string topRight = "╗";
            string bottomLeft = "╚";
            string bottomRight = "╝";
            string doubleHorizontal = "═";
            string doubleVertical = "║";
            string horizontal = "-";
            string vertical = "|";
            string corner = "+";

            // Draw top border with double lines
            SetCursorPosition(x, y);
            if (!isLinux) Write(SetForegroundColor(color.r, color.g, color.b) + topLeft + new string(doubleHorizontal[0], width - 2) + topRight + ResetColor());
            else Write(SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + ResetColor());

            // Draw sides and content area
            for (int i = 1; i < height - 1; i++)
            {
                if (y + i >= 0 && y + i < Console.WindowHeight)
                {
                    SetCursorPosition(x, y + i);
                    if (!isLinux) Write(SetForegroundColor(color.r, color.g, color.b) + doubleVertical + new string(' ', width - 2) + doubleVertical + ResetColor());
                    else Write(SetForegroundColor(color.r, color.g, color.b) + vertical + new string(' ', width - 2) + vertical + ResetColor());
                }
            }

            // Draw bottom border with double lines
            if (y + height - 1 >= 0 && y + height - 1 < Console.WindowHeight)
            {
                SetCursorPosition(x, y + height - 1);
                if (!isLinux) Write(SetForegroundColor(color.r, color.g, color.b) + bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight + ResetColor());
                else Write(SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + ResetColor());
            }

            // Write the decorated title in the middle of the top of the box if not empty or whitespace
            if (!string.IsNullOrWhiteSpace(title))
            {
                string decoratedTitle = $"{titleLeftDecor} {title} {titleRightDecor}";
                int titleLength = decoratedTitle.Length;
                int padding = (width - 2 - titleLength) / 2;
                int cursorX = x + 1 + padding;
                int cursorY = y;

                if (cursorX + titleLength < Console.WindowWidth && cursorY < Console.WindowHeight)
                {
                    SetCursorPosition(cursorX, cursorY);
                    // Using same green as DrawBox for consistency, or could be passed in.
                    Write(SetForegroundColor(0, 100, 0) + decoratedTitle + ResetColor());
                }
            }

            // Add decorative corners (with bounds checking)
            if (!isLinux)
            {
                // Top left
                if (x >= 0 && y >= 0 && x < Console.WindowWidth && y < Console.WindowHeight)
                {
                    SetCursorPosition(x, y);
                    Write(SetForegroundColor(color.r, color.g, color.b) + topLeft + ResetColor());
                }
                // Top right
                if (x + width - 1 >= 0 && y >= 0 && x + width - 1 < Console.WindowWidth && y < Console.WindowHeight)
                {
                    SetCursorPosition(x + width - 1, y);
                    Write(SetForegroundColor(color.r, color.g, color.b) + topRight + ResetColor());
                }
                // Bottom left
                if (x >= 0 && y + height - 1 >= 0 && x < Console.WindowWidth && y + height - 1 < Console.WindowHeight)
                {
                    SetCursorPosition(x, y + height - 1);
                    Write(SetForegroundColor(color.r, color.g, color.b) + bottomLeft + ResetColor());
                }
                // Bottom right
                if (x + width - 1 >= 0 && y + height - 1 >= 0 && x + width - 1 < Console.WindowWidth && y + height - 1 < Console.WindowHeight)
                {
                    SetCursorPosition(x + width - 1, y + height - 1);
                    Write(SetForegroundColor(color.r, color.g, color.b) + bottomRight + ResetColor());
                }
            }
            else 
            {
                // Top left
                if (x >= 0 && y >= 0 && x < Console.WindowWidth && y < Console.WindowHeight)
                {
                    SetCursorPosition(x, y);
                    Write(SetForegroundColor(color.r, color.g, color.b) + corner + ResetColor());
                }
                // Top right
                if (x + width - 1 >= 0 && y >= 0 && x + width - 1 < Console.WindowWidth && y < Console.WindowHeight)
                {
                    SetCursorPosition(x + width - 1, y);
                    Write(SetForegroundColor(color.r, color.g, color.b) + corner + ResetColor());
                }
                // Bottom left
                if (x >= 0 && y + height - 1 >= 0 && x < Console.WindowWidth && y + height - 1 < Console.WindowHeight)
                {
                    SetCursorPosition(x, y + height - 1);
                    Write(SetForegroundColor(color.r, color.g, color.b) + corner + ResetColor());
                }
                // Bottom right
                if (x + width - 1 >= 0 && y + height - 1 >= 0 && x + width - 1 < Console.WindowWidth && y + height - 1 < Console.WindowHeight)
                {
                    SetCursorPosition(x + width - 1, y + height - 1);
                    Write(SetForegroundColor(color.r, color.g, color.b) + corner + ResetColor());
                }
            }
        }

        public static void DisplayCenteredTextAtCords(string text, int x, int y, (int r, int g, int b) color)
        {
            string[] lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            int maxLineWidth = lines.Max(l => l.Length);
            int width = maxLineWidth + 2;
            int height = lines.Length + 2;

            int startX = x - width / 2;
            int startY = y - height / 2;

            // Safety check - don't draw if text would be outside console bounds
            if (startX + 1 < 0 || startY < 0 || startX + width >= Console.WindowWidth || startY + height >= Console.WindowHeight) return;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineY = startY + 1 + i;
                int lineX = startX + 1;
                
                // Check bounds for each line
                if (lineX >= 0 && lineY >= 0 && lineX < Console.WindowWidth && lineY < Console.WindowHeight)
                {
                    SetCursorPosition(lineX, lineY);
                    Write(SetForegroundColor(color.r, color.g, color.b) + lines[i] + ResetColor());
                }
            }
        }
    }
}
