# Tredjepartslisenser for nettstedet (`site/`)

Selve koden og dokumentasjonen i repoet er egne verk. Følgende vendrede filer i `site/assets/`
er hentet fra andre og har egne lisenser:

## designsystemet.no

- **Filer:** `assets/css/vendor/designsystemet.css`, `assets/css/vendor/designsystemet-theme.css`
- **Kilde:** [`@digdir/designsystemet-css`](https://www.npmjs.com/package/@digdir/designsystemet-css) v1.21.0
- **Opphav:** Digitaliseringsdirektoratet (Digdir) — <https://designsystemet.no/>, <https://github.com/digdir/designsystemet>
- **Lisens:** MIT

Filene er kopiert uendret inn i repoet for at nettstedet ikke skal ha en kjøretidsavhengighet til en ekstern CDN.

## Inter (font)

- **Fil:** `assets/fonts/Inter-variable-latin.woff2`
- **Opphav:** Rasmus Andersson m.fl. — <https://rsms.me/inter/>
- **Lisens:** SIL Open Font License 1.1

## Source Serif 4 (font)

- **Fil:** `assets/fonts/SourceSerif4-variable-latin.woff2`
- **Opphav:** Adobe — <https://github.com/adobe-fonts/source-serif>
- **Lisens:** SIL Open Font License 1.1

Begge fontfilene er variabel-fonter (latin-subsett, dekker æ/ø/å) og selv-hostet, se `assets/css/fonts.css`.

## Diagrammer

`assets/img/*.svg` er kopier av `docs/arkitektur-oversikt.svg`, `docs/smart-launch-sekvens.svg` og `docs/nettverksruting.svg` i dette repoet.
