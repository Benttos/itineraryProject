const map = L.map('map').setView([46.7, 2.5], 6);

// Charger les tuiles OpenStreetMap
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 19,
  attribution: '© OpenStreetMap contributeurs'
}).addTo(map);




let pingmarker = L.marker([48.8566, 2.3522],{color: '##ff000a'}).addTo(map) ;

const coordonne = document.querySelector('itinerary-component');

coordonne.addEventListener('coordonne-selected', (event) => {
    const { cityCoord } = event.detail;
    console.log(cityCoord);
    if (pingmarker) {
        map.removeLayer(pingmarker);
    }
    console.log("ici");
    console.log(cityCoord[0]);
    pingmarker = L.marker([cityCoord[0][1], cityCoord[0][0]]).addTo(map);
    map.setView([cityCoord[0][1], cityCoord[0][0]]);
});
