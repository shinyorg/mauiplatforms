using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.TvOS;

public static class EssentialsExtensions
{
    public static void UseTvOSEssentials()
    {
        SetStaticField(typeof(AppInfo), "currentImplementation", new AppInfoImplementation());
        SetStaticField(typeof(DeviceInfo), "currentImplementation", new DeviceInfoImplementation());
        SetStaticField(typeof(Connectivity), "currentImplementation", new ConnectivityImplementation());
        SetStaticField(typeof(DeviceDisplay), "currentImplementation", new DeviceDisplayImplementation());
        SetStaticField(typeof(FileSystem), "currentImplementation", new FileSystemImplementation());
    }

    static void SetStaticField(Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, value);
    }
}
