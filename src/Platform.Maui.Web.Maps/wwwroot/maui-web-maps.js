// Leaflet bridge for Platform.Maui.Web.Maps.
// Every export is bound by Interop/MapsJs.cs through [JSImport]; keep the two in sync.
//
// Events go to .NET as onEvent(type, json). The return value suppresses the default action:
//   marker  -> true keeps the info window closed     info    -> true closes the info window
//   cluster -> true skips zooming into the cluster   others  -> ignored

const states = new WeakMap();
const lib = path => new URL(`lib/${path}`, import.meta.url).href;
const DEFAULT_STROKE = '#3388ff';
const PIN_SVG = '<svg xmlns="http://www.w3.org/2000/svg" width="27" height="41" viewBox="0 0 27 41" aria-hidden="true">'
    + '<path d="M13.5 1C6.6 1 1 6.6 1 13.5 1 23 13.5 40 13.5 40S26 23 26 13.5C26 6.6 20.4 1 13.5 1z" fill="#ea4335" stroke="#a52714" stroke-width="1.5"/>'
    + '<circle cx="13.5" cy="13.5" r="4.5" fill="#fff"/></svg>';

let leafletLoad;
let clusterLoad;

// ---- loading ----------------------------------------------------------------

function loadCss(href) {
    if (document.querySelector(`link[data-maui-map="${href}"]`)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.dataset.mauiMap = href;
        link.onload = resolve;
        link.onerror = () => reject(new Error(`Failed to load ${href}`));
        document.head.append(link);
    });
}

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = () => reject(new Error(`Failed to load ${src}`));
        document.head.append(script);
    });
}

// A failed load is forgotten, so the next map tries again.
function ensureLeaflet() {
    leafletLoad ??= Promise.all([
        loadCss(lib('leaflet/leaflet.css')),
        loadCss(new URL('maui-web-maps.css', import.meta.url).href),
        window.L ? Promise.resolve() : loadScript(lib('leaflet/leaflet.js')),
    ]).catch(err => { leafletLoad = null; throw err; });
    return leafletLoad;
}

function ensureCluster() {
    clusterLoad ??= ensureLeaflet()
        .then(() => Promise.all([
            loadCss(lib('leaflet.markercluster/MarkerCluster.css')),
            loadCss(lib('leaflet.markercluster/MarkerCluster.Default.css')),
            L.MarkerClusterGroup ? Promise.resolve() : loadScript(lib('leaflet.markercluster/leaflet.markercluster.js')),
        ]))
        .catch(err => { clusterLoad = null; throw err; });
    return clusterLoad;
}

// ---- map --------------------------------------------------------------------

const point = latlng => JSON.stringify({ lat: latlng.lat, lng: latlng.lng });

export async function create(host, optionsJson, onEvent, clusterIcon) {
    await ensureLeaflet();

    const el = document.createElement('div');
    el.className = 'maui-map-leaflet';
    el.style.position = 'absolute';
    el.style.inset = '0';
    host.append(el);

    // zoomSnap 0 lets MoveToRegion fit a span exactly, as the native maps do.
    const map = L.map(el, { zoomSnap: 0, zoomDelta: 1, wheelPxPerZoomLevel: 120, worldCopyJump: true });
    map.setView([0, 0], 1);

    const s = {
        map, el, onEvent, clusterIcon,
        options: JSON.parse(optionsJson),
        destroyed: false,
        sized: false,
        pending: null,
        mapType: -1,
        base: null,
        labels: null,
        pins: new Map(),
        plain: L.layerGroup().addTo(map),
        clustering: false,
        customClusterIcons: false,
        groups: new Map(),
        shapes: new Map(),
        user: null,
        watch: null,
        observer: null,
    };
    states.set(host, s);

    map.on('moveend', () => reportRegion(s));
    map.on('click', e => onEvent('click', point(e.latlng)));
    // Leaflet raises contextmenu for a right click and for a long press on touch screens.
    map.on('contextmenu', e => onEvent('longclick', point(e.latlng)));

    // The map has no size until MAUI lays it out (and again whenever its page is shown), so sizing
    // drives Leaflet's layout and the region that was requested before the map could show it.
    s.observer = new ResizeObserver(() => resized(s));
    s.observer.observe(el);
}

export function destroy(host) {
    const s = states.get(host);
    if (!s) return;
    states.delete(host);
    s.destroyed = true;
    s.observer?.disconnect();
    if (s.watch !== null) navigator.geolocation.clearWatch(s.watch);
    s.map.remove();
    s.el.remove();
}

