// Отрисовка и управление прототипом: два экрана «Стройка» и «Добыча».
(function () {
  'use strict';
  const C = window.Core;
  const $ = (sel, el = document) => el.querySelector(sel);
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const rng = Math.random;
  const XP = 100 / C.COLS; // ширина клетки в %
  const YP = 100 / C.ROWS; // высота клетки в %

  // Цвет блоков = уровень детали: зелёный 1, коричневый 2, серый 3, синий 4, фиолетовый 5 (самый ценный).
  const COLOR_VAR = ['var(--c-green)', 'var(--c-brown)', 'var(--c-gray)', 'var(--c-blue)', 'var(--c-purple)'];
  const COLOR_HEX = ['#4fb35a', '#a8703f', '#98a2ad', '#4f8fe0', '#8e5bd6'];
  const TIER_VAR = [null, 'var(--t1)', 'var(--t2)', 'var(--t3)', 'var(--t4)', 'var(--t5)', 'var(--t6)', 'var(--t7)'];
  const TIER_NAME = [null, 'Глина', 'Кирпичи', 'Стенки', 'Перекрытия и кровля', 'Окна и двери', 'Декор', 'Фигурки'];
  const SAVE_KEY = 'castles-and-chest-proto-v2';

  // ---------- Состояние и сохранение ----------
  let S = load() || C.newGame();
  C.syncIds(S);
  save();
  let armed = null; // выбранный помощник на поле: 'broom' | 'hammer' | null

  // Ежедневная выдача: метлы, молотки, билеты. День считается по часам устройства
  // (в игре на Яндексе возьмём время сервера, чтобы нельзя было перевести часы).
  const today = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
  function checkDaily() {
    const got = C.dailyGrant(S, today());
    if (!got) return;
    save();
    const parts = [got.broom && `метлы +${got.broom}`, got.hammer && `молотки +${got.hammer}`, got.tickets && `билеты +${got.tickets}`].filter(Boolean);
    setTimeout(() => modal(`<h3>Подарок дня</h3>
      <div class="list">${toolChip('broom', S.tools.broom)}${toolChip('hammer', S.tools.hammer)}<span class="part">${ICON.ticket} Билеты <span class="cnt">${S.tickets}</span></span></div>
      <p class="hint">${parts.length ? `Пополнено: ${parts.join(', ')}.` : 'Всё и так было полным.'} Каждый день метлы и молотки пополняются до 3, билеты до 15. Один билет — один забег.</p>`,
      [['Отлично', 'primary', () => render()]]), 200);
  }
  document.addEventListener('visibilitychange', () => { if (!document.hidden) checkDaily(); });
  let screen = S.run ? 'mine' : 'build';
  let busy = false;

  function load() {
    try {
      const hot = window.claude && window.claude.hot && window.claude.hot.data;
      if (hot && hot.state) return hot.state;
      const raw = localStorage.getItem(SAVE_KEY);
      const s = raw && JSON.parse(raw);
      if (!s || s.v !== 2) return null;
      // Забег со старым размером поля (10×10) не продолжаем.
      if (s.run && (s.run.board.length !== C.ROWS || s.run.board[0].length !== C.COLS)) s.run = null;
      return C.ensureMeta(s);
    } catch (e) { return null; }
  }
  function save() {
    try { localStorage.setItem(SAVE_KEY, JSON.stringify(S)); } catch (e) { /* без сохранения тоже играется */ }
  }
  try { window.claude && window.claude.hot && window.claude.hot.snapshot(() => ({ state: S })); } catch (e) { /* нет хука */ }

  // ---------- Мелочи интерфейса ----------
  let toastTimer;
  function toast(text) {
    let t = $('.toast');
    if (!t) { t = document.createElement('div'); t.className = 'toast'; document.body.append(t); }
    t.textContent = text;
    t.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => { t.hidden = true; }, 2200);
  }

  function modal(html, buttons) {
    const o = document.createElement('div');
    o.className = 'overlay';
    o.innerHTML = `<div class="modal" role="dialog">${html}<div class="actions" style="justify-content:center"></div></div>`;
    const row = $('.actions', o);
    buttons.forEach(([label, cls, fn]) => {
      const b = document.createElement('button');
      b.className = 'btn ' + cls;
      b.textContent = label;
      b.onclick = () => { if (fn && fn(o) === false) return; o.remove(); };
      row.append(b);
    });
    document.body.append(o);
    row.firstChild && row.firstChild.focus();
    return o;
  }

  function chestSVG(color, stars) {
    const col = COLOR_HEX[color];
    return `<svg viewBox="0 0 52 44" aria-hidden="true">
      <rect x="5" y="17" width="42" height="24" rx="4" fill="${col}" stroke="#4a3320" stroke-width="2.5"/>
      <path d="M5 19 Q5 5 26 5 Q47 5 47 19 Z" fill="${col}" stroke="#4a3320" stroke-width="2.5"/>
      <path d="M8 14 Q26 4 44 14" stroke="rgba(255,255,255,.45)" stroke-width="3" fill="none"/>
      <rect x="4" y="17" width="44" height="5" fill="#f2b134" stroke="#4a3320" stroke-width="2"/>
      <rect x="21" y="19" width="10" height="11" rx="2" fill="#f2b134" stroke="#4a3320" stroke-width="2"/>
      ${stars ? `<text x="26" y="39" text-anchor="middle" font-size="10" font-weight="800" fill="#fff" stroke="#4a3320" stroke-width="2.5" paint-order="stroke" font-family="Rubik, sans-serif">${'★'.repeat(stars)}</text>` : ''}
    </svg>`;
  }

  const ICON = {
    broom: '<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M17 2 L11 12" stroke="#8a5a34" stroke-width="2.6" stroke-linecap="round"/><path d="M8 11 L14 14 L11 22 L3 19 Z" fill="#f2c45a" stroke="#8a6410" stroke-width="1.4" stroke-linejoin="round"/><path d="M6 17 L5.2 20 M8.5 18 L7.8 21 M11 18.5 L10.4 21.5" stroke="#8a6410" stroke-width="1.2"/></svg>',
    hammer: '<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 9 L4 21" stroke="#8a5a34" stroke-width="2.8" stroke-linecap="round"/><path d="M9 4 L17 2 L21 8 L13 11 Z" fill="#98a2ad" stroke="#4d5560" stroke-width="1.4" stroke-linejoin="round"/></svg>',
    ticket: '<svg class="ico" viewBox="0 0 24 24" aria-hidden="true"><path d="M3 7 H21 V10 A2 2 0 0 0 21 14 V17 H3 V14 A2 2 0 0 0 3 10 Z" fill="#f2b134" stroke="#8a6410" stroke-width="1.4"/><path d="M15 7.5 V16.5" stroke="#8a6410" stroke-width="1.2" stroke-dasharray="1.5 1.5"/></svg>',
  };
  const toolChip = (k, n) => `<span class="part">${ICON[k]} ${C.TOOLS[k].name} <span class="cnt">${n}</span></span>`;

  // Склад по уровням: строка на каждый уровень 1–7, слева цветная плашка с номером и названием.
  // chip(k) — HTML одной детали или '' (не показывать).
  function tierRows(chip, tiers = [1, 2, 3, 4, 5, 6, 7]) {
    return `<div class="tiers">${tiers.map((t) => {
      const chips = C.partsOfTier(t).map(chip).join('');
      return `<div class="tier-row ${chips ? '' : 'none'}" style="--tc:${TIER_VAR[t]}">
        <span class="tier-badge"><b>${t}</b>${TIER_NAME[t]}</span>
        <div class="tier-parts">${chips || '<span class="dash">—</span>'}</div></div>`;
    }).join('')}</div>`;
  }

  function partChip(type, extra = '') {
    const p = C.PARTS[type];
    return `<span class="part ${extra}"><span class="sw" style="background:${TIER_VAR[p.tier]}"></span>${p.name}</span>`;
  }

  // ---------- Картинки замков ----------
  // Каждая деталь — свой кусок SVG; индекс совпадает со слотом в Core.CASTLES[n].slots.
  const A = {
    clay: (x, y, w, h) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="3" fill="#b9825c" stroke="#7a4f30" stroke-width="2"/>`,
    wall: (x, y, w, h) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="url(#bricks)" stroke="#7b6a55" stroke-width="2"/>`,
    beam: (x, y, w) => `<rect x="${x}" y="${y}" width="${w}" height="9" rx="2" fill="#4f7fb8" stroke="#2f5482" stroke-width="2"/>`,
    roof: (x1, x2, yb, yt) => `<polygon points="${x1},${yb} ${x2},${yb} ${(x1 + x2) / 2},${yt}" fill="#3d6fae" stroke="#22477a" stroke-width="2.5" stroke-linejoin="round"/>`,
    win: (x, y, w, h) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${w / 2}" fill="#bfeec9" stroke="#3f7a4a" stroke-width="3"/><path d="M${x + w / 2} ${y + 2} V${y + h - 2} M${x + 2} ${y + h / 2} H${x + w - 2}" stroke="#3f7a4a" stroke-width="2"/>`,
    porch: (x, y, w, h) => `<polygon points="${x - 8},${y} ${x + w + 8},${y} ${x + w / 2},${y - 16}" fill="#d39a12" stroke="#8a6410" stroke-width="2"/><rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${w / 2.2}" fill="#6a9a4a" stroke="#3d6128" stroke-width="2.5"/><circle cx="${x + w - 8}" cy="${y + h * 0.55}" r="2.5" fill="#f2b134"/><rect x="${x - 8}" y="${y + h}" width="${w + 16}" height="7" rx="2" fill="#b3a898" stroke="#7b6a55" stroke-width="2"/>`,
    spire: (cx, yb, yt) => `<polygon points="${cx - 7},${yb} ${cx + 7},${yb} ${cx},${yt + 14}" fill="#e2b13c" stroke="#8a6410" stroke-width="2"/><path d="M${cx} ${yt + 14} V${yt}" stroke="#5e3b20" stroke-width="2.5"/><polygon points="${cx},${yt} ${cx + 18},${yt + 5} ${cx},${yt + 10}" fill="#e2524a" stroke="#7d2620" stroke-width="1.5"/>`,
    lantern: (x, y) => `<path d="M${x - 14} ${y - 4} H${x + 6} V${y}" fill="none" stroke="#3b3b3b" stroke-width="3"/><circle cx="${x + 6}" cy="${y + 12}" r="13" fill="rgba(255,220,120,.35)"/><rect x="${x}" y="${y}" width="12" height="20" rx="3" fill="#f6d36b" stroke="#5a4632" stroke-width="2.5"/>`,
    princess: (x, y) => `<polygon points="${x - 13},${y} ${x + 13},${y} ${x},${y - 30}" fill="#f29ac0" stroke="#a24a78" stroke-width="2"/><circle cx="${x}" cy="${y - 35}" r="7" fill="#ffd9b8" stroke="#a24a78" stroke-width="1.5"/><polygon points="${x - 6},${y - 41} ${x - 6},${y - 48} ${x - 2},${y - 44} ${x},${y - 49} ${x + 2},${y - 44} ${x + 6},${y - 48} ${x + 6},${y - 41}" fill="#f2b134" stroke="#8a6410" stroke-width="1"/>`,
    knight: (x, y) => `<rect x="${x - 8}" y="${y - 28}" width="16" height="28" rx="4" fill="#b8c1cc" stroke="#55606e" stroke-width="2"/><circle cx="${x}" cy="${y - 35}" r="8" fill="#cfd6de" stroke="#55606e" stroke-width="2"/><path d="M${x - 5} ${y - 35} H${x + 5}" stroke="#55606e" stroke-width="2"/><path d="M${x + 14} ${y} V${y - 34} M${x + 9} ${y - 12} H${x + 19}" stroke="#55606e" stroke-width="2.5"/><path d="M${x - 16} ${y - 22} h12 v10 q-6 8 -12 0 z" fill="#c8443b" stroke="#7d2620" stroke-width="1.5"/>`,
  };

  const CASTLE_ART = {
    // Замок 1. Слоты: 0–3 глина (фундамент), 4–7 стенки, 8–9 перекрытия, 10 крыша, 11 черепица,
    // 12–13 окна, 14 крыльцо, 15 шпиль, 16 фонарик.
    1: {
      label: 'Башня в 2 этажа',
      parts: [
        A.clay(96, 298, 32, 14), A.clay(128, 298, 32, 14), A.clay(160, 298, 32, 14), A.clay(192, 298, 32, 14),
        A.wall(100, 236, 60, 62), A.wall(160, 236, 60, 62), A.wall(100, 164, 60, 64), A.wall(160, 164, 60, 64),
        A.beam(92, 228, 136), A.beam(92, 156, 136),
        A.roof(84, 236, 158, 82),
        '<path d="M106 142 Q114 148 122 142 Q130 148 138 142 Q146 148 154 142 Q162 148 170 142 Q178 148 186 142 Q194 148 202 142 Q210 148 214 142 M124 122 Q132 128 140 122 Q148 128 156 122 Q164 128 172 122 Q180 128 188 122 Q196 128 198 122 M140 104 Q148 110 156 104 Q164 110 172 104 Q178 109 181 104" class="line" fill="none" stroke="#22477a" stroke-width="3.5" stroke-linecap="round"/>',
        A.win(144, 176, 32, 38), A.win(180, 248, 28, 32),
        A.porch(114, 256, 36, 40),
        A.spire(160, 86, 26),
        A.lantern(222, 250),
      ],
      order: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 13, 14, 10, 11, 15, 16],
    },
    // Замок 2 (как в игре на Unity): 0–14 глина, 15–19 нижние стены, 20 ворота, 21–22 нижние окна,
    // 23–25 зубцы и балкон, 26–28 верхние стены, 29–31 верхние окна, 32–36 крыши, 37–41 шпили,
    // 42–43 крыльца, 44–45 фонари, 46 рыцарь, 47 принцесса. Рисунок примерный.
    2: {
      label: 'Замок с башнями',
      parts: [
        ...Array.from({ length: 15 }, (_, i) => A.clay(70 + i * 12, 298, 12, 14)),
        ...Array.from({ length: 5 }, (_, i) => A.wall(70 + i * 36, 236, 36, 62)),
        A.porch(142, 256, 36, 40),
        A.win(78, 250, 24, 30), A.win(222, 250, 24, 30),
        A.beam(70, 228, 180), A.beam(100, 170, 120), A.beam(130, 112, 60),
        A.wall(100, 176, 40, 52), A.wall(140, 118, 40, 110), A.wall(180, 176, 40, 52),
        A.win(108, 186, 24, 30), A.win(148, 130, 24, 30), A.win(188, 186, 24, 30),
        A.roof(66, 236, 48, 26), A.roof(98, 176, 44, 24), A.roof(136, 118, 48, 26), A.roof(178, 176, 44, 24), A.roof(206, 236, 48, 26),
        A.spire(90, 206, 5), A.spire(120, 150, 5), A.spire(160, 90, 6), A.spire(200, 150, 5), A.spire(230, 206, 5),
        A.porch(98, 256, 34, 40), A.porch(188, 256, 34, 40),
        A.lantern(96, 254), A.lantern(220, 254),
        A.knight(284, 312), A.princess(38, 312),
      ],
      order: [...Array(48).keys()],
    },
  };

  function castleSVG(castle = C.castleOf(S)) {
    const art = CASTLE_ART[castle.id];
    const flowers = [[30, 322, '#e2524a'], [52, 330, '#f2b134'], [64, 318, '#fff'], [258, 320, '#e2524a'], [276, 330, '#8e5bd6'], [300, 318, '#f2b134'], [90, 330, '#8e5bd6'], [236, 330, '#fff']]
      .map(([x, y, c]) => `<circle cx="${x}" cy="${y}" r="4" fill="${c}"/><circle cx="${x}" cy="${y}" r="1.6" fill="#f2b134"/>`).join('');
    const fence = Array.from({ length: 6 }, (_, i) => `<rect x="${6 + i * 10}" y="284" width="5" height="20" rx="2" fill="#e8d3b0" stroke="#8a5a34"/>`).join('')
      + Array.from({ length: 6 }, (_, i) => `<rect x="${260 + i * 10}" y="284" width="5" height="20" rx="2" fill="#e8d3b0" stroke="#8a5a34"/>`).join('');
    const slots = art.order.map((i) => `<g class="slot" data-slot="${i}" tabindex="0" role="button">${art.parts[i]}</g>`).join('');
    return `<svg viewBox="0 0 320 340" role="img" aria-label="Усадьба: ${art.label}">
      <defs>
        <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#bfe4f7"/><stop offset="1" stop-color="#e8f6fc"/></linearGradient>
        <pattern id="bricks" width="20" height="12" patternUnits="userSpaceOnUse">
          <rect width="20" height="12" fill="#c9ced6"/>
          <path d="M0 6 H20 M0 12 H20 M10 0 V6 M0 6 V12 M20 6 V12" stroke="#9aa2ad" stroke-width="1.2"/>
        </pattern>
      </defs>
      <rect width="320" height="340" fill="url(#sky)"/>
      <circle cx="286" cy="40" r="20" fill="#ffe08a"/>
      <ellipse cx="70" cy="300" rx="120" ry="60" fill="#9fd08a"/>
      <ellipse cx="270" cy="296" rx="110" ry="54" fill="#93c97e"/>
      <rect y="300" width="320" height="40" fill="#7cc06a"/>
      ${fence}
      <ellipse cx="160" cy="312" rx="100" ry="8" fill="rgba(0,0,0,.12)"/>
      ${slots}
      ${flowers}
    </svg>`;
  }

  // ---------- Окно поверх с перерисовкой ----------
  // render() возвращает HTML; клики по [data-act] уходят в onAct(act, el, close, redraw).
  function sheet(render, onAct, cls = '') {
    const o = document.createElement('div');
    o.className = 'overlay';
    const box = document.createElement('div');
    box.className = 'modal ' + cls;
    box.setAttribute('role', 'dialog');
    o.append(box);
    const close = () => o.remove();
    const redraw = () => { box.innerHTML = render(); };
    redraw();
    o.addEventListener('click', (e) => {
      if (e.target === o) return close();
      const el = e.target.closest('[data-act]');
      if (el && !el.disabled) onAct(el.dataset.act, el, close, redraw);
    });
    document.body.append(o);
    const first = box.querySelector('button');
    first && first.focus();
    return { close, redraw };
  }

  // Реклама за награду: в прототипе это просто пауза.
  function fakeAd(done) {
    const o = document.createElement('div');
    o.className = 'overlay ad';
    o.innerHTML = '<div class="modal"><h3>Реклама</h3><p class="hint">В игре здесь будет ролик. В прототипе просто пауза.</p></div>';
    document.body.append(o);
    setTimeout(() => { o.remove(); done(); }, 1400);
  }

  const partName = (t) => C.PARTS[t].name;
  const tierOf = (t) => C.PARTS[t].tier;

  // ---------- 1. Стройка ----------
  function renderBuild() {
    const castle = C.castleOf(S);
    const done = S.filled.filter(Boolean).length;
    const total = castle.slots.length;
    const root = $('#build');
    root.innerHTML = `
      <div class="bar">
        <button class="btn small icon" id="menu" aria-label="Настройки">⚙</button>
        <div class="bar-title"><h1 class="title">Замок ${castle.id}: ${castle.name}</h1></div>
        <span class="pill"><b>${done}</b> из ${total}</span>
      </div>
      <div class="progressbar" aria-hidden="true"><i style="width:${(done / total) * 100}%"></i></div>
      <div class="castle-wrap">${castleSVG()}</div>
      ${todoRow(castle)}
      <section class="panel row-panel">
        <h2>Сундуки${S.chests.length ? ` · ${S.chests.length}` : ''}</h2>
        <div class="strip">${S.chests.length
          ? S.chests.map((ch, i) => `<button class="chest-btn" data-chest="${i}" aria-label="Открыть сундук">${chestSVG(ch.color, ch.stars)}</button>`).join('')
          : '<span class="empty">Пусто. Сундуки добываются на поле.</span>'}</div>
        <button class="btn small wood" id="openAll" ${S.chests.length ? '' : 'disabled'}>Открыть все</button>
      </section>
      <section class="panel stock">
        <h2>Склад по уровням <button class="btn small wood" id="merge">Собрать 2 → 1</button></h2>
        ${tierRows((k) => {
          const n = S.inv[k];
          if (!n) return '';
          const need = Math.max(0, C.need(S, k));
          return `<button class="part" data-part="${k}">${partName(k)} <span class="cnt">${n}</span>${need ? `<span class="need" title="Не хватает для замка">−${need}</span>` : ''}</button>`;
        })}
      </section>
      ${mineButton()}`;

    S.filled.forEach((f, i) => {
      const g = root.querySelector(`.slot[data-slot="${i}"]`);
      if (!f) g.classList.add('ghost');
      if (!f && S.inv[castle.slots[i]] > 0) g.classList.add('ready');
      g.setAttribute('aria-label', `${partName(castle.slots[i])}${f ? ', стоит' : ', пустое место'}`);
    });
  }

  // «Осталось поставить»: пустые места по типам; тап открывает окно места и подсвечивает его на замке.
  function todoRow(castle) {
    const left = {};
    castle.slots.forEach((k, i) => { if (!S.filled[i]) left[k] = (left[k] || 0) + 1; });
    const keys = Object.keys(left);
    if (!keys.length) return '';
    return `<div class="todo"><span class="lbl">Осталось поставить:</span>${keys.map((k) =>
      `<button class="need-chip ${S.inv[k] > 0 ? 'have' : ''}" data-todo="${k}"><span class="sw" style="background:${TIER_VAR[tierOf(k)]}"></span>${partName(k).toLowerCase()}${left[k] > 1 ? ` ×${left[k]}` : ''}</button>`).join('')}</div>`;
  }

  // Кнопка «Добывать»: продолжение забега бесплатно, новый забег стоит 1 билет.
  function mineButton() {
    if (S.run) return `<button class="btn primary" id="toMine">▶ Продолжить · ${S.run.moves} ход.</button>`;
    if (S.tickets > 0) return `<button class="btn primary" id="toMine">▶ Добывать · ${S.opts.moves} ходов <span class="ticket">${ICON.ticket}${S.tickets}</span></button>`;
    return `<div class="no-tickets"><button class="btn primary" disabled>Билеты кончились, новые завтра</button>
      <button class="btn wood" id="ticketAd">+1 билет · реклама</button></div>`;
  }

  function afterPlace(placed) {
    save();
    renderBuild();
    placed.forEach((i) => { const g = $(`#build .slot[data-slot="${i}"]`); g && g.classList.add('just'); });
    if (C.isComplete(S)) setTimeout(castleDone, 600);
  }

  function onBuildClick(e) {
    const t = e.target.closest('button, .slot');
    if (!t) return;
    if (t.id === 'toMine') return go('mine');
    if (t.id === 'ticketAd') return fakeAd(() => { S.tickets++; save(); renderBuild(); toast('Билет получен'); });
    if (t.id === 'menu') return settings();
    if (t.id === 'openAll') return openChests(S.chests.map((_, i) => i));
    if (t.id === 'merge') return mergeSheet();
    if (t.dataset.chest) return openChests([+t.dataset.chest]);
    if (t.dataset.todo) {
      const i = C.castleOf(S).slots.findIndex((k, n) => k === t.dataset.todo && !S.filled[n]);
      const g = $(`#build .slot[data-slot="${i}"]`);
      if (g) { g.classList.remove('flash'); g.getBoundingClientRect(); g.classList.add('flash'); }
      return slotSheet(i);
    }
    if (t.dataset.part) {
      const k = t.dataset.part;
      if (tierOf(k) === C.MAX_TIER) return toast('Фигурки — высший уровень, их ставят на замок');
      if (!S.inv[k]) return toast(`${partName(k)}: на складе нет`);
      return mergeSheet({ pick: k });
    }
    if (t.classList.contains('slot')) {
      const i = +t.dataset.slot;
      if (S.filled[i]) return toast(`${partName(C.castleOf(S).slots[i])} уже стоит`);
      return slotSheet(i);
    }
  }

  // ---------- 2. Пустое место ----------
  function slotSheet(i) {
    const type = C.castleOf(S).slots[i];
    const tier = tierOf(type);
    const color = C.PART_COLOR[type];
    const lower = tier > 1 ? C.partsOfTier(tier - 1).reduce((a, k) => a + S.inv[k], 0) : 0;
    const have = S.inv[type];
    const from = [];
    if (tier > 1) from.push(`Собрать из 2 деталей ${tier - 1}-го уровня · есть ${lower}`);
    if (color !== undefined) from.push(`<span class="sw" style="background:${COLOR_VAR[color]}"></span> ${C.COLORS[color].name} сундук`);
    if (tier >= 6) from.push(`<span class="sw" style="background:${COLOR_VAR[4]}"></span> Шанс из фиолетового сундука ★4–5`);
    else if (tier > 1) from.push(`Шанс из ${['зелёного', 'коричневого', 'серого', 'синего'][tier - 2]} сундука ★4–5`);
    sheet(() => `
      <h3>${partName(type)}</h3>
      <p class="sub">${tier}-й уровень · ${TIER_NAME[tier]}</p>
      <p class="big-count">На складе: <b>${have}</b></p>
      ${have ? '' : `<ul class="from">${from.map((f) => `<li>${f}</li>`).join('')}</ul>`}
      <div class="actions">${have
        ? '<button class="btn primary" data-act="place">Поставить</button>'
        : `${tier > 1 ? `<button class="btn wood" data-act="merge" ${lower >= 2 ? '' : 'disabled'}>Собрать</button>` : ''}<button class="btn primary" data-act="mine">Добывать</button>`}
      </div>`,
    (act, el, close) => {
      close();
      if (act === 'place') { if (C.place(S, i)) afterPlace([i]); }
      if (act === 'merge') mergeSheet({ target: type });
      if (act === 'mine') go('mine');
    });
  }

  // ---------- 3. Сборка 2 → 1 ----------
  function mergeSheet(opt = {}) {
    let pick = opt.pick ? [opt.pick] : [];
    let target = opt.target || null;
    const tierWanted = target ? tierOf(target) - 1 : null;
    const free = (k) => S.inv[k] - pick.filter((x) => x === k).length;

    const render = () => {
      const tier = pick.length ? tierOf(pick[0]) : tierWanted;
      const cells = [0, 1].map((n) => pick[n]
        ? `<button class="cell-slot full" data-act="unpick" data-n="${n}">${partChip(pick[n])}</button>`
        : '<span class="cell-slot">пусто</span>').join('<span class="plus">+</span>');
      const tiers = tier ? [tier] : [1, 2, 3, 4, 5, 6].filter((t) => C.partsOfTier(t).some((k) => S.inv[k] > 0));
      const stock = tiers.length ? tierRows((k) => (S.inv[k] > 0
        ? `<button class="part ${free(k) > 0 ? '' : 'zero'}" data-act="pick" data-k="${k}" ${free(k) > 0 ? '' : 'disabled'}>${partName(k)} <span class="cnt">${free(k)}</span></button>`
        : ''), tiers) : '';
      const targets = tier ? C.partsOfTier(tier + 1) : [];
      if (target && tier && tierOf(target) !== tier + 1) target = null;
      const ok = pick.length === 2 && target;
      return `
        <h3>Собрать деталь</h3>
        <div class="cells">${cells}</div>
        <p class="sub">${pick.length < 2 ? `Выбери ${pick.length ? 'вторую' : 'две'} детали${tier ? ` ${tier}-го уровня` : ' одного уровня'}` : '↓ во что собрать'}</p>
        ${stock || '<p class="empty">На складе нечего собирать</p>'}
        ${targets.length ? `<div class="targets">${targets.map((t) => {
          const need = Math.max(0, C.need(S, t));
          return `<button class="target ${t === target ? 'on' : ''}" data-act="target" data-k="${t}">${partName(t)}${need ? ' <span class="star" title="Нужно замку">★</span>' : ''}</button>`;
        }).join('')}</div>` : ''}
        <div class="actions"><button class="btn primary" data-act="do" ${ok ? '' : 'disabled'}>Собрать</button><button class="btn" data-act="close">Отмена</button></div>`;
    };
    sheet(render, (act, el, close, redraw) => {
      if (act === 'close') return close();
      if (act === 'pick') { if (pick.length < 2) pick.push(el.dataset.k); else pick = [pick[0], el.dataset.k]; }
      if (act === 'unpick') pick.splice(+el.dataset.n, 1);
      if (act === 'target') target = el.dataset.k;
      if (act === 'do') {
        if (C.merge(S, pick[0], pick[1], target)) {
          toast(`Собрано: ${partName(target)}`);
          save(); renderBuild(); close();
          return;
        }
      }
      redraw();
    }, 'wide');
  }

  // ---------- 4. Открытие сундуков ----------
  function openChests(indexes) {
    const got = [];
    const opened = [];
    indexes.sort((a, b) => b - a).forEach((i) => {
      const ch = S.chests.splice(i, 1)[0];
      opened.push(ch);
      got.push(...C.openChest(S, ch, rng));
    });
    save();
    renderBuild();
    const m = {};
    got.forEach((t) => { m[t] = (m[t] || 0) + 1; });
    const chips = Object.keys(m).map((t) => `<span class="part fly"><span class="sw" style="background:${TIER_VAR[tierOf(t)]}"></span>${partName(t)}${m[t] > 1 ? ` <span class="cnt">×${m[t]}</span>` : ''}</span>`).join('');
    modal(`<h3>${indexes.length > 1 ? `Открыто сундуков: ${indexes.length}` : 'Сундук открыт'}</h3>
      <div class="list opened">${opened.slice(0, 8).map((ch) => `<span class="chest-btn open">${chestSVG(ch.color, ch.stars)}</span>`).join('')}${opened.length > 8 ? `<span class="empty">+${opened.length - 8}</span>` : ''}</div>
      <div class="list">${chips}</div>`, [['Забрать', 'primary', null]]);
  }

  // ---------- 8. Замок построен ----------
  function castleDone() {
    S.built++;
    const castle = C.castleOf(S);
    const last = C.isLastCastle(S);
    save();
    modal(`<h3>${castle.name}: построено!</h3>
      <div class="done-pic">${castleSVG(castle)}</div>
      <p>Забегов на этот замок: <b>${S.runs}</b>.${last ? ' Это последний замок демоверсии.' : ''}</p>
      ${last ? '' : burnNote()}`, [
      last
        ? ['Пройти демо заново', 'primary', () => { const opts = S.opts; S = C.newGame(); S.opts = opts; S.chests = []; save(); go('build'); }]
        : ['Следующий замок', 'primary', () => { C.nextCastle(S); save(); go('build'); }],
    ]);
  }

  function burnNote() {
    const b = C.burnPreview(S);
    const lost = [b.parts ? `деталей: ${b.parts}` : '', b.chests ? `закрытых сундуков: ${b.chests}` : ''].filter(Boolean).join(', ');
    const kept = b.kept.map(({ type, n }) => `${partName(type).toLowerCase()}${n > 1 ? ` ×${n}` : ''}`).join(', ');
    return `<p class="hint burn">${lost ? `При переходе сгорят ${lost}.` : 'Склад пуст, сгорать нечему.'} ${kept ? `Фигурки остаются: ${kept}.` : 'С собой переходят только фигурки.'}</p>`;
  }

  // ---------- 9. Настройки ----------
  function settings() {
    const o = S.opts;
    modal(`<h3>Настройки</h3>
      <label for="optSound">Звук <input id="optSound" type="checkbox" ${o.sound !== false ? 'checked' : ''}></label>
      <label for="optMusic">Музыка <input id="optMusic" type="checkbox" ${o.music !== false ? 'checked' : ''}></label>
      <label for="optLang">Язык <select id="optLang"><option>Русский</option><option disabled>English (позже)</option></select></label>
      <label>Без рекламы <span class="hint">покупка, позже</span></label>
      <details class="dev"><summary>Для теста баланса</summary>
        <label for="optMoves">Ходов на забег <input id="optMoves" type="number" min="5" max="99" value="${o.moves}"></label>
        <label for="optKeep">Сундуки на поле в конце забега достаются игроку <input id="optKeep" type="checkbox" ${o.keepOnEnd ? 'checked' : ''}></label>
        <p class="hint">По документу они сгорают: замок 1 строится примерно за 6 забегов, замок 2 ещё за 14. Если отдавать, примерно 2 и 4. Действует со следующего забега.</p>
        <button class="btn small" type="button" id="reset">Начать игру заново</button>
      </details>`, [
      ['Закрыть', 'primary', (ov) => {
        const m = Math.round(+$('#optMoves', ov).value);
        o.moves = Math.min(99, Math.max(5, m || 30));
        o.keepOnEnd = $('#optKeep', ov).checked;
        o.sound = $('#optSound', ov).checked;
        o.music = $('#optMusic', ov).checked;
        save(); render();
      }],
    ]);
    $('#reset').onclick = (e) => { e.target.closest('.overlay').remove(); confirmReset(); };
  }

  function confirmReset() {
    modal('<h3>Начать заново?</h3><p>Склад, сундуки и замок обнулятся.</p>', [
      ['Да, заново', 'wood', () => { const opts = S.opts; S = C.newGame(); S.opts = opts; save(); go('build'); }],
      ['Отмена', '', null],
    ]);
  }

  // ---------- 5. Добыча ----------
  const els = new Map(); // id клетки → DOM

  function renderMineShell() {
    const root = $('#mine');
    root.innerHTML = `
      <div class="bar">
        <button class="btn small icon" id="toBuild" aria-label="На Стройку">←</button>
        <span class="pill" id="moves"></span>
        <span class="pill" id="got"></span>
        <button class="btn small icon" id="restart" aria-label="Начать забег заново" title="Начать заново">↻</button>
      </div>
      <div class="needs" id="needs"></div>
      <div class="tools" id="tools"></div>
      <div class="board" id="board" aria-label="Поле 10 на 10"></div>
      <section class="panel row-panel">
        <h2>Добыто</h2>
        <div class="strip" id="ribbon"></div>
      </section>`;
    els.clear();
    $('#board').addEventListener('pointerdown', onBoardTap);
    $('#toBuild').onclick = () => go('build');
    $('#restart').onclick = () => { if (!busy) confirmRestart(); };
    $('#tools').addEventListener('click', (e) => {
      const b = e.target.closest('[data-tool]');
      if (!b || busy) return;
      const k = b.dataset.tool;
      if (armed === k) armed = null;
      else if (S.tools[k] > 0) armed = k;
      else return toast(`${C.TOOLS[k].name}: кончились, новые завтра`);
      renderTools();
    });
    renderBoard(S.run.board, {});
    showStars(S.run.board);
    renderHud();
    if (S.run.moves <= 0) setTimeout(outOfMoves, 300);
  }

  // Полоса «Нужно замку»: детали, которых не хватает, и цвет блоков, из которого они падают.
  function renderNeeds() {
    const box = $('#needs');
    if (!box) return;
    const list = C.missing(S);
    if (!list.length) { box.innerHTML = '<span class="lbl">Замку всего хватает: открывай сундуки и строй</span>'; return; }
    // Декор и фигурки не падают с поля обычным цветом, поэтому показываем их одним значком на уровень.
    const sum = (t) => list.filter(({ type }) => tierOf(type) === t).reduce((a, x) => a + x.n, 0);
    const deco = sum(6), figs = sum(7);
    box.innerHTML = '<span class="lbl">Нужно замку:</span>' + list.filter(({ type }) => C.PART_COLOR[type] !== undefined).map(({ type, n }) =>
      `<span class="need-chip"><span class="sw" style="background:${COLOR_VAR[C.PART_COLOR[type]]}"></span>${partName(type).toLowerCase()}${n > 1 ? ` ×${n}` : ''}</span>`).join('')
      + (deco ? `<span class="need-chip" title="Собирается из 2 деталей 5-го уровня или выпадает из фиолетового сундука ★4–5"><span class="sw" style="background:var(--t6)"></span>декор ×${deco}</span>` : '')
      + (figs ? `<span class="need-chip" title="Собирается из 2 деталей декора или выпадает из фиолетового сундука ★4–5"><span class="sw" style="background:var(--t7)"></span>фигурки ×${figs}</span>` : '');
  }

  function renderTools() {
    const box = $('#tools');
    if (!box) return;
    box.innerHTML = Object.keys(C.TOOLS).map((k) =>
      `<button class="tool ${armed === k ? 'on' : ''} ${S.tools[k] ? '' : 'zero'}" data-tool="${k}" aria-pressed="${armed === k}">${ICON[k]}<span>${C.TOOLS[k].name}</span><b>${S.tools[k]}</b></button>`).join('')
      + `<span class="tool-hint">${armed ? C.TOOLS[armed].hint + '. Ход не тратится.' : 'Помощники: тапни, потом тапни поле'}</span>`;
    const board = $('#board');
    board.classList.toggle('armed', !!armed);
    board.dataset.tool = armed || '';
  }

  function renderHud() {
    const m = $('#moves');
    if (!m || !S.run) return;
    m.innerHTML = `Ходы: <b>${S.run.moves}</b>`;
    m.classList.toggle('low', S.run.moves <= 5);
    $('#got').innerHTML = `Сундуки: <b>${S.run.got.length}</b>`;
    $('#ribbon').innerHTML = S.run.got.length
      ? S.run.got.map((ch) => `<span class="chest-btn">${chestSVG(ch.color, ch.stars)}</span>`).join('')
      : '<span class="empty">Сундук упадёт сюда, когда уйдёт за нижний край поля</span>';
    renderNeeds();
    renderTools();
  }

  function cellHTML(x) {
    return x.k === 'c' ? chestSVG(x.color, x.stars) : `<div class="blk" style="--col:${COLOR_VAR[x.color]}"></div>`;
  }

  // Синхронизирует DOM с массивом поля. leave: класс анимации для исчезнувших клеток.
  function renderBoard(b, { spawns, leave }) {
    const board = $('#board');
    const from = new Map((spawns || []).map((s) => [s.id, s.from]));
    const seen = new Set();
    for (let r = 0; r < C.ROWS; r++)
      for (let c = 0; c < C.COLS; c++) {
        const x = b[r][c];
        if (!x) continue;
        seen.add(x.id);
        let el = els.get(x.id);
        if (!el) {
          el = document.createElement('div');
          el.className = 'cell';
          el.innerHTML = cellHTML(x);
          el.style.left = c * XP + '%';
          el.style.top = (from.has(x.id) ? from.get(x.id) : r) * YP + '%';
          if (!from.has(x.id) && leave) el.classList.add('pop');
          board.append(el);
          els.set(x.id, el);
          el.getBoundingClientRect(); // применить стартовую позицию до перехода
          el.classList.remove('pop');
        }
        el.style.left = c * XP + '%';
        el.style.top = r * YP + '%';
      }
    for (const [id, el] of els) {
      if (seen.has(id)) continue;
      els.delete(id);
      el.classList.add(leave || 'pop');
      setTimeout(() => el.remove(), 400);
    }
  }

  // Подсказка: на каждом блоке группы из 4+ рисуем, сколько звёзд даст сундук.
  // null — убрать подсказки (на время анимации хода).
  function showStars(b) {
    els.forEach((el) => { const st = el.querySelector('.st'); if (st) st.remove(); });
    if (!b) return;
    for (const g of C.allGroups(b)) {
      const stars = C.starsFor(g.length);
      if (!stars) continue;
      const html = stars <= 3 ? '★'.repeat(stars) : '★★★<br>' + '★'.repeat(stars - 3);
      for (const [r, c] of g) {
        const el = els.get(b[r][c].id);
        const blk = el && el.querySelector('.blk');
        if (!blk) continue;
        const st = document.createElement('span');
        st.className = 'st' + (stars > 3 ? ' two' : '');
        st.innerHTML = html;
        blk.append(st);
      }
    }
  }

  function floatText(r, c, text) {
    const f = document.createElement('div');
    f.className = 'float';
    f.textContent = text;
    f.style.left = (c + 0.5) * XP + '%';
    f.style.top = (r + 0.5) * YP + '%';
    $('#board').append(f);
    setTimeout(() => f.remove(), 900);
  }

  async function onBoardTap(e) {
    if (busy || !S.run || S.run.moves <= 0) return;
    const rect = e.currentTarget.getBoundingClientRect();
    const c = Math.floor(((e.clientX - rect.left) / rect.width) * C.COLS);
    const r = Math.floor(((e.clientY - rect.top) / rect.height) * C.ROWS);
    if (r < 0 || r >= C.ROWS || c < 0 || c >= C.COLS) return;
    const b = S.run.board;
    const cell = b[r][c];
    if (!cell) return;
    if (armed) return applyTool(armed, r, c);
    if (cell.k === 'c') return toast('Сундук не нажимается. Убери блоки под ним, и он упадёт.');
    if (C.group(b, r, c).length < 2) {
      const el = els.get(cell.id);
      if (el) { el.classList.remove('shake'); el.getBoundingClientRect(); el.classList.add('shake'); }
      return;
    }

    busy = true;
    showStars(null);
    const frames = [];
    const res = C.doMove(b, r, c, C.castleOf(S), rng, (phase, data) => {
      frames.push({ phase, data, snap: b.map((row) => row.slice()) });
    });
    await playFrames(frames, r, c);
    S.chests.push(...res.chests);
    S.run.got.push(...res.chests);
    if (res.chests.length >= 2) toast(`Серия: ${res.chests.length} сундука за ход!`);
    S.run.moves--;
    save();
    renderHud();
    showStars(b);
    busy = false;
    if (S.run.moves <= 0) setTimeout(outOfMoves, 400);
  }

  // Метла или молоток: ход не тратится, награды за убранные блоки нет, сундуки могут упасть за край.
  async function applyTool(tool, r, c) {
    const b = S.run.board;
    if (tool === 'hammer' && b[r][c].k === 'c') return toast('Молоток бьёт только блоки, сундук не разбить');
    busy = true;
    showStars(null);
    const frames = [];
    const res = C.useTool(b, tool, r, c, C.castleOf(S), rng, (phase, data) => {
      frames.push({ phase, data, snap: b.map((row) => row.slice()) });
    });
    if (!res) { busy = false; showStars(b); return toast('В этом ряду нет блоков'); }
    S.tools[tool]--;
    armed = null;
    await playFrames(frames, r, c);
    S.chests.push(...res.chests);
    S.run.got.push(...res.chests);
    if (res.chests.length) toast(res.chests.length > 1 ? `Упало сундуков: ${res.chests.length}` : 'Сундук упал за край!');
    save();
    renderHud();
    showStars(b);
    busy = false;
  }

  async function playFrames(frames, r, c) {
    for (const f of frames) {
      if (f.phase === 'remove') {
        renderBoard(f.snap, { leave: 'pop' });
        if (f.data.tool) floatText(r, f.data.tool === 'broom' ? (C.COLS - 1) / 2 : c, C.TOOLS[f.data.tool].name);
        else if (f.data.chest) floatText(r, c, `Сундук ${'★'.repeat(f.data.chest.stars)}`);
        else if (f.data.size <= 3) floatText(r, c, 'пусто');
        await sleep(200);
      } else if (f.phase === 'gravity') {
        renderBoard(f.snap, {});
        await sleep(230);
      } else if (f.phase === 'collect') {
        renderBoard(f.snap, { leave: 'drop' });
        await sleep(300);
      } else if (f.phase === 'refill') {
        renderBoard(f.snap, { spawns: f.data.spawns });
        await sleep(240);
      } else if (f.phase === 'shuffle') {
        toast('Ходов нет, поле перемешано');
        renderBoard(f.snap, {});
        await sleep(300);
      }
    }
  }

  // «Начать заново» во время добычи: новое поле и полные ходы за 1 билет.
  // Сундуки, уже упавшие за край, остаются; сундуки на поле ведут себя как в конце забега.
  function confirmRestart() {
    if (S.tickets < 1) return toast('Билеты кончились, начать заново можно завтра');
    const left = chestsOnBoard();
    modal(`<h3>Начать заново?</h3>
      <p>Будет новое поле и ${S.opts.moves} ходов. Это стоит 1 билет, у тебя их ${S.tickets}.</p>
      <p class="hint">Добытые сундуки останутся.${left ? (S.opts.keepOnEnd ? ` Сундуки с поля (${left}) тоже достанутся тебе.` : ` Сундуки на поле (${left}) сгорят.`) : ''}</p>`, [
      ['Начать заново', 'primary', () => {
        C.endRun(S);
        C.newRun(S, rng);
        armed = null;
        save();
        renderMineShell();
        toast(`Новое поле. Билетов осталось: ${S.tickets}`);
      }],
      ['Отмена', '', null],
    ]);
  }

  const chestsOnBoard = () => S.run.board.reduce((n, row) => n + row.filter((x) => x && x.k === 'c').length, 0);

  // ---------- 6. Ходы кончились ----------
  function outOfMoves() {
    if (!S.run || S.run.moves > 0 || $('.overlay')) return;
    const left = chestsOnBoard();
    const canExtra = !S.run.extraUsed;
    modal(`<h3>Ходы кончились</h3>
      ${left ? `<p>На поле ещё <b>${left}</b> ${left === 1 ? 'сундук' : left < 5 ? 'сундука' : 'сундуков'}. ${S.opts.keepOnEnd ? 'Они достанутся тебе.' : 'Если закончить, они сгорят.'}</p>` : '<p>Сундуков на поле не осталось.</p>'}
      ${canExtra ? '' : '<p class="hint">Дополнительные ходы уже брали в этом забеге.</p>'}`,
    [
      ...(canExtra ? [['+5 ходов · реклама', 'primary', () => {
        fakeAd(() => { S.run.moves += 5; S.run.extraUsed = true; save(); renderHud(); });
      }]] : []),
      ['Закончить', canExtra ? '' : 'primary', () => finishRun()],
    ]);
  }

  // ---------- 7. Итог забега ----------
  function finishRun() {
    const sum = C.endRun(S);
    save();
    let doubled = false;
    const list = (got) => got.length
      ? `<div class="list">${got.map((ch) => `<span class="chest-btn">${chestSVG(ch.color, ch.stars)}</span>`).join('')}</div>`
      : '<p>Ни один сундук не упал за край.</p>';
    sheet(() => `
      <h3>Добыто сундуков: ${sum.got.length * (doubled ? 2 : 1)}</h3>
      ${list(doubled ? sum.got.concat(sum.got) : sum.got)}
      ${sum.left ? `<p class="hint">${sum.kept ? `Из них ${sum.left} — с поля, они достались тебе.` : `Сгорело на поле: ${sum.left}.`}</p>` : ''}
      <div class="actions">
        ${sum.got.length && !doubled ? '<button class="btn wood" data-act="double">Удвоить · реклама</button>' : ''}
        <button class="btn primary" data-act="build">Открывать в Стройке</button>
      </div>`,
    (act, el, close, redraw) => {
      if (act === 'double') {
        fakeAd(() => {
          S.chests.push(...sum.got.map((ch) => Object.assign({}, ch)));
          doubled = true; save(); redraw();
        });
      }
      if (act === 'build') { close(); go('build'); }
    });
  }

  // ---------- Переключение экранов ----------
  function go(name) {
    if (name === 'mine' && !S.run) {
      if (!C.newRun(S, rng)) { toast('Билеты кончились. Новые завтра'); name = 'build'; }
      save();
    }
    armed = null;
    screen = name;
    render();
    window.scrollTo(0, 0);
  }

  function render() {
    $('#build').hidden = screen !== 'build';
    $('#mine').hidden = screen !== 'mine';
    if (screen === 'build') renderBuild(); else renderMineShell();
  }

  $('#build').addEventListener('click', onBuildClick);
  $('#build').addEventListener('keydown', (e) => {
    if ((e.key === 'Enter' || e.key === ' ') && e.target.classList.contains('slot')) { e.preventDefault(); onBuildClick(e); }
  });
  render();
  checkDaily();
})();
