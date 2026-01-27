# Page-Level Access Control

The wiki supports optional page-level access control that allows fine-grained permissions management based on file patterns and user groups.

## Enabling Page-Level Permissions

Page-level permissions are enabled by default. To disable them, set:

```csharp
services.AddWiki(options =>
{
    options.UsePageLevelPermissions = false;
    // ... other options
});
```

## Access Control Rules File

Access rules are stored in a `.wikipermissions` file at the root of the wiki repository. This file is managed through Git like all other wiki content.

### File Format

The file uses a simple pipe-delimited format:

```
# Comment lines start with #
Pattern | ReadGroups | WriteGroups
```

- **Pattern**: A glob pattern matching page names
  - `*` matches any characters except `/` (single path segment)
  - `**` matches any characters including `/` (multiple path segments)
- **ReadGroups**: Comma-separated list of groups that can read matching pages (empty = all users)
- **WriteGroups**: Comma-separated list of groups that can edit matching pages (empty = all users)

### Rule Evaluation

Rules are evaluated **in order from top to bottom**. The first rule that matches a page determines the access permissions.

### Examples

```
# Admin pages - only admin group
admin/** | admin | admin

# Private pages - authenticated users can read, editors can write
private/* | users, editors | editors

# Public documentation - everyone can read, users can write
docs/** | | users

# Default - everyone has full access
* | |
```

## User Groups

Groups are defined by your `IWikiUserService` implementation through the `IWikiUserWithPermissions.Groups` property.

Example implementation:

```csharp
public class MyWikiUserWithPermissions : IWikiUserWithPermissions
{
    public string[] Groups => user.IsAdmin ? ["admin", "users"] : ["users"];
    // ... other properties
}
```

## Admin Interface

Administrators (users with `CanAdmin = true`) can manage access rules through the wiki UI:

1. Navigate to **Site Map**
2. Click **Access Control** button
3. Click **Edit Rules** to modify the `.wikipermissions` file

## Performance

Access rules are cached in memory for 15 minutes expiration to ensure good performance. The cache is automatically cleared when rules are updated.

## Default Behavior

- If no `.wikipermissions` file exists, all users have full access to all pages
- If page-level permissions are disabled, all users have full access regardless of rules
- If no rule matches a page, the page is accessible to all users
