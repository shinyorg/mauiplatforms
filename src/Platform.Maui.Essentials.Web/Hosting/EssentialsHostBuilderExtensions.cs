using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Essentials.Web.Accessibility;
using Microsoft.Maui.Essentials.Web.AppModel;
using Microsoft.Maui.Essentials.Web.Communication;
using Microsoft.Maui.Essentials.Web.DataTransfer;
using Microsoft.Maui.Essentials.Web.Devices;
using Microsoft.Maui.Essentials.Web.Media;
using Microsoft.Maui.Essentials.Web.Networking;
using Microsoft.Maui.Essentials.Web.Sensors;
using Microsoft.Maui.Essentials.Web.Storage;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web;

public static class EssentialsHostBuilderExtensions
{
	/// <summary>
	/// Registers browser implementations of MAUI Essentials and wires the static facades
	/// (<c>Preferences.Default</c>, <c>Geolocation.Default</c>, …) to them when the app is built.
	/// </summary>
	public static MauiAppBuilder UseWebEssentials(this MauiAppBuilder builder)
	{
		var services = builder.Services;

		// App model — IAppInfo comes from the core backend (WebAppInfo).
		services.TryAddSingleton<IAppInfo, WebAppInfo>();
		services.TryAddSingleton<IVersionTracking, WebVersionTracking>();
		services.TryAddSingleton<IBrowser, WebBrowser>();
		services.TryAddSingleton<ILauncher, WebLauncher>();
		services.TryAddSingleton<IMap, WebMap>();
		services.TryAddSingleton<IAppActions, WebAppActions>();

		// Devices
		services.TryAddSingleton<IDeviceInfo, WebDeviceInfo>();
		services.TryAddSingleton<IDeviceDisplay, WebDeviceDisplay>();
		services.TryAddSingleton<IBattery, WebBattery>();
		services.TryAddSingleton<IVibration, WebVibration>();
		services.TryAddSingleton<IHapticFeedback, WebHapticFeedback>();
		services.TryAddSingleton<IFlashlight, WebFlashlight>();

		// Storage
		services.TryAddSingleton<IPreferences, WebPreferences>();
		services.TryAddSingleton<ISecureStorage, WebSecureStorage>();
		services.TryAddSingleton<IFileSystem, WebFileSystem>();
		services.TryAddSingleton<IFilePicker, WebFilePicker>();

		// Networking, data transfer & communication
		services.TryAddSingleton<IConnectivity, WebConnectivity>();
		services.TryAddSingleton<IClipboard, WebClipboard>();
		services.TryAddSingleton<IShare, WebShare>();
		services.TryAddSingleton<IEmail, WebEmail>();
		services.TryAddSingleton<IPhoneDialer, WebPhoneDialer>();
		services.TryAddSingleton<ISms, WebSms>();
		services.TryAddSingleton<IContacts, WebContacts>();

		// Sensors & location
		services.TryAddSingleton<IGeolocation, WebGeolocation>();
		services.TryAddSingleton<IGeocoding, WebGeocoding>();
		services.TryAddSingleton<IAccelerometer, WebAccelerometer>();
		services.TryAddSingleton<IGyroscope, WebGyroscope>();
		services.TryAddSingleton<IMagnetometer, WebMagnetometer>();
		services.TryAddSingleton<IOrientationSensor, WebOrientationSensor>();
		services.TryAddSingleton<ICompass, WebCompass>();
		services.TryAddSingleton<IBarometer, WebBarometer>();

		// Media & accessibility
		services.TryAddSingleton<ITextToSpeech, WebTextToSpeech>();
		services.TryAddSingleton<IMediaPicker, WebMediaPicker>();
		services.TryAddSingleton<IScreenshot, WebScreenshot>();
		services.TryAddSingleton<ISemanticScreenReader, WebSemanticScreenReader>();

		services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeService, EssentialsInitializer>());
		return builder;
	}
}
