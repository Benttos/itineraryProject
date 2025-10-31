class ItineraryComponent extends HTMLElement {
    constructor() {
        super();
        let shadowRoot = this.attachShadow({ mode: 'open' });

        fetch('/itineraryComponent/itineraryComponent.html')
        .then(response => response.text())
        .then(data => {
            let template = new DOMParser().parseFromString(data, 'text/html')
            .querySelector('template').content;
            this.shadowRoot.appendChild(template.cloneNode(true));

            const departValue = shadowRoot.getElementById('departInput').value;
            const arriveeValue = shadowRoot.getElementById('arriveeInput').value;

            const liDepart = document.createElement('li');
            const departBtn = shadowRoot.getElementById('departBtn');
            


            const ulArrivee = shadowRoot.getElementById('arrivalsList');
            const arriveeBtn = shadowRoot.getElementById('arriveeBtn');

            const itineraryButton = shadowRoot.getElementById('itinerayButton');

            let departCoordinate ;
            let arrivalCoordinate ;
            let route ;

            let myTimeout;

            itineraryButton.addEventListener('click',async ()=>{
                console.log("debut :"+departCoordinate );
                console.log("fin :"+arrivalCoordinate)
                if(departCoordinate!=null && arrivalCoordinate!=null){
                    const departCoordinateParse = String(departCoordinate).split(',');
                    const arrivalCoordinateParse = String(arrivalCoordinate).split(',');       
                    console.log("Parsed"+departCoordinateParse+"end"+arrivalCoordinateParse);
                    route = await this.getRoute(departCoordinateParse[1],departCoordinateParse[0],arrivalCoordinateParse[1],arrivalCoordinateParse[0]);
                    console.log(route);
                }
            });


            shadowRoot.getElementById('departInput').addEventListener('input', () => {
                if(myTimeout) {
                    clearTimeout(myTimeout);
                }
                myTimeout = setTimeout(async () => {
                    const departValue = shadowRoot.getElementById('departInput').value;
                    const city = await this.getCity(departValue,"depart");
                }, 500);
            });

            shadowRoot.getElementById('departuresList').addEventListener('click',async (event) => {
                const li = event.target.closest('li');
                if (!li || !shadowRoot.getElementById('departuresList').contains(li)) return; 
                shadowRoot.getElementById('departInput').value = li.textContent;
                departCoordinate = await this.getCity(li.textContent,"depart");
              });

              shadowRoot.getElementById('arrivalsList').addEventListener('click',async (event) => {
                const li = event.target.closest('li');
                if (!li || !shadowRoot.getElementById('arrivalsList').contains(li)) return; 
                shadowRoot.getElementById('arriveeInput').value = li.textContent;
                arrivalCoordinate = await this.getCity(li.textContent,"arriver");
                console.log("after call:"+arrivalCoordinate)
              });

            shadowRoot.getElementById('arriveeInput').addEventListener('input', () => {
                if(myTimeout) {
                    clearTimeout(myTimeout);
                }
                myTimeout = setTimeout(async () => {
                    const arriveeValue = shadowRoot.getElementById('arriveeInput').value;

                this.getCity(arriveeValue,"arriver");  
                    
                }, 500);
            });

        });
    }

  


    async getRoute(fromLat,fromLon,toLat,toLon){
        const url = `http://localhost:8733/Design_Time_Addresses/ServerProject/Server/getItinerary?fromLat=${fromLat}&fromLon=${fromLon}&toLat=${toLat}&toLon=${toLon}`;
        console.log('getRoute called with:', fromLat, fromLon, toLat, toLon);

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        console.log('raw getRoute response:', data);

        let parsed;
        if (data && typeof data.GetItinerayResult === 'string') {
            try {
                parsed = JSON.parse(data.GetItinerayResult);
            } catch (e) {
                console.error('Failed to parse GetItinerayResult:', e);
                parsed = data.GetItinerayResult;
            }
        } else {
            parsed = data;
        }

        const normalized = {
            route: parsed && parsed.routes ? parsed.routes : parsed
        };

        console.log('normalized route:', normalized);
        this.dispatchEvent(new CustomEvent('Itinerary',{
            detail: { route: normalized.route[0] },
            composed: true,
            bubbles: true,
        }));
        return normalized;
    }

    async getCity(name,position) { 
        const ulDepart = this.shadowRoot.getElementById('departuresList');
        const ulArrivee = this.shadowRoot.getElementById('arrivalsList');
        let currentUl = position === "depart" ? ulDepart : ulArrivee;
        currentUl.innerHTML = '';
        if (!name) return null;
        console.log(name);
        const url = `https://api-adresse.data.gouv.fr/search/?q=${encodeURIComponent(name)}&limit=5`;
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();

        const cityMap = data.features.map(elem => elem.properties.label);
        const cityCoord = data.features.map(elem => elem.geometry.coordinates); 

        if (cityMap && cityMap.length) {
            for (let element of cityMap) {
                let currentLi = document.createElement('li');
                currentLi.textContent = element;    
                currentUl.appendChild(currentLi);
            }

            this.dispatchEvent(new CustomEvent('coordonne-selected',{
                detail: { cityCoord, position },  
                composed: true,
                bubbles: true,
            }));
            console.log("valeur ", cityCoord[0]);
            return cityCoord[0]; 
        }

        return null;
    };
}



customElements.define('itinerary-component', ItineraryComponent);
