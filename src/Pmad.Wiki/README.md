# Pmad.Wiki

`Pmad.Wiki` is an ASP.NET Core library that lets host a git/markdown based wiki into an MVC application.

Features:
- Git based wiki storage (powered by Pmad.Git)
- Markdown rendering (powered by Markdig)
- Diagram/sketch support (powered by Excalidraw)
- Slide show support (powered by Reveal.js)
- Bootstrap 5 based UI
- Easy integration into existing ASP.NET Core MVC applications
- Customizable layout support - use your own application's layout
- Page localization support
- Media file support (images, videos, PDFs, and other files from the Git repository)
- Optional page-level access control with group-based permissions
- In-memory caching for optimal performance
- Version history with visual diff comparison (powered by Mergely)

## Configuration

### Media Files

The wiki supports serving media files (images, videos, PDFs, etc.) directly from the Git repository. Media files can be referenced in your markdown pages using standard markdown syntax:

```markdown
![Image description](images/logo.png)
![Relative path](../shared/banner.jpg)
[Download PDF](documents/guide.pdf)
```

Supported media file types by default:
- **Images**: `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.webp`
- **Videos**: `.mp4`, `.webm`, `.ogg`
- **Documents**: `.pdf`

You can change allowed extensions by modifying the `AllowedMediaExtensions` property in `WikiOptions`:

```csharp
builder.Services.AddControllersWithViews()
    .AddWiki(options =>
    {
        options.AllowedMediaExtensions = new () { ... };
    });
```

Media files are subject to the same access control rules as wiki pages. When page-level permissions are enabled, media files are tested against access rules with their full path.

The media files are served through the `/wiki/media/{path}` route and are automatically linked when you use relative paths in your markdown.

### Custom Layout

You can specify a custom layout for wiki pages by setting the `Layout` property in `WikiOptions`:

```csharp
builder.Services.AddControllersWithViews()
    .AddWiki(options =>
    {
        options.RepositoryRoot = "/path/to/repositories";
        options.WikiRepositoryName = "wiki";
        options.Layout = "_WikiLayout"; // Use your custom layout
        // ... other options
    });
```

If not set, wiki pages will use the default layout defined in your application's `_ViewStart.cshtml`.

## Third-Party Libraries

This project uses the following third-party libraries:

- **Markdig** - Markdown processor for .NET
- **Bootstrap 5** - Front-end framework
- **Bootstrap Icons** - Icon library
- **Reveal.js** - Slide show presentation framework
- **Excalidraw** - Virtual whiteboard / diagram editor (MIT License)
- **React** - UI runtime bundled with Excalidraw (MIT License)
- **Mergely** - Text diff and merge library (Mozilla Public License Version 1.1)
  - Copyright © Jamie Peabody
  - Used for displaying side-by-side diff comparisons
  - See `wwwroot/lib/mergely/LICENSE` for full license text

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for complete licensing information.