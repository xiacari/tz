using K4os.Compression.LZ4;
using Newtonsoft.Json.Linq;
using System.Text;
using ZstdNet;

namespace tz
{
    public class Response
    {
        private const byte MAGIC_LZ4_BYTE = 0x59;

        private readonly string _type;
        private readonly JObject? _content;
        private readonly CompressionFormat _compressionFormat;
        private readonly int _length;
        private readonly int _contentLength;
        private readonly bool _hasDateTime;
        private readonly DateTime _dateTime;

        public Response(byte[] file, int startIndex, int length)
        {
            _length = length;
            var currentIndex = startIndex;
            var endIndex = startIndex + _length;

            // get path length
            var typeLength = file[currentIndex];
            currentIndex++;

            // get path
            var path = new StringBuilder();
            for (var i = 0; i < typeLength; i++, currentIndex++)
            {
                path.Append((char)file[currentIndex]);
            }
            _type = path.ToString();
            currentIndex++;

            // get format length
            var formatLength = file[currentIndex];
            currentIndex++;

            // get format
            var format = new StringBuilder();
            for (var i = 0; i < formatLength; i++, currentIndex++)
            {
                format.Append((char)file[currentIndex]);
            }
            _compressionFormat = format.ToString() switch
            {
                "lz4" => CompressionFormat.LZ4,
                "zstd" => CompressionFormat.ZSTD,
                _ => throw new Exception("Unknow compression format.")
            };

            // decompress response body
            if (_compressionFormat == CompressionFormat.ZSTD)
            {
                currentIndex += 2;
                var compressed = file[currentIndex..endIndex];
                using var decompressor = new Decompressor();
                // compressed data begins after 2 or 3 bits from format string end.
                // did't find way to defy how many exactly, so trying both variants
                try
                {
                    var content = Encoding.Default.GetString(decompressor.Unwrap(compressed));
                    _content = JObject.Parse(content);
                }
                catch (ZstdException e)
                {
                    currentIndex++;
                    compressed = file[currentIndex..endIndex];
                    var decompressed = decompressor.Unwrap(compressed);
                    var content = Encoding.Default.GetString(decompressed);
                    _content = JObject.Parse(content);
                    _contentLength = decompressed.Length;
                }
            }
            if (_compressionFormat == CompressionFormat.LZ4)
            {
                currentIndex++;
                var magicByte = file[currentIndex];
                // in some wierd cases body is presented in uncompressed state.
                // trying to decompress such responses breaks decompressor i use.
                // this cases appears when the next byte is equal to 0x59. don't know why.
                if (magicByte == MAGIC_LZ4_BYTE)
                {
                    currentIndex += 9;
                    var compressed = file[currentIndex..endIndex];
                    var content = Encoding.Default.GetString(compressed);
                    _content = JObject.Parse(content);
                    _contentLength = compressed.Length;
                }
                // get 2 4-bit integets. first for the decompressed body length, second is for compressed.
                else
                {
                    currentIndex += 2;
                    var d0 = file[currentIndex];
                    var d1 = file[currentIndex + 1];
                    var d2 = file[currentIndex + 2];
                    var d3 = file[currentIndex + 3];
                    var c0 = file[currentIndex + 4];
                    var c1 = file[currentIndex + 5];
                    var c2 = file[currentIndex + 6];
                    var c3 = file[currentIndex + 7];
                    currentIndex += 8;
                    var decompressedLength = d3 << 24 | d2 << 16 | d1 << 8 | d0;
                    var compressedLength = c3 << 24 | c2 << 16 | c1 << 8 | c0;
                    var compressed = file[currentIndex..endIndex];
                    var decompressed = new byte[decompressedLength];
                    LZ4Codec.Decode(compressed, 0, compressedLength, decompressed, 0, decompressedLength);
                    var content = Encoding.Default.GetString(decompressed);
                    _content = JObject.Parse(content);
                    _contentLength = decompressed.Length;
                }
            }

            // some responses have current moment timestamps in them.
            // use them to get response time.
            _hasDateTime = TryGetDateTime(out var dateTime);
            if (_hasDateTime)
            {
                _dateTime = dateTime;
            }
        }

        private bool TryGetDateTime(out DateTime dateTime)
        {
            dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            if (!(_content?["data"] is JObject data))
                return false;
            if (!(data["ts"]?.Value<long>() is long timeStamp))
                return false;
            if (timeStamp == 0)
                return false;
            dateTime = dateTime.AddMilliseconds(timeStamp).ToLocalTime();
            return true;
        }

        public override string ToString()
        {
            return _hasDateTime ? $"Type: {Type}, Time: {_dateTime}, Compression: {CompressionFormat}, Size: {Length}"
                                : $"Type: {Type}, Compression: {CompressionFormat}, Size: {Length}";
        }

        public string Type => _type;
        public JObject? Content => _content;
        public CompressionFormat CompressionFormat => _compressionFormat;
        public int Length => _length;
        public int ContentLength => _contentLength;
        public bool HasDateTime => _hasDateTime;
        public DateTime DateTime => _dateTime;
    }
}
