class aside extends HTMLElement {
    constructor() {
        super();
        let shadowRoot = this.attachShadow({ mode: 'open' });

        fetch('../aside/aside.html')
        .then(response => response.text())
        .then(data => {
            let template = new DOMParser().parseFromString(data, 'text/html')
            .querySelector('template').content;
            this.shadowRoot.appendChild(template.cloneNode(true));
        });
    }
}

customElements.define('custom-aside', aside);