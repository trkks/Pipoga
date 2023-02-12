using System;
using System.Linq;

using Microsoft.Xna.Framework;

// Apparently C# `args` do not include the executable name?
if (args.Length > 0)
{
    using Game game = args[0] switch
    {
    "Chip8" =>
        new Pipoga.Examples.Chip8(args),
    "PixelLinesApp" =>
        new Pipoga.Examples.PixelLinesApp(args),
    _ =>
        throw new Exception($"Not a valid game name '{args[0]}'"),
    };
    game.Run();
}
else
{
    throw new Exception("Need game name");
}
