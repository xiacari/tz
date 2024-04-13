using System.Text;
using System.Xml.Linq;

namespace tz
{
    public static class Parser
    {
        public static Block[] _blocks;

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
                var block = new Block(file, currentIndex);
                currentIndex += block.Length + 2;
                blocks.Add(block);
            }
            _blocks = blocks.ToArray();
        }

        private static void Parse()
        {
            Console.WriteLine(GetParse());
            Console.WriteLine("File is parsed successfully.");
        }

        private static void PrintAll()
        {
            Console.WriteLine($"Blocks: {_blocks.Length}");
            for (var i = 0; i < _blocks.Length; i++)
            {
                for (var j = 0; j < _blocks[i].ResponseCount; j++)
                {
                    Console.WriteLine($"{i}.{j}:\n{GetResponse(i, j, Newtonsoft.Json.Formatting.None)}");
                }
            }
        }

        private static void Info()
        {
            Console.WriteLine(GetInfo());
        }

        private static void Save(string path)
        {
            path += "_parsed";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var fileInfo = GetParse() + "\n" + GetInfo();
            File.WriteAllText(path + "/info.txt", fileInfo);

            path += "/responses";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            for (var i = 0; i < _blocks.Length; i++)
            {
                for (var j = 0; j < _blocks[i].ResponseCount; j++)
                {
                    var responsePath = path + $"/{i}.{j}.txt";
                    var response = GetResponse(i, j, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(responsePath, response);
                }
            }
            Console.WriteLine("Parsed info is saved.");
        }

        private static string GetParse()
        {
            var result = new StringBuilder();

            result.AppendLine($"Blocks: {_blocks.Length}");
            for (var i = 0; i < _blocks.Length; i++)
            {
                result.AppendLine($"{i}: {_blocks[i]}");
                for (var j = 0; j < _blocks[i].ResponseCount; j++)
                {
                    result.AppendLine($"  {j}: {_blocks[i].Responses[j]}");
                }
            }
            return result.ToString();
        }

        private static string GetInfo()
        {
            var result = new StringBuilder();

            var responses = _blocks.SelectMany(x => x.Responses).ToArray();
            var sorted = responses.GroupBy(x => x.Type)
                .Select(x => x.ToArray())
                .OrderByDescending(x => x.Length)
                .ToArray();

            for (var i = 0; i < sorted.Length; i++)
            {
                var avarageSize = (int)Math.Floor(sorted[i].Average(x => x.ContentLength));
                result.AppendLine($"{i}: {sorted[i][0].Type}");
                result.AppendLine($"  Count: {sorted[i].Length}");
                result.AppendLine($"  Average Size: {avarageSize}");
            }

            var firstDateTime = responses.FirstOrDefault(x => x.HasDateTime)?.DateTime;
            var lastDateTime = responses.LastOrDefault(x => x.HasDateTime)?.DateTime;
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

        private static string GetResponse(int blockIndex, int responseIndex, Newtonsoft.Json.Formatting formatting)
        {
            var result = new StringBuilder();

            var response = _blocks[blockIndex].Responses[responseIndex];
            result.AppendLine($"  Type: {response.Type}");
            if (response.HasDateTime)
            {
                result.AppendLine($"Time: {response.DateTime}");
            }
            result.AppendLine($"  Compression format: {response.CompressionFormat}");
            result.AppendLine($"  Size (compressed): {response.Length}");
            result.AppendLine($"  Size (decompressed): {response.ContentLength}");
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
