namespace tz
{
    public class Block
    {
        private readonly Response[] _responses;
        private readonly int _length;

        public Block(int length, RawBlock raw)
        {
            _length = length;
            _responses = raw.Responses.Select(x => new Response(x)).ToArray();
        }

        public override string ToString()
        {
            return $"Block Size: {Length}, Response Count: {ResponseCount}";
        }

        public Response[] Responses => _responses;
        public int ResponseCount => _responses.Length;
        public int Length => _length;
    }
}

