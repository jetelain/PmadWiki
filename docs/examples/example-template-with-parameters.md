# Example Template with Custom Parameters

This is an example of a wiki template that uses custom parameters.

To use this in your wiki, create a file in `_templates/` directory (e.g., `_templates/Feature-Doc.md`) with the following content:

```markdown
---
title: Feature Documentation
description: Document a new feature with ticket tracking
location: "Features/{category}"
pattern: "{ticket-id}-{name}"
parameters:
  - name: category
    type: enum
    label: Feature Category
    required: true
    help: Category for organizing features
    options:
      - core
      - ui
      - api
      - backend
      - infrastructure
  - name: ticket-id
    type: text
    label: Ticket ID
    required: true
    help: Issue tracker ticket ID (e.g., JIRA-123, GH-456)
  - name: name
    type: text
    label: Feature Name
    required: true
    help: Short descriptive name for the feature
  - name: author
    type: text
    label: Author
    required: true
    help: Your name
  - name: release-date
    type: date
    label: Target Release Date
    help: When this feature should be released
---

# {name}

**Ticket:** {ticket-id}  
**Author:** {author}  
**Created:** {date}  
**Target Release:** {release-date}  
**Category:** {category}

## Summary

[Brief description of the feature]

## Requirements

### Functional
- [ ] Requirement 1
- [ ] Requirement 2

### Technical
- [ ] Technical requirement 1
- [ ] Technical requirement 2

## Design

[Design notes and diagrams]

## Implementation

[Implementation details]

## Testing

- [ ] Unit tests
- [ ] Integration tests
- [ ] Manual testing

## Status

- [ ] Design approved
- [ ] Development started
- [ ] Code review
- [ ] Testing complete
- [ ] Documentation updated
- [ ] Released

## Notes

[Additional notes]
```

## Usage Flow

When a user creates a page from this template:

1. **Select Template**: User clicks "Use This Template" on the "Feature Documentation" card

2. **Fill Parameters**: User sees a form with the custom parameter fields:
   - Category: dropdown with options `core`, `ui`, `api`, `backend`, `infrastructure` (required)
   - Ticket ID: (empty, required)
   - Feature Name: (empty, required)
   - Author: (empty, required)
   - Target Release Date: (empty, optional)

3. **Enter Values**: User enters:
   - Category: `ui` (selected from dropdown)
   - Ticket ID: `JIRA-789`
   - Feature Name: `Dark Mode Toggle`
   - Author: `Jane Smith`
   - Target Release Date: `2024-06-15`

4. **Auto-Update**: As the user types, the page location and name update automatically:
   - **Category selected**: `ui` (enum values are used as-is)
   - **Location updates**: `Features/ui`
   - **Ticket + Name**: `JIRA-789-Dark-Mode-Toggle`
   - **Full path shown**: `Features/ui/JIRA-789-Dark-Mode-Toggle`

5. **Create Page**: User clicks "Create Page"

6. **Result**: New page created at `Features/ui/JIRA-789-Dark-Mode-Toggle` with content:

```markdown
# Dark Mode Toggle

**Ticket:** JIRA-789  
**Author:** Jane Smith  
**Created:** 2024-01-15  
**Target Release:** 2024-06-15  
**Category:** ui

## Summary
...
```

## Sanitization

All parameter values are automatically sanitized for use in page names:

- **Spaces** → hyphens: `Dark Mode` → `Dark-Mode`
- **Special chars** removed: `UI & Components` → `UI-Components`
- **Accents** normalized: `Café` → `Cafe`
- **Multiple hyphens** collapsed: `UI---Mode` → `UI-Mode`
- **Invalid chars** removed: `UI@Mode!` → `UIMode`

This ensures all generated page names are valid and safe.
