using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;

namespace Microsoft.Maui.Essentials.MacOS;

public static class EssentialsExtensions
{
    public static void UseMacOSEssentials()
    {
        SetStaticField(typeof(AppInfo), "currentImplementation", new AppInfoImplementation());
        SetStaticField(typeof(DeviceInfo), "currentImplementation", new DeviceInfoImplementation());
        SetStaticField(typeof(Connectivity), "currentImplementation", new ConnectivityImplementation());
        SetStaticField(typeof(Battery), "defaultImplementation", new BatteryImplementation());
    }

    static void SetStaticField(Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, value);
    }
}
