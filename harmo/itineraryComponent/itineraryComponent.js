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

            

            let myTimeout;

            shadowRoot.getElementById('departInput').addEventListener('input', () => {
                if(myTimeout) {
                    clearTimeout(myTimeout);
                }
                myTimeout = setTimeout(async () => {
                    const departValue = shadowRoot.getElementById('departInput').value;
                    const city = await this.getCity(departValue,"depart");
                }, 500);
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

    async getCity(name,position) { 
        const ulDepart = this.shadowRoot.getElementById('departuresList');
        const ulArrivee = this.shadowRoot.getElementById('arrivalsList')
        let currentUl= null;
        if(position == "depart"){
            currentUl = ulDepart;
        }else{currentUl = ulArrivee;}
        currentUl.innerHTML = ''; // Clear previous results

        console.log(name);
        let url = `https://api-adresse.data.gouv.fr/search/?q=${name}&limit=5`;
        const response = await fetch(url).then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        }).then(data => {
            console.log("ici");
            let cityMap = data.features.map(function(elem){
                return elem.properties.label;
            });
            let cityCoord = data.features.map(function(elem){   
                return elem.geometry.coordinates
            });
            console.log(cityMap);
            console.log(cityCoord);
            if (cityMap) {
                for(let element of cityMap){
                    let currentLi = document.createElement('li');
                    currentLi.textContent = element;    
                    currentUl.appendChild(currentLi);
                }

                this.dispatchEvent(new CustomEvent('coordonne-selected',{
                    detail: {cityCoord},  
                    composed: true,
                    bubbles: true,

                }));
            }
        });
    };
}



customElements.define('itinerary-component', ItineraryComponent);