function resized(s) {
    if (s.destroyed) return;
    s.map.invalidateSize({ pan: false });
    if (s.el.clientWidth === 0 || s.el.clientHeight === 0) return;

    const first = !s.sized;
    s.sized = true;
    if (s.pending) {
        const region = s.pending;
        s.pending = null;
        fitRegion(s, region, false);
    } else if (first) {
        reportRegion(s);
    }
}

function reportRegion(s) {
    if (!s.sized || s.destroyed) return;
    const bounds = s.map.getBounds();
    const center = s.map.getCenter().wrap();
    s.onEvent('region', JSON.stringify({
        lat: center.lat,
        lng: center.lng,
        latDeg: bounds.getNorth() - bounds.getSouth(),
        lngDeg: bounds.getEast() - bounds.getWest(),
    }));
}

const clampLat = lat => Math.max(-85, Math.min(85, lat));

function fitRegion(s, r, animated) {
    const bounds = L.latLngBounds(
        [clampLat(r.lat - r.latDeg / 2), r.lng - r.lngDeg / 2],
        [clampLat(r.lat + r.latDeg / 2), r.lng + r.lngDeg / 2]);
    s.map.fitBounds(bounds, { animate: animated });
}

export function moveTo(host, lat, lng, latDeg, lngDeg, animated) {
    const s = states.get(host);
    if (!s) return;
    const region = { lat, lng, latDeg, lngDeg };
    if (s.sized) fitRegion(s, region, animated);
    else s.pending = region;
}

let SeamlessTileLayer;

// At fractional zoom levels (zoomSnap 0) browsers leave hairline seams between the scaled tiles, so
// base tiles are drawn a pixel larger and overlap their neighbours (maui-web-maps.css turns off the
// blend Leaflet uses for the same purpose, which would brighten the overlap). _initTile is internal to
// Leaflet, which is pinned (lib/leaflet is 1.9.4).
function tileLayer(t, seamless) {
    SeamlessTileLayer ??= L.TileLayer.extend({
        _initTile(tile) {
            L.TileLayer.prototype._initTile.call(this, tile);
            const size = this.getTileSize();
            tile.style.width = `${size.x + 1}px`;
            tile.style.height = `${size.y + 1}px`;
        },
    });
    const options = { attribution: t.attribution, maxZoom: t.maxZoom, subdomains: t.subdomains };
    return seamless
        ? new SeamlessTileLayer(t.url, { ...options, className: 'maui-map-seamless' })
        : L.tileLayer(t.url, options);
}

// 0 = Street, 1 = Satellite, 2 = Hybrid (Microsoft.Maui.Maps.MapType).
export function setMapType(host, type) {
    const s = states.get(host);
    if (!s || s.mapType === type) return;
    s.mapType = type;
    s.base?.remove();
    s.labels?.remove();

    const { street, satellite, labels } = s.options;
    s.base = tileLayer(type !== 0 && satellite ? satellite : street, true).addTo(s.map);
    // Overlapping transparent label tiles would double the labels' edges, and their gaps don't show.
    s.labels = type === 2 && satellite && labels ? tileLayer(labels, false).addTo(s.map) : null;
}

function toggle(handler, on) {
    if (on) handler.enable();
    else handler.disable();
}

export function setInteraction(host, scroll, zoom) {
    const s = states.get(host);
    if (!s) return;
    const m = s.map;
    toggle(m.dragging, scroll);
    toggle(m.scrollWheelZoom, zoom);
    toggle(m.doubleClickZoom, zoom);
    toggle(m.touchZoom, zoom);
    toggle(m.boxZoom, zoom);
    toggle(m.keyboard, scroll || zoom);
    if (zoom) m.zoomControl.addTo(m);
    else m.zoomControl.remove();
}

// ---- pins -------------------------------------------------------------------

let defaultIcon;

function imageIcon(url, className) {
    const img = document.createElement('img');
    img.src = url;
    img.alt = '';
    // iconSize null leaves the size to the image; maui-web-maps.css anchors it.
    return L.divIcon({ className, html: img, iconSize: null });
}

function pinIcon(p) {
    if (p.icon) return imageIcon(p.icon, 'maui-map-pin maui-map-pin-image');
    defaultIcon ??= L.divIcon({ className: 'maui-map-pin', html: PIN_SVG, iconSize: [27, 41], iconAnchor: [13.5, 41] });
    return defaultIcon;
}

function groupFor(s, name) {
    let group = s.groups.get(name);
    if (group) return group;

    group = L.markerClusterGroup({
        zoomToBoundsOnClick: false,
        showCoverageOnHover: false,
        iconCreateFunction: cluster => clusterIconFor(s, cluster),
    });
    group.on('clusterclick', e => {
        const cluster = e.layer;
        const ids = cluster.getAllChildMarkers().map(m => m.mauiId);
        if (s.onEvent('cluster', JSON.stringify({ ids, ...cluster.getLatLng() }))) return;
        if (s.map.getZoom() >= s.map.getMaxZoom()) cluster.spiderfy();
        else cluster.zoomToBounds({ padding: [24, 24] });
    });
    group.addTo(s.map);
    s.groups.set(name, group);
    return group;
}

