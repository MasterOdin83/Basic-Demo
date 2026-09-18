// QA/production API origins — scheme included, or HttpClient treats them as relative paths.
export const environment = {
  production: true,
  // The STS is TurboEmpresa's API (QA): one STS for every site (Héctor, 2026-09-17). Its /api/auth issues the
  // JWTs that Basic.API validates (same Jwt__Key / Issuer / Audience in both App Services).
  stsUrl: 'https://qa-turboempresa-api-c4ejaqfngrbhf2cu.centralus-01.azurewebsites.net',
  // The demo's tasks API lives in SpartanIT.API since 2026-09-17 (Security Demo is part of Spartan IT; qa-demo-api is gone).
  apiUrl: 'https://qa-spartanit-api-g4eah5e3d6auesbn.westus3-01.azurewebsites.net',
  spartanItAboutUrl: 'https://proud-coast-051f45010.7.azurestaticapps.net/en/about',
  // reCAPTCHA Enterprise site key of TurboEmpresa (the STS verifies login/register tokens against it).
  // Héctor: add this SWA hostname (and the real domain later) to that key's allowed domains in Google Cloud.
  recaptchaSiteKey: '6LcyD1YtAAAAAAzNAvNQYKWRu6mHKbtYPQfEGYbx',
};
