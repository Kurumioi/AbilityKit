using System;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Platform.Console_
{
    /// <summary>
    /// Console 平台输入实现
    /// </summary>
    public sealed class ConsoleInputSource : IInputSource
    {
        public bool HasInputAvailable()
        {
            try { return System.Console.KeyAvailable; }
            catch { return false; }
        }

        public bool TryReadKey(out InputKey key)
        {
            key = InputKey.None;
            try
            {
                if (!System.Console.KeyAvailable) return false;
                var consoleKey = System.Console.ReadKey(true);
                key = ConvertKey(consoleKey);
                return key != InputKey.None;
            }
            catch { return false; }
        }

        public (float dx, float dz) GetMoveInput()
        {
            try
            {
                if (!System.Console.KeyAvailable) return (0f, 0f);
                var key = System.Console.ReadKey(true).Key;
                return key switch
                {
                    System.ConsoleKey.W or System.ConsoleKey.UpArrow => (0f, -1f),
                    System.ConsoleKey.S or System.ConsoleKey.DownArrow => (0f, 1f),
                    System.ConsoleKey.A or System.ConsoleKey.LeftArrow => (-1f, 0f),
                    System.ConsoleKey.D or System.ConsoleKey.RightArrow => (1f, 0f),
                    _ => (0f, 0f)
                };
            }
            catch { return (0f, 0f); }
        }

        public bool IsKeyDown(InputKey key)
        {
            try
            {
                if (!System.Console.KeyAvailable) return false;
                while (System.Console.KeyAvailable)
                {
                    var consoleKey = System.Console.ReadKey(true);
                    if (ConvertKey(consoleKey) == key) return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static InputKey ConvertKey(System.ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key switch
            {
                System.ConsoleKey.W or System.ConsoleKey.UpArrow => InputKey.Up,
                System.ConsoleKey.S or System.ConsoleKey.DownArrow => InputKey.Down,
                System.ConsoleKey.A or System.ConsoleKey.LeftArrow => InputKey.Left,
                System.ConsoleKey.D or System.ConsoleKey.RightArrow => InputKey.Right,
                System.ConsoleKey.J => InputKey.Skill1,
                System.ConsoleKey.K => InputKey.Skill2,
                System.ConsoleKey.L => InputKey.Skill3,
                System.ConsoleKey.Spacebar => InputKey.Attack,
                System.ConsoleKey.H => InputKey.Help,
                System.ConsoleKey.Q => InputKey.Quit,
                System.ConsoleKey.Enter => InputKey.Confirm,
                System.ConsoleKey.Escape => InputKey.Cancel,
                System.ConsoleKey.Tab => InputKey.Menu,
                System.ConsoleKey.P or System.ConsoleKey.Pause => InputKey.Pause,
                _ => InputKey.None
            };
        }
    }
}
