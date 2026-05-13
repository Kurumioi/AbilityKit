using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Platform.Console_
{
    /// <summary>
    /// Console 平台输出实现
    /// </summary>
    public sealed class ConsoleOutput : IOutput
    {
        private readonly Dictionary<OutputChannel, ConsoleColor> _colors;

        public ConsoleOutput()
        {
            _colors = new Dictionary<OutputChannel, ConsoleColor>
            {
                { OutputChannel.System, ConsoleColor.Cyan },
                { OutputChannel.Phase, ConsoleColor.Green },
                { OutputChannel.View, ConsoleColor.Blue },
                { OutputChannel.Sync, ConsoleColor.DarkCyan },
                { OutputChannel.Input, ConsoleColor.Magenta },
                { OutputChannel.Battle, ConsoleColor.White },
                { OutputChannel.Skill, ConsoleColor.Yellow },
                { OutputChannel.Damage, ConsoleColor.Red },
                { OutputChannel.Buff, ConsoleColor.DarkYellow },
                { OutputChannel.Projectile, ConsoleColor.DarkMagenta },
                { OutputChannel.Area, ConsoleColor.DarkCyan },
                { OutputChannel.Entity, ConsoleColor.Gray },
                { OutputChannel.Config, ConsoleColor.DarkGreen },
                { OutputChannel.Debug, ConsoleColor.DarkGray },
                { OutputChannel.Warning, ConsoleColor.Yellow },
                { OutputChannel.Error, ConsoleColor.Red },
                { OutputChannel.Trace, ConsoleColor.DarkMagenta }
            };
        }

        public void Write(OutputChannel channel, string message)
        {
            var prefix = GetPrefix(channel);
            var color = _colors.TryGetValue(channel, out var c) ? c : ConsoleColor.Gray;

            var originalColor = System.Console.ForegroundColor;
            try
            {
                System.Console.ForegroundColor = color;
                System.Console.WriteLine($"{prefix} {message}");
            }
            finally
            {
                System.Console.ForegroundColor = originalColor;
            }
        }

        public void WriteFormat(OutputChannel channel, string format, params object[] args)
        {
            Write(channel, string.Format(format, args));
        }

        public void Clear()
        {
            System.Console.Clear();
        }

        public void WriteSeparator(OutputChannel channel = OutputChannel.System, char c = '=', int length = 60)
        {
            Write(channel, new string(c, length));
        }

        public void WriteTitle(OutputChannel channel, string title, char borderChar = '=', int width = 60)
        {
            var innerWidth = width - 4;
            var padding = Math.Max(0, (innerWidth - title.Length) / 2);
            var padded = new string(' ', padding) + title + new string(' ', innerWidth - padding - title.Length);

            WriteSeparator(channel, borderChar, width);
            Write(channel, "|" + padded + "|");
            WriteSeparator(channel, borderChar, width);
        }

        private static string GetPrefix(OutputChannel channel)
        {
            return channel switch
            {
                OutputChannel.System => "[SYS]",
                OutputChannel.Phase => "[PHASE]",
                OutputChannel.View => "[VIEW]",
                OutputChannel.Sync => "[SYNC]",
                OutputChannel.Input => "[INPUT]",
                OutputChannel.Battle => "[BATTLE]",
                OutputChannel.Skill => "[SKILL]",
                OutputChannel.Damage => "[DMG]",
                OutputChannel.Buff => "[BUFF]",
                OutputChannel.Projectile => "[PROJ]",
                OutputChannel.Area => "[AREA]",
                OutputChannel.Entity => "[ENTITY]",
                OutputChannel.Config => "[CONFIG]",
                OutputChannel.Debug => "[DEBUG]",
                OutputChannel.Warning => "[WARN]",
                OutputChannel.Error => "[ERROR]",
                OutputChannel.Trace => "[TRACE]",
                _ => "[??]"
            };
        }
    }
}
