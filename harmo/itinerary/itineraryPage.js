const map = L.map('map').setView([46.7, 2.5], 6);

// Charger les tuiles OpenStreetMap
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 19,
  attribution: '© OpenStreetMap contributeurs'
}).addTo(map);




let DepartPingmarker = L.marker([48.8566, 2.3522],{color: '##ff000a'}).addTo(map) ;
let ArrivalPingmarker;
let DepartCoordinates;
let ArrivalCordinates;
let routeLayer; // couche de l'itinéraire courant
const bikeStationMarkers = [];
const bikeRouteLayers = [];

// Normalise OSM/GeoJSON segments to a Leaflet-friendly GeoJSON object
const normalizeGeoJSON = (segment) => {
  const parseMaybeJSON = (val) => {
    if (typeof val === 'string') {
      try { return JSON.parse(val); }
      catch (e) { console.warn('GeoJSON parse failed', e, val); return null; }
    }
    return val;
  };

  const base = parseMaybeJSON(Array.isArray(segment) ? segment[0] : segment);
  if (!base) return null;

  // Geometry nested in a route list (even with OSM GeoJSON inside routes)
  const embeddedGeometry = base?.routes?.[0]?.geometry ?? base?.route?.geometry;
  if (embeddedGeometry) return parseMaybeJSON(embeddedGeometry);

  // OSM/ORS FeatureCollection or Feature
  if (base?.type === 'FeatureCollection' && Array.isArray(base.features)) return base;
  if (base?.type === 'Feature' && base.geometry) return parseMaybeJSON(base);

  // Feature-like object without explicit type
  if (base?.features?.[0]?.geometry) {
    const geom = parseMaybeJSON(base.features[0].geometry);
    if (geom?.type && geom?.coordinates) {
      return { type: 'Feature', geometry: geom, properties: base.features[0].properties ?? {} };
    }
  }

  // Direct geometry fallback
  const geometry = parseMaybeJSON(base.geometry ?? base);
  if (geometry?.type && (geometry?.coordinates || geometry?.features)) return geometry;

  return null;
};

// Convert LineString coordinates to Leaflet LatLng tuples with a basic lon/lat heuristic
const lineStringToLatLngs = (coords = []) => {
  return coords.map((pair) => {
    const [a, b] = pair;
    const looksLikeLatLon = Math.abs(a) <= 90 && Math.abs(b) <= 180;
    const looksLikeLonLat = Math.abs(a) <= 180 && Math.abs(b) <= 90;
    if (looksLikeLatLon) return [a, b];   // already [lat, lon]
    if (looksLikeLonLat) return [b, a];   // swap from [lon, lat]
    return [b, a]; // fallback swap
  });
};

// Draw geometry with fallbacks: GeoJSON -> polyline(LineString) -> FeatureCollection first LineString
const drawGeometry = (geometry) => {
  if (!geometry) return null;
  let layer = L.geoJSON(geometry);
  if (layer?.getLayers && layer.getLayers().length === 0 && geometry.type === 'LineString') {
    const latlngs = lineStringToLatLngs(geometry.coordinates);
    layer = L.polyline(latlngs, { color: 'blue', weight: 4 });
    console.warn('GeoJSON vide, utilisation du polyline fallback', latlngs.slice(0,3));
  }
  if (layer?.getLayers && layer.getLayers().length === 0 && geometry?.features?.length) {
    const g = geometry.features[0]?.geometry;
    if (g?.type === 'LineString') {
      const latlngs = lineStringToLatLngs(g.coordinates);
      layer = L.polyline(latlngs, { color: 'blue', weight: 4 });
      console.warn('Fallback FeatureCollection -> polyline', latlngs.slice(0,3));
    }
  }
  if (layer) layer.addTo(map);
  return layer;
};

//const clearLayers = (layers) => {
//  while (layers.length) {
//    const layer = layers.pop();
//    if (layer) {
//      map.removeLayer(layer);
 //   }
//  }
//};

const coordonne = document.querySelector('itinerary-component');

