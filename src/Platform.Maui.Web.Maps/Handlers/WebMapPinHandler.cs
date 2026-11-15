using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;

namespace Microsoft.Maui.Platform.Web.Maps.Handlers;

/// <summary>
/// A <c>Pin</c> (<see cref="IMapPin"/>) on Platform.Maui.Web. The marker itself lives in the map's
/// Leaflet layer; this handler only forwards the pin's property changes to its <see cref="WebMapHandler"/>.
/// </summary>
public class WebMapPinHandler : MapPinHandler
{
	public static new IPropertyMapper<IMapPin, WebMapPinHandler> Mapper =
		new PropertyMapper<IMapPin, WebMapPinHandler>(ElementHandler.ElementMapper)
		{
			[nameof(IMapPin.Location)] = MapPin,
			[nameof(IMapPin.Label)] = MapPin,
			[nameof(IMapPin.Address)] = MapPin,
			[nameof(IMapPin.ImageSource)] = MapPin,
			[nameof(IMapPin.ClusteringIdentifier)] = MapPin,
		};

	public WebMapPinHandler() : base(Mapper)
	{
	}

	// Nothing to create: the map handler owns the marker.
	protected override object CreatePlatformElement() => new();

	public static void MapPin(WebMapPinHandler handler, IMapPin pin)
		=> (pin.Parent?.Handler as WebMapHandler)?.UpdatePin(pin);
}
