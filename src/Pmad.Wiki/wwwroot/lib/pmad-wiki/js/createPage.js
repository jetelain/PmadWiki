document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const locationInput = document.querySelector('input[name="Location"]');
    const nameInput = document.querySelector('input[name="PageName"]');
    const fullPathInput = document.getElementById('full-page-path');
    const parameterInputs = document.querySelectorAll('.template-parameter');
    const locationPattern = document.querySelector('input[name="LocationPattern"]')?.value || '';
    const pageNamePattern = document.querySelector('input[name="PageNamePattern"]')?.value || '';
    const browserTimestampValue = document.querySelector('input[name="BrowserTimestamp"]')?.value;
    const browserTimestamp = browserTimestampValue ? new Date(browserTimestampValue) : new Date();

    // Track if user has manually edited location or page name
    let locationManuallyEdited = false;
    let pageNameManuallyEdited = false;

    function sanitizeValue(value) {
        if (!value) return '';

        // Normalize and remove accents
        value = value.normalize('NFD').replace(/[\u0300-\u036f]/g, '');

        // Replace spaces and dots with hyphens
        value = value.replace(/[\s.]+/g, '-');

        // Remove invalid characters (keep only alphanumeric, hyphens, underscores)
        value = value.replace(/[^a-zA-Z0-9_-]/g, '');

        // Remove leading/trailing hyphens and underscores
        value = value.replace(/^[-_]+|[-_]+$/g, '');

        // Collapse multiple hyphens/underscores
        value = value.replace(/--+/g, '-').replace(/__+/g, '_');

        return value;
    }

    function sanitizeLocation(value) {
        if (!value) return '';

        // Split by directory separator, sanitize each part, and rejoin
        const parts = value.split('/').map(sanitizeValue).filter(p => p.length > 0);
        return parts.join('/');
    }

    function getParameterValues() {
        const values = {};
        parameterInputs.forEach(input => {
            const paramName = input.dataset.paramName;
            let value = input.value;

            // Sanitize the value for use in page names/locations
            if (input.type === 'date' || input.type === 'datetime-local') {
                // For dates, keep the format as-is but remove separators for page names
                value = value.replace(/[T:\s]/g, '-');
            }

            values[paramName] = sanitizeValue(value);
        });
        return values;
    }

    function resolvePlaceholders(pattern, paramValues) {
        if (!pattern) return '';

        let result = pattern;

        // Replace parameter placeholders
        for (const [key, value] of Object.entries(paramValues)) {
            const escapedKey = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const placeholder = new RegExp(`\\{${escapedKey}\\}`, 'gi');
            result = result.replace(placeholder, value);
        }

        // Date placeholders resolved using the browser timestamp captured when the page was loaded
        const tsYear  = browserTimestamp.getFullYear().toString();
        const tsMonth = (browserTimestamp.getMonth() + 1).toString().padStart(2, '0');
        const tsDay   = browserTimestamp.getDate().toString().padStart(2, '0');
        const tsHours = browserTimestamp.getHours().toString().padStart(2, '0');
        const tsMins  = browserTimestamp.getMinutes().toString().padStart(2, '0');
        const tsSecs  = browserTimestamp.getSeconds().toString().padStart(2, '0');
        result = result.replace(/\{date\}/gi, `${tsYear}-${tsMonth}-${tsDay}`);
        result = result.replace(/\{datetime\}/gi, `${tsYear}-${tsMonth}-${tsDay}-${tsHours}${tsMins}${tsSecs}`);
        result = result.replace(/\{year\}/gi, tsYear);
        result = result.replace(/\{month\}/gi, tsMonth);
        result = result.replace(/\{day\}/gi, tsDay);

        return result;
    }

    function updateSuggestedValues() {
        const paramValues = getParameterValues();

        // Update location if not manually edited and pattern exists
        if (!locationManuallyEdited && locationPattern) {
            const resolvedLocation = resolvePlaceholders(locationPattern, paramValues);
            const sanitizedLocation = sanitizeLocation(resolvedLocation);
            locationInput.value = sanitizedLocation;
        }

        // Update page name if not manually edited and pattern exists
        if (!pageNameManuallyEdited && pageNamePattern) {
            const resolvedName = resolvePlaceholders(pageNamePattern, paramValues);
            const sanitizedName = sanitizeValue(resolvedName);
            nameInput.value = sanitizedName;
        }

        updateFullPath();
    }

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

    // Listen to parameter changes
    parameterInputs.forEach(input => {
        input.addEventListener('input', updateSuggestedValues);
    });

    // Track manual edits to location and page name
    locationInput.addEventListener('input', function() {
        locationManuallyEdited = true;
        updateFullPath();
    });

    nameInput.addEventListener('input', function() {
        pageNameManuallyEdited = true;
        updateFullPath();
    });

    // Initial update
    updateFullPath();
});