coordonne.addEventListener('coordonne-selected', (event) => {
    const { cityCoord } = event.detail;
    const  position = event.detail.position;
    
    // Si l'utilisateur modifie un point, retirer l'itinéraire existant
    if (routeLayer) {
      map.removeLayer(routeLayer);
      routeLayer = undefined;
    }

    if(position === "depart"){
      if (DepartPingmarker) {
        map.removeLayer(DepartPingmarker);
      }
      console.log("ici");
      console.log(cityCoord[0]);
      DepartPingmarker = L.marker([cityCoord[0][1], cityCoord[0][0]]).addTo(map);
      map.setView([cityCoord[0][1], cityCoord[0][0]]);
      DepartCoordinates = [cityCoord[0][1], cityCoord[0][0]];
  }else{
    if (ArrivalPingmarker) {
      map.removeLayer(ArrivalPingmarker);
    }
    console.log(cityCoord[0]);
    ArrivalPingmarker = L.marker([cityCoord[0][1], cityCoord[0][0]]).addTo(map);
    
    map.setView([cityCoord[0][1], cityCoord[0][0]]);
    ArrivalCordinates = [cityCoord[0][1], cityCoord[0][0]] ;
  }
});

//to do probleme avec l'affichage des routes on a bien les ping si velo mais besoin de restart pour afficher les elements après ca s'affiche plus 
document.addEventListener('bestBikeItinerary',(event)=> {
    const { itinerary } = event.detail || {};
    if (!itinerary) {
      console.warn('bestBikeItinerary reçu sans contenu', event.detail);
      return;
    }
    console.log("bien arriver ici ", itinerary);
    // Nettoie les anciennes couches spécifiques au vélo


    //if (routeLayer) {
    //  map.removeLayer(routeLayer);
    //  routeLayer = undefined;
    //}
    //clearLayers(bikeStationMarkers);
    //clearLayers(bikeRouteLayers);

    const startStation = itinerary?.stationCoordinate?.startStation;
    const endStation = itinerary?.stationCoordinate?.endStation;
    const itineraries = itinerary?.itineraries;

    
    console.log("element un a un ",startStation,endStation,itineraries);
    // Ajoute les marqueurs des stations vélo
    if (startStation?.lat != null && startStation?.lon != null) {
      bikeStationMarkers.push(L.marker([startStation.lat, startStation.lon]).addTo(map));
    }
    if (endStation?.lat != null && endStation?.lon != null) {
      bikeStationMarkers.push(L.marker([endStation.lat, endStation.lon]).addTo(map));
    }

    // Affiche les trois route (pied -> vélo -> pied)
    if (Array.isArray(itineraries)) {
      for (const segment of itineraries) {
        const geometry = normalizeGeoJSON(segment);
        if (!geometry) {
          console.warn('GeoJSON de segment introuvable', segment);
          continue;
        }
        console.log("itineraire a afficher",geometry);
        console.log("le truc geo ",L.geoJSON(geometry).addTo(map));
      }
    }
});


document.addEventListener('Itinerary',(event) => {
  // je recupere que une route et je prend la geometry qui est un ensemble de point qui mis dans Geojson me fait un itinineraire 
    const { itinerary } = event.detail || {};
    console.log("a pied recu", itinerary);
    const first = Array.isArray(itinerary?.itineraries)
      ? itinerary.itineraries[0]
      : (itinerary?.itineraries ?? itinerary);
    if (!first) { console.warn('Itinerary reçu sans route', event.detail); return; }
    const geometry = normalizeGeoJSON(first);
    if (!geometry) { console.warn('GeoJSON introuvable pour Itinerary', first); return; }
    // Remplacer l'ancien itinéraire par le nouveau, si présent
    if (routeLayer) {
      map.removeLayer(routeLayer);
    }
    console.log("route a pied a afficher ",geometry);
    routeLayer = drawGeometry(geometry);
    if (routeLayer && routeLayer.getBounds) {
      map.fitBounds(routeLayer.getBounds(), { padding: [20, 20] });
    }
    if (routeLayer && routeLayer.getLatLngs) {
      const preview = routeLayer.getLatLngs();
      console.log('points (aperçu) : ', preview?.[0]?.[0] ?? preview?.[0]);
    }
});


