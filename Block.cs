public class Block
{
    private readonly int _length;

    public Block(byte[] file, int startIndex)
    {
        // get block length;
        var l0 = file[startIndex];
        var l1 = file[startIndex + 1];
        _length = l1 << 8 | l0;
    }

    public override string ToString()
    {
        return $"Block Size: {Length}";
    }

    public int Length => _length;
}
