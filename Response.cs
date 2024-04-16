using K4os.Compression.LZ4;
using Newtonsoft.Json.Linq;
using System.Text;
using ZstdNet;

namespace tz
{
    public class Response
    {
        private readonly string _type;
        private readonly JObject? _content;
        private readonly CompressionFormat _compressionFormat;
        private readonly int _compressedSize;
        private readonly int _decompressedSize;
        private readonly DateTime _dateTime;

        public Response(RawResponse raw)
        {
            var contentBytes = raw.Content.ToByteArray();
            _compressedSize = contentBytes.Length;
            
            _type = raw.Type;
            _dateTime = GetDateTime(raw.Timestamp);

            if (raw.Compression == "zstd")
            {
                _compressionFormat = CompressionFormat.ZSTD;
                using var decompressor = new Decompressor();
                var decompressed = decompressor.Unwrap(contentBytes);
                var content = Encoding.Default.GetString(decompressed);
                _content = JObject.Parse(content);
                _decompressedSize = decompressed.Length;
            }
            else if (raw.Compression == "lz4")
            {
                _compressionFormat = CompressionFormat.LZ4;
                var d0 = contentBytes[0];
                var d1 = contentBytes[1];
                var d2 = contentBytes[2];
                var d3 = contentBytes[3];
                var c0 = contentBytes[4];
                var c1 = contentBytes[5];
                var c2 = contentBytes[6];
                var c3 = contentBytes[7];
                var decompressedLength = d3 << 24 | d2 << 16 | d1 << 8 | d0;
                var compressedLength = c3 << 24 | c2 << 16 | c1 << 8 | c0;
                if (decompressedLength == compressedLength)
                {
                    var decompressed = contentBytes[8..];
                    var content = Encoding.Default.GetString(decompressed);
                    _content = JObject.Parse(content);
                    _decompressedSize = decompressed.Length;
                }
                else
                {
                    var compressed = contentBytes[8..];
                    var decompressed = new byte[decompressedLength];
                    LZ4Codec.Decode(compressed, 0, compressedLength, decompressed, 0, decompressedLength);
                    var content = Encoding.Default.GetString(decompressed);
                    _content = JObject.Parse(content);
                    _decompressedSize = decompressed.Length;
                }
            }
            else
                throw new Exception("Unable to defy compression format.");
        }

        private DateTime GetDateTime(ulong timestamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
            return dateTime;
        }

        public override string ToString()
        {
            return $"Type: {Type}, Time: {DateTime}, Compression: {CompressionFormat}, Size: {CompressedSize}";
        }

        public string Type => _type;
        public JObject? Content => _content;
        public CompressionFormat CompressionFormat => _compressionFormat;
        public int CompressedSize => _compressedSize;
        public int DecompressedSize => _decompressedSize;
        public DateTime DateTime => _dateTime;
    }
}
