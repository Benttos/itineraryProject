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

//to do si on veut ameliorer et mettre pls station mettre une liste de station et pas debut/fin afficcher les ping associer 
document.addEventListener('bestBikeItineray',(event)=> {
    //je recupere mes donner comme je le souhaite sans les avoir parser avant car déja mis dedans ce que j'avais besoin
    const {itinerary} = event.detail || {};
    //mes trois itineraire pied/velo/pied 
    const routes = itinerary.itineraries;
    //mtn que j'ai mes routes je veux afficher mes stations de vélo avec des points en plus de mes points de départ et d'arriver
    console.log("je vais afficher les ping des satation")
    const bikeStartStationLat = itinerary.startStation.lat ;
    const bikeStartStationLon = itinerary.startStation.lon ;
    const bikeEndStationLat = itinerary.endStation.lat ;
    const bikeEndStationLon = itinerary.endStation.lon ;
    //mtn on les affiches 
    const startBikeStationPing = L.marker(bikeStartStationLat,bikeStartStationLon).addTo(map) ;
    const endbikeStationPing = L.marker(bikeEndStationLat,bikeEndStationLon).addTo(map) ;
    //et mtn on affiche les itineraires 
    if(Array.isArray(routes)){
      console.log("je vais afficher les routes mtn")
      for(route in routes){
        //recupere mon element en position 0 car la qu'est la geometrie
        const first = Array.isArray(route) ? route[0] : route ;
        const geometry = first.geometry || first ;
        L.geoJSON(geometry).addTo(map);
      }
    }
});


document.addEventListener('Itinerary',(event) => {
  // je recupere que une route et je prend la geometry qui est un ensemble de point qui mis dans Geojson me fait un itinineraire 
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


