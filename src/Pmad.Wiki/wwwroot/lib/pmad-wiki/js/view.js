document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".wiki-content pre").forEach(function (element) {

        const wrapper = document.createElement('div');
        wrapper.classList.add('wiki-pre-wrapper');
        element.parentNode.insertBefore(wrapper, element);
        wrapper.appendChild(element);

        const btn = document.createElement('button');
        btn.classList.add('wiki-pre-copy-btn', 'btn', 'btn-secondary', 'btn-sm');
        btn.setAttribute('title', 'Copier');
        btn.innerHTML = '<i class="bi bi-clipboard"></i>';
        wrapper.appendChild(btn);

        btn.addEventListener('click', function () {
            const text = element.innerText || element.textContent;
            navigator.clipboard.writeText(text).then(function () {
                btn.innerHTML = '<i class="bi bi-clipboard-check"></i>';
                btn.classList.replace('btn-secondary', 'btn-success');
                setTimeout(function () {
                    btn.innerHTML = '<i class="bi bi-clipboard"></i>';
                    btn.classList.replace('btn-success', 'btn-secondary');
                }, 2000);
            });
        });

    });

});