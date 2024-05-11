namespace HZDCoreTools.Util;

using System;

/// <summary>
/// CRC32-C.
/// </summary>
internal static class CRC32C
{
    private static readonly uint[] _lookupTable;

    static CRC32C()
    {
        // Castagnoli-CRC used by SSE4.2 instructions
        _lookupTable = new uint[256];

        for (uint i = 0; i < _lookupTable.Length; i++)
        {
            uint r = i;

            for (int j = 0; j < 8; j++)
                r = (r & 1) != 0 ? ((r >> 1) ^ 0x82F63B78) : (r >> 1);

            _lookupTable[i] = r;
        }
    }

    /// <summary>
    /// CRC32-C.
    /// </summary>
    /// <param name="data">The input data for which to calculate the CRC32-C checksum.</param>
    /// <param name="seed">The seed value for the CRC32-C calculation. Defaults to 0.</param>
    /// <returns>The calculated CRC32-C checksum of the input data.</returns>
    public static uint Checksum(ReadOnlySpan<byte> data, uint seed = 0)
    {
        for (int i = 0; i < data.Length; i++)
            seed = _lookupTable[(byte)seed ^ data[i]] ^ (seed >> 8);

        return seed;
    }
}
