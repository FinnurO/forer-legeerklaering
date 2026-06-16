/**
 * SMART on FHIR Auth Mock Server
 *
 * Kjorer pa port 9090 og simulerer en EPJ sin SMART-autorisasjonsserver.
 * Proxyer alle FHIR-kall videre til HAPI FHIR pa port 8080.
 *
 * Bruk: node server.js
 * SMART launch URL:
 *   http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch
 *     ?iss=http://localhost:9090
 *     &launch=enc-sophie-001
 */

const express = require("express");
const { createProxyMiddleware } = require("http-proxy-middleware");
const crypto = require("crypto");
const path = require("path");

const app = express();
app.use(express.urlencoded({ extended: true }));
app.use(express.json());

const HAPI_FHIR = "http://localhost:8080";
const MOCK_PORT = 9090;
// HOST_IP er adressen som Altinn-appen (på Windows) bruker for å nå mock og HAPI FHIR
const HOST_IP = process.env.HOST_IP || "172.30.80.1";
const MOCK_BASE = `http://localhost:${MOCK_PORT}`;

// Hardkodede testkontekster: launch-token -> FHIR-ressurser
// fhirUser peker til HOST_IP slik at Altinn-appen kan hente ressursen direkte
const LAUNCH_CONTEXTS = {
  "enc-sophie-001": {
    patient: "sophie-salt",
    encounter: "enc-sophie-001",
    fhirUser: `http://${HOST_IP}:8080/fhir/Practitioner/lege-ola`,
  },
};

// Midlertidig lagring av auth-koder
const authCodes = new Map();

// -----------------------------------------------------------------------
// EPJ Simulator — demo-grensesnitt for å starte SMART EHR Launch
// -----------------------------------------------------------------------
app.get(["/", "/epj"], (req, res) => {
  res.sendFile(path.join(__dirname, "epj-simulator.html"));
});

// -----------------------------------------------------------------------
// SMART metadata discovery
// -----------------------------------------------------------------------
app.get("/.well-known/smart-configuration", (req, res) => {
  res.json({
    issuer: MOCK_BASE,
    authorization_endpoint: `${MOCK_BASE}/auth`,
    token_endpoint: `${MOCK_BASE}/token`,
    capabilities: [
      "launch-ehr",
      "client-confidential-symmetric",
      "client-public",
      "permission-patient",
      "permission-user",
      "permission-offline",
      "context-ehr-patient",
      "context-ehr-encounter",
    ],
    scopes_supported: [
      "openid", "profile", "fhirUser", "launch", "launch/patient",
      "launch/encounter", "offline_access",
      "patient/Patient.read", "patient/Encounter.read",
      "patient/Condition.read", "patient/Observation.read",
      "user/Practitioner.read", "user/Organization.read", "user/PractitionerRole.read",
    ],
    response_types_supported: ["code"],
    code_challenge_methods_supported: ["S256"],
  });
});

// -----------------------------------------------------------------------
// Authorization endpoint — simulerer EPJ sin innloggingsside
// Returnerer kode umiddelbart (ingen brukerinteraksjon nodvendig i mock)
// -----------------------------------------------------------------------
app.get("/auth", (req, res) => {
  const { redirect_uri, state, launch, code_challenge, code_challenge_method } = req.query;

  if (!redirect_uri) return res.status(400).send("Missing redirect_uri");

  const code = crypto.randomBytes(16).toString("hex");
  authCodes.set(code, {
    launch: launch || "enc-sophie-001",
    redirect_uri,
    code_challenge,
    code_challenge_method,
    created: Date.now(),
  });

  // Rydd opp koder eldre enn 10 min
  for (const [k, v] of authCodes) {
    if (Date.now() - v.created > 600_000) authCodes.delete(k);
  }

  const url = new URL(redirect_uri);
  url.searchParams.set("code", code);
  if (state) url.searchParams.set("state", state);

  console.log(`[auth] Utsteder kode for launch=${launch}, redirect til ${redirect_uri}`);
  res.redirect(url.toString());
});

// -----------------------------------------------------------------------
// Token endpoint — bytter kode mot access token med SMART-claims
// -----------------------------------------------------------------------
app.post("/token", (req, res) => {
  const { code, grant_type, redirect_uri, code_verifier } = req.body;

  if (grant_type !== "authorization_code") {
    return res.status(400).json({ error: "unsupported_grant_type" });
  }

  const stored = authCodes.get(code);
  if (!stored) {
    return res.status(400).json({ error: "invalid_grant", error_description: "Ukjent eller utlopt kode" });
  }

  authCodes.delete(code);

  const ctx = LAUNCH_CONTEXTS[stored.launch] || LAUNCH_CONTEXTS["enc-sophie-001"];

  const tokenResponse = {
    access_token: `mock-token-${crypto.randomBytes(8).toString("hex")}`,
    token_type: "Bearer",
    expires_in: 3600,
    scope: "openid profile fhirUser launch patient/Patient.read patient/Encounter.read patient/Condition.read user/Practitioner.read user/Organization.read",
    // SMART launch context
    patient: ctx.patient,
    encounter: ctx.encounter,
    fhirUser: ctx.fhirUser,
  };

  console.log(`[token] Utsteder token: patient=${ctx.patient}, encounter=${ctx.encounter}`);
  res.json(tokenResponse);
});

// -----------------------------------------------------------------------
// Proxy alle /fhir/* kall til HAPI FHIR
// -----------------------------------------------------------------------
app.use(
  "/fhir",
  createProxyMiddleware({
    target: HAPI_FHIR,
    changeOrigin: true,
    on: {
      error: (err, req, res) => {
        console.error("[proxy] FHIR-feil:", err.message);
        res.status(502).json({ error: "HAPI FHIR ikke tilgjengelig pa port 8080" });
      },
    },
  })
);

app.listen(MOCK_PORT, () => {
  console.log(`
SMART Auth Mock kjorer pa ${MOCK_BASE}

EPJ Simulator (anbefalt startpunkt):
  ${MOCK_BASE}/epj

Endepunkter:
  GET  ${MOCK_BASE}/.well-known/smart-configuration
  GET  ${MOCK_BASE}/auth
  POST ${MOCK_BASE}/token
  *    ${MOCK_BASE}/fhir/* -> HAPI FHIR (${HAPI_FHIR})

Manuell SMART launch URL:
  http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch?iss=http://localhost:9090&launch=enc-sophie-001
  `);
});
