using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Internal;

partial class Program
{
        #region save selection GUI
        static (int x, int y) terminalCentre = (Console.WindowWidth / 2, Console.WindowHeight / 2);
        static int menuWidth = 95;
        static int menuHeight = 50;
        static int numberOfRows = 8;
        static int heightOffset = Math.Max(0, (Console.WindowHeight - (13 + (6 * numberOfRows))) / 4);
        static string delete = @"
.__@@__.
 \##$$/ 
 /@$$$\ ";
        static string select = @"
 $%\
 ##$%}
 $%/";
        static List<(int r, int g, int b)> colors =
        [
            ColorSpectrum.ANTIQUE_WHITE,
            ColorSpectrum.BEIGE
        ];
        public static void DrawSaveSelectionGUI()
        {
            terminalCentre = (Console.WindowWidth / 2, Console.WindowHeight / 2);
            heightOffset = Math.Max(0, (Console.WindowHeight - (13 + (6 * numberOfRows))) / 4);
            GUI.Clear();
            var folderPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            Directory.CreateDirectory(folderPath);
            GUI.DrawColoredBox(terminalCentre.x - menuWidth / 2, terminalCentre.y - menuHeight / 2 + heightOffset, menuWidth, 10, "", ColorSpectrum.LIGHT_CYAN);
            GUI.DisplayCenteredTextAtCords(Map.Title, terminalCentre.x, terminalCentre.y - menuHeight / 2 + heightOffset + 5, ColorSpectrum.CYAN);
            string[] files = [.. Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Data/Saves"), "*.chmb")
                .Concat(Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Data/Saves"), "*.json"))
                .Select(f => Path.GetFileName(f))];

            for (int i = 0; i < numberOfRows; i++)
            {
                int index = colors.Count > i ? i : i % colors.Count;
                DrawSaveFileBox(i, colors[index], false, i == 0 ? 1 : 4);
            }
            DrawLoadButton(false);
            ManageSaveSlots();
        }
        public static void DrawSaveFileBox(int index, (int r, int g, int b) color, bool isSelected, int currentSelection, List<string>? typedLettersBuffer = null)
        {
            int x = terminalCentre.x - menuWidth / 2;
            int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + index * 5 + 1;

            bool isBox0Selected = currentSelection == 1 || isSelected;
            bool isBox1Selected = currentSelection == 0 || isSelected;
            bool isBox2Selected = currentSelection == 2 || isSelected;
            if (currentSelection == 4)
            {
                isBox0Selected = false;
                isBox1Selected = false;
                isBox2Selected = false;
            }

            DrawSelectableBox(x + 11, y, menuWidth - 22, 5, isBox0Selected, isSelected, color);
            DrawSelectableBox(x, y, 10, 5, isBox1Selected, isSelected, ColorSpectrum.YELLOW);
            DrawSelectableBox(x + menuWidth - 10, y, 10, 5, isBox2Selected, isSelected, ColorSpectrum.LIGHT_GREEN);
            GUI.Write(GUI.ResetColor());

            // Draw the delete ASCII in the first small box (with bounds checking)
            var deleteLines = delete.Split('\n');
            for (int i = 0; i < deleteLines.Length; i++)
            {
                int lineX = x + 1;
                int lineY = y + i;
                if (lineX >= 0 && lineY >= 0 && lineX < Console.WindowWidth && lineY < Console.WindowHeight)
                {
                    GUI.SetCursorPosition(lineX, lineY);
                    GUI.Write(GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b) + deleteLines[i] + GUI.ResetColor());
                }
            }

            // Draw the select ASCII in the second small box (with bounds checking)
            var selectLines = select.Split('\n');
            for (int i = 0; i < selectLines.Length; i++)
            {
                int lineX = x + menuWidth - 8;
                int lineY = y + i;
                if (lineX >= 0 && lineY >= 0 && lineX < Console.WindowWidth && lineY < Console.WindowHeight)
                {
                    GUI.SetCursorPosition(lineX, lineY);
                    GUI.Write(GUI.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b) + selectLines[i] + GUI.ResetColor());
                }
            }
            if (typedLettersBuffer != null) DisplayCustomLetters(index, typedLettersBuffer);
            else if (slots[index].name != null) DisplayCustomLetters(index, ConvertStringToList(slots[index].name ?? ""));
            if (currentSelection == 5)
            {
                for (int i = 0; i < 3; i++)
                {
                    GUI.SetCursorPosition(x + 11, y++ + i);
                    GUI.Write(new string(' ', menuWidth - 24));
                }
            }

        }
        private static List<string> ConvertStringToList(string str)
        {
            List<string> list = [];
            foreach (char c in str)
            {
                if (c == ' ') list.Add("Spacebar");
                else if (char.IsDigit(c))
                {
                    list.Add($"D{c}");
                }
                else list.Add(char.ToUpper(c).ToString());
            }
            return list;
        }
        public static void ManageSaveSlots()
        {
            int currentIndex = 0; // Index of the currently selected slot
            int currentSection = 1; // 0=delete, 1=main, 2=select 4=none 5=none
            bool isTyping = false;
            bool isLoad = false;
            bool shouldMenu = true;
            List<string> typedLettersBuffer = [];
            string previousBuffer = "";

            void RedrawSaveUI(bool isSelected, int currentSelection, List<string>? typedLettersBuffer = null)
            {
                int index = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                if (!isLoad && typedLettersBuffer != null) DrawSaveFileBox(currentIndex, colors[index], isSelected, currentSelection, typedLettersBuffer);
                else if (!isLoad) DrawSaveFileBox(currentIndex, colors[index], isSelected, currentSection);
                else DrawSaveFileBox(currentIndex, colors[index], isSelected, 4);
            }

            while (shouldMenu)
            {
                while (!isConfiguring)
                {
                    var key = Console.ReadKey(true).Key;
                    if (isTyping)
                    {

                        switch (key)
                        {
                            case ConsoleKey.Enter:
                                string name = "";
                                bool nonSpaceFound = false;
                                foreach (var letter in typedLettersBuffer)
                                {
                                    switch (letter)
                                    {
                                        case "Spacebar":
                                            name += " ";
                                            break;
                                        case "D0":
                                            name += "0";
                                            nonSpaceFound = true;
                                            break;
                                        case "D1":
                                            name += "1";
                                            nonSpaceFound = true;
                                            break;
                                        case "D2":
                                            name += "2";
                                            nonSpaceFound = true;
                                            break;
                                        case "D3":
                                            name += "3";
                                            nonSpaceFound = true;
                                            break;
                                        case "D4":
                                            name += "4";
                                            nonSpaceFound = true;
                                            break;
                                        case "D5":
                                            name += "5";
                                            nonSpaceFound = true;
                                            break;
                                        case "D6":
                                            name += "6";
                                            nonSpaceFound = true;
                                            break;
                                        case "D7":
                                            name += "7";
                                            nonSpaceFound = true;
                                            break;
                                        case "D8":
                                            name += "8";
                                            nonSpaceFound = true;
                                            break;
                                        case "D9":
                                            name += "9";
                                            nonSpaceFound = true;
                                            break;
                                        default:
                                            name += letter;
                                            nonSpaceFound = true;
                                            break;
                                    }
                                }
                                // Only exit typing mode if the name contains non-space characters.
                                if (!nonSpaceFound || string.IsNullOrWhiteSpace(name) || typedLettersBuffer.Count < 1) break;
                                isTyping = false;

                                // Ensure unique name to avoid conflicts
                                var existingNames = slots.Where(s => s.name != null && s != slots[currentIndex])
                                                        .Select(s => s.name!)
                                                        .ToList();
                                string uniqueName = GetUniqueChamberName(name, existingNames);

                                // Get old file path
                                string oldName = slots[currentIndex].name ?? "NEW CHAMBER";
                                string oldExt = File.Exists(Path.Combine("Data/Saves", oldName + ".chmb")) ? ".chmb" : ".json";
                                string oldPath = Path.Combine("Data/Saves", oldName + oldExt);

                                // Use helper method to rename file and update config
                                string? newFilePath = RenameChamberFile(oldPath, uniqueName, slots[currentIndex].chamber);

                                // Update slot with the final unique name
                                slots[currentIndex] = (slots[currentIndex].chamber, uniqueName, false, false, false);

                                typedLettersBuffer = [];
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                break;
                            case ConsoleKey.Escape:
                                isTyping = false;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                break;
                            default:
                                if (key == ConsoleKey.Backspace && typedLettersBuffer.Count > 0)
                                {
                                    typedLettersBuffer.RemoveAt(typedLettersBuffer.Count - 1);
                                    RedrawSaveUI(slots[currentIndex].isSelected, currentSection, typedLettersBuffer);
                                    int x = terminalCentre.x - menuWidth / 2;
                                    int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + currentIndex * 5 + 2;
                                    DisplayCustomLetters(currentIndex, typedLettersBuffer);
                                }
                                else if (
                                    (
                                        (key == ConsoleKey.Spacebar && typedLettersBuffer.Count > 0) ||
                                        key == ConsoleKey.A || key == ConsoleKey.B || key == ConsoleKey.C || key == ConsoleKey.D ||
                                        key == ConsoleKey.E || key == ConsoleKey.F || key == ConsoleKey.G || key == ConsoleKey.H ||
                                        key == ConsoleKey.I || key == ConsoleKey.J || key == ConsoleKey.K || key == ConsoleKey.L ||
                                        key == ConsoleKey.M || key == ConsoleKey.N || key == ConsoleKey.O || key == ConsoleKey.P ||
                                        key == ConsoleKey.Q || key == ConsoleKey.R || key == ConsoleKey.S || key == ConsoleKey.T ||
                                        key == ConsoleKey.U || key == ConsoleKey.V || key == ConsoleKey.W || key == ConsoleKey.X ||
                                        key == ConsoleKey.Y || key == ConsoleKey.Z || key == ConsoleKey.D0 || key == ConsoleKey.D1 ||
                                        key == ConsoleKey.D2 || key == ConsoleKey.D3 || key == ConsoleKey.D4 || key == ConsoleKey.D5 ||
                                        key == ConsoleKey.D6 || key == ConsoleKey.D7 || key == ConsoleKey.D8 || key == ConsoleKey.D9
                                    )
                                    && GetTypedLettersListLenght(typedLettersBuffer)
                                        + GetLetter(ConvertConsoleKeyToLetter(key.ToString())).Split('\n')[0].Length + 1 < 67)
                                {
                                    typedLettersBuffer.Add(key.ToString());
                                    DisplayCustomLetters(currentIndex, typedLettersBuffer);
                                }
                                break;
                        }
                        continue;
                    }

                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.W:
                            if (slots[currentIndex].isSelected)
                            {
                                currentSection = 2;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            if (currentIndex > 0 && !isLoad)
                            {
                                // Clear the current slot's visual selection
                                int currentColorIndex = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                                DrawSaveFileBox(currentIndex, colors[currentColorIndex], slots[currentIndex].isSelected, 4);

                                currentIndex--;

                                // Draw the new slot with proper selection
                                int newColorIndex = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                                DrawSaveFileBox(currentIndex, colors[newColorIndex], slots[currentIndex].isSelected, currentSection);
                            }
                            if (isLoad)
                            {
                                isLoad = false;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                DrawLoadButton(isLoad);
                            }
                            break;
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.S:
                            if (slots[currentIndex].isSelected && !isLoad)
                            {
                                currentSection = 2;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            if (currentIndex < slots.Count - 1 && !isLoad)
                            {
                                // Clear the current slot's visual selection
                                int currentColorIndex = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                                DrawSaveFileBox(currentIndex, colors[currentColorIndex], slots[currentIndex].isSelected, 4);

                                currentIndex++;

                                // Draw the new slot with proper selection
                                int newColorIndex = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                                DrawSaveFileBox(currentIndex, colors[newColorIndex], slots[currentIndex].isSelected, currentSection);
                            }
                            else if (!isLoad)
                            {
                                isLoad = true;
                                currentSection = 1;
                                RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                DrawLoadButton(isLoad);
                            }
                            break;
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.A:
                            if (!isLoad)
                            {
                                if (currentSection > 0)
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection--;
                                }
                                else
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection = 1; // Move to Delete if possible
                                }
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            break;
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.D:
                            if (!isLoad)
                            {
                                if (currentSection < 2)
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection++;
                                }
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            break;
                        case ConsoleKey.Enter:
                            bool canLoadAnything = false;
                            foreach (var slot in slots)
                            {
                                if (slot.isSelected)
                                {
                                    canLoadAnything = true;
                                    break;
                                }
                            }
                            if (isLoad && canLoadAnything)
                            {
                                LoadSelectedSlots();
                                shouldMenu = false;
                                return;
                            }
                            else if (slots[currentIndex].isSelected)
                            {
                                var slot = slots[currentIndex];
                                slot.isSelected = false;
                                slots[currentIndex] = slot;
                            }
                            else if (currentSection == 1 && !isLoad)
                            {
                                typedLettersBuffer = ConvertStringToList(slots[currentIndex].name ?? "");
                                previousBuffer = slots[currentIndex].name ?? "";
                                isTyping = true;
                            }
                            else if (currentSection == 0)
                            {
                                if (!slots[currentIndex].isEmpty || slots[currentIndex].name != null)
                                {
                                    string chmbDel = Path.Combine(Environment.CurrentDirectory, "Data/Saves", (slots[currentIndex].name ?? "") + ".chmb");
                                    string jsonDel = Path.Combine(Environment.CurrentDirectory, "Data/Saves", (slots[currentIndex].name ?? "") + ".json");
                                    DeleteMap(File.Exists(chmbDel) ? chmbDel : jsonDel);
                                    slots[currentIndex] = (new Map(), null, false, true, true);
                                    RedrawSaveUI(slots[currentIndex].isSelected, 1);
                                    RedrawSaveUI(slots[currentIndex].isSelected, 2);
                                }
                            }
                            else if (currentSection == 2)
                            {
                                if (slots[currentIndex].chamber.seed == 0) slots[currentIndex] = AddNewChamber(slots[currentIndex]);
                                else
                                {
                                    var slot = slots[currentIndex];
                                    slot.isSelected = !slot.isSelected;
                                    slots[currentIndex] = slot;
                                    RedrawSaveUI(slots[currentIndex].isSelected, 2);
                                }
                            }
                            RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            break;
                    }
                    foreach (var slot in slots)
                    {
                        if (slot.isSelected)
                        {
                            int index = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                            DrawSaveFileBox(slots.IndexOf(slot), colors[index], true, currentSection);
                        }
                    }
                }
            }
        }
        public static void DisplayCustomLetters(int currentIndex, List<string> buffer)
        {
            int x = terminalCentre.x - menuWidth / 2 + 13;
            int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + currentIndex * 5 + 1;

            // Safety check - don't draw if position would be outside console bounds
            if (x < 0 || y < 0 || x >= Console.WindowWidth || y >= Console.WindowHeight) return;

            int currentX = x;

            foreach (var letter in buffer)
            {
                if (letter == "Spacebar" || letter == " ")
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int lineX = currentX;
                        int lineY = y + 1 + i;
                        if (lineX >= 0 && lineY >= 0 && lineX < Console.WindowWidth && lineY < Console.WindowHeight)
                        {
                            GUI.SetCursorPosition(lineX, lineY);
                            GUI.Write(new string(' ', 3));
                        }
                    }
                    currentX += 4; // 5 spaces plus 1 for spacing between letters
                    continue;
                }

                string letterString = GetLetter(letter);
                var lines = letterString.Split('\n');

                int letterWidth = lines.Max(line => line.Length);

                for (int i = 0; i < lines.Length; i++)
                {
                    int lineX = currentX;
                    int lineY = y + i;
                    if (lineX >= 0 && lineY >= 0 && lineX < Console.WindowWidth && lineY < Console.WindowHeight)
                    {
                        GUI.SetCursorPosition(lineX, lineY);
                        GUI.Write(lines[i]);
                    }
                }

                currentX += letterWidth + 1; // Add 1 for spacing between letters
            }
        }
        public static string GetLetter(string letter)
        {
        return letter switch
        {
            "A" => Characters.A,
            "B" => Characters.B,
            "C" => Characters.C,
            "D" => Characters.D,
            "E" => Characters.E,
            "F" => Characters.F,
            "G" => Characters.G,
            "H" => Characters.H,
            "I" => Characters.I,
            "J" => Characters.J,
            "K" => Characters.K,
            "L" => Characters.L,
            "M" => Characters.M,
            "N" => Characters.N,
            "O" => Characters.O,
            "P" => Characters.P,
            "Q" => Characters.Q,
            "R" => Characters.R,
            "S" => Characters.S,
            "T" => Characters.T,
            "U" => Characters.U,
            "V" => Characters.V,
            "W" => Characters.W,
            "X" => Characters.X,
            "Y" => Characters.Y,
            "Z" => Characters.Z,
            "D0" => Characters.Zero,
            "D1" => Characters.One,
            "D2" => Characters.Two,
            "D3" => Characters.Three,
            "D4" => Characters.Four,
            "D5" => Characters.Five,
            "D6" => Characters.Six,
            "D7" => Characters.Seven,
            "D8" => Characters.Eight,
            "D9" => Characters.Nine,
            _ => Characters.Unknown,
        };
    }
        public static string ConvertConsoleKeyToLetter(string key)
        {
        return key switch
        {
            "Spacebar" => " ",
            "D0" => "0",
            "D1" => "1",
            "D2" => "2",
            "D3" => "3",
            "D4" => "4",
            "D5" => "5",
            "D6" => "6",
            "D7" => "7",
            "D8" => "8",
            "D9" => "9",
            _ => key,
        };
    }
        public static int GetTypedLettersListLenght(List<string> list)
        {
            int length = 0;
            foreach (var letter in list)
            {
                if (letter == "Spacebar" || letter == " ") length += 4;
                else
                {
                    string letterRepresentation = GetLetter(letter);
                    int letterWidth = letterRepresentation.Split('\n').Max(line => line.Length);
                    length += letterWidth + 1;
                }
            }
            return length;
        }
        public static void DrawLoadButton(bool isLoad)
        {
            DrawSelectableBox(terminalCentre.x - 5, terminalCentre.y - menuHeight / 2 + heightOffset + 9 + numberOfRows * 5 + 1, 10, 3, false, false, ColorSpectrum.LIGHT_GREEN);
            GUI.SetCursorPosition(terminalCentre.x - 2, terminalCentre.y - menuHeight / 2 + heightOffset + 9 + numberOfRows * 5 + 2);
            GUI.Write(GUI.ResetColor());
            string foreground = isLoad ? GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) : "";
            string background = isLoad ? GUI.SetBackgroundColor(ColorSpectrum.SILVER.r, ColorSpectrum.SILVER.g, ColorSpectrum.SILVER.b) : "";
            GUI.Write(background + foreground + "LOAD" + GUI.ResetColor());
        }
        public static void DrawSelectableBox(int x, int y, int width, int height, bool isSelected, bool isFullySelected, (int r, int g, int b) color)
        {
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

            // Define selection colors
            (int r, int g, int b) normalSelectionColor = ColorSpectrum.SILVER;
            (int r, int g, int b) selectedColor = ColorSpectrum.YELLOW;

            // Set background color based on selection
            string background;
            if (isFullySelected) background = GUI.SetBackgroundColor(selectedColor.r, selectedColor.g, selectedColor.b);
            else if (isSelected) background = GUI.SetBackgroundColor(normalSelectionColor.r, normalSelectionColor.g, normalSelectionColor.b);
            else background = "";

            // Draw top border with double lines
            GUI.SetCursorPosition(x, y);
            if (!isLinux)
                GUI.Write(background + GUI.SetForegroundColor(color.r, color.g, color.b) + topLeft + new string(doubleHorizontal[0], width - 2) + topRight + GUI.ResetColor());
            else GUI.Write(background + GUI.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + GUI.ResetColor());

            // Draw sides and content area
            for (int i = 1; i < height - 1; i++)
            {
                GUI.SetCursorPosition(x, y + i);
                if (!isLinux)
                {
                    GUI.Write(
                        background +
                        GUI.SetForegroundColor(color.r, color.g, color.b) + doubleVertical + GUI.ResetColor() +
                        new string(' ', width - 2) +
                        background +
                        GUI.SetForegroundColor(color.r, color.g, color.b) + doubleVertical + GUI.ResetColor()
                    );
                }
                else
                {
                    GUI.Write(
                        background +
                        GUI.SetForegroundColor(color.r, color.g, color.b) + vertical + GUI.ResetColor() +
                        new string(' ', width - 2) +
                        background +
                        GUI.SetForegroundColor(color.r, color.g, color.b) + vertical + GUI.ResetColor()
                    );
                }
            }

            // Draw bottom border with double lines
            GUI.SetCursorPosition(x, y + height - 1);
            if (!isLinux)
                GUI.Write(background + GUI.SetForegroundColor(color.r, color.g, color.b) + bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight + GUI.ResetColor());
            else GUI.Write(background + GUI.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + GUI.ResetColor());

            // Reset background color
            if (isSelected) GUI.Write(GUI.ResetColor());
        }
        public static (Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) AddNewChamber((Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) chamber)
        {
            isConfiguring = true;

            // Only get config if not already configured (for testing purposes)
            bool shouldSave;
            if (chamber.chamber.conf.ShouldSave == false && chamber.chamber.conf.Name != "NEW CHAMBER") shouldSave = false;
            else
            {
                isConfiguring = chamber.chamber.GetConfig();
                SaveUserConfig(chamber.chamber.conf);
                DisplayCenteredText(loadingAsciiArt);
                shouldSave = chamber.chamber.conf.ShouldSave;
            }

            // Use the name that was already set (which should already be unique from the typing logic)
            // Only apply uniqueness logic if no name was set
            string finalName;
            if (!string.IsNullOrWhiteSpace(chamber.name)) finalName = chamber.name;
            else
            {
                // No name was set, use default and make it unique
                string baseName = "NEW CHAMBER";
                var savePath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
                var existingNames = Directory.GetFiles(savePath, "*.chmb")
                                            .Concat(Directory.GetFiles(savePath, "*.json"))
                                            .Select(Path.GetFileNameWithoutExtension)
                                            .Where(name => name != null)
                                            .Cast<string>()
                                            .ToList();
                finalName = GetUniqueChamberName(baseName, existingNames);
            }

            chamber.chamber.conf.Name = finalName;
            chamber.name = finalName;

            if (shouldSave) chamber.chamber.Generate();
            if (shouldSave) SaveMap(chamber.chamber);
            (Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) newSlot = shouldSave ? (chamber.chamber, chamber.name, chamber.isSelected, chamber.isTyping, false) : (new Map(), null, false, true, true);
            GUI.Clear();
            GUI.DrawColoredBox(terminalCentre.x - menuWidth / 2, terminalCentre.y - menuHeight / 2 + heightOffset, menuWidth, 10, "", ColorSpectrum.LIGHT_CYAN);
            GUI.DisplayCenteredTextAtCords(Map.Title, terminalCentre.x, terminalCentre.y - menuHeight / 2 + heightOffset + 5, ColorSpectrum.CYAN);
            for (int i = 0; i < numberOfRows; i++)
            {
                int index = colors.Count > i ? i : i % colors.Count;
                DrawSaveFileBox(i, colors[index], false, 4);
            }
            DrawLoadButton(false);
            return newSlot;
        }
        public static void LoadSelectedSlots()
        {
            isConfiguring = false;
            isMenu = false;
            foreach (var (chamber, _, isSelected, _, _) in slots)
            {
                if (!isSelected) continue;
                chamber.InvalidateFramebuffer();
                chambers.Add(chamber);
            }
        }
        #endregion

        #region error box
        public static void ShowErrorBox(string reason)
        {
            bool isLinux = Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX;
            string[] errorLetters = ["E", "R", "R", "O", "R"];

            int asciiW = 0;
            foreach (var l in errorLetters)
            {
                var lines = GetLetter(l).Split('\n');
                asciiW += lines.Max(ln => ln.Length) + 1;
            }
            asciiW -= 1;

            int hPad   = 4;
            int boxW   = asciiW + hPad * 2 + 2;
            int topH   = 7;
            int totalH = topH + 2;

            int cx = Console.WindowWidth  / 2;
            int cy = Console.WindowHeight / 2;
            int bx = cx - boxW / 2;
            int by = cy - totalH / 2;

            var errorColor = ColorSpectrum.INDIAN_RED;
            GUI.Clear();
            GUI.DrawColoredBox(bx, by, boxW, totalH, "", errorColor);

            int asciiX = bx + 1 + (boxW - 2 - asciiW) / 2;
            int asciiY = by + 1;
            int curX   = asciiX;
            foreach (var l in errorLetters)
            {
                var lines = GetLetter(l).Split('\n');
                int lw = lines.Max(ln => ln.Length);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (curX >= 0 && asciiY + i >= 0 && curX < Console.WindowWidth && asciiY + i < Console.WindowHeight)
                    {
                        GUI.SetCursorPosition(curX, asciiY + i);
                        GUI.Write(lines[i]);
                    }
                }
                curX += lw + 1;
            }

            int divY = by + topH - 1;
            if (divY >= 0 && divY < Console.WindowHeight)
            {
                GUI.SetCursorPosition(bx, divY);
                string divLine = isLinux
                    ? "+" + new string('-', boxW - 2) + "+"
                    : "╠" + new string('═', boxW - 2) + "╣";
                GUI.Write(GUI.SetForegroundColor(errorColor.r, errorColor.g, errorColor.b) + divLine + GUI.ResetColor());
            }

            GUI.DisplayCenteredTextAtCords(reason, cx, by + topH, errorColor);
        }
        #endregion
}
