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

}

customElements.define('custom-head', Head);

