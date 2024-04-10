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
                case "list":
                    List(arguments);
                    break;
                case "response":
                    Response(arguments);
                    break;
                case "info":
                    Info(arguments);
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
        Console.WriteLine("    Parses file to blocks of responses.");
        Console.WriteLine("    You need to use this command before using commands below.\n");
        Console.WriteLine("    3: list");
        Console.WriteLine("    Displays list of parsed blocks and responses. List indexes can be used as arguments.\n");
        Console.WriteLine("    4: response [blockIndex] <responseIndex>");
        Console.WriteLine("    Displays full information about specified response.");
        Console.WriteLine("    If <responseIndex> left empty, selects the first response in block.\n");
        Console.WriteLine("    5: info");
        Console.WriteLine("    Displays every response type count and their average byte size.");
        Console.WriteLine("    Also shows session's start and end approximate time.\n");
        Console.WriteLine("    6: exit");
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
        Console.WriteLine($"    Blocks: {_blocks.Length}");
        Console.WriteLine();
    }
    private static void List(string[] args)
    {
        if (_blocks == null)
        {
            Console.WriteLine("    Nothing is parsed yet. Use \"parse\" command.");
            return;
        }
        for (var i = 0; i < _blocks.Length; i++)
        {
            Console.WriteLine($"    {i}: {_blocks[i]}");
            for (var j = 0; j < _blocks[i].ResponseCount; j++)
            {
                Console.WriteLine($"        {j}: {_blocks[i].Responses[j]}");
            }
        }
        Console.WriteLine();
    }
    private static void Response(string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            Console.WriteLine("    Wrong arguments count.");
            return;
        }
        if (!int.TryParse(args[0], out var blockIndex))
        {
            Console.WriteLine("    Block index must be a number");
            return;
        }
        var responseIndex = 0;
        if (args.Length == 2)
        {
            if (!int.TryParse(args[1], out responseIndex))
            {
                Console.WriteLine("    Response index must be a number");
                return;
            }
        }
        if (_blocks == null)
        {
            Console.WriteLine("    Nothing is parsed yet. Use \"parse\" command.");
            return;
        }
        if (blockIndex < 0 || blockIndex >= _blocks.Length)
        {
            Console.WriteLine("    Block index is out of range.");
            return;
        }
        var block = _blocks[blockIndex];
        if (responseIndex < 0 || responseIndex >= block.ResponseCount)
        {
            Console.WriteLine("    Response index is out of range.");
            return;
        }

        var response = _blocks[blockIndex].Responses[responseIndex];
        Console.WriteLine($"    Type: {response.Type}");
        Console.WriteLine($"    Compression format: {response.CompressionFormat}");
        Console.WriteLine($"    Size: {response.Length}");
        Console.WriteLine($"    Content: {response.Content}");
        Console.WriteLine();
    }

    private static void Info(string[] args)
    {
        var responses = _blocks.SelectMany(x => x.Responses).ToArray();
        var sorted = responses.GroupBy(x => x.Type)
            .Select(x => x.ToArray())
            .OrderByDescending(x => x.Length)
            .ToArray();

        for (var i = 0; i < sorted.Length; i++)
        {
            var avarageSize = (int)Math.Floor(sorted[i].Average(x => x.Length));
            Console.WriteLine($"    {i}: {sorted[i][0].Type}");
            Console.WriteLine($"      Count: {sorted[i].Length}");
            Console.WriteLine($"      Average Size: {avarageSize}");
        }

        var firstDateTime = responses.FirstOrDefault(x => x.HasDateTime)?.DateTime;
        var lastDateTime = responses.LastOrDefault(x => x.HasDateTime)?.DateTime;
        if (firstDateTime != null && lastDateTime != null)
        {
            var sessionTime = lastDateTime - firstDateTime;
            Console.WriteLine($"\n    Session time: {sessionTime?.ToString(@"hh\:mm\:ss")}\n");
        }
        else
        {
            Console.WriteLine("\n    Session time: Unknown\n");
        }
    }
}