using System;
using System.Linq;

using Microsoft.Xna.Framework;

namespace Pipoga.Examples
{
    public static class Program
    {
        static (string, Func<string[], Game>)[] examples = {
            ("pixel-lines", (args) => new PixelLinesApp(args)),
        };

        static void RunApp(string[] args)
        {
            if (args.Length >= 1)
            {
                string name = args[0];
                var example = Array.Find(examples, x => x.Item1 == name);
                if (example.Item2 != null)
                {
                    using (var app =
                        example.Item2(args.Skip(1).ToArray()))
                    {
                        app.Run();
                    }
                }
                else
                {
                    throw new ArgumentException($"Example not found '{name}'");
                };
            }
            else
            {
                throw new ArgumentException(
                    "Need name of an example as first argument"
                );
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                RunApp(args);
            }
            catch (ArgumentException e)
            {
                // List the possible examples.
                throw new ArgumentException(
                    Enumerable.Aggregate(
                        examples,
                        $"{e.Message}. Possible examples are:",
                        (acc, x) => $"{acc}\n  {x.Item1}"
                    )
                );
            }
        }
    }
}
