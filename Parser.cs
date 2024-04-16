using System.Text;

namespace tz
{
    public static class Parser
    {
        public static Block[] _blocks;
        public static Response[] _responses;

        public static void Main(string[] args)
        {
            
            if (args.Length < 1)
            {
                Console.WriteLine("Wrong arguments.");
                return;
            }
            var command = args[0];
            var arguments = args[1..];

            if (arguments.Length < 1)
            {
                Console.WriteLine("Wrong arguments.");
                return;
            }
            var path = arguments[0];
            if (!File.Exists(path))
            {
                Console.WriteLine("    File does not exits.");
                return;
            }
            ParseFile(path);

            switch (command)
            {
                case "parse":
                    Parse();
                    break;
                case "printall":
                    PrintAll();
                    break;
                case "info":
                    Info();
                    break;
                case "save":
                    Save(path);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
            
        }
        
        private static void ParseFile(string path)
        {
            var blocks = new List<Block>();
            var file = File.ReadAllBytes(path);
            var currentIndex = 0;
            while (currentIndex < file.Length)
            {
                var len0 = file[currentIndex];
                var len1 = file[currentIndex + 1];
                var blockLength = len1 << 8 | len0;
                var start = currentIndex + 2;
                var end = start + blockLength;
                var blockBytes = file[start..end];
                var block = RawBlock.Parser.ParseFrom(blockBytes);
                currentIndex += blockLength + 2;
                blocks.Add(new Block(blockLength, block));
            }
            _blocks = blocks.ToArray();
            _responses = blocks.SelectMany(x => x.Responses).ToArray();
        }
        
        
        private static void Parse()
        {
            Console.WriteLine(GetParseText());
            Console.WriteLine("File is parsed successfully.");
        }
        
        
        private static void PrintAll()
        {
            Console.WriteLine($"Responses: {_responses.Length}");
            for (var i = 0; i < _responses.Length; i++)
            {
                Console.WriteLine($"{i}:\n{GetResponseText(_responses[i], Newtonsoft.Json.Formatting.None)}");
            }
        }
        
        private static void Info()
        {
            Console.WriteLine(GetInfoText());
        }
        
        private static void Save(string path)
        {
            path += "_parsed";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var fileInfo = GetParseText() + "\n" + GetInfoText();
            File.WriteAllText(path + "/info.txt", fileInfo);

            path += "/responses";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            for (var i = 0; i < _responses.Length; i++)
            {
                var responsePath = path + $"/{i}.txt";
                var response = GetResponseText(_responses[i], Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(responsePath, response);
            }
            Console.WriteLine("Parsed info is saved.");
        }
        
        
        private static string GetParseText()
        {
            var result = new StringBuilder();

            for (var i = 0; i < _blocks.Length; i++)
            {
                result.AppendLine($"{i}: {_blocks[i]}");
                for (var j = 0; j < _blocks[i].Responses.Length; j++)
                {
                    result.AppendLine($"  {j}: {_blocks[i].Responses[j]}");
                }
            }
            result.AppendLine($"\nBlocks: {_blocks.Length}");
            result.AppendLine($"Responses: {_responses.Length}");
            return result.ToString();
        }
        
        private static string GetInfoText()
        {
            var result = new StringBuilder();

            var responses = _blocks.SelectMany(x => x.Responses).ToArray();
            var sorted = responses.GroupBy(x => x.Type)
                .Select(x => x.ToArray())
                .OrderByDescending(x => x.Length)
                .ToArray();

            for (var i = 0; i < sorted.Length; i++)
            {
                var avarageSize = (int)Math.Floor(sorted[i].Average(x => x.DecompressedSize));
                result.AppendLine($"{i}: {sorted[i][0].Type}");
                result.AppendLine($"  Count: {sorted[i].Length}");
                result.AppendLine($"  Average Size: {avarageSize}");
            }

            var firstDateTime = responses.First()?.DateTime;
            var lastDateTime = responses.Last()?.DateTime;
            if (firstDateTime != null && lastDateTime != null)
            {
                var sessionTime = lastDateTime - firstDateTime;
                result.AppendLine($"\nSession time: {sessionTime?.ToString(@"hh\:mm\:ss")}");
            }
            else
            {
                result.AppendLine("\nSession time: Unknown");
            }
            return result.ToString();
        }

        private static string GetResponseText(Response response, Newtonsoft.Json.Formatting formatting)
        {
            var result = new StringBuilder();

            result.AppendLine($"  Type: {response.Type}");
            result.AppendLine($"  Time: {response.DateTime}");
            result.AppendLine($"  Compression format: {response.CompressionFormat}");
            result.AppendLine($"  Size (compressed): {response.CompressedSize}");
            result.AppendLine($"  Size (decompressed): {response.DecompressedSize}");
            if (formatting == Newtonsoft.Json.Formatting.None)
            {
                result.AppendLine($"  Content: {response.Content?.ToString(formatting)}");
            }
            else
            {
                result.AppendLine($"  Content:\n{response.Content?.ToString(formatting)}");
            }
            
            return result.ToString();
        }
        
    }
}
