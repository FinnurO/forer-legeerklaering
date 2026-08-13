# smarthealthit-testing

Verktøy for å teste appens SMART EHR Launch-flyt mot [launch.smarthealthit.org](https://launch.smarthealthit.org/), en offentlig ekstern SMART on FHIR-testserver. Full forklaring: [docs/TESTGUIDE-SMARTHEALTHIT.md](../../docs/TESTGUIDE-SMARTHEALTHIT.md).

- `generate-client-assertion-jwk.ps1` — genererer et RSA-nøkkelpar for `private_key_jwt`-testing. Privat nøkkel skrives kun til en lokal fil utenfor repoet.
- `test-smart-launch.ps1` — kjører hele launch → authorize → callback-kjeden med `curl`, uten interaktiv nettleser. Støtter Public/Confidential Symmetric/Confidential Asymmetric klienttyper og launcherens "Simulated Error"-scenarioer.

Ingen av disse er automatiske CI-tester — se §1 i testguiden for hvorfor, og hva som eventuelt mangler for å få det til.
