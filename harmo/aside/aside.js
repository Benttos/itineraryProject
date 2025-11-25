class aside extends HTMLElement {
    constructor() {
        super();
        let shadowRoot = this.attachShadow({ mode: 'open' });
        // STOMP configuration (override via global vars if needed)
        this.stompUrl = window?.STOMP_BROKER_URL || 'ws://localhost:61614/stomp';
        this.stompLogin = window?.STOMP_LOGIN || 'admin';
        this.stompPasscode = window?.STOMP_PASSCODE || 'admin';
        this.stompClient = null;

        fetch('../aside/aside.html')
        .then(response => response.text())
        .then(data => {
            let template = new DOMParser().parseFromString(data, 'text/html')
            .querySelector('template').content;
            this.shadowRoot.appendChild(template.cloneNode(true));
            this.loadRanking();
            this.initStomp();
        });
    }

    async loadRanking() {
        const list = this.shadowRoot.getElementById('classement');
        if (!list) return;
        list.textContent = 'Chargement...';
        try {
            const res = await fetch('http://localhost:8733/Design_Time_Addresses/ServerProject/Server/getAllUserInfo');
            if (!res.ok) throw new Error('Requête échouée');
            const raw = await res.json();
            // Supporte les réponses enveloppées ou stringifiées
            let data = raw?.GetAllUserInfoResult ?? raw;
            if (typeof data === 'string') {
                try { data = JSON.parse(data); } catch {}
            }
            let users = Array.isArray(data)
                ? data
                : (data?.users ?? data?.Users ?? data?.userList ?? data?.UserList);
            if (!Array.isArray(users) && data && typeof data === 'object') {
                const firstArrayProp = Object.values(data).find(v => Array.isArray(v));
                if (firstArrayProp) users = firstArrayProp;
            }
            console.log('Classement utilisateurs (brut) :', raw, 'normalisé :', users);
            users.sort((a, b) => (Number(b?.token) || 0) - (Number(a?.token) || 0));
            list.innerHTML = '';
            users.forEach(u => {
                const li = document.createElement('li');
                li.textContent = `${u?.username ?? 'Inconnu'} - ${u?.token ?? 0} token(s)`;
                list.appendChild(li);
            });
            if (!users.length) list.textContent = 'Aucun utilisateur.';
        } catch (e) {
            console.error('Impossible de charger les utilisateurs', e);
            list.textContent = 'Erreur de chargement.';
        }
    }

    async loadStompLibrary() {
        if (window?.StompJs?.Client) return true;
        return new Promise((resolve, reject) => {
            const existing = document.querySelector('script[data-stompjs]');
            if (existing) {
                existing.addEventListener('load', () => resolve(true), { once: true });
                existing.addEventListener('error', reject, { once: true });
                return;
            }
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/@stomp/stompjs@7.0.0/bundles/stomp.umd.min.js';
            script.async = true;
            script.dataset.stompjs = 'true';
            script.onload = () => resolve(true);
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    async initStomp() {
        try {
            await this.loadStompLibrary();
        } catch (e) {
            console.error('Impossible de charger stompjs', e);
            return;
        }
        if (this.stompClient) {
            return;
        }
        const Client = window?.StompJs?.Client;
        if (!Client) {
            console.warn('StompJs.Client introuvable après chargement');
            return;
        }
        this.stompClient = new Client({
            brokerURL: this.stompUrl,
            connectHeaders: {
                login: this.stompLogin,
                passcode: this.stompPasscode,
            },
            reconnectDelay: 5000,
            debug: (str) => console.debug('[STOMP]', str),
            onConnect: () => {
                this.subscribeQueues();
            },
            onStompError: (frame) => {
                console.error('Erreur STOMP', frame.headers['message'], frame.body);
            }
        });
        this.stompClient.activate();
    }

    subscribeQueues() {
        if (!this.stompClient || !this.stompClient.connected) return;
        try {
            const destinations = [
                '/queue/USER_TOKENS_UPDATED',
                '/queue/RANKING_FIRST_CHANGED',
            ];
            destinations.forEach((dest) => {
                this.stompClient.subscribe(dest, (message) => {
                    if (dest.includes('USER_TOKENS_UPDATED')) {
                        this.handleTokensUpdated(message);
                    } else {
                        this.handleRankingFirstChanged(message, dest);
                    }
                });
            });
        } catch (e) {
            console.error('Erreur de souscription STOMP', e);
        }
    }

    handleTokensUpdated(message) {
        // Met à jour le classement et notifie le reste de l'app
        this.loadRanking();
        let detail = null;
        try { detail = message?.body ? JSON.parse(message.body) : null; } catch {}
        window.dispatchEvent(new CustomEvent('user-tokens-updated', {
            detail,
            composed: true,
            bubbles: true
        }));
    }

    handleRankingFirstChanged(message, dest) {
        console.debug('RANKING_FIRST_CHANGED reçu depuis', dest, 'payload brut:', message?.body);
        let info = null;
        try { info = message?.body ? JSON.parse(message.body) : null; } catch {}
        const name = info?.username || info?.user || 'Un nouveau leader';
        alert(`${name} est maintenant premier du classement !`);
    }
}

customElements.define('custom-aside', aside);
