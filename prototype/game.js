// Отрисовка и управление прототипом: два экрана «Стройка» и «Добыча».
(function () {
  'use strict';
  const C = window.Core;
  const $ = (sel, el = document) => el.querySelector(sel);
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const rng = Math.random;
  const XP = 100 / C.COLS; // ширина клетки в %
  const YP = 100 / C.ROWS; // высота клетки в %

  const COLOR_VAR = ['var(--c-purple)', 'var(--c-gray)', 'var(--c-brown)', 'var(--c-blue)', 'var(--c-red)'];
  const COLOR_HEX = ['#8e5bd6', '#98a2ad', '#a8703f', '#4fa8e8', '#e2524a'];
  const TIER_VAR = [null, 'var(--t1)', 'var(--t2)', 'var(--t3)', 'var(--t4)'];
  const TIER_NAME = [null, 'Простые', 'Конструкция', 'Проёмы и кровля', 'Украшения'];
  const SAVE_KEY = 'castles-and-chest-proto-v1';

  // ---------- Состояние и сохранение ----------
  let S = load() || C.newGame();
  C.syncIds(S);
  save();
  let screen = S.run ? 'mine' : 'build';
  let busy = false;

  function load() {
    try {
      const hot = window.claude && window.claude.hot && window.claude.hot.data;
      if (hot && hot.state) return hot.state;
      const raw = localStorage.getItem(SAVE_KEY);
      const s = raw && JSON.parse(raw);
      if (!s || s.v !== 1) return null;
      // Забег со старым размером поля (10×10) не продолжаем.
      if (s.run && s.run.board.length !== C.ROWS) s.run = null;
      return s;
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

  function partChip(type, extra = '') {
    const p = C.PARTS[type];
    return `<span class="part ${extra}"><span class="sw" style="background:${TIER_VAR[p.tier]}"></span>${p.name}</span>`;
  }

  // ---------- Картинка замка ----------
  // Слоты (порядок как в Core.CASTLES[0].slots):
  // 0–3 стенки, 4–5 перекрытия, 6 крыша, 7 черепица, 8–9 окна, 10 крыльцо, 11 шпиль, 12 фонарик.
  const SLOT_SVG = {
    0: '<rect x="100" y="236" width="60" height="64" fill="url(#bricks)" stroke="#7b6a55" stroke-width="2"/>',
    1: '<rect x="160" y="236" width="60" height="64" fill="url(#bricks)" stroke="#7b6a55" stroke-width="2"/>',
    2: '<rect x="100" y="164" width="60" height="64" fill="url(#bricks)" stroke="#7b6a55" stroke-width="2"/>',
    3: '<rect x="160" y="164" width="60" height="64" fill="url(#bricks)" stroke="#7b6a55" stroke-width="2"/>',
    4: '<rect x="92" y="228" width="136" height="9" rx="2" fill="#8a5a34" stroke="#5e3b20" stroke-width="2"/>',
    5: '<rect x="92" y="156" width="136" height="9" rx="2" fill="#8a5a34" stroke="#5e3b20" stroke-width="2"/>',
    6: '<polygon points="84,158 236,158 160,82" fill="#c4473d" stroke="#7d2620" stroke-width="2.5" stroke-linejoin="round"/>',
    7: '<path d="M106 142 Q114 148 122 142 Q130 148 138 142 Q146 148 154 142 Q162 148 170 142 Q178 148 186 142 Q194 148 202 142 Q210 148 214 142 M124 122 Q132 128 140 122 Q148 128 156 122 Q164 128 172 122 Q180 128 188 122 Q196 128 198 122 M140 104 Q148 110 156 104 Q164 110 172 104 Q178 109 181 104" fill="none" stroke="#8f2a24" stroke-width="3.5" stroke-linecap="round"/>',
    8: '<rect x="144" y="176" width="32" height="38" rx="16" fill="#9fdcff" stroke="#5a4632" stroke-width="3.5"/><path d="M160 178 V212 M146 196 H174" stroke="#5a4632" stroke-width="2.5"/>',
    9: '<rect x="180" y="248" width="28" height="32" rx="14" fill="#9fdcff" stroke="#5a4632" stroke-width="3.5"/><path d="M194 250 V278 M182 265 H206" stroke="#5a4632" stroke-width="2.5"/>',
    10: '<polygon points="104,258 160,258 132,240" fill="#c4473d" stroke="#7d2620" stroke-width="2"/><rect x="114" y="258" width="36" height="42" rx="16" fill="#7a4a26" stroke="#4a2c14" stroke-width="2.5"/><circle cx="142" cy="281" r="2.5" fill="#f2b134"/><rect x="104" y="300" width="56" height="8" rx="2" fill="#b3a898" stroke="#7b6a55" stroke-width="2"/>',
    11: '<polygon points="153,86 167,86 160,44" fill="#e2b13c" stroke="#8a6410" stroke-width="2"/><path d="M160 44 V26" stroke="#5e3b20" stroke-width="2.5"/><polygon points="160,26 180,31 160,37" fill="#e2524a" stroke="#7d2620" stroke-width="1.5"/>',
    12: '<path d="M220 248 H236 V252" fill="none" stroke="#3b3b3b" stroke-width="3"/><circle cx="236" cy="264" r="13" fill="rgba(255,220,120,.35)" stroke="none"/><rect x="230" y="252" width="12" height="20" rx="3" fill="#f6d36b" stroke="#5a4632" stroke-width="2.5"/>',
  };
  const DRAW_ORDER = [0, 1, 2, 3, 4, 5, 8, 9, 10, 6, 7, 11, 12];

  function castleSVG() {
    const flowers = [[30, 312, '#e2524a'], [48, 322, '#f2b134'], [64, 314, '#fff'], [258, 316, '#e2524a'], [276, 324, '#8e5bd6'], [292, 312, '#f2b134'], [80, 326, '#8e5bd6'], [244, 328, '#fff']]
      .map(([x, y, c]) => `<circle cx="${x}" cy="${y}" r="4" fill="${c}"/><circle cx="${x}" cy="${y}" r="1.6" fill="#f2b134"/>`).join('');
    const fence = Array.from({ length: 9 }, (_, i) => `<rect x="${12 + i * 10}" y="284" width="5" height="20" rx="2" fill="#e8d3b0" stroke="#8a5a34"/>`).join('')
      + Array.from({ length: 9 }, (_, i) => `<rect x="${222 + i * 10}" y="284" width="5" height="20" rx="2" fill="#e8d3b0" stroke="#8a5a34"/>`).join('');
    const slots = DRAW_ORDER.map((i) => `<g class="slot" data-slot="${i}" tabindex="0" role="button">${SLOT_SVG[i]}</g>`).join('');
    return `<svg viewBox="0 0 320 340" role="img" aria-label="Усадьба: башня в 2 этажа">
      <defs>
        <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#bfe4f7"/><stop offset="1" stop-color="#e8f6fc"/></linearGradient>
        <pattern id="bricks" width="20" height="12" patternUnits="userSpaceOnUse">
          <rect width="20" height="12" fill="#d9c7ad"/>
          <path d="M0 6 H20 M0 12 H20 M10 0 V6 M0 6 V12 M20 6 V12" stroke="#b39b7c" stroke-width="1.2"/>
        </pattern>
      </defs>
      <rect width="320" height="340" fill="url(#sky)"/>
      <circle cx="270" cy="50" r="22" fill="#ffe08a"/>
      <ellipse cx="70" cy="300" rx="120" ry="60" fill="#9fd08a"/>
      <ellipse cx="270" cy="296" rx="110" ry="54" fill="#93c97e"/>
      <rect y="300" width="320" height="40" fill="#7cc06a"/>
      ${fence}
      <ellipse cx="160" cy="306" rx="80" ry="8" fill="rgba(0,0,0,.12)"/>
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
      <section class="panel row-panel">
        <h2>Сундуки${S.chests.length ? ` · ${S.chests.length}` : ''}</h2>
        <div class="strip">${S.chests.length
          ? S.chests.map((ch, i) => `<button class="chest-btn" data-chest="${i}" aria-label="Открыть сундук">${chestSVG(ch.color, ch.stars)}</button>`).join('')
          : '<span class="empty">Пусто. Сундуки добываются на поле.</span>'}</div>
        <button class="btn small wood" id="openAll" ${S.chests.length ? '' : 'disabled'}>Открыть все</button>
      </section>
      <section class="panel row-panel">
        <h2>Склад</h2>
        <div class="strip parts">${C.PART_KEYS.filter((k) => S.inv[k] > 0).map((k) => {
          const n = S.inv[k];
          const need = Math.max(0, C.need(S, k));
          return `<button class="part" data-part="${k}"><span class="sw" style="background:${TIER_VAR[tierOf(k)]}"></span>${partName(k)} <span class="cnt">${n}</span>${need ? `<span class="need" title="Не хватает для замка">−${need}</span>` : ''}</button>`;
        }).join('') || '<span class="empty">Пусто. Детали выпадают из сундуков.</span>'}</div>
        <button class="btn small wood" id="merge">Собрать</button>
      </section>
      <button class="btn primary" id="toMine">▶ ${S.run ? `Продолжить · ${S.run.moves} ход.` : `Добывать · ${S.opts.moves} ходов`}</button>`;

    S.filled.forEach((f, i) => {
      const g = root.querySelector(`.slot[data-slot="${i}"]`);
      if (!f) g.classList.add('ghost');
      if (!f && S.inv[castle.slots[i]] > 0) g.classList.add('ready');
      g.setAttribute('aria-label', `${partName(castle.slots[i])}${f ? ', стоит' : ', пустое место'}`);
    });
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
    if (t.id === 'menu') return settings();
    if (t.id === 'openAll') return openChests(S.chests.map((_, i) => i));
    if (t.id === 'merge') return mergeSheet();
    if (t.dataset.chest) return openChests([+t.dataset.chest]);
    if (t.dataset.part) {
      const k = t.dataset.part;
      if (tierOf(k) === 4) return toast('Украшения — высшая ступень, их ставят на замок');
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
    if (tier > 1) from.push(`Собрать из 2 деталей ${tier - 1}-й ступени · есть ${lower}`);
    if (color !== undefined) from.push(`<span class="sw" style="background:${COLOR_VAR[color]}"></span> ${C.COLORS[color].name} сундук`);
    if (tier === 4) from.push('Изредка из сундуков с 5 звёздами');
    sheet(() => `
      <h3>${partName(type)}</h3>
      <p class="sub">${tier}-я ступень · ${TIER_NAME[tier]}</p>
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
      const stock = C.PART_KEYS.filter((k) => tierOf(k) < 4 && (tier == null || tierOf(k) === tier))
        .map((k) => `<button class="part ${free(k) > 0 ? '' : 'zero'}" data-act="pick" data-k="${k}" ${free(k) > 0 ? '' : 'disabled'}><span class="sw" style="background:${TIER_VAR[tierOf(k)]}"></span>${partName(k)} <span class="cnt">${free(k)}</span></button>`).join('');
      const targets = tier ? C.partsOfTier(tier + 1) : [];
      if (target && tier && tierOf(target) !== tier + 1) target = null;
      const ok = pick.length === 2 && target;
      return `
        <h3>Собрать деталь</h3>
        <div class="cells">${cells}</div>
        <p class="sub">${pick.length < 2 ? `Выбери ${pick.length ? 'вторую' : 'две'} детали${tier ? ` ${tier}-й ступени` : ' одной ступени'}` : '↓ во что собрать'}</p>
        <div class="list">${stock || '<span class="empty">На складе нечего собирать</span>'}</div>
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
    save();
    modal(`<h3>Башня построена!</h3>
      <div class="done-pic">${castleSVG()}</div>
      <p>Забегов на этот замок: <b>${S.runs}</b>. Следующие замки ещё не придуманы, поэтому в прототипе башня строится заново.</p>`, [
      ['Следующий замок', 'primary', () => {
        const opts = S.opts;
        S = C.newGame(); S.opts = opts; S.chests = [];
        save(); go('build');
      }],
    ]);
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
        <p class="hint">По документу они сгорают: башня строится примерно за 7 забегов. Если отдавать, примерно за 2. Действует со следующего забега.</p>
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
      </div>
      <div class="needs" id="needs"></div>
      <div class="board" id="board" aria-label="Поле 12 на 10"></div>
      <section class="panel row-panel">
        <h2>Добыто</h2>
        <div class="strip" id="ribbon"></div>
      </section>`;
    els.clear();
    $('#board').addEventListener('pointerdown', onBoardTap);
    $('#toBuild').onclick = () => go('build');
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
    // Украшения собираются, а не падают с поля, поэтому показываем их одним значком.
    const deco = list.filter(({ type }) => C.PART_COLOR[type] === undefined).reduce((a, x) => a + x.n, 0);
    box.innerHTML = '<span class="lbl">Нужно замку:</span>' + list.filter(({ type }) => C.PART_COLOR[type] !== undefined).map(({ type, n }) =>
      `<span class="need-chip"><span class="sw" style="background:${COLOR_VAR[C.PART_COLOR[type]]}"></span>${partName(type).toLowerCase()}${n > 1 ? ` ×${n}` : ''}</span>`).join('')
      + (deco ? `<span class="need-chip" title="Собираются из деталей 3-й ступени"><span class="sw merge">⇡</span>украшения ×${deco}</span>` : '');
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
    for (const f of frames) {
      if (f.phase === 'remove') {
        renderBoard(f.snap, { leave: 'pop' });
        if (f.data.chest) floatText(r, c, `Сундук ${'★'.repeat(f.data.chest.stars)}`);
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
    if (name === 'mine' && !S.run) { C.newRun(S, rng); save(); }
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
})();
