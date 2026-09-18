// QA/production API origins — scheme included, or HttpClient treats them as relative paths.
export const environment = {
  production: true,
  // The STS is TurboEmpresa's API (QA): one STS for every site (Héctor, 2026-09-17). Its /api/auth issues the
  // JWTs that Basic.API validates (same Jwt__Key / Issuer / Audience in both App Services).
  stsUrl: 'https://qa-turboempresa-api-c4ejaqfngrbhf2cu.centralus-01.azurewebsites.net',
  apiUrl: 'https://qa-demo-api-a3dhdwf0aqdbcnck.centralus-01.azurewebsites.net',
  spartanItAboutUrl: 'https://proud-coast-051f45010.7.azurestaticapps.net/en/about',
  // Cloudflare Turnstile TEST site key (always passes, visible widget). Replace with the real
  // site key of the widget Héctor creates in dash.cloudflare.com → Turnstile; its secret goes
  // to the STS as the Captcha__Secret App Setting.
  turnstileSiteKey: '1x00000000000000000000AA',
};
