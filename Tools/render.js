// Renders all static art for Barvaz The Dealer to PNG with headless Chromium.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = path.join(__dirname, 'out');
fs.mkdirSync(OUT, { recursive: true });
const DIR = __dirname;

const b64 = (f) => fs.readFileSync(path.join(DIR, f)).toString('base64');
const FONTS = `
@font-face { font-family: Secular; src: url(data:font/ttf;base64,${b64('SecularOne-Regular.ttf')}); }
@font-face { font-family: RubikB; src: url(data:font/ttf;base64,${b64('Rubik-Bold.ttf')}); }
@font-face { font-family: RubikM; src: url(data:font/ttf;base64,${b64('Rubik-Medium.ttf')}); }
* { margin:0; padding:0; box-sizing:border-box; }
html,body { background: transparent; }
.emoji { font-family: 'Noto Color Emoji'; }
.suit { font-family: 'DejaVu Sans'; }
`;

const RED = '#c8102e', BLACK = '#15151a';
const GOLD = '#e8c66a';

// ---------------------------------------------------------------- room
function room() {
  // bokeh dots
  let bokeh = '';
  let seed = 7;
  const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const cols = ['255,200,120', '255,150,80', '230,70,70', '120,160,255', '255,230,180'];
  for (let i = 0; i < 70; i++) {
    const x = rnd() * 1920, y = 330 + rnd() * 380, s = 14 + rnd() * 70, c = cols[Math.floor(rnd() * cols.length)];
    const a = 0.08 + rnd() * 0.3;
    bokeh += `<div style="position:absolute;left:${x - s / 2}px;top:${y - s / 2}px;width:${s}px;height:${s}px;border-radius:50%;
      background:radial-gradient(circle, rgba(${c},${a}) 0%, rgba(${c},${a * 0.7}) 55%, rgba(${c},0) 72%);"></div>`;
  }
  // wall panels
  let panels = '';
  for (let i = 0; i < 8; i++) {
    const x = 20 + i * 240;
    panels += `<div style="position:absolute;left:${x}px;top:40px;width:200px;height:300px;border-radius:6px;
      border:3px solid rgba(232,198,106,0.28); box-shadow: inset 0 0 0 8px rgba(0,0,0,0.25), inset 0 0 40px rgba(0,0,0,0.6), 0 0 0 1px rgba(0,0,0,0.6);
      background: repeating-radial-gradient(circle at 50% 50%, rgba(255,255,255,0.025) 0 6px, rgba(0,0,0,0.02) 6px 14px),
                  linear-gradient(180deg, rgba(120,30,40,0.35), rgba(40,8,14,0.4));"></div>`;
  }
  const sconce = (x, y) => `
    <div style="position:absolute;left:${x - 260}px;top:${y - 230}px;width:520px;height:520px;border-radius:50%;
      background:radial-gradient(circle, rgba(255,210,140,0.45) 0%, rgba(255,170,90,0.18) 30%, rgba(255,150,80,0) 65%);"></div>
    <div style="position:absolute;left:${x - 8}px;top:${y + 40}px;width:16px;height:70px;background:linear-gradient(90deg,#6b5320,#f3d98a,#6b5320);border-radius:4px;"></div>
    <div style="position:absolute;left:${x - 30}px;top:${y + 100}px;width:60px;height:16px;background:linear-gradient(90deg,#6b5320,#f3d98a,#6b5320);border-radius:8px;"></div>
    <div style="position:absolute;left:${x - 44}px;top:${y - 40}px;width:88px;height:84px;
      clip-path: polygon(18% 0, 82% 0, 100% 100%, 0 100%);
      background:linear-gradient(180deg, #fff6dc 0%, #ffd58a 50%, #e89d43 100%); box-shadow:0 0 30px #ffcf7a;"></div>
    <div style="position:absolute;left:${x - 46}px;top:${y + 40}px;width:92px;height:8px;background:linear-gradient(90deg,#6b5320,#f3d98a,#6b5320);border-radius:4px;"></div>`;
  return `<div style="position:relative;width:1920px;height:1080px;overflow:hidden;
      background: radial-gradient(ellipse 70% 60% at 50% 25%, #5a1f24 0%, #33121a 40%, #1a090d 75%, #0b0406 100%);">
    ${panels}
    <!-- crown molding -->
    <div style="position:absolute;left:0;top:0;width:1920px;height:26px;background:linear-gradient(180deg,#140608,#3b2410 60%,#8a6a2a 85%,#2a1606);"></div>
    <!-- chair rail -->
    <div style="position:absolute;left:0;top:356px;width:1920px;height:14px;background:linear-gradient(180deg,#2a1606,#c9a453 45%,#5c3f14 100%);box-shadow:0 4px 12px rgba(0,0,0,0.6);"></div>
    <!-- lower wall: dark wood -->
    <div style="position:absolute;left:0;top:370px;width:1920px;height:710px;
      background: repeating-linear-gradient(90deg, rgba(0,0,0,0.18) 0 2px, transparent 2px 120px),
                  linear-gradient(180deg,#2b150c 0%, #160a06 60%, #0b0503 100%);"></div>
    ${bokeh}
    ${sconce(470, 250)}
    ${sconce(1450, 250)}
    <!-- neon sign -->
    <div style="position:absolute;left:60px;top:70px;width:420px;height:120px;border-radius:18px;
         background:rgba(10,4,6,0.55); border:2px solid rgba(255,90,140,0.35); box-shadow:0 0 40px rgba(255,60,120,0.25), inset 0 0 30px rgba(255,60,120,0.15);"></div>
    <div dir="rtl" style="position:absolute;left:60px;top:78px;width:420px;text-align:center;font-family:Secular;font-size:66px;color:#ffe3f0;
         text-shadow:0 0 6px #ff4f9a,0 0 16px #ff2f86,0 0 36px #ff2f86,0 0 70px #ff1f70;">קזינו ברווז</div>
    <div class="emoji" style="position:absolute;left:30px;top:40px;font-size:64px;transform:rotate(-14deg);filter:drop-shadow(0 0 12px rgba(255,220,120,0.7));">🦆</div>
    <!-- overhead spotlight -->
    <div style="position:absolute;left:360px;top:-200px;width:1200px;height:1100px;border-radius:50%;
         background:radial-gradient(ellipse at 50% 40%, rgba(255,235,200,0.14) 0%, rgba(255,220,170,0.06) 40%, rgba(255,220,170,0) 70%);"></div>
  </div>`;
}

