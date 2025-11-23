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

            const itineraryButton = shadowRoot.getElementById('itinerayButton');
            const prevBtn = shadowRoot.getElementById('prevStepBtn');
            const nextBtn = shadowRoot.getElementById('nextStepBtn');
            const routeSelect = shadowRoot.getElementById('routeSelect');
            this.currentStepIndex = -1;
            this.currentRouteIndex = -1;
            this.steps = [];
            this.routes = [];
            const updateNav = () => {
                if (!prevBtn || !nextBtn) return;
                prevBtn.disabled = this.currentStepIndex <= 0;
                nextBtn.disabled = this.currentStepIndex < 0 || this.currentStepIndex >= this.steps.length - 1;
            };
            if (prevBtn) prevBtn.addEventListener('click', () => { this.moveStep(-1); updateNav(); });
            if (nextBtn) nextBtn.addEventListener('click', () => { this.moveStep(1); updateNav(); });

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
                    const payload = await this.getBestRoute(departCoordinateParse[1],departCoordinateParse[0],arrivalCoordinateParse[1],arrivalCoordinateParse[0]);
                    this.renderStepsFromPayload(payload);
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
                // Masque la liste après sélection
                shadowRoot.getElementById('departuresList').innerHTML = '';
              });

              shadowRoot.getElementById('arrivalsList').addEventListener('click',async (event) => {
                const li = event.target.closest('li');
                if (!li || !shadowRoot.getElementById('arrivalsList').contains(li)) return; 
                shadowRoot.getElementById('arriveeInput').value = li.textContent;
                arrivalCoordinate = await this.getCity(li.textContent,"arriver");
                console.log("after call:"+arrivalCoordinate)
                // Masque la liste après sélection
                shadowRoot.getElementById('arrivalsList').innerHTML = '';
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

    async getBestRoute(fromLat,fromLon,toLat,toLon){
        const url =`http://localhost:8733/Design_Time_Addresses/ServerProject/Server/bestItinerary?fromLat=${fromLat}&fromLon=${fromLon}&toLat=${toLat}&toLon=${toLon}`;
        console.log('getbestItinerary called with:', fromLat, fromLon, toLat, toLon,url);
        const response = await fetch(url);
        if(!response.ok) {
            throw new Error('Reponse From best itinerary not ok')
        }
        const data = await response.json();
        console.log('mes data',data,typeof data);
        let raw = data?.BestItineraryResult
            ?? data?.bestItinerary
            ?? data;
        try {
            if (typeof raw === 'string') raw = JSON.parse(raw);
        } catch (e) {
            console.warn('Impossible de parser bestItinerary', e, raw);
            return;
        }
        const payload = raw;
        const methode = payload?.methode;
        console.log("je vais me deplacer a ", methode)
        if(methode === "withBike"){
            this.dispatchEvent(new CustomEvent('bestBikeItinerary',{
                detail : { itinerary: payload },
                composed: true,
                bubbles: true 
            }));
        }else{
            this.dispatchEvent(new CustomEvent('Itinerary',{
                detail : {itinerary: payload},
                composed: true ,
                bubbles: true 
            }));
        }

        return payload;
        }


    async getRoute(fromLat,fromLon,toLat,toLon){
        const url = `http://localhost:8733/Design_Time_Addresses/ServerProject/Server/getBikeItineray?fromLat=${fromLat}&fromLon=${fromLon}&toLat=${toLat}&toLon=${toLon}`;
        console.log('getRoute called with:', fromLat, fromLon, toLat, toLon,url);

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        console.log('raw getRoute response:', data, typeof data);


        // Normalise la réponse (gère plusieurs noms/propriétés possibles et JSON stringifié)
        let raw = data?.GetBikeItineraryResult
            ?? data?.GetBikeItinerayResult
            ?? data?.GetItineraryResult
            ?? data?.GetItinerayResult
            ?? data;
        try {
            if (typeof raw === 'string') raw = JSON.parse(raw);
        } catch (e) {
            console.warn('Réponse impossible à parser', e, raw);
            return;
        }

        // Récupère la première route s'il y a une liste
        const route = Array.isArray(raw?.routes) ? raw.routes[0] : (raw?.route ?? raw);

        // Normalise la géométrie en GeoJSON
        let geometry = route?.geometry ?? route;
        if (typeof geometry === 'string') {
            try { geometry = JSON.parse(geometry); } catch {}
        }
        if (!geometry?.type || !geometry?.coordinates) {
            console.warn('GeoJSON invalide', { raw, route, geometry });
            return;
        }
        // Assure que route.geometry est l’objet GeoJSON
        route.geometry = geometry;

        this.dispatchEvent(new CustomEvent('Itinerary', {
            detail: { route },
            composed: true,
            bubbles: true
        }));
        return route;
    }

    clearSteps() {
        const container = this.shadowRoot?.getElementById('routeSteps');
        if (container) {
            const cur = this.shadowRoot.getElementById('currentStep');
            const prog = this.shadowRoot.getElementById('stepProgress');
            if (cur) cur.textContent = '';
            if (prog) prog.textContent = '';
        }
        this.steps = [];
        this.currentStepIndex = -1;
        const prevBtn = this.shadowRoot?.getElementById('prevStepBtn');
        const nextBtn = this.shadowRoot?.getElementById('nextStepBtn');
        if (prevBtn) prevBtn.disabled = true;
        if (nextBtn) nextBtn.disabled = true;
    }

    formatDistance(meters) {
        if (meters == null) return '';
        if (meters < 1000) return `${Math.round(meters)} m`;
        return `${(meters/1000).toFixed(1)} km`;
    }

    buildInstruction(step) {
        const instr = step?.instruction || step?.maneuver?.instruction;
        if (instr) return instr;
        const type = step?.maneuver?.type;
        const mod = step?.maneuver?.modifier;
        const name = step?.name || '';
        const parts = [type, mod, name].filter(Boolean);
        return parts.join(' ');
    }

    renderSteps(route) {
        // Initialise steps et affiche la première
        const leg = route?.legs?.[0];
        const steps = Array.isArray(leg?.steps) ? leg.steps : [];
        this.steps = steps;
        this.currentStepIndex = steps.length ? 0 : -1;
        this.renderCurrentStep();
    }

    renderStepsFromPayload(payload) {
        const steps = this.extractSteps(payload);
        this.steps = steps;
        this.currentStepIndex = steps.length ? 0 : -1;
        this.renderCurrentStep();
    }

    moveStep(delta) {
        if (!Array.isArray(this.steps) || !this.steps.length) return;
        const next = this.currentStepIndex + delta;
        if (next < 0 || next >= this.steps.length) return;
        this.currentStepIndex = next;
        this.renderCurrentStep();
    }

    renderCurrentStep() {
        const cur = this.shadowRoot?.getElementById('currentStep');
        const prog = this.shadowRoot?.getElementById('stepProgress');
        const prevBtn = this.shadowRoot?.getElementById('prevStepBtn');
        const nextBtn = this.shadowRoot?.getElementById('nextStepBtn');
        if (!cur || !prog) return;
        if (this.currentStepIndex < 0) {
            cur.textContent = 'Aucune étape disponible.';
            prog.textContent = '';
            if (prevBtn) prevBtn.disabled = true;
            if (nextBtn) nextBtn.disabled = true;
            return;
        }
        const step = this.steps[this.currentStepIndex];
        const mode = step?._mode === 'bike' ? '[Velo] ' : (step?._mode === 'walk' ? '[A pied] ' : '');
        const text = mode + (this.buildInstruction(step) || (step?.name ? `Suivre ${step.name}` : 'Continuer'));
        const dist = this.formatDistance(step?.distance);
        cur.textContent = dist ? `${text} - ${dist}` : text;
        prog.textContent = `Etape ${this.currentStepIndex + 1}/${this.steps.length}`;
        if (prevBtn) prevBtn.disabled = this.currentStepIndex <= 0;
        if (nextBtn) nextBtn.disabled = this.currentStepIndex >= this.steps.length - 1;
    }

    extractStepsFromSegment(segment) {
        if (!segment) return [];
        const segs = segment?.segments;
        if (Array.isArray(segs) && Array.isArray(segs[0]?.steps)) {
            return segs.flatMap(s => s?.steps || []);
        }
        const propSegs = segment?.properties?.segments;
        if (Array.isArray(propSegs) && Array.isArray(propSegs[0]?.steps)) {
            return propSegs.flatMap(s => s?.steps || []);
        }
        const featSegs = segment?.features?.[0]?.properties?.segments;
        if (Array.isArray(featSegs) && Array.isArray(featSegs[0]?.steps)) {
            return featSegs.flatMap(s => s?.steps || []);
        }
        const legsSteps = segment?.legs?.[0]?.steps;
        if (Array.isArray(legsSteps)) return legsSteps;
        return [];
    }

    detectMode(segment, idx, total) {
        const profile = (segment?.profile || segment?.mode || segment?.properties?.profile || segment?.properties?.mode || '').toLowerCase();
        if (profile.includes('bike') || profile.includes('cycle')) return 'bike';
        if (profile.includes('foot') || profile.includes('walk')) return 'walk';
        if (total >= 3 && idx === 1) return 'bike';
        return 'walk';
    }

    extractSegments(payload) {
        if (!payload) return [];
        if (Array.isArray(payload?.itineraries)) return payload.itineraries;
        if (payload?.itineraries) return [payload.itineraries];
        if (Array.isArray(payload?.routes)) return payload.routes;
        if (payload?.route) return [payload.route];
        if (Array.isArray(payload)) return payload;
        return [payload];
    }

    extractSteps(payload) {
        const segments = this.extractSegments(payload);
        const all = [];
        segments.forEach((segment, idx) => {
            const mode = this.detectMode(segment, idx, segments.length);
            const steps = this.extractStepsFromSegment(segment);
            steps.forEach(s => all.push({ ...s, _mode: mode }));
        });
        return all;
    }

    async getCity(name,position) { 
        const ulDepart = this.shadowRoot.getElementById('departuresList');
        const ulArrivee = this.shadowRoot.getElementById('arrivalsList');
        let currentUl = position === "depart" ? ulDepart : ulArrivee;
        currentUl.innerHTML = '';
        // Dès qu'on modifie un point, on efface les étapes affichées
        this.clearSteps();
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
 
