# Tailwind CSS Build Notes

## Input file
Ensure `wwwroot/css/input.css` contains:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

## Build command
Run Tailwind during development or watch mode:

```bash
npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/app.min.css --watch
```

## Include compiled CSS
Reference the generated bundle in `_Host.cshtml` (Blazor Server) or `wwwroot/index.html` (Blazor WASM):

```html
<link rel="stylesheet" href="app.min.css" />
```