// ---------------------------------------------------------------- table (transparent PNG)
function table() {
  const cx = 960, cy = 1000;
  const ell = (rx, ry, style) => `<div style="position:absolute;left:${cx - rx}px;top:${cy - ry}px;width:${rx * 2}px;height:${ry * 2}px;border-radius:50%;${style}"></div>`;
  // card guides
  let guides = '';
  for (let i = 0; i < 5; i++) {
    const x = cx + (i - 2) * 165, y = 860;
    guides += `<div style="position:absolute;left:${x - 72}px;top:${y - 99}px;width:144px;height:198px;border-radius:14px;border:2px solid rgba(232,198,106,0.22);"></div>`;
    const dx = cx + (i - 2) * 124, dy = 590;
    guides += `<div style="position:absolute;left:${dx - 58}px;top:${dy - 79}px;width:116px;height:158px;border-radius:12px;border:2px solid rgba(232,198,106,0.16);"></div>`;
  }
  const betSpot = (x, y, label, amount) => `
    <div style="position:absolute;left:${x - 78}px;top:${y - 78}px;width:156px;height:156px;border-radius:50%;
      border:4px solid rgba(240,210,130,0.85); box-shadow: inset 0 0 0 7px rgba(0,0,0,0.12), inset 0 0 0 9px rgba(240,210,130,0.35), 0 0 18px rgba(0,0,0,0.25);
      background: radial-gradient(circle, rgba(0,0,0,0.12), rgba(0,0,0,0.22));"></div>
    <div dir="rtl" style="position:absolute;left:${x - 120}px;top:${y + 86}px;width:240px;text-align:center;font-family:RubikB;font-size:28px;color:rgba(255,236,190,0.85);text-shadow:0 2px 3px rgba(0,0,0,0.5);">${label} <span dir="ltr">${amount}</span></div>`;
  // chip tray
  let tray = '';
  const trayCols = ['#c4161c', '#1f4fb5', '#178a3e', '#1b1b1f', '#6b2aa0', '#e0b030'];
  for (let i = 0; i < 12; i++) {
    const c = trayCols[Math.floor(i / 2)];
    tray += `<div style="position:absolute;left:${16 + i * 38}px;top:10px;width:32px;height:48px;border-radius:5px;
      background: repeating-linear-gradient(180deg, ${c} 0 5px, rgba(255,255,255,0.85) 5px 7px, ${c} 7px 12px),
                  ${c}; box-shadow: inset 0 0 0 1px rgba(0,0,0,0.4), inset 3px 0 4px rgba(255,255,255,0.25), inset -3px 0 4px rgba(0,0,0,0.35);"></div>`;
  }
  return `<div style="position:relative;width:1920px;height:1080px;overflow:hidden;">
    ${ell(990, 600, 'background:radial-gradient(ellipse, rgba(0,0,0,0.75) 60%, rgba(0,0,0,0) 72%);')}
    ${ell(952, 562, `background: radial-gradient(ellipse at 50% 35%, #3a2a24 0%, #1b120f 55%, #0b0706 100%);
        box-shadow: inset 0 18px 22px rgba(255,255,255,0.10), inset 0 -10px 30px rgba(0,0,0,0.9), 0 -4px 10px rgba(0,0,0,0.6);`)}
    ${ell(936, 546, 'border:2px dashed rgba(255,230,190,0.18);')}
    ${ell(906, 516, 'background:linear-gradient(180deg,#f4d98c 0%, #a87b2c 40%, #5b3d10 100%);')}
    ${ell(898, 508, `background:
        radial-gradient(ellipse 60% 60% at 50% 30%, rgba(255,255,255,0.10), rgba(255,255,255,0) 70%),
        radial-gradient(ellipse at 50% 20%, #18784a 0%, #0f5c37 45%, #083a22 100%);
        box-shadow: inset 0 14px 30px rgba(0,0,0,0.55);`)}
    <svg width="1920" height="1080" style="position:absolute;left:0;top:0;mix-blend-mode:overlay;opacity:0.35">
      <filter id="n"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="saturate" values="0"/></filter>
      <ellipse cx="${cx}" cy="${cy}" rx="898" ry="508" filter="url(#n)"/>
    </svg>
    ${ell(800, 420, 'border:3px solid rgba(232,198,106,0.42);')}
    ${ell(788, 408, 'border:1px solid rgba(232,198,106,0.25);')}
    ${guides}
    <div dir="rtl" style="position:absolute;left:460px;top:672px;width:1000px;text-align:center;font-family:Secular;font-size:64px;color:rgba(240,210,130,0.32);letter-spacing:2px;">ברווז הדילר</div>

    ${betSpot(440, 840, 'אנטה', '$5')}
    ${betSpot(1480, 840, 'העלאה', '$10')}
    <!-- chip tray -->
    <div style="position:absolute;left:${cx - 245}px;top:438px;width:490px;height:68px;border-radius:10px;
       background:linear-gradient(180deg,#050506,#1c1c22); box-shadow: inset 0 6px 10px rgba(0,0,0,0.9), 0 2px 0 rgba(255,255,255,0.08), 0 6px 14px rgba(0,0,0,0.6);">${tray}</div>
    <!-- card shoe -->
    <div style="position:absolute;left:1335px;top:525px;width:150px;height:110px;border-radius:12px;transform:rotate(-8deg);
       background:linear-gradient(160deg,#7a1020,#3a0610); box-shadow: 0 10px 20px rgba(0,0,0,0.6), inset 0 2px 0 rgba(255,255,255,0.25);">
       <div style="position:absolute;left:14px;top:12px;width:122px;height:70px;border-radius:8px;background:linear-gradient(180deg,#f4efe4,#cfc6b4);box-shadow:inset 0 -6px 8px rgba(0,0,0,0.3);"></div>
       <div style="position:absolute;left:14px;top:88px;width:122px;height:10px;border-radius:5px;background:linear-gradient(90deg,#6b5320,#f3d98a,#6b5320);"></div>
    </div>
  </div>`;
}

// ---------------------------------------------------------------- cards
const PIP = {
  2: [[.5, .18], [.5, .82]],
  3: [[.5, .18], [.5, .5], [.5, .82]],
  4: [[.3, .18], [.7, .18], [.3, .82], [.7, .82]],
  5: [[.3, .18], [.7, .18], [.5, .5], [.3, .82], [.7, .82]],
  6: [[.3, .18], [.7, .18], [.3, .5], [.7, .5], [.3, .82], [.7, .82]],
  7: [[.3, .18], [.7, .18], [.5, .34], [.3, .5], [.7, .5], [.3, .82], [.7, .82]],
  8: [[.3, .18], [.7, .18], [.5, .34], [.3, .5], [.7, .5], [.5, .66], [.3, .82], [.7, .82]],
  9: [[.3, .18], [.7, .18], [.3, .39], [.7, .39], [.5, .5], [.3, .61], [.7, .61], [.3, .82], [.7, .82]],
  10: [[.3, .18], [.7, .18], [.5, .29], [.3, .39], [.7, .39], [.3, .61], [.7, .61], [.5, .71], [.3, .82], [.7, .82]],
};
const SUITS = { S: '♠', H: '♥', D: '♦', C: '♣' };
const W = 280, H = 392;
function cardFace(rank, suit) {
  const color = (suit === 'H' || suit === 'D') ? RED : BLACK;
  const sym = SUITS[suit];
  const label = { 14: 'A', 13: 'K', 12: 'Q', 11: 'J' }[rank] || String(rank);
  const corner = (rot) => `<div style="position:absolute;${rot ? 'right:12px;bottom:12px;transform:rotate(180deg);' : 'left:12px;top:12px;'}width:50px;text-align:center;color:${color};">
      <div style="font-family:Secular;font-size:${label === '10' ? 40 : 46}px;line-height:44px;letter-spacing:${label === '10' ? -3 : 0}px;">${label}</div>
      <div class="suit" style="font-size:34px;line-height:34px;">${sym}</div></div>`;
  let center = '';
  const area = { x: 58, y: 40, w: W - 116, h: H - 80 };
  if (rank <= 10) {
    for (const [px, py] of PIP[rank]) {
      const x = area.x + px * area.w, y = area.y + py * area.h;
      center += `<div class="suit" style="position:absolute;left:${x - 40}px;top:${y - 36}px;width:80px;text-align:center;font-size:60px;line-height:72px;color:${color};${py > .55 ? 'transform:rotate(180deg);' : ''}">${sym}</div>`;
    }
  } else if (rank === 14) {
    center = `<div class="suit" style="position:absolute;left:0;top:${H / 2 - 95}px;width:${W}px;text-align:center;font-size:170px;line-height:190px;color:${color};
      text-shadow: 0 3px 0 rgba(0,0,0,0.12);">${sym}</div>`;
    if (suit === 'S') center += `<div class="emoji" style="position:absolute;left:${W / 2 - 22}px;top:${H / 2 - 4}px;font-size:40px;">🦆</div>`;
  } else {
    const face = { 13: '🤴', 12: '👸', 11: '💂' }[rank];
    center = `<div style="position:absolute;left:52px;top:52px;width:${W - 104}px;height:${H - 104}px;border-radius:10px;border:3px solid ${color};
        background:linear-gradient(180deg, ${color === RED ? '#fde8e8' : '#e8ecf6'}, #fffaf0 50%, ${color === RED ? '#fde8e8' : '#e8ecf6'});
        box-shadow: inset 0 0 0 4px #fffaf0, inset 0 0 0 6px ${GOLD};">
        <div class="emoji" style="position:absolute;left:0;top:44px;width:100%;text-align:center;font-size:110px;">${face}</div>
        <div class="suit" style="position:absolute;left:12px;bottom:6px;font-size:40px;color:${color};">${sym}</div>
        <div class="suit" style="position:absolute;right:12px;top:6px;font-size:40px;color:${color};">${sym}</div>
        <div style="position:absolute;left:0;bottom:16px;width:100%;text-align:center;font-family:Secular;font-size:44px;color:${color};">${label}</div>
      </div>`;
  }
  return `<div style="position:relative;width:${W}px;height:${H}px;border-radius:22px;overflow:hidden;
      background: radial-gradient(ellipse at 30% 20%, #ffffff 0%, #fbf8f1 50%, #efe9dc 100%);
      box-shadow: inset 0 0 0 2px rgba(0,0,0,0.12), inset 0 0 0 7px #fffdf8, inset 0 0 0 8px rgba(0,0,0,0.06);">
      ${corner(false)}${corner(true)}${center}</div>`;
}
function cardBack() {
  return `<div style="position:relative;width:${W}px;height:${H}px;border-radius:22px;overflow:hidden;background:#fbf8f1;box-shadow: inset 0 0 0 2px rgba(0,0,0,0.15);">
    <div style="position:absolute;left:14px;top:14px;right:14px;bottom:14px;border-radius:14px;
       background:
         repeating-linear-gradient(45deg, rgba(255,215,140,0.16) 0 3px, transparent 3px 18px),
         repeating-linear-gradient(-45deg, rgba(255,215,140,0.16) 0 3px, transparent 3px 18px),
         radial-gradient(ellipse at 50% 40%, #9b1426 0%, #6a0b18 60%, #3e050d 100%);
       box-shadow: inset 0 0 0 4px ${GOLD}, inset 0 0 0 7px #4a0812, inset 0 0 0 9px rgba(232,198,106,0.6);"></div>
    <div style="position:absolute;left:${W / 2 - 70}px;top:${H / 2 - 70}px;width:140px;height:140px;border-radius:50%;
       background:radial-gradient(circle,#fff6dc,#e8c66a 60%,#9a7424); box-shadow:0 0 0 5px #4a0812, 0 0 0 8px ${GOLD}, 0 6px 14px rgba(0,0,0,0.5);"></div>
    <div class="emoji" style="position:absolute;left:0;top:${H / 2 - 50}px;width:100%;text-align:center;font-size:84px;line-height:100px;">🦆</div>
  </div>`;
}

// ---------------------------------------------------------------- chips
function chip(color, edge, value) {
  const S = 200;
  let inserts = '';
  for (let i = 0; i < 8; i++) {
    inserts += `<div style="position:absolute;left:${S / 2 - 12}px;top:2px;width:24px;height:30px;border-radius:4px;background:${edge};
       transform-origin:12px ${S / 2 - 2}px;transform:rotate(${i * 45}deg);"></div>`;
  }
  return `<div style="position:relative;width:${S}px;height:${S}px;">
    <div style="position:absolute;inset:0;border-radius:50%;background:radial-gradient(circle at 40% 35%, ${color} 0%, ${color} 60%, rgba(0,0,0,0.35) 100%), ${color};
        box-shadow: inset 0 0 0 3px rgba(0,0,0,0.25), inset 0 6px 8px rgba(255,255,255,0.25);"></div>
    ${inserts}
    <div style="position:absolute;left:34px;top:34px;width:132px;height:132px;border-radius:50%;border:4px dashed ${edge};opacity:0.9;"></div>
    <div style="position:absolute;left:44px;top:44px;width:112px;height:112px;border-radius:50%;
        background:radial-gradient(circle at 40% 35%, #ffffff, #efe6cf); box-shadow: inset 0 0 0 4px ${color}, inset 0 -4px 6px rgba(0,0,0,0.2);"></div>
    <div style="position:absolute;left:0;top:${S / 2 - 30}px;width:${S}px;text-align:center;font-family:RubikB;font-size:${value.length > 3 ? 42 : 50}px;line-height:60px;color:${color};">${value}</div>
    <div style="position:absolute;inset:0;border-radius:50%;background:linear-gradient(140deg, rgba(255,255,255,0.35), rgba(255,255,255,0) 45%);"></div>
  </div>`;
}

// ---------------------------------------------------------------- UI frames (9-slice sources)
function button(c1, c2, c3) {
  return `<div style="position:relative;width:160px;height:110px;padding:6px;">
    <div style="position:absolute;left:6px;top:10px;width:148px;height:94px;border-radius:30px;background:rgba(0,0,0,0.55);filter:blur(4px);"></div>
    <div style="position:absolute;left:6px;top:4px;width:148px;height:94px;border-radius:30px;background:linear-gradient(180deg,${c1} 0%, ${c2} 55%, ${c3} 100%);
      box-shadow: inset 0 2px 0 rgba(255,255,255,0.65), inset 0 -5px 0 rgba(0,0,0,0.30), 0 0 0 2px rgba(0,0,0,0.55), 0 0 0 4px rgba(232,198,106,0.75);"></div>
    <div style="position:absolute;left:14px;top:8px;width:132px;height:36px;border-radius:24px 24px 14px 14px;background:linear-gradient(180deg,rgba(255,255,255,0.55),rgba(255,255,255,0));"></div>
  </div>`;
}
function panel() {
  return `<div style="position:relative;width:128px;height:128px;padding:8px;">
    <div style="position:absolute;inset:8px;border-radius:22px;background:linear-gradient(180deg, rgba(28,16,18,0.94), rgba(10,6,8,0.94));
      box-shadow: inset 0 0 0 2px rgba(232,198,106,0.85), inset 0 0 0 5px rgba(0,0,0,0.6), inset 0 0 0 6px rgba(232,198,106,0.3), 0 6px 10px rgba(0,0,0,0.5);"></div>
  </div>`;
}
function bubble() {
  return `<div style="position:relative;width:128px;height:128px;padding:8px;">
    <div style="position:absolute;inset:8px;border-radius:30px;background:linear-gradient(180deg,#fffdf6,#f1e7d0);
      box-shadow: inset 0 0 0 3px #2a1a10, inset 0 -6px 0 rgba(0,0,0,0.08), 0 6px 10px rgba(0,0,0,0.45);"></div>
  </div>`;
}
function bubbleTail() {
  return `<svg width="70" height="70" viewBox="0 0 70 70"><path d="M70 10 L0 40 L70 55 Z" fill="#f8f1e2" stroke="#2a1a10" stroke-width="3" stroke-linejoin="round"/>
    <rect x="62" y="12" width="8" height="42" fill="#f8f1e2"/></svg>`;
}

function emoji(e, size) {
  return `<div class="emoji" style="width:${size}px;height:${size}px;font-size:${Math.round(size * 0.82)}px;line-height:${size}px;text-align:center;">${e}</div>`;
}

function paytable() {
  const rows = [['רויאל פלאש', '100'], ['סטרייט פלאש', '50'], ['רביעייה', '20'], ['פול האוס', '7'], ['פלאש', '5'], ['סטרייט', '4'], ['שלישייה', '3'], ['שני זוגות', '2'], ['זוג או פחות', '1']];
  let r = '';
  rows.forEach(([n, v], i) => {
    r += `<div dir="rtl" style="position:absolute;left:28px;top:${88 + i * 34}px;width:284px;height:34px;display:flex;justify-content:space-between;align-items:center;
      font-family:RubikM;font-size:22px;color:#f8ecd2;border-bottom:1px solid rgba(232,198,106,0.12);">
      <span>${n}</span><span dir="ltr" style="font-family:RubikB;color:${GOLD};">${v} : 1</span></div>`;
  });
  return `<div style="position:relative;width:340px;height:440px;">
    <div style="position:absolute;inset:6px;border-radius:20px;background:linear-gradient(180deg,rgba(26,14,16,0.95),rgba(8,5,6,0.95));
      box-shadow: inset 0 0 0 2px rgba(232,198,106,0.9), inset 0 0 0 6px rgba(0,0,0,0.6), inset 0 0 0 7px rgba(232,198,106,0.35), 0 10px 24px rgba(0,0,0,0.6);"></div>
    <div dir="rtl" style="position:absolute;left:0;top:22px;width:340px;text-align:center;font-family:Secular;font-size:36px;color:${GOLD};text-shadow:0 2px 0 #000;">ההעלאה משלמת</div>
    ${r}
    <div dir="rtl" style="position:absolute;left:0;top:404px;width:340px;text-align:center;font-family:RubikM;font-size:17px;color:rgba(255,255,255,0.55);">האנטה משלמת <span dir="ltr">1:1</span> · החלפת קלף: <span dir="ltr">$5</span> ואז <span dir="ltr">$10</span></div>
  </div>`;
}

async function main() {
  const browser = await chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  async function shot(name, html, w, h, transparent = true) {
    await page.setViewportSize({ width: w, height: h });
    await page.setContent(`<!doctype html><html><head><meta charset="utf-8"><style>${FONTS}</style></head><body>${html}</body></html>`);
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(60);
    await page.screenshot({ path: path.join(OUT, name + '.png'), omitBackground: transparent, clip: { x: 0, y: 0, width: w, height: h } });
  }
  const only = process.argv[2];
  const want = (k) => !only || only.split(',').includes(k);

  if (want('room')) await shot('room', room(), 1920, 1080, false);
  if (want('table')) await shot('table', table(), 1920, 1080);
  if (want('cards')) {
    fs.mkdirSync(path.join(OUT, 'Cards'), { recursive: true });
    for (const s of 'SHDC') for (let r = 2; r <= 14; r++) await shot('Cards/' + s + r, cardFace(r, s), W, H);
    await shot('Cards/back', cardBack(), W, H);
  }
  if (want('chips')) {
    fs.mkdirSync(path.join(OUT, 'Chips'), { recursive: true });
    await shot('Chips/chip5', chip('#c4161c', '#fff', '$5'), 200, 200);
    await shot('Chips/chip10', chip('#1f4fb5', '#fff', '$10'), 200, 200);
    await shot('Chips/chip25', chip('#178a3e', '#fff', '$25'), 200, 200);
    await shot('Chips/chip100', chip('#1b1b1f', '#e8c66a', '$100'), 200, 200);
    await shot('Chips/chip500', chip('#6b2aa0', '#ffe58a', '$500'), 200, 200);
  }
  if (want('ui')) {
    fs.mkdirSync(path.join(OUT, 'UI'), { recursive: true });
    await shot('UI/btn_gold', button('#fff0b0', '#e9b84a', '#a26e17'), 160, 110);
    await shot('UI/btn_red', button('#ff8a8a', '#c8202c', '#6e0a12'), 160, 110);
    await shot('UI/btn_green', button('#9df0b4', '#1f9a4c', '#0c5226'), 160, 110);
    await shot('UI/btn_blue', button('#a8c4ff', '#2f5fd0', '#132e7a'), 160, 110);
    await shot('UI/btn_purple', button('#e2b8ff', '#8a3fd0', '#44156e'), 160, 110);
    await shot('UI/panel', panel(), 128, 128);
    await shot('UI/bubble', bubble(), 128, 128);
    await shot('UI/bubble_tail', bubbleTail(), 70, 70);
    await shot('UI/paytable', paytable(), 340, 440);
  }
  if (want('fx')) {
    fs.mkdirSync(path.join(OUT, 'FX'), { recursive: true });
    await shot('FX/hand', emoji('🖐️', 512), 512, 512);
    await shot('FX/boom', emoji('💥', 256), 256, 256);
    await shot('FX/dizzy', emoji('💫', 256), 256, 256);
    await shot('FX/star', emoji('⭐', 128), 128, 128);
    await shot('FX/duck', emoji('🦆', 256), 256, 256);
    await shot('FX/sweat', emoji('💦', 128), 128, 128);
    await shot('FX/money', emoji('🤑', 256), 256, 256);
  }
  await browser.close();
}
main().catch((e) => { console.error(e); process.exit(1); });
