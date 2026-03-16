document.addEventListener("DOMContentLoaded", function () {

    let config = {
        labels: {
            copy: 'Copy',
            copyToClipboard: 'Copy to clipboard'
        }
    };

    const configElement = document.getElementById('wiki-view-config');
    if (configElement) {
        try {
            config = JSON.parse(configElement.textContent);
        } catch (e) {
            console.error('Failed to parse wiki view config:', e);
        }
    }

    if (navigator.clipboard && navigator.clipboard.writeText && window.isSecureContext) {
        document.querySelectorAll(".wiki-content pre").forEach(function (element) {

            const wrapper = document.createElement('div');
            wrapper.classList.add('wiki-pre-wrapper');
            element.parentNode.insertBefore(wrapper, element);
            wrapper.appendChild(element);

            const btn = document.createElement('button');
            btn.classList.add('wiki-pre-copy-btn', 'btn', 'btn-secondary', 'btn-sm');
            btn.setAttribute('title', config.labels.copy);
            btn.setAttribute('aria-label', config.labels.copyToClipboard);
            btn.innerHTML = '<i class="bi bi-clipboard"></i>';
            wrapper.appendChild(btn);

            btn.addEventListener('click', function () {
                const text = element.innerText || element.textContent;
                navigator.clipboard.writeText(text)
                    .then(function () {
                        btn.innerHTML = '<i class="bi bi-clipboard-check"></i>';
                        btn.classList.replace('btn-secondary', 'btn-success');
                        setTimeout(function () {
                            btn.innerHTML = '<i class="bi bi-clipboard"></i>';
                            btn.classList.replace('btn-success', 'btn-secondary');
                        }, 2000);
                    })
                    .catch(function (e) {
                        console.error('Failed to copy text:', e);
                    });
            });

        });
    }

});