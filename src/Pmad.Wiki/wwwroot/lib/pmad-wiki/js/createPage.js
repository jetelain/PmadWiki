document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const locationInput = document.querySelector('input[name="Location"]');
    const nameInput = document.querySelector('input[name="PageName"]');
    const fullPathInput = document.getElementById('full-page-path');

    function updateFullPath() {
        const location = locationInput.value.trim();
        const name = nameInput.value.trim();

        if (location && name) {
            fullPathInput.value = location + '/' + name;
        } else if (name) {
            fullPathInput.value = name;
        } else {
            fullPathInput.value = '';
        }
    }

    locationInput.addEventListener('input', updateFullPath);
    nameInput.addEventListener('input', updateFullPath);

    // Initial update
    updateFullPath();
});