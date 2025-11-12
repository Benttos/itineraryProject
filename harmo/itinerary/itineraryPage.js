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


document.addEventListener('Itinerary', e => {
console.log('doc got Itinerary', e.detail, e.composed, e.bubbles, e.composedPath());
});
console.log('host:', coordonne);


document.addEventListener('Itinerary',(event) => {
    const { route } = event.detail || {};
    const first = Array.isArray(route) ? route[0] : route;
    const geoCoordinate = route.geometry;
    if (!first) { console.warn('Itinerary reçu sans route', event.detail); return; }
    const geometry = first.geometry || first;
    console.log("rooute sur la map",geoCoordinate)
    // Remplacer l'ancien itinéraire par le nouveau, si présent
    if (routeLayer) {
      map.removeLayer(routeLayer);
    }
    routeLayer = L.geoJSON(geometry).addTo(map);
});


