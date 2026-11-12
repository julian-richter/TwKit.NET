# TwKit.net

Add `TwKit.net` to the project that compiles Blazor WebAssembly:

```xml
<PackageReference Include="TwKit.net" Version="0.1.1" />
```

Add `Styles/app.css` in that same project:

```css
@import "tailwindcss";
```

For a standalone WebAssembly app, link the generated stylesheet from `wwwroot/index.html`:

```html
<link rel="stylesheet" href="twkit.css" />
```

For a hosted Blazor Web App, put the package reference on the `.Client` project. The pages live in the server project, so Tailwind has to read that folder. In the client `Styles/app.css`, `@source` is the path from that file to the server project:

```css
@import "tailwindcss";
@source "../../YourApp";
```

`TwKitTailwindSources` goes in a `PropertyGroup` in the `.Client` `.csproj`. It is the same server project, this time relative to the client project file. TwKit watches that folder and rebuilds `twkit.css` when a page there changes. `YourApp` is the server project's folder name.

```xml
<PropertyGroup>
    <!-- Path from the .Client project to the server project -->
    <TwKitTailwindSources>..\YourApp</TwKitTailwindSources>
</PropertyGroup>
```

Link the stylesheet from the host `Components/App.razor`:

```html
<link rel="stylesheet" href="@Assets["twkit.css"]" />
```

Use Tailwind classes in your `.razor` files and build. The first build downloads the Tailwind CLI, then writes `wwwroot/twkit.css`.
