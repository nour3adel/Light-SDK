# Light SDK Website

This folder contains the modern static landing page for Light SDK.

## Local preview

Open `website/index.html` directly in a browser, or serve it with any static server.

## GitHub Pages deployment

The repository includes `.github/workflows/deploy-website.yml`.

1. Push changes to `main`.
2. In GitHub repository settings, set Pages source to **GitHub Actions**.
3. The workflow deploys the `website` folder automatically.

## Customize content

- Main page markup: `website/index.html`
- Styling and theme: `website/styles.css`
- Interactions (before/after slider + reveal effects): `website/script.js`
- Demo comparison images: `website/assets/`
