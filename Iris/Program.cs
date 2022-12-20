using Iris;

if (args.Length == 0)
{
    Console.WriteLine("Please enter a ROM file name.");
    Console.WriteLine("Usage: Iris.exe <rom file>");
    return 1;
}

GBA gba = new();
gba.LoadROM(args[0]);
gba.Run();

return 0;
