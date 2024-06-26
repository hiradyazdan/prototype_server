using System;

namespace prototype_server.Specs.Config.Utils.Helpers
{
    internal static partial class Helpers
    {
        internal static Guid ConvertBytesToGuid(byte[] valueBytes)
        {
            const int guidByteSize = 16;

            if (valueBytes.Length != guidByteSize)
            {
                Array.Resize(ref valueBytes, guidByteSize);
            }

            return new Guid(valueBytes);
        }
    }
}