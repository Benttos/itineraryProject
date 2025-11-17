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
        const item = Array.isArray(segment) ? segment[0] : segment;
        if (!item) continue;
        let geometry = item.routes[0].geometry ?? item;
        if (typeof geometry === 'string') {
          try { geometry = JSON.parse(geometry); }
          catch (e) { console.warn('GeoJSON de segment invalide', segment, e); continue; }
        }
        if (geometry?.type && geometry?.coordinates) {
          console.log("itineraire a afficher",geometry);
          console.log("le truc geo ",L.geoJSON(geometry).addTo(map));
        }
      }
    }
});


document.addEventListener('Itinerary',(event) => {
  // je recupere que une route et je prend la geometry qui est un ensemble de point qui mis dans Geojson me fait un itinineraire 
    const { itinerary } = event.detail || {};
    console.log("a pied recu", itinerary);
    const first = Array.isArray(itinerary.itineraries) ? itinerary.itineraries[0] : itinerary.itineraries;
    if (!first) { console.warn('Itinerary reçu sans route', event.detail); return; }
    const geometry = first.routes[0].geometry || first;
    // Remplacer l'ancien itinéraire par le nouveau, si présent
    if (routeLayer) {
      map.removeLayer(routeLayer);
    }
    console.log("route a pied a afficher ",geometry);
    routeLayer = L.geoJSON(geometry).addTo(map);
});


