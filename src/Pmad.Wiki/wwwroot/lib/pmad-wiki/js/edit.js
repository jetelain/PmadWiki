document.addEventListener('DOMContentLoaded', function () {
    const textarea = document.getElementById('content-textarea');
    if (!textarea) return;

    // Get configuration from the JSON script tag
    let config = {
        apiEndpoints: {
            previewMarkdown: '/Wiki/PreviewMarkdown',
            uploadMedia: '/Wiki/UploadMedia',
            getAccessiblePages: '/Wiki/GetAccessiblePages'
        },
        currentPage: {
            pageName: '',
            culture: ''
        },
        labels: {
            edit: 'Edit', 
            preview: 'Preview',
            noContentToPreview: 'No content to preview.',
            failedToRenderPreview: 'Failed to render preview. Please try again.',
            uploading: 'Uploading...',
            uploadFailed: 'Upload failed:',
            close: 'Close',
            failedToLoadPages: 'Failed to load pages. Please try again.',
            boldText: 'bold text',
            italicText: 'italic text',
            strikethroughText: 'strikethrough text',
            code: 'code',
            heading1: 'Heading 1',
            heading2: 'Heading 2',
            heading3: 'Heading 3',
            listItem: 'List item',
            quote: 'Quote',
            linkText: 'link text',
            altText: 'alt text'
        }
    };

    const configElement = document.getElementById('wiki-edit-config');
    if (configElement) {
        try {
            config = JSON.parse(configElement.textContent);
        } catch (e) {
            console.error('Failed to parse wiki edit config:', e);
        }
    }

    // Track uploaded media files
    const uploadedMedia = new Set();
    const temporaryMediaIdsInput = document.getElementById('temporary-media-ids');

    // Track changes for unsaved warning
    const form = textarea.closest('form');
    const commitMessageInput = document.querySelector('input[name="CommitMessage"]');
    let initialContent = textarea.value;
    let initialCommitMessage = commitMessageInput ? commitMessageInput.value : '';
    let hasUnsavedChanges = false;
    let isFormSubmitting = false;

    function checkForChanges() {
        const contentChanged = textarea.value !== initialContent;
        const commitMessageChanged = commitMessageInput && commitMessageInput.value !== initialCommitMessage;
        hasUnsavedChanges = contentChanged || commitMessageChanged;
    }

    // Listen for content changes
    textarea.addEventListener('input', checkForChanges);
    if (commitMessageInput) {
        commitMessageInput.addEventListener('input', checkForChanges);
    }

    // Track when form is being submitted
    if (form) {
        form.addEventListener('submit', function () {
            isFormSubmitting = true;
        });
    }

    // Warn before leaving page with unsaved changes
    window.addEventListener('beforeunload', function (e) {
        if (hasUnsavedChanges && !isFormSubmitting) {
            e.preventDefault();
            // Modern browsers ignore custom messages, but setting returnValue triggers the warning
            e.returnValue = '';
            return '';
        }
    });

    // Preview toggle functionality
    const togglePreviewBtn = document.getElementById('togglePreview');
    const previewContainer = document.getElementById('preview-container');
    const previewContent = document.getElementById('preview-content');
    const previewLoading = document.getElementById('preview-loading');
    const previewButtonText = document.getElementById('previewButtonText');
    const markdownToolbarButtons = document.querySelectorAll('[data-markdown-action]');
    let isPreviewMode = false;
    let previewDebounceTimer = null;

    function toggleEditingButtons(hide) {
        markdownToolbarButtons.forEach(button => {
            if (hide) {
                button.disabled = true;
                button.classList.add('disabled');
            } else {
                button.disabled = false;
                button.classList.remove('disabled');
            }
        });
    }

    if (togglePreviewBtn && previewContainer && previewContent) {
        togglePreviewBtn.addEventListener('click', function () {
            isPreviewMode = !isPreviewMode;

            if (isPreviewMode) {
                textarea.classList.add("d-none");
                previewContainer.classList.remove("d-none");
                previewButtonText.textContent = config.labels.edit;
                togglePreviewBtn.querySelector('i').className = 'bi bi-pencil';
                toggleEditingButtons(true);
                updatePreview();
            } else {
                textarea.classList.remove("d-none");
                previewContainer.classList.add("d-none");
                previewButtonText.textContent = config.labels.preview;
                togglePreviewBtn.querySelector('i').className = 'bi bi-eye';
                toggleEditingButtons(false);
            }
        });

        let revealInstance = null;
        let revealCssLoaded = false;
        let revealJsLoaded = false;
        let currentRevealTheme = null;

        function ensureRevealCss() {
            if (revealCssLoaded) return;
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = 'https://cdn.jsdelivr.net/npm/reveal.js@5/dist/reveal.css';
            document.head.appendChild(link);
            revealCssLoaded = true;
        }

        function ensureRevealThemeCss(theme) {
            if (currentRevealTheme === theme) return;
            const oldLink = document.getElementById('reveal-preview-theme-css');
            if (oldLink) {
                oldLink.remove();
            }
            const link = document.createElement('link');
            link.id = 'reveal-preview-theme-css';
            link.rel = 'stylesheet';
            link.href = `https://cdn.jsdelivr.net/npm/reveal.js@5/dist/theme/${theme}.css`;
            document.head.appendChild(link);
            currentRevealTheme = theme;
        }

        async function ensureRevealJs() {
            if (revealJsLoaded) return;
            await new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/reveal.js@5/dist/reveal.js';
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            });
            revealJsLoaded = true;
        }

        async function initSlideshowPreview() {
            if (revealInstance) {
                revealInstance.destroy();
                revealInstance = null;
            }
            const revealEl = previewContent.querySelector('.reveal');
            if (!revealEl) {
                previewContent.classList.remove('wiki-slideshow-wrapper');
                previewContainer.style.overflowY = '';
                return;
            }
            previewContent.classList.add('wiki-slideshow-wrapper');
            previewContainer.style.overflowY = 'hidden';
            ensureRevealCss();
            ensureRevealThemeCss(revealEl.dataset.theme || 'black');
            await ensureRevealJs();
            revealInstance = new Reveal(revealEl, { embedded: true });
            await revealInstance.initialize();
        }

        async function updatePreview() {
            if (!isPreviewMode) return;

            const markdown = textarea.value;

            if (!markdown.trim()) {
                if (revealInstance) {
                    revealInstance.destroy();
                    revealInstance = null;
                }
                previewContent.classList.remove('wiki-slideshow-wrapper');
                previewContainer.style.overflowY = '';
                const emptyMessage = document.createElement('p');
                emptyMessage.className = 'text-muted';
                emptyMessage.textContent = config.labels.noContentToPreview;
                previewContent.innerHTML = '';
                previewContent.appendChild(emptyMessage);
                return;
            }

            previewLoading.classList.remove("d-none");
            previewContent.classList.add("d-none");

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

                const request = {
                    markdown: markdown,
                    pageName: config.currentPage.pageName || null,
                    culture: config.currentPage.culture || null
                };

                const response = await fetch(config.apiEndpoints.previewMarkdown, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(request)
                });

                if (!response.ok) {
                    throw new Error('Failed to render preview');
                }

                const html = await response.text();
                previewContent.innerHTML = html;
                // Make element visible before slideshow init so reveal.js can measure dimensions
                previewLoading.classList.add("d-none");
                previewContent.classList.remove("d-none");
                try {
                    await initSlideshowPreview();
                } catch (slideError) {
                    console.error('Error initializing slideshow preview:', slideError);
                    previewContent.classList.remove('wiki-slideshow-wrapper');
                    previewContainer.style.overflowY = '';
                }
            } catch (error) {
                console.error('Error rendering preview:', error);
                if (revealInstance) {
                    revealInstance.destroy();
                    revealInstance = null;
                }
                previewContent.classList.remove('wiki-slideshow-wrapper');
                previewContainer.style.overflowY = '';
                previewContent.innerHTML = '';
                previewContent.appendChild(createAlert(config.labels.failedToRenderPreview));
                previewLoading.classList.add("d-none");
                previewContent.classList.remove("d-none");
            }
        }

        // Auto-update preview on content change (debounced)
        textarea.addEventListener('input', function () {
            if (isPreviewMode) {
                if (previewDebounceTimer) {
                    clearTimeout(previewDebounceTimer);
                }
                previewDebounceTimer = setTimeout(() => {
                    updatePreview();
                }, 1000);
            }
        });
    }

    // Drag and drop support for media upload
    textarea.addEventListener('dragover', function (e) {
        e.preventDefault();
        e.stopPropagation();
        textarea.classList.add('drag-over');
    });

    textarea.addEventListener('dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
        textarea.classList.remove('drag-over');
    });

    textarea.addEventListener('drop', async function (e) {
        e.preventDefault();
        e.stopPropagation();
        textarea.classList.remove('drag-over');

        const files = e.dataTransfer.files;
        if (files.length > 0) {
            await handleFileUploads(files);
        }
    });

    // Paste support for media upload
    textarea.addEventListener('paste', async function (e) {
        const items = e.clipboardData?.items;
        if (!items) return;

        const files = [];
        for (let i = 0; i < items.length; i++) {
            if (items[i].kind === 'file') {
                const file = items[i].getAsFile();
                if (file) {
                    files.push(file);
                }
            }
        }

        if (files.length > 0) {
            e.preventDefault();
            await handleFileUploads(files);
        }
    });

    async function handleFileUploads(files) {
        for (const file of files) {
            await uploadFile(file);
        }
    }

    async function uploadFile(file) {
        const formData = new FormData();
        formData.append('file', file);

        try {
            showUploadIndicator(file.name);

            const response = await fetch(config.apiEndpoints.uploadMedia, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Upload failed');
            }

            const result = await response.json();

            // Track the uploaded media
            uploadedMedia.add(result.temporaryId);
            updateTemporaryMediaIds();

            // Insert markdown reference at cursor position
            const markdownRef = isImageFile(file.name)
                ? `![${file.name}](${result.url})`
                : `[${file.name}](${result.url})`;

            insertTextWithUndo(textarea, textarea.selectionStart, textarea.selectionEnd, markdownRef, markdownRef.length);

            hideUploadIndicator();
        } catch (error) {
            console.error('Upload error:', error);
            showUploadError(file.name, error.message);
            hideUploadIndicator();
        }
    }

    function updateTemporaryMediaIds() {
        if (temporaryMediaIdsInput) {
            temporaryMediaIdsInput.value = Array.from(uploadedMedia).join(',');
        }
    }

    function isImageFile(fileName) {
        const ext = fileName.toLowerCase().split('.').pop();
        return ['png', 'jpg', 'jpeg', 'gif', 'svg', 'webp'].includes(ext);
    }

    function createAlert(message, type = 'danger') {
        const alert = document.createElement('div');
        alert.className = `alert alert-${type}`;
        alert.textContent = message;
        return alert;
    }

    function showUploadIndicator(fileName) {
        const indicator = document.createElement('div');
        indicator.id = 'upload-indicator';
        indicator.className = 'alert alert-info';

        const wrapper = document.createElement('div');
        wrapper.className = 'd-flex align-items-center';

        const spinner = document.createElement('div');
        spinner.className = 'spinner-border spinner-border-sm me-2';
        spinner.setAttribute('role', 'status');

        const visuallyHidden = document.createElement('span');
        visuallyHidden.className = 'visually-hidden';
        visuallyHidden.textContent = config.labels.uploading;

        spinner.appendChild(visuallyHidden);

        const textDiv = document.createElement('div');
        textDiv.textContent = config.labels.uploading + ' ' + fileName;

        wrapper.appendChild(spinner);
        wrapper.appendChild(textDiv);

        indicator.appendChild(wrapper);
        textarea.parentElement.insertBefore(indicator, textarea);
    }

    function hideUploadIndicator() {
        const indicator = document.getElementById('upload-indicator');
        if (indicator) {
            indicator.remove();
        }
    }

    function showUploadError(fileName, errorMessage) {
        const existingError = document.getElementById('upload-error');
        if (existingError) {
            existingError.remove();
        }

        const errorAlert = document.createElement('div');
        errorAlert.id = 'upload-error';
        errorAlert.className = 'alert alert-danger alert-dismissible fade show';
        errorAlert.setAttribute('role', 'alert');

        const errorText = document.createElement('div');
        const strongLabel = document.createElement('strong');
        strongLabel.textContent = config.labels.uploadFailed;
        errorText.appendChild(strongLabel);
        errorText.appendChild(document.createTextNode(` ${fileName} - ${errorMessage}`));

        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'btn-close';
        closeButton.setAttribute('data-bs-dismiss', 'alert');
        closeButton.setAttribute('aria-label', config.labels.close);

        errorAlert.appendChild(errorText);
        errorAlert.appendChild(closeButton);

        textarea.parentElement.insertBefore(errorAlert, textarea);

        setTimeout(() => {
            if (errorAlert.parentElement) {
                errorAlert.classList.remove('show');
                setTimeout(() => errorAlert.remove(), 150);
            }
        }, 5000);
    }

    // Markdown formatting toolbar handlers
    document.querySelectorAll('[data-markdown-action]').forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            const action = this.getAttribute('data-markdown-action');
            if (action !== 'page-link') {
                applyMarkdown(textarea, action);
            }
        });
    });

    // Page link modal search
    const pageSearchInput = document.getElementById('pageSearchInput');
    const pageList = document.getElementById('pageList');
    const pageListContainer = document.getElementById('pageListContainer');
    const pageLinkModal = document.getElementById('pageLinkModal');

    let pagesLoaded = false;

    if (pageLinkModal) {
        // Load pages when modal is shown
        pageLinkModal.addEventListener('show.bs.modal', async function () {
            if (!pagesLoaded) {
                try {
                    const currentPageName = config.currentPage.pageName;
                    const response = await fetch(`${config.apiEndpoints.getAccessiblePages}?currentPageName=${encodeURIComponent(currentPageName)}`);

                    if (!response.ok) {
                        throw new Error('Failed to load pages');
                    }

                    const html = await response.text();

                    if (pageList) {
                        pageList.innerHTML = html;
                        attachPageLinkHandlers();
                    }

                    pagesLoaded = true;

                    if (pageListContainer) {
                        pageListContainer.style.display = 'none';
                    }
                    if (pageList) {
                        pageList.style.display = '';
                    }
                } catch (error) {
                    console.error('Error loading pages:', error);
                    if (pageListContainer) {
                        pageListContainer.innerHTML = '';
                        pageListContainer.appendChild(createAlert(config.labels.failedToLoadPages));
                    }
                }
            }
        });
    }

    function attachPageLinkHandlers() {
        if (!pageList) return;

        const pageItems = pageList.querySelectorAll('.page-link-item');

        pageItems.forEach(item => {
            item.addEventListener('click', function () {
                const relativePath = this.getAttribute('data-relative-path');
                const pageTitle = this.getAttribute('data-page-title');
                handlePageSelection(relativePath, pageTitle);
            });
        });
    }

    if (pageSearchInput && pageList) {
        pageSearchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase();
            const items = pageList.querySelectorAll('.page-link-item');

            items.forEach(item => {
                const pageName = item.getAttribute('data-page-name').toLowerCase();
                const pageTitle = (item.getAttribute('data-page-title') || '').toLowerCase();

                if (pageName.includes(searchTerm) || pageTitle.includes(searchTerm)) {
                    item.style.display = '';
                } else {
                    item.style.display = 'none';
                }
            });
        });

        function handlePageSelection(relativePath, pageTitle) {
            const modalElement = document.getElementById('pageLinkModal');

            modalElement.addEventListener('hidden.bs.modal', () => {
                pageSearchInput.value = '';
                const items = pageList.querySelectorAll('.page-link-item');
                items.forEach(i => i.style.display = '');
                insertWikiLink(textarea, relativePath, pageTitle);
            }, { once: true });

            const modal = bootstrap.Modal.getInstance(modalElement);
            modal?.hide();
        }
    }

    function applyMarkdown(textarea, action) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);

        let newText = '';
        let cursorOffset = 0;

        switch (action) {
            case 'bold':
                newText = `**${selectedText || config.labels.boldText}**`;
                cursorOffset = selectedText ? newText.length : 2;
                break;
            case 'italic':
                newText = `*${selectedText || config.labels.italicText}*`;
                cursorOffset = selectedText ? newText.length : 1;
                break;
            case 'strikethrough':
                newText = `~~${selectedText || config.labels.strikethroughText}~~`;
                cursorOffset = selectedText ? newText.length : 2;
                break;
            case 'code':
                newText = `\`${selectedText || config.labels.code}\``;
                cursorOffset = selectedText ? newText.length : 1;
                break;
            case 'h1':
                newText = `# ${selectedText || config.labels.heading1}`;
                cursorOffset = selectedText ? newText.length : 2;
                break;
            case 'h2':
                newText = `## ${selectedText || config.labels.heading2}`;
                cursorOffset = selectedText ? newText.length : 3;
                break;
            case 'h3':
                newText = `### ${selectedText || config.labels.heading3}`;
                cursorOffset = selectedText ? newText.length : 4;
                break;
            case 'ul':
                newText = selectedText ? selectedText.split('\n').map(line => `- ${line}`).join('\n') : `- ${config.labels.listItem}`;
                cursorOffset = newText.length;
                break;
            case 'ol':
                newText = selectedText ? selectedText.split('\n').map((line, i) => `${i + 1}. ${line}`).join('\n') : `1. ${config.labels.listItem}`;
                cursorOffset = newText.length;
                break;
            case 'quote':
                newText = selectedText ? selectedText.split('\n').map(line => `> ${line}`).join('\n') : `> ${config.labels.quote}`;
                cursorOffset = newText.length;
                break;
            case 'link':
                newText = `[${selectedText || config.labels.linkText}](url)`;
                cursorOffset = selectedText ? newText.length - 4 : 1;
                break;
            case 'image':
                newText = `![${selectedText || config.labels.altText}](image-url)`;
                cursorOffset = selectedText ? newText.length - 11 : 2;
                break;
            case 'codeblock':
                newText = `\`\`\`\n${selectedText || config.labels.code}\n\`\`\``;
                cursorOffset = selectedText ? 4 + selectedText.length : 4;
                break;
            case 'hr':
                newText = '\n---\n';
                cursorOffset = newText.length;
                break;
            case 'table':
                newText = '| Header 1 | Header 2 |\n| --- | --- |\n| Cell 1 | Cell 2 |';
                cursorOffset = 2;
                break;
            default:
                return;
        }

        insertTextWithUndo(textarea, start, end, newText, cursorOffset);
    }

    function insertWikiLink(textarea, relativePath, pageTitle) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);

        const linkText = selectedText || pageTitle;
        const wikiLink = `[${linkText}](${relativePath}.md)`;

        insertTextWithUndo(textarea, start, end, wikiLink, wikiLink.length);
    }

    // Media Gallery modal functionality
    const mediaGalleryModal = document.getElementById('mediaGalleryModal');
    const mediaSearchInput = document.getElementById('mediaSearchInput');
    const mediaGallery = document.getElementById('mediaGallery');
    const mediaGalleryContainer = document.getElementById('mediaGalleryContainer');
    
    let mediaLoaded = false;
    let allMediaItems = [];

    if (mediaGalleryModal) {
        // Load media when modal is shown
        mediaGalleryModal.addEventListener('show.bs.modal', async function () {
            if (!mediaLoaded) {
                try {
                    const currentPageName = config.currentPage.pageName;
                    const response = await fetch(`${config.apiEndpoints.getMediaGallery}?currentPageName=${encodeURIComponent(currentPageName)}`);

                    if (!response.ok) {
                        throw new Error('Failed to load media');
                    }

                    const html = await response.text();

                    if (mediaGallery) {
                        mediaGallery.innerHTML = html;
                        attachMediaGalleryHandlers();
                        allMediaItems = Array.from(mediaGallery.querySelectorAll('.media-gallery-item'));
                    }

                    mediaLoaded = true;

                    if (mediaGalleryContainer) {
                        mediaGalleryContainer.style.display = 'none';
                    }
                    if (mediaGallery) {
                        mediaGallery.style.display = '';
                    }
                } catch (error) {
                    console.error('Error loading media gallery:', error);
                    if (mediaGalleryContainer) {
                        mediaGalleryContainer.innerHTML = '';
                        mediaGalleryContainer.appendChild(createAlert(config.labels.failedToLoadMedia));
                    }
                }
            }
        });
    }

    function attachMediaGalleryHandlers() {
        if (!mediaGallery) return;

        const insertButtons = mediaGallery.querySelectorAll('.insert-media-btn');

        insertButtons.forEach(button => {
            button.addEventListener('click', function () {
                const card = this.closest('.media-gallery-item');
                const mediaPath = card.getAttribute('data-path');
                const fileName = card.getAttribute('data-filename');
                const mediaUrl = card.getAttribute('data-url');
                const mediaType = card.getAttribute('data-media-type');
                
                handleMediaSelection(mediaPath, fileName, mediaUrl, mediaType);
            });
        });
    }

    if (mediaSearchInput && mediaGallery) {
        mediaSearchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase();

            allMediaItems.forEach(item => {
                const path = item.getAttribute('data-path').toLowerCase();
                const fileName = item.getAttribute('data-filename').toLowerCase();

                if (path.includes(searchTerm) || fileName.includes(searchTerm)) {
                    item.closest('.col-md-4').style.display = '';
                } else {
                    item.closest('.col-md-4').style.display = 'none';
                }
            });
        });
    }

    function handleMediaSelection(mediaPath, fileName, mediaUrl, mediaType) {
        const modalElement = document.getElementById('mediaGalleryModal');

        modalElement.addEventListener('hidden.bs.modal', () => {
            if (mediaSearchInput) {
                mediaSearchInput.value = '';
                allMediaItems.forEach(item => {
                    item.closest('.col-md-4').style.display = '';
                });
            }
            insertMediaReference(textarea, mediaPath, fileName, mediaType);
        }, { once: true });

        const modal = bootstrap.Modal.getInstance(modalElement);
        modal?.hide();
    }

    function insertMediaReference(textarea, mediaPath, fileName, mediaType) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;

        let markdownRef;
        if (mediaType === 'image') {
            markdownRef = `![${fileName}](${mediaPath})`;
        } else {
            markdownRef = `[${fileName}](${mediaPath})`;
        }

        insertTextWithUndo(textarea, start, end, markdownRef, markdownRef.length);
    }

    function insertTextWithUndo(textarea, start, end, newText, cursorOffset) {
        textarea.focus();
        textarea.setSelectionRange(start, end);

        let success = false;

        // Try execCommand first for better undo/redo support
        try {
            if (document.execCommand && document.queryCommandSupported('insertText')) {
                success = document.execCommand('insertText', false, newText);
            }
        } catch (e) {
            success = false;
        }

        // Fallback if execCommand fails or is not supported
        if (!success) {
            textarea.setRangeText(newText, start, end, 'select');
            textarea.dispatchEvent(new InputEvent('input', { bubbles: true, cancelable: true }));
        }

        // Set cursor position
        const newPosition = start + cursorOffset;
        textarea.setSelectionRange(newPosition, newPosition);
    }

    // Front matter modal
    const frontMatterModal = document.getElementById('frontMatterModal');
    const saveFrontMatterBtn = document.getElementById('saveFrontMatter');

    function extractFrontMatter(content) {
        const match = /^\uFEFF?\s*---\s*\r?\n([\s\S]*?)\r?\n---\s*\r?\n/.exec(content);
        if (!match) return { yamlText: '', body: content };
        const yamlText = match[1].replace(/\r\n/g, '\n').replace(/\r/g, '\n');
        return { yamlText, body: content.slice(match[0].length) };
    }

    function getYamlFieldValue(yamlText, key) {
        const match = new RegExp(`^${key}:\\s*(.*)$`, 'm').exec(yamlText);
        if (!match) return null;
        const rawVal = match[1].trim();
        if (rawVal === 'true') return true;
        if (rawVal === 'false') return false;

        // Handle quoted scalars: strip quotes and unescape content
        if (
            rawVal.length >= 2 &&
            ((rawVal.startsWith('"') && rawVal.endsWith('"')) ||
             (rawVal.startsWith("'") && rawVal.endsWith("'")))
        ) {
            const quoteChar = rawVal[0];
            let inner = rawVal.slice(1, -1);

            if (quoteChar === '"') {
                // Inverse of yamlEscapeString: "\\" -> "\", and \" -> "
                inner = inner.replace(/\\\\/g, '\\').replace(/\\"/g, '"');
            } else if (quoteChar === "'") {
                // YAML single-quoted style: '' represents a single '
                inner = inner.replace(/''/g, "'");
            }

            return inner || null;
        }

        return rawVal || null;
    }

    function yamlEscapeString(str) {
        if (/[:#{}\[\]]/.test(str) || str !== str.trim()) {
            return '"' + str.replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"';
        }
        return str;
    }

    function setYamlField(yamlText, key, value) {
        const lineRegex = new RegExp(`^${key}:.*$`, 'm');
        if (value === null || value === '' || value === false || value === undefined) {
            return yamlText.replace(lineRegex, '').replace(/\n{3,}/g, '\n').replace(/^\n+|\n+$/g, '');
        }
        const yamlValue = typeof value === 'boolean' || typeof value === 'number' ? `${value}` : yamlEscapeString(value);
        const newLine = `${key}: ${yamlValue}`;
        if (lineRegex.test(yamlText)) {
            return yamlText.replace(lineRegex, newLine);
        }
        return (yamlText ? yamlText + '\n' : '') + newLine;
    }

    function buildContent(yamlText, body) {
        const trimmedYaml = yamlText.trim();
        if (!trimmedYaml) return body;
        return `---\n${trimmedYaml}\n---\n${body}`;
    }

    if (frontMatterModal) {
        // Wire up dependency relationships declared via data-fm-depends-on
        document.querySelectorAll('[data-fm-depends-on]').forEach(input => {
            const dependsOnKey = input.dataset.fmDependsOn;
            if (!dependsOnKey) return;
            const parentInput = document.querySelector(`[data-fm-key="${dependsOnKey}"]`);
            if (!parentInput) return;
            parentInput.addEventListener('change', function () {
                input.disabled = !this.checked;
                if (!this.checked) {
                    if (input.type === 'checkbox') {
                        input.checked = false;
                    } else {
                        input.value = '';
                    }
                }
            });
        });

        frontMatterModal.addEventListener('show.bs.modal', function () {
            const { yamlText } = extractFrontMatter(textarea.value);
            document.querySelectorAll('[data-fm-key]').forEach(input => {
                const value = getYamlFieldValue(yamlText, input.dataset.fmKey);
                if (input.dataset.fmType === 'checkbox') {
                    input.checked = value === true;
                    const dependsOnKey = input.dataset.fmDependsOn;
                    if (dependsOnKey) {
                        const parent = document.querySelector(`[data-fm-key="${dependsOnKey}"]`);
                        input.disabled = parent ? !parent.checked : false;
                    }
                } else if (input.dataset.fmType === 'number') {
                    let numericValue = null;
                    if (value !== undefined && value !== null && value !== '') {
                        const parsed = typeof value === 'number' ? value : parseInt(value, 10);
                        numericValue = !isNaN(parsed) ? parsed : null;
                    }
                    input.value = numericValue !== null && numericValue !== 0 ? String(numericValue) : '';
                } else {
                    input.value = value || '';
                }
                if (input.dataset.fmType !== 'checkbox' && input.dataset.fmDependsOn) {
                    const parent = document.querySelector(`[data-fm-key="${input.dataset.fmDependsOn}"]`);
                    input.disabled = parent ? !parent.checked : false;
                }
            });
        });

        if (saveFrontMatterBtn) {
            saveFrontMatterBtn.addEventListener('click', function () {
                const { yamlText, body } = extractFrontMatter(textarea.value);
                let newYaml = yamlText;
                document.querySelectorAll('[data-fm-key]').forEach(input => {
                    let value;
                    if (input.dataset.fmType === 'checkbox') {
                        value = input.checked;
                    } else if (input.dataset.fmType === 'number') {
                        const num = parseInt(input.value, 10);
                        value = !isNaN(num) && num !== 0 ? num : null;
                    } else {
                        value = input.value.trim();
                    }
                    newYaml = setYamlField(newYaml, input.dataset.fmKey, value);
                });
                const newContent = buildContent(newYaml, body);
                frontMatterModal.addEventListener('hidden.bs.modal', () => {
                    insertTextWithUndo(textarea, 0, textarea.value.length, newContent, newContent.length);
                }, { once: true });
                const modal = bootstrap.Modal.getInstance(frontMatterModal);
                modal?.hide();
            });
        }
    }
});