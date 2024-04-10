public class Block
{
    private const byte MAGIC_PATH_START_0 = 0x31;
    private const byte MAGIC_PATH_START_1 = 0x1A;

    private readonly Response[] _responses;
    private readonly int _length;

    public Block(byte[] file, int startIndex)
    {
        // get block length;
        var l0 = file[startIndex];
        var l1 = file[startIndex + 1];
        _length = l1 << 8 | l0;
        var currentIndex = startIndex + 2;

        // go to block path string section
        currentIndex += 10;
        while (file[currentIndex] != MAGIC_PATH_START_0 ||
            file[currentIndex + 1] != MAGIC_PATH_START_1)
        {
            currentIndex++;
        }
        currentIndex += 1;

        // get "heading"
        // every response in one block shares the same starting 11 bytes".
        // "heading" is the word I use to call them. I don't know their real name or pupouse yet.
        // presumably they store date and time in some format.
        var responseHeading = new byte[11];
        for (var i = 0; i < 11; i++)
        {
            responseHeading[10 - i] = file[currentIndex - i];
        }
        currentIndex++;

        // split responces by "heading"
        var responses = new List<Response>();
        var responseStart = currentIndex;
        var blockEnd = startIndex + _length + 2;
        while (currentIndex < blockEnd)
        {
            var headingCheckIndex = 0;
            while (file[currentIndex + headingCheckIndex] == responseHeading[headingCheckIndex])
            {
                headingCheckIndex++;
                if (headingCheckIndex == responseHeading.Length)
                {
                    var responseLength = currentIndex - responseStart;
                    var response = new Response(file, responseStart, responseLength);
                    responses.Add(response);
                    currentIndex += responseHeading.Length;
                    responseStart = currentIndex;
                    break;
                }
            }
            currentIndex++;
        }
        var lastResponseLength = currentIndex - responseStart;
        var lastResponse = new Response(file, responseStart, lastResponseLength);
        responses.Add(lastResponse);
        _responses = responses.ToArray();
    }

    public override string ToString()
    {
        return $"Block Size: {Length}, Response Count: {ResponseCount}";
    }

    public Response[] Responses => _responses;
    public int ResponseCount => _responses.Length;
    public int Length => _length;
}
