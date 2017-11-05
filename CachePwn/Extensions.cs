using System.Runtime.InteropServices;

namespace CachePwn
{
    public static class Extensions
    {
        public static byte[] Serialise(this object obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            try
            {
                byte[] payload = new byte[Marshal.SizeOf(obj.GetType())];
                Marshal.Copy(handle.AddrOfPinnedObject(), payload, 0, payload.Length);
                return payload;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
