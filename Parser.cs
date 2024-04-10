public static class Parser
{
    public static Block[] _blocks;

    public static void Main(string[] args)
    {
        Console.WriteLine("Parser program is running. Type \"help\" to see full command list.");
        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
                continue;
            var split = input.Split(' ');
            var command = split[0];
            var arguments = split[1..];
            switch (command)
            {
                case "help":
                    Help(arguments);
                    break;
                case "clear":
                    Clear(arguments);
                    break;
                case "parse":
                    Parse(arguments);
                    break;
                case "exit":
                    return;
                default:
                    Console.WriteLine("    Unknow command. Type \"help\" to see full command list.");
                    break;
            }
        }
    }

    private static void Help(string[] args)
    {
        Console.WriteLine("    0: help");
        Console.WriteLine("    Displays full list of commands.\n");
        Console.WriteLine("    1: clear");
        Console.WriteLine("    Clears console.\n");
        Console.WriteLine("    2: parse [filePath]");
        Console.WriteLine("    Parses file to blocks. Displays each block size.");
        Console.WriteLine("    3: exit");
        Console.WriteLine("    Exits the program.\n");
    }

    private static void Clear(string[] args)
    {
        Console.Clear();
    }

    private static void Parse(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("    Wrong arguments count.");
            return;
        }
        var path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine("    File does not exits.");
            return;
        }

        var blocks = new List<Block>();
        var file = File.ReadAllBytes(path);
        var currentIndex = 0;
        while (currentIndex < file.Length)
        {
            var block = new Block(file, currentIndex);
            currentIndex += block.Length + 2;
            blocks.Add(block);
        }
        _blocks = blocks.ToArray();
        Console.WriteLine("    File parsed successfully.");
        Console.WriteLine($"    Block count: {_blocks.Length}");
        for (var i = 0; i < _blocks.Length; i++)
        {
            Console.WriteLine($"    {i}: {_blocks[i]}");
        }
        Console.WriteLine();
    }
}