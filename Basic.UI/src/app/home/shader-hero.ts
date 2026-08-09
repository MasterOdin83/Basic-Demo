const VERTEX_SRC = `#version 300 es
precision highp float;
in vec4 position;
void main(){gl_Position=position;}`;

// Radar/circuit-scan ambient background: drifting slate haze, a faint circuit grid,
// and a rotating amber sweep from center — Security Demo's own palette, not ported
// content from any other project's shader.
const FRAGMENT_SRC = `#version 300 es
precision highp float;
out vec4 O;
uniform vec2 resolution;
uniform float time;
#define FC gl_FragCoord.xy
#define T time
#define R resolution
float rnd(vec2 p){p=fract(p*vec2(12.9898,78.233));p+=dot(p,p+34.56);return fract(p.x*p.y);}
float noise(vec2 p){
  vec2 i=floor(p),f=fract(p),u=f*f*(3.-2.*f);
  float a=rnd(i),b=rnd(i+vec2(1,0)),c=rnd(i+vec2(0,1)),d=rnd(i+1.);
  return mix(mix(a,b,u.x),mix(c,d,u.x),u.y);
}
float fbm(vec2 p){
  float t=.0,a=.5;
  for(int i=0;i<5;i++){t+=a*noise(p);p*=2.02;a*=.5;}
  return t;
}
void main(void){
  vec2 uv=(FC-.5*R)/min(R.x,R.y);
  vec3 amber=vec3(1.,.69,.13);
  vec3 slate=vec3(.39,.45,.55);

  float haze=fbm(uv*1.6+vec2(T*.03,-T*.02));
  vec3 col=vec3(.01)+slate*haze*.05;

  vec2 grid=abs(fract(uv*9.-T*.02)-.5);
  float lines=smoothstep(.02,.0,min(grid.x,grid.y));
  col+=lines*slate*.12;

  float ang=atan(uv.y,uv.x);
  float sweep=fract((ang-T*.4)/6.2831853);
  float beam=smoothstep(.12,.0,sweep)*smoothstep(0.,1.4,1.-length(uv)*1.1);
  col+=beam*amber*.9;

  float pulse=pow(fbm(uv*3.+T*.15),6.)*.8;
  col+=pulse*amber;

  col*=1.-.4*dot(uv,uv);
  // Alpha follows brightness: the near-black haze stays translucent so the CSS
  // grid behind the canvas shows through, only the lit shapes (beam/pulses/grid
  // lines) paint opaque.
  float a=clamp(dot(col,vec3(.4,.4,.4))*2.4,0.,1.);
  O=vec4(col,a);
}`;

const VERTICES = new Float32Array([-1, 1, -1, -1, 1, 1, 1, -1]);
const MOTION_QUERY = '(prefers-reduced-motion: reduce)';

function compile(gl: WebGL2RenderingContext, type: number, source: string): WebGLShader {
  const shader = gl.createShader(type)!;
  gl.shaderSource(shader, source);
  gl.compileShader(shader);
  if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
    console.error('shader-hero compile error:', gl.getShaderInfoLog(shader));
  }
  return shader;
}

// Full-bleed animated shader background for the splash hero. No-ops (canvas stays
// transparent, content overlay unaffected) without WebGL2, and renders one static
// frame instead of looping under reduced motion — same guard style as
// reveal.ts/spotlight.ts/splash-parallax.ts.
export function observeShaderHero(canvas: HTMLCanvasElement | null): () => void {
  if (!canvas) return () => {};
  const gl = canvas.getContext('webgl2', { alpha: true, premultipliedAlpha: false });
  if (!gl) return () => {};

  gl.enable(gl.BLEND);
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
  gl.clearColor(0, 0, 0, 0);

  const vs = compile(gl, gl.VERTEX_SHADER, VERTEX_SRC);
  const fs = compile(gl, gl.FRAGMENT_SHADER, FRAGMENT_SRC);
  const program = gl.createProgram()!;
  gl.attachShader(program, vs);
  gl.attachShader(program, fs);
  gl.linkProgram(program);
  if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
    console.error('shader-hero link error:', gl.getProgramInfoLog(program));
    return () => {};
  }

  gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
  gl.bufferData(gl.ARRAY_BUFFER, VERTICES, gl.STATIC_DRAW);
  const positionLoc = gl.getAttribLocation(program, 'position');
  gl.enableVertexAttribArray(positionLoc);
  gl.vertexAttribPointer(positionLoc, 2, gl.FLOAT, false, 0, 0);
  gl.useProgram(program);

  const resolutionLoc = gl.getUniformLocation(program, 'resolution');
  const timeLoc = gl.getUniformLocation(program, 'time');
  const dpr = Math.max(1, 0.5 * window.devicePixelRatio);

  const resize = () => {
    canvas.width = canvas.clientWidth * dpr;
    canvas.height = canvas.clientHeight * dpr;
    gl.viewport(0, 0, canvas.width, canvas.height);
  };
  resize();
  window.addEventListener('resize', resize, { passive: true });

  const render = (now: number) => {
    // Blending is on (for the translucent haze), so the framebuffer must be reset
    // each frame — otherwise every frame blends onto the last one instead of
    // replacing it, and the trail never clears.
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.uniform2f(resolutionLoc, canvas.width, canvas.height);
    gl.uniform1f(timeLoc, now * 1e-3);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  };

  if (matchMedia(MOTION_QUERY).matches) {
    render(0);
    return () => window.removeEventListener('resize', resize);
  }

  let frameId = requestAnimationFrame(function loop(now) {
    render(now);
    frameId = requestAnimationFrame(loop);
  });

  return () => {
    window.removeEventListener('resize', resize);
    cancelAnimationFrame(frameId);
  };
}