function clusterIconFor(s, cluster) {
    const url = s.customClusterIcons ? s.clusterIcon(JSON.stringify({
        ids: cluster.getAllChildMarkers().map(m => m.mauiId),
        count: cluster.getChildCount(),
        ...cluster.getLatLng(),
    })) : null;
    return url
        ? imageIcon(url, 'maui-map-pin maui-map-cluster-image')
        : L.MarkerClusterGroup.prototype._defaultIconCreateFunction(cluster);
}

const layerFor = (s, p) => s.clustering ? groupFor(s, p.group) : s.plain;

function addPin(s, p) {
    const marker = L.marker([p.lat, p.lng], { icon: pinIcon(p), title: p.label, alt: p.label, riseOnHover: true });
    marker.mauiId = p.id;
    marker.on('click', () => {
        const entry = s.pins.get(p.id);
        if (entry && !s.onEvent('marker', JSON.stringify({ id: p.id }))) openInfo(s, entry);
    });
    s.pins.set(p.id, { marker, data: p, popup: null });
    layerFor(s, p).addLayer(marker);
}

function updatePinEntry(s, entry, p) {
    const old = entry.data;
    entry.data = p;
    const marker = entry.marker;
    if (old.group !== p.group && s.clustering) {
        groupFor(s, old.group).removeLayer(marker);
        groupFor(s, p.group).addLayer(marker);
    }
    if (old.lat !== p.lat || old.lng !== p.lng) marker.setLatLng([p.lat, p.lng]);
    if (old.icon !== p.icon) marker.setIcon(pinIcon(p));
    if (old.label !== p.label) {
        marker.options.title = marker.options.alt = p.label;
        marker.getElement()?.setAttribute('title', p.label);
    }
    if (entry.popup?.isOpen()) openInfo(s, entry);
}

function removePin(s, id) {
    const entry = s.pins.get(id);
    s.pins.delete(id);
    if (entry.popup) s.map.closePopup(entry.popup);
    layerFor(s, entry.data).removeLayer(entry.marker);
}

export function setPins(host, json) {
    const s = states.get(host);
    if (!s) return;
    const list = JSON.parse(json);
    const keep = new Set(list.map(p => p.id));
    [...s.pins.keys()].filter(id => !keep.has(id)).forEach(id => removePin(s, id));
    list.forEach(p => {
        const entry = s.pins.get(p.id);
        if (entry) updatePinEntry(s, entry, p);
        else addPin(s, p);
    });
}

export function updatePin(host, json) {
    const s = states.get(host);
    if (!s) return;
    const p = JSON.parse(json);
    const entry = s.pins.get(p.id);
    if (entry) updatePinEntry(s, entry, p);
    else addPin(s, p);
}

// ---- info windows -----------------------------------------------------------

function infoContent(s, p) {
    const content = document.createElement('div');
    content.className = 'maui-map-info';
    const label = document.createElement('div');
    label.className = 'maui-map-info-label';
    label.textContent = p.label ?? '';
    content.append(label);
    if (p.address) {
        const address = document.createElement('div');
        address.className = 'maui-map-info-address';
        address.textContent = p.address;
        content.append(address);
    }
    content.addEventListener('click', () => {
        const entry = s.pins.get(p.id);
        if (entry && s.onEvent('info', JSON.stringify({ id: p.id }))) s.map.closePopup(entry.popup);
    });
    return content;
}

function openInfo(s, entry) {
    // The popup sits above the marker's image: 41px for the default pin, the image height otherwise.
    const img = entry.marker.getElement()?.querySelector('img');
    const height = img ? img.offsetHeight : 41;
    entry.popup ??= L.popup({ autoPanPadding: [16, 16] });
    entry.popup.options.offset = L.point(0, 7 - height);
    entry.popup
        .setLatLng(entry.marker.getLatLng())
        .setContent(infoContent(s, entry.data))
        .openOn(s.map);
}

export function showInfoWindow(host, id) {
    const s = states.get(host);
    const entry = s?.pins.get(id);
    if (!entry) return;
    // A clustered pin is first zoomed or spiderfied into view.
    if (s.clustering) groupFor(s, entry.data.group).zoomToShowLayer(entry.marker, () => openInfo(s, entry));
    else openInfo(s, entry);
}

export function hideInfoWindow(host, id) {
    const s = states.get(host);
    const entry = s?.pins.get(id);
    if (entry?.popup) s.map.closePopup(entry.popup);
}

