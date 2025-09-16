using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase.Game
{
    public class DifficultyScene
    {
        public DifficultyLevel SelectedDifficulty { get; private set; } = DifficultyLevel.Medium;
        
        public void Run()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== PACMAN: DIFFICULTY SELECTION ===");
            Console.WriteLine();
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Select difficulty level:");
            Console.WriteLine();
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("1. EASY");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   - Ghosts patrol and move randomly");
            Console.WriteLine("   - Limited vision (3 cells)");
            Console.WriteLine("   - Minimal cooperation (10%)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("2. MEDIUM");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   - Ghosts can hunt the player");
            Console.WriteLine("   - Medium vision (5 cells)");
            Console.WriteLine("   - Moderate cooperation (30%)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("3. HARD");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   - Ghosts block escape routes");
            Console.WriteLine("   - Extended vision (7 cells)");
            Console.WriteLine("   - High cooperation (60%)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Emergent behavior: ghosts use simple rules,");
            Console.WriteLine("but together create complex collective behavior!");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press 1, 2 or 3 to select:");
            Console.ResetColor();

            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case '1':
                        SelectedDifficulty = DifficultyLevel.Easy;
                        ShowDifficultyConfirmation("EASY");
                        return;
                    case '2':
                        SelectedDifficulty = DifficultyLevel.Medium;
                        ShowDifficultyConfirmation("MEDIUM");
                        return;
                    case '3':
                        SelectedDifficulty = DifficultyLevel.Hard;
                        ShowDifficultyConfirmation("HARD");
                        return;
                }
            }
        }

        private void ShowDifficultyConfirmation(string levelName)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Selected level: {levelName}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press any key to start the game...");
            Console.ResetColor();
            Console.ReadKey(true);
        }
    }
}
