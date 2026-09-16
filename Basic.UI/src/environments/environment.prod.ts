// QA/production API origins — scheme included, or HttpClient treats them as relative paths.
export const environment = {
  production: true,
  // STS de QA hoy: qa-demo-sts. El STS compartido nuevo es
  // https://qa-mercenaries-sts-gcexaggxdme7gffs.westus3-01.azurewebsites.net (workflow
  // master_qa-mercenaries-sts.yml): cambiar aquí cuando su primer deploy esté en verde.
  stsUrl: 'https://qa-demo-sts-h3dxfshgapatdsdf.centralus-01.azurewebsites.net',
  apiUrl: 'https://qa-demo-api-a3dhdwf0aqdbcnck.centralus-01.azurewebsites.net',
  spartanItAboutUrl: 'https://proud-coast-051f45010.7.azurestaticapps.net/en/about',
  // Cloudflare Turnstile TEST site key (always passes, visible widget). Replace with the real
  // site key of the widget Héctor creates in dash.cloudflare.com → Turnstile; its secret goes
  // to the STS as the Captcha__Secret App Setting.
  turnstileSiteKey: '1x00000000000000000000AA',
};
