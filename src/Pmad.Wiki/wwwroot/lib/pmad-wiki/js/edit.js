document.addEventListener('DOMContentLoaded', function () {
    const textarea = document.getElementById('content-textarea');
    if (!textarea) return;

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
                    const currentPageName = pageLinkModal.getAttribute('data-current-page');
                    const response = await fetch(`/Wiki/GetAccessiblePages?currentPageName=${encodeURIComponent(currentPageName)}`);
                    
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
                        pageListContainer.innerHTML = '<div class="alert alert-danger">Failed to load pages. Please try again.</div>';
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
});

function applyMarkdown(textarea, action) {
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selectedText = textarea.value.substring(start, end);
    
    let newText = '';
    let cursorOffset = 0;
    
    switch (action) {
        case 'bold':
            newText = `**${selectedText || 'bold text'}**`;
            cursorOffset = selectedText ? newText.length : 2;
            break;
        case 'italic':
            newText = `*${selectedText || 'italic text'}*`;
            cursorOffset = selectedText ? newText.length : 1;
            break;
        case 'strikethrough':
            newText = `~~${selectedText || 'strikethrough text'}~~`;
            cursorOffset = selectedText ? newText.length : 2;
            break;
        case 'code':
            newText = `\`${selectedText || 'code'}\``;
            cursorOffset = selectedText ? newText.length : 1;
            break;
        case 'h1':
            newText = `# ${selectedText || 'Heading 1'}`;
            cursorOffset = selectedText ? newText.length : 2;
            break;
        case 'h2':
            newText = `## ${selectedText || 'Heading 2'}`;
            cursorOffset = selectedText ? newText.length : 3;
            break;
        case 'h3':
            newText = `### ${selectedText || 'Heading 3'}`;
            cursorOffset = selectedText ? newText.length : 4;
            break;
        case 'ul':
            newText = selectedText ? selectedText.split('\n').map(line => `- ${line}`).join('\n') : '- List item';
            cursorOffset = newText.length;
            break;
        case 'ol':
            newText = selectedText ? selectedText.split('\n').map((line, i) => `${i + 1}. ${line}`).join('\n') : '1. List item';
            cursorOffset = newText.length;
            break;
        case 'quote':
            newText = selectedText ? selectedText.split('\n').map(line => `> ${line}`).join('\n') : '> Quote';
            cursorOffset = newText.length;
            break;
        case 'link':
            newText = `[${selectedText || 'link text'}](url)`;
            cursorOffset = selectedText ? newText.length - 4 : 1;
            break;
        case 'image':
            newText = `![${selectedText || 'alt text'}](image-url)`;
            cursorOffset = selectedText ? newText.length - 11 : 2;
            break;
        case 'codeblock':
            newText = `\`\`\`\n${selectedText || 'code'}\n\`\`\``;
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
