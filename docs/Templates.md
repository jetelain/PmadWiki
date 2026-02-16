# Wiki Templates

Templates allow you to create reusable page structures that can be used when creating new wiki pages. This feature helps maintain consistency across your wiki and speeds up page creation.

## Overview

Templates are special wiki pages that contain:
- **Content structure**: Pre-formatted markdown content
- **Front matter metadata**: Configuration for the template (location, naming pattern, description)
- **Placeholders**: Dynamic values that can be filled when creating a page

## Creating a Template

### Template Location

Templates can be stored in two ways:

1. **Dedicated Templates Directory** (Recommended for global templates)
   ```
   _templates/
   ├── Article.md
   ├── Meeting-Notes.md
   └── Project-Plan.md
   ```

2. **Context-Specific Templates** (For section-specific templates)
   ```
   Projects/_template.md
   Meetings/2024/_template.md
   Documentation/_template.md
   ```

### Template Structure

A template is a markdown file with optional front matter:

```markdown
---
title: Meeting Notes Template
description: Template for weekly team meetings
location: "Meetings/{year}"
pattern: "{date}-Meeting"
---

# Meeting Notes - {date}

**Date:** {date}  
**Attendees:**
- 

## Agenda

1. 

## Discussion Notes



## Action Items

- [ ] 
- [ ] 

## Next Meeting

**Date:** 
```

### Front Matter Properties

| Property | Description | Example |
|----------|-------------|---------|
| `title` | Display name shown in template selector | `Meeting Notes Template` |
| `description` | Help text describing the template's purpose | `Template for weekly team meetings` |
| `location` | Default folder for new pages (supports placeholders) | `"Meetings/{year}"` |
| `pattern` | Naming pattern for new pages (supports placeholders) | `"{date}-Meeting"` |

Warning: When using special characters like curly braces `{}` or square brackets `[]` in YAML Front Matter, you must wrap the entire value in quotes. Failing to do so will cause a parsing error.

### Supported Placeholders

Placeholders are automatically replaced when creating a page:

| Placeholder | Description | Example Output |
|-------------|-------------|----------------|
| `{date}` | Current date (ISO format) | `2024-01-15` |
| `{datetime}` | Current date and time | `2024-01-15-143022` |
| `{year}` | Current year | `2024` |
| `{month}` | Current month (2 digits) | `01` |
| `{day}` | Current day (2 digits) | `15` |

## Using Templates

### Step 1: Access Template Selection

There are multiple ways to access the template creation workflow:

1. **From Site Map**: Click the "Create New Page" button
2. **From Any Page**: Click the "New" button (when editing is enabled)
3. **Direct URL**: Navigate to `/Wiki/Create`

### Step 2: Choose a Template

The template selection screen shows:
- **Blank Page**: Start with an empty page
- **Available Templates**: All templates you have access to
  - Template name (from `title` or H1 heading)
  - Description (from front matter)
  - Default location (if specified)
  - Name pattern (if specified)

Click "Use This Template" on your chosen template.

### Step 3: Specify Page Details

Fill in the page information:

1. **Location** (optional): The folder path where the page will be created
   - Leave empty to create at the root level
   - Use `/` to separate folders
   - Example: `Projects/2024`

2. **Page Name** (required): The name of the new page
   - Only letters, numbers, hyphens, and underscores allowed
   - Example: `Q1-Planning`

3. **Full Page Path** (read-only): Preview of the complete page path
   - Automatically calculated from Location + Page Name
   - Example: `Projects/2024/Q1-Planning`

The form will show validation errors if:
- Invalid characters are used
- Required fields are empty
- Page name contains directory traversal attempts

### Step 4: Edit and Save

After clicking "Create Page", you'll be redirected to the edit page with the template content pre-filled. You can:
- Modify the content as needed
- Add or remove sections
- Save the page when ready

## Template Examples

### Example 1: Simple Article Template

**File**: `_templates/Article.md`

```markdown
---
title: Article Template
description: Standard article structure
---

# Article Title

**Author:** 
**Date:** 

## Summary



## Introduction



## Main Content



## Conclusion



## References

- 
```

### Example 2: Meeting Notes with Patterns

**File**: `_templates/Meeting-Notes.md`

```markdown
---
title: Meeting Notes
description: Weekly team meeting template
location: "Meetings/{year}"
pattern: "{date}-Meeting"
---

# Meeting - {date}

**Date:** {date}  
**Time:**  
**Attendees:**
- 

## Agenda

1. Review previous action items
2. Current sprint updates
3. Blockers and issues
4. Next steps

## Discussion



## Decisions Made

- 

## Action Items

- [ ] Action item 1 - Owner - Due date
- [ ] Action item 2 - Owner - Due date

## Next Meeting

**Date:**  
**Topics:**
```

