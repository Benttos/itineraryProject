class Head extends HTMLElement {
    constructor() {
        super();
        let shadowRoot = this.attachShadow({ mode: 'open' });

        fetch('../header/head.html')
        .then(response => response.text())
        .then(data => {
            let template = new DOMParser().parseFromString(data, 'text/html')
            .querySelector('template').content;
            this.shadowRoot.appendChild(template.cloneNode(true));
            this.toggleMenu();
            this.initLogin();
        });
    }

    toggleMenu() {
    let burgerMenu = this.shadowRoot.getElementById("burger_menu");
    let menuIsVisible = true;
    let mobileMenu = this.shadowRoot.getElementById("mobileMenu");

    burgerMenu.addEventListener("click", function() {
        if (menuIsVisible) {
            burgerMenu.classList.remove("open");
            burgerMenu.classList.add("close");
            mobileMenu.classList.remove("visible");
            mobileMenu.classList.add("hidden");


        } else {
            burgerMenu.classList.remove("close");
            burgerMenu.classList.add("open");
            mobileMenu.classList.remove("hidden");
            mobileMenu.classList.add("visible");
        }
        menuIsVisible = !menuIsVisible;
    });
}

    initLogin() {
        const overlay = this.shadowRoot.getElementById('loginOverlay');
        const openBtn = this.shadowRoot.getElementById('loginLink');
        const submitBtn = this.shadowRoot.getElementById('loginSubmit');
        const cancelBtn = this.shadowRoot.getElementById('loginCancel');
        const input = this.shadowRoot.getElementById('usernameInput');
        const close = () => {
            if (overlay) overlay.classList.add('hidden');
            if (input) input.value = '';
        };
        if (openBtn && overlay) {
            openBtn.addEventListener('click', () => {
                overlay.classList.remove('hidden');
                input?.focus();
            });
        }
        if (cancelBtn) cancelBtn.addEventListener('click', close);
        if (overlay) {
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) close();
            });
        }
        const setButtonLabel = (label) => {
            if (openBtn && label !== undefined && label !== null) {
                openBtn.textContent = `${label} biken`;
            }
        };
        if (submitBtn) {
            submitBtn.addEventListener('click', async () => {
                const username = input?.value?.trim();
                if (!username) return;
                try {
                    const url = `http://localhost:8733/Design_Time_Addresses/ServerProject/Server/getUserInfo?username=${encodeURIComponent(username)}`;
                    const res = await fetch(url);
                    if (!res.ok) throw new Error('Réponse invalide du serveur');
                    const raw = await res.text();
                    let data;
                    try { data = JSON.parse(raw); } catch { data = raw; }
                    console.log("data recuperee", data);
                    // Déplie le cas où l'API renvoie { GetUserInfoResult: '{ "username": "...", "token": ... }' }
                    if (data?.GetUserInfoResult && typeof data.GetUserInfoResult === 'string') {
                        try { data = JSON.parse(data.GetUserInfoResult); } catch {}
                    }
                    let token = data?.token ?? data?.Token ?? data?.userToken ?? data;
                    if (typeof token === 'object') {
                        token = token?.token ?? token?.Token ?? token?.value;
                    }
                    if (token !== undefined && token !== null && token !== '') {
                        setButtonLabel(token);
                        this.dispatchEvent(new CustomEvent('user-login', {
                            detail: { username, token },
                            composed: true,
                            bubbles: true
                        }));
                    } else {
                        setButtonLabel('Connexion');
                        console.warn('Token introuvable dans la réponse', data);
                    }
                } catch (e) {
                    console.error('Echec récupération token', e);
                    setButtonLabel('Connexion');
                }
                close();
            });
        }
        if (input) {
            input.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    submitBtn?.click();
                }
            });
        }
    }

}

customElements.define('custom-head', Head);
