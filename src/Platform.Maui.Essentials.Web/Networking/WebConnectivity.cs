using Microsoft.Maui.Networking;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Networking;

/// <summary>
/// navigator.onLine plus the Network Information API where available. onLine only says a
/// network is attached, not that the internet is reachable, so "online" maps to Internet.
/// </summary>
public class WebConnectivity : IConnectivity
{
	EventHandler<ConnectivityChangedEventArgs>? _changed;
	bool _observing;
	NetworkAccess _lastAccess;
	ConnectionProfile[] _lastProfiles = [];

	public NetworkAccess NetworkAccess
		=> BrowserJs.Global("navigator")?.GetPropertyAsBoolean("onLine") == false ? NetworkAccess.None : NetworkAccess.Internet;

	public IEnumerable<ConnectionProfile> ConnectionProfiles
	{
		get
		{
			if (NetworkAccess == NetworkAccess.None)
				return [];
			// Without the module the type is unknown rather than wrong.
			var type = EssentialsJs.IsLoaded ? EssentialsJs.ConnectionType() : "";
			return [type switch
			{
				"wifi" => ConnectionProfile.WiFi,
				"ethernet" => ConnectionProfile.Ethernet,
				"cellular" => ConnectionProfile.Cellular,
				"bluetooth" => ConnectionProfile.Bluetooth,
				_ => ConnectionProfile.Unknown,
			}];
		}
	}

	public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged
	{
		add
		{
			_changed += value;
			if (_observing)
				return;
			// Listeners stay registered for the page's lifetime; events are dropped while unsubscribed.
			_observing = true;
			_lastAccess = NetworkAccess;
			_lastProfiles = ConnectionProfiles.ToArray();
			EssentialsJs.WhenLoaded(() => EssentialsJs.ObserveConnectivity(OnChanged));
		}
		remove => _changed -= value;
	}

	void OnChanged()
	{
		var access = NetworkAccess;
		var profiles = ConnectionProfiles.ToArray();
		if (access == _lastAccess && profiles.SequenceEqual(_lastProfiles))
			return;
		_lastAccess = access;
		_lastProfiles = profiles;
		_changed?.Invoke(this, new ConnectivityChangedEventArgs(access, profiles));
	}
}