When using this template:
- Location will be pre-filled with: `Meetings/2024` (current year)
- Page name will be suggested as: `2024-01-15-Meeting` (current date)
- Final page path: `Meetings/2024/2024-01-15-Meeting`

## Template Management

### Viewing Available Templates

Templates are automatically discovered from:
1. All files in the `_templates/` directory
2. Any file named `_template.md` anywhere in the wiki

### Template Permissions

Templates follow the same permission system as regular pages:
- **View Permission**: Required to see and use the template
- **Edit Permission**: Required to create or modify templates
- **Page-Level Permissions**: If enabled, templates can have restricted access

### Editing Templates

To edit an existing template:

1. Navigate to the template page (e.g., `_templates/Article`)
2. Click "Edit"
3. Modify the front matter or content
4. Save changes

Changes to templates take effect immediately for new pages created from that template.

### Deleting Templates

Templates are regular wiki pages and can be deleted like any other page. However, templates that are in use won't affect existing pages created from them.

## Best Practices

### 1. Use Clear Naming

Choose descriptive names that clearly indicate the template's purpose:
- ✅ Good: "Meeting-Notes", "Project-Documentation", "Bug-Report"
- ❌ Bad: "Template1", "New-Page", "Misc"

### 2. Provide Good Descriptions

Write helpful descriptions in the front matter:
```markdown
---
description: Template for documenting API endpoints with examples and parameters
---
```

### 3. Use Logical Locations

Set default locations that match your wiki's structure:
```markdown
---
location: API/Documentation
---
```

## Troubleshooting

### Template Not Appearing

**Problem**: Your template doesn't show in the template selector.

**Solutions**:
1. Verify the file is named correctly:
   - In `_templates/` directory with `.md` extension
   - Named `_template.md` in a subdirectory
2. Check page-level permissions if enabled
3. Ensure you have view permission for the template
4. Verify the template page exists (visit it directly)

### Placeholders Not Replaced

**Problem**: Placeholders like `{date}` appear as literal text.

**Solutions**:
1. Use placeholders in the **location** or **pattern** front matter for automatic replacement
2. Placeholders in content are **not** automatically replaced - they serve as visual guides for users
3. For automatic date in content, users must manually replace them when editing

### Invalid Page Name Error

**Problem**: Cannot create page from template due to validation errors.

**Solutions**:
1. Check the generated page name contains only: letters, numbers, hyphens, underscores
2. Ensure location doesn't contain invalid characters
3. Verify no `..` or `//` in the path
4. Make sure the path doesn't start or end with `/`

### Template Access Denied

**Problem**: Cannot see or use certain templates.

**Solutions**:
1. Verify you have edit permission (templates require edit access)
2. Check page-level permissions if enabled
3. Ensure the template page exists and hasn't been deleted
4. Contact a wiki administrator for access

## Technical Notes

### Template Storage

Templates are stored as regular wiki pages in the Git repository. They are not stored separately, which means:
- They benefit from version control
- They can be edited through the wiki interface
- They follow the same backup and restore processes
- They can be cloned/branched with the wiki

### Template Discovery

The wiki scans for templates on each page load by:
1. Querying all pages through `IWikiPageService`
2. Filtering pages that match template naming conventions
3. Loading front matter metadata
4. Extracting display names from front matter or H1 headings

### Front Matter Parsing

Front matter must:
- Be at the very start of the file
- Use `---` delimiters on separate lines
- Follow YAML-like syntax: `key: value`
- Come before any markdown content

### Pattern Generation

Pattern placeholders are replaced server-side:
- Date/time values use UTC timezone
- Patterns are evaluated when displaying the page name form
- Invalid characters are not automatically stripped (validation will fail)

## FAQ

**Q: Can I use templates across different languages/cultures?**  
A: Yes, templates have no culture by default and can be used to create pages in any culture.

**Q: Can I nest template locations?**  
A: Yes, use forward slashes in the location field: `Projects/2024/Q1`

**Q: Can I create a template from an existing page?**  
A: Yes, navigate to the page, copy its content, and save it as a template in `_templates/`

**Q: How many templates can I create?**  
A: There is no hard limit, but keep templates organized and purposeful for best user experience.

**Q: Can templates include media files?**  
A: No, templates contain only markdown content. Media files can be added when editing the new page.

**Q: Can I share templates between wikis?**  
A: Templates are stored in the Git repository and can be copied or merged between wiki repositories.

**Q: Do changes to templates affect existing pages?**  
A: No, templates are only applied when creating a new page. Existing pages are independent.

**Q: Can I use variables other than the predefined placeholders?**  
A: No, only the predefined placeholders are supported. Custom variables would need to be manually replaced by users.

---

**Need Help?** Contact your wiki administrator or [open an issue on GitHub](https://github.com/jetelain/PmadWiki/issues).
