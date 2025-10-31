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

const coordonne = document.querySelector('itinerary-component');

coordonne.addEventListener('coordonne-selected', (event) => {
    const { cityCoord } = event.detail;
    const  position = event.detail.position;
    
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



coordonne.addEventListener('Itinerary',(event) => {
    const {route} = event.detail;
    console.log("voici la :" + route);
    geoCoordinate = route.geometry;
    console.log("ce que je dois mettre sur la map : "+geoCoordinate);
    L.geoJSON(geoCoordinate).addTo(map);
});