// ---- clustering -------------------------------------------------------------

export async function setClustering(host, enabled, customIcons) {
    const s = states.get(host);
    if (!s) return;
    s.customClusterIcons = customIcons;
    if (enabled) await ensureCluster();
    if (s.destroyed || s.clustering === enabled) return;

    s.clustering = enabled;
    const entries = [...s.pins.values()];
    if (enabled) {
        s.plain.clearLayers();
        const byGroup = Map.groupBy(entries, e => e.data.group);
        byGroup.forEach((members, name) => groupFor(s, name).addLayers(members.map(e => e.marker)));
    } else {
        s.groups.forEach(group => group.remove());
        s.groups.clear();
        entries.forEach(e => s.plain.addLayer(e.marker));
    }
}

export function refreshClusters(host, customIcons) {
    const s = states.get(host);
    if (!s) return;
    s.customClusterIcons = customIcons;
    s.groups.forEach(group => group.refreshClusters());
}

// ---- shapes -----------------------------------------------------------------

function shapeStyle(d) {
    const filled = !!d.fill && d.kind !== 'polyline';
    return {
        stroke: d.width > 0,
        color: d.stroke ?? DEFAULT_STROKE,
        weight: d.width,
        opacity: 1,
        fill: filled,
        fillColor: d.fill ?? undefined,
        fillOpacity: filled ? 1 : 0,
        // A click on a shape is the shape's, not the map's, as on the native maps.
        bubblingMouseEvents: false,
    };
}

function createShape(d) {
    const style = shapeStyle(d);
    if (d.kind === 'circle') return L.circle(d.center, { ...style, radius: d.radius });
    if (d.kind === 'polygon') return L.polygon(d.points, style);
    if (d.kind === 'polyline') return L.polyline(d.points, style);
    return null;
}

// Shapes are cheap to rebuild, so any change replaces the layer.
function upsertShape(s, d) {
    s.shapes.get(d.id)?.layer?.remove();
    const layer = createShape(d);
    layer?.on('click', () => s.onEvent('element', JSON.stringify({ id: d.id })));
    if (layer && d.visible) layer.addTo(s.map);
    s.shapes.set(d.id, { layer, data: d });
}

// Leaflet draws vectors in the order they were added; restack by ZIndex, then collection order.
function restack(s) {
    [...s.shapes.values()]
        .filter(e => e.layer && e.data.visible)
        .sort((a, b) => a.data.z - b.data.z || a.data.order - b.data.order)
        .forEach(e => e.layer.bringToFront());
}

export function setElements(host, json) {
    const s = states.get(host);
    if (!s) return;
    const list = JSON.parse(json);
    const keep = new Set(list.map(d => d.id));
    [...s.shapes.keys()].filter(id => !keep.has(id)).forEach(id => {
        s.shapes.get(id).layer?.remove();
        s.shapes.delete(id);
    });
    list.forEach(d => upsertShape(s, d));
    restack(s);
}

export function updateElement(host, json) {
    const s = states.get(host);
    if (!s) return;
    upsertShape(s, JSON.parse(json));
    restack(s);
}

// ---- user location ----------------------------------------------------------

export function setShowUser(host, show) {
    const s = states.get(host);
    if (!s || show === (s.watch !== null)) return;

    if (!show) {
        navigator.geolocation.clearWatch(s.watch);
        s.watch = null;
        s.user?.ring.remove();
        s.user?.dot.remove();
        s.user = null;
        return;
    }

    if (!navigator.geolocation) {
        s.onEvent('usererror', JSON.stringify({ message: 'This browser has no geolocation.' }));
        return;
    }
    s.watch = navigator.geolocation.watchPosition(
        pos => showUser(s, pos),
        err => s.onEvent('usererror', JSON.stringify({ message: err.message })),
        { enableHighAccuracy: true });
}

function showUser(s, pos) {
    if (s.destroyed || s.watch === null) return;
    const latlng = [pos.coords.latitude, pos.coords.longitude];
    const accuracy = pos.coords.accuracy ?? 0;
    if (s.user) {
        s.user.ring.setLatLng(latlng).setRadius(accuracy);
        s.user.dot.setLatLng(latlng);
    } else {
        s.user = {
            ring: L.circle(latlng, { radius: accuracy, stroke: false, fillColor: '#1a73e8', fillOpacity: 0.15, interactive: false }).addTo(s.map),
            dot: L.circleMarker(latlng, { radius: 7, color: '#fff', weight: 2, fillColor: '#1a73e8', fillOpacity: 1, interactive: false }).addTo(s.map),
        };
    }
    s.onEvent('user', JSON.stringify({ lat: latlng[0], lng: latlng[1] }));
}
