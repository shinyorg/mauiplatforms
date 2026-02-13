using Foundation;
using UIKit;
using ObjCRuntime;
using Microsoft.Maui.Devices;

namespace Microsoft.Maui.Essentials.TvOS;

class DeviceInfoImplementation : IDeviceInfo
{
    public string Model
    {
        get
        {
            try
            {
                var pLen = IntPtr.Zero;
                var pStr = IntPtr.Zero;

                NativeMethods.sysctlbyname("hw.machine", IntPtr.Zero, ref pLen, IntPtr.Zero, 0);
                if (pLen == IntPtr.Zero)
                    return UIDevice.CurrentDevice.Model;

                pStr = System.Runtime.InteropServices.Marshal.AllocHGlobal(pLen);
                NativeMethods.sysctlbyname("hw.machine", pStr, ref pLen, IntPtr.Zero, 0);
                return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(pStr) ?? UIDevice.CurrentDevice.Model;
            }
            catch
            {
                return UIDevice.CurrentDevice.Model;
            }
        }
    }

    public string Manufacturer => "Apple";

    public string Name => UIDevice.CurrentDevice.Name;

    public string VersionString => UIDevice.CurrentDevice.SystemVersion;

    public Version Version => Utils.ParseVersion(VersionString);

    public DevicePlatform Platform => DevicePlatform.tvOS;

    public DeviceIdiom Idiom => DeviceIdiom.TV;

    public DeviceType DeviceType =>
        Runtime.Arch == Arch.DEVICE ? DeviceType.Physical : DeviceType.Virtual;

    static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("libc")]
        public static extern int sysctlbyname(string property, IntPtr output, ref IntPtr oldLen, IntPtr newp, uint newlen);
    }
}
