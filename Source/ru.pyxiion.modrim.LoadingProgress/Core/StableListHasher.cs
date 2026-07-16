using System.Text;

namespace ru.pyxiion.modrim.LoadingProgress;

internal static class StableListHasher
{
    public static int ComputeListHash(IEnumerable<string> items)
    {
        var combined = string.Join("\0", items ?? []);
        var data = Encoding.UTF8.GetBytes(combined);
        return MurmurHash3(data, 42 + 69 + 420);
    }

    private static int MurmurHash3(byte[] data, uint seed)
    {
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;
        const int r1 = 15;
        const int r2 = 13;
        const uint m = 5;
        const uint n = 0xe6546b64;

        var hash = seed;
        var length = data.Length;
        var remainder = length & 3;
        var blocks = length / 4;

        for (var i = 0; i < blocks; i++)
        {
            var index = i * 4;
            var k = BitConverter.ToUInt32(data, index);

            k *= c1;
            k = RotateLeft(k, r1);
            k *= c2;

            hash ^= k;
            hash = RotateLeft(hash, r2);
            hash = (hash * m) + n;
        }

        uint k1 = 0;
        if (remainder > 0)
        {
            switch (remainder)
            {
                case 3:
                    k1 ^= (uint)data[length - 3] << 16;
                    goto case 2;
                case 2:
                    k1 ^= (uint)data[length - 2] << 8;
                    goto case 1;
                case 1:
                    k1 ^= data[length - 1];
                    k1 *= c1;
                    k1 = RotateLeft(k1, r1);
                    k1 *= c2;
                    hash ^= k1;
                    break;
                default:
                    throw new NotSupportedException("Invalid remainder length for MurmurHash3.");
            }
        }

        hash ^= (uint)length;
        hash = FMix(hash);

        return unchecked((int)hash);
    }

    private static uint RotateLeft(uint x, int r) => (x << r) | (x >> (32 - r));

    private static uint FMix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}
