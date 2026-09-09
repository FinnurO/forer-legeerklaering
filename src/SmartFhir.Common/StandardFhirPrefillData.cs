namespace SmartFhir.Common
{
    /// <summary>
    /// Standard sett med FHIR-utledede felter som går igjen i (nesten) alle henvisnings-/
    /// erklæringsskjemaer uavhengig av hvilken helsepersonellgruppe som er henviser/utsteder:
    /// pasient (Patient), henviser (Practitioner, evt. via PractitionerRole), virksomhet
    /// (Organization) og konsultasjon (Encounter). Diagnose (Condition) er tatt med siden den
    /// også er svært vanlig, men er ikke relevant for alle caser (f.eks. kjeveortopedisk-caset,
    /// der den kliniske vurderingen er en Helfo-spesifikk bittanomali-klassifisering, ikke en
    /// ICD-10-diagnosekode — se docs/SKJEMA-KJEVEORTOPEDISK.md).
    ///
    /// "Henviser" er brukt som feltprefiks (ikke "Lege") fordi HPR-nummeret (NorwegianHealthOids.Hpr)
    /// dekker all autorisert helsepersonell, ikke bare leger — bekreftet på tvers av
    /// ForerLegeerklaering (lege) og AppKjeveortopedisk (tannlege/tannpleier).
    ///
    /// Hver apps egen FhirPrefillService mapper disse feltene til sin egen datamodells
    /// feltnavn (f.eks. Henviser_Hpr -> Lege_HPR for ForerLegeerklaeringModel).
    /// </summary>
    public class StandardFhirPrefillData
    {
        // --- Pasient (FHIR Patient) ---
        public string Pasient_Fnr { get; set; }
        public string Pasient_Fornavn { get; set; }
        public string Pasient_Etternavn { get; set; }
        public string Pasient_Fodselsdato { get; set; }
        public string Pasient_Kjonn { get; set; }

        /// <summary>
        /// Første oppføring i Patient.address, formatert som én tekststreng (linje(r), postnr,
        /// poststed). Lagt til 2026-09-09 for kjeveortopedisk-caset, som (i motsetning til
        /// ForerLegeerklaering) trenger pasientens adresse — men feltet er generisk nok til å
        /// gjenbrukes av senere caser også.
        /// </summary>
        public string Pasient_Adresse { get; set; }

        // --- Henviser (FHIR Practitioner, via fhirUser) ---
        public string Henviser_Hpr { get; set; }
        public string Henviser_Fornavn { get; set; }
        public string Henviser_Etternavn { get; set; }

        // --- Virksomhet (FHIR Organization, via PractitionerRole eller Encounter.serviceProvider) ---
        public string Virksomhet_Navn { get; set; }
        public string Virksomhet_Orgnr { get; set; }
        public string Virksomhet_HerId { get; set; }

        // --- Konsultasjon (FHIR Encounter) ---
        public string Konsultasjon_Dato { get; set; }

        // --- Diagnose (FHIR Condition) — ikke relevant for alle caser, se klassekommentar ---
        public string Diagnose_Kode { get; set; }
        public string Diagnose_Tekst { get; set; }
    }
}
