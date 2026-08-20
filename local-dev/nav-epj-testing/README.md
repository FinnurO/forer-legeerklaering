# nav-epj-testing

Verktøy for å sette opp og teste [`navikt/nav-epj`](https://github.com/navikt/nav-epj) lokalt som et Norway-tilpasset SMART on FHIR-testmiljø (korrekte fnr/HPR-OID-er, i motsetning til launch.smarthealthit.org). Full forklaring: [docs/NAV-EPJ-TESTMILJO.md](../../docs/NAV-EPJ-TESTMILJO.md).

- `nav-epj-local-fixes.patch` — git-patch med seks rettelser som er nødvendige for at `nav-epj` sin SMART-launch-flyt skal fungere i det hele tatt (den er per 2026-08-20 helt ikke-funksjonell uten disse, for alle klienter). Anvend med `git apply` fra roten av en `nav-epj`-klone.
- `seed-and-get-launch-url.ps1` — oppretter en testpasient + konsultasjon + diagnose via `nav-epj` sitt REST-API, og skriver ut en ferdig launch-URL som kan limes inn i en nettleser for å teste hele flyten manuelt.

Ingen av disse er sendt til NAV — se §5 i testguiden for anbefaling om det.
