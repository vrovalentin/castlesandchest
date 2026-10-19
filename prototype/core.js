// Логика игры без отрисовки: поле, сундуки, склад, замок.
// Работает и в браузере (window.Core), и в Node (симулятор sim.js).
// Правила взяты из design/game-design.md v0.6; при переносе в Unity это пойдёт в Game.Core.
(function (root) {
  'use strict';

  // Поле: 12 строк × 10 столбцов (вертикально, под телефон).
  const ROWS = 12;
  const COLS = 10;

  // 0 — фиолетовый «мусорный» (кирпичи), дальше цвета деталей.
  const COLORS = [
    { key: 'purple', name: 'Фиолетовый', gives: 'кирпичи' },
    { key: 'gray', name: 'Серый', gives: 'стенка' },
    { key: 'brown', name: 'Коричневый', gives: 'перекрытие' },
    { key: 'blue', name: 'Голубой', gives: 'окно / дверь' },
    { key: 'red', name: 'Красный', gives: 'крыша / черепица' },
  ];

  const PARTS = {
    brick: { name: 'Кирпич', tier: 1 },
    wall: { name: 'Стенка', tier: 2 },
    floor: { name: 'Перекрытие', tier: 2 },
    window: { name: 'Окно', tier: 3 },
    door: { name: 'Дверь', tier: 3 },
    roof: { name: 'Крыша', tier: 3 },
    tiles: { name: 'Черепица', tier: 3 },
    porch: { name: 'Крыльцо', tier: 4 },
    lantern: { name: 'Фонарик', tier: 4 },
    spire: { name: 'Шпиль', tier: 4 },
    cornice: { name: 'Карниз', tier: 4 },
  };
  const PART_KEYS = Object.keys(PARTS);
  const partsOfTier = (t) => PART_KEYS.filter((k) => PARTS[k].tier === t);

  const COLOR_PARTS = { 1: ['wall'], 2: ['floor'], 3: ['window', 'door'], 4: ['roof', 'tiles'] };

  // Замок 1 «Башня в 2 этажа» (раздел 5 дизайн-документа). Порядок слотов совпадает с картинкой.
  const CASTLES = [
    {
      id: 1,
      name: 'Башня в 2 этажа',
      moves: 30,
      trashShare: 0.25,
      helpChance: 0.2,
      slots: [
        'wall', 'wall', 'wall', 'wall',
        'floor', 'floor',
        'roof', 'tiles',
        'window', 'window',
        'porch', 'spire', 'lantern',
      ],
    },
  ];

  function starsFor(size) {
    if (size >= 19) return 5;
    if (size >= 13) return 4;
    if (size >= 9) return 3;
    if (size >= 6) return 2;
    if (size >= 4) return 1;
    return 0;
  }

  // ---------- Поле ----------
  const ids = { next: 1 };
  const mkBlock = (color) => ({ id: ids.next++, k: 'b', color });

  function randColor(castle, rng) {
    if (rng() < castle.trashShare) return 0;
    return 1 + Math.floor(rng() * 4);
  }

  function hasMove(b) {
    for (let r = 0; r < ROWS; r++)
      for (let c = 0; c < COLS; c++) {
        const x = b[r][c];
        if (!x || x.k !== 'b') continue;
        const right = c + 1 < COLS && b[r][c + 1];
        const down = r + 1 < ROWS && b[r + 1][c];
        if (right && right.k === 'b' && right.color === x.color) return true;
        if (down && down.k === 'b' && down.color === x.color) return true;
      }
    return false;
  }

  function shuffle(b, rng) {
    const pos = [];
    const colors = [];
    for (let r = 0; r < ROWS; r++)
      for (let c = 0; c < COLS; c++)
        if (b[r][c] && b[r][c].k === 'b') { pos.push([r, c]); colors.push(b[r][c]); }
    for (let t = 0; t < 20; t++) {
      for (let i = colors.length - 1; i > 0; i--) {
        const j = Math.floor(rng() * (i + 1));
        [colors[i], colors[j]] = [colors[j], colors[i]];
      }
      pos.forEach(([r, c], i) => { b[r][c] = colors[i]; });
      if (hasMove(b)) return;
    }
    // Не вышло: ставим пару принудительно.
    for (const [r, c] of pos) {
      const right = c + 1 < COLS && b[r][c + 1];
      if (right && right.k === 'b') { right.color = b[r][c].color; return; }
      const down = r + 1 < ROWS && b[r + 1][c];
      if (down && down.k === 'b') { down.color = b[r][c].color; return; }
    }
  }

  function genBoard(castle, rng) {
    const b = [];
    for (let r = 0; r < ROWS; r++) {
      b.push([]);
      for (let c = 0; c < COLS; c++) b[r].push(mkBlock(randColor(castle, rng)));
    }
    if (!hasMove(b)) shuffle(b, rng);
    return b;
  }

  function group(b, r, c) {
    const start = b[r] && b[r][c];
    if (!start || start.k !== 'b') return [];
    const seen = new Set([r * COLS + c]);
    const q = [[r, c]];
    const out = [];
    while (q.length) {
      const [y, x] = q.pop();
      out.push([y, x]);
      for (const [dy, dx] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const ny = y + dy, nx = x + dx;
        if (ny < 0 || ny >= ROWS || nx < 0 || nx >= COLS) continue;
        const cell = b[ny][nx];
        if (!cell || cell.k !== 'b' || cell.color !== start.color || seen.has(ny * COLS + nx)) continue;
        seen.add(ny * COLS + nx);
        q.push([ny, nx]);
      }
    }
    return out;
  }

  // Все группы поля (для подсказки «сколько звёзд даст группа»).
  function allGroups(b) {
    const seen = new Set();
    const out = [];
    for (let r = 0; r < ROWS; r++)
      for (let c = 0; c < COLS; c++) {
        if (seen.has(r * COLS + c)) continue;
        const g = group(b, r, c);
        g.forEach(([y, x]) => seen.add(y * COLS + x));
        if (g.length) out.push(g);
      }
    return out;
  }

  function gravity(b) {
    let moved = false;
    for (let c = 0; c < COLS; c++) {
      let w = ROWS - 1;
      for (let r = ROWS - 1; r >= 0; r--) {
        if (b[r][c]) {
          if (w !== r) { b[w][c] = b[r][c]; b[r][c] = null; moved = true; }
          w--;
        }
      }
    }
    return moved;
  }

  function collectBottom(b) {
    const got = [];
    for (let c = 0; c < COLS; c++) {
      const x = b[ROWS - 1][c];
      if (x && x.k === 'c') { got.push(x); b[ROWS - 1][c] = null; }
    }
    return got;
  }

  // Возвращает новые блоки с рядом, откуда они «падают» (для анимации).
  function refill(b, castle, rng) {
    const spawns = [];
    for (let c = 0; c < COLS; c++) {
      let empty = 0;
      while (empty < ROWS && !b[empty][c]) empty++;
      for (let r = empty - 1; r >= 0; r--) {
        const below = r + 1 < ROWS ? b[r + 1][c] : null;
        const help = below && below.k === 'b' && rng() < castle.helpChance;
        const blk = mkBlock(help ? below.color : randColor(castle, rng));
        b[r][c] = blk;
        spawns.push({ id: blk.id, from: r - empty });
      }
    }
    return spawns;
  }

  // Один ход. onPhase(имя, данные) вызывается после каждой фазы — отрисовка снимает по ним кадры.
  function doMove(b, r, c, castle, rng, onPhase) {
    const g = group(b, r, c);
    if (g.length < 2) return null;
    const color = b[r][c].color;
    const removed = g.map(([y, x]) => b[y][x].id);
    g.forEach(([y, x]) => { b[y][x] = null; });
    let chest = null;
    if (g.length >= 4) {
      chest = { id: ids.next++, k: 'c', color, stars: starsFor(g.length) };
      b[r][c] = chest;
    }
    onPhase && onPhase('remove', { removed, chest, size: g.length, r, c });

    const collected = [];
    for (;;) {
      if (gravity(b)) onPhase && onPhase('gravity', {});
      const got = collectBottom(b);
      if (!got.length) break;
      collected.push(...got);
      onPhase && onPhase('collect', { got });
    }
    const spawns = refill(b, castle, rng);
    onPhase && onPhase('refill', { spawns });
    if (!hasMove(b)) { shuffle(b, rng); onPhase && onPhase('shuffle', {}); }

    const chests = collected.map((x) => ({ color: x.color, stars: x.stars, series: collected.length }));
    if (chests.length >= 3) chests[chests.length - 1].bonus = true;
    return { size: g.length, color, chests };
  }

  // ---------- Мета: склад, сундуки, замок ----------
  function newGame() {
    const inv = {};
    PART_KEYS.forEach((k) => { inv[k] = 0; });
    return {
      v: 1,
      castle: 0,
      inv,
      chests: [
        // Подарок на старт, чтобы «Стройка» сразу была не пустой.
        { color: 0, stars: 1, series: 2 },
        { color: 1, stars: 2, series: 1 },
      ],
      filled: CASTLES[0].slots.map(() => false),
      run: null,
      runs: 0,
      built: 0,
      // Настройки баланса для плейтеста (раздел 3.1): ходы на забег и судьба сундуков в конце забега.
      opts: { moves: CASTLES[0].moves, keepOnEnd: false },
    };
  }

  const castleOf = (s) => CASTLES[s.castle % CASTLES.length];

  function need(s, type) {
    const slots = castleOf(s).slots;
    let n = 0;
    slots.forEach((t, i) => { if (t === type && !s.filled[i]) n++; });
    return n - s.inv[type];
  }

  function pickNeeded(s, options, rng) {
    let best = -Infinity, list = [];
    for (const o of options) {
      const n = need(s, o);
      if (n > best) { best = n; list = [o]; } else if (n === best) list.push(o);
    }
    return list[Math.floor(rng() * list.length)];
  }

  function openChest(s, chest, rng) {
    const out = [];
    const add = (t) => { s.inv[t]++; out.push(t); };
    if (chest.color === 0) {
      const n = chest.series >= 2 ? 3 : 2;
      for (let i = 0; i < n; i++) add('brick');
      return out;
    }
    const count = chest.stars <= 2 ? 1 : chest.stars <= 4 ? 2 : 3;
    const up = (chest.stars >= 4 && rng() < 0.25) || (chest.bonus && rng() < 0.5);
    const upOptions = chest.color <= 2 ? partsOfTier(3) : partsOfTier(4);
    for (let i = 0; i < count; i++) {
      add(pickNeeded(s, up && i === 0 ? upOptions : COLOR_PARTS[chest.color], rng));
    }
    return out;
  }

  // Сборка: любые 2 детали одной ступени → 1 деталь следующей [по умолчанию].
  function canMerge(s, a, b) {
    if (!a || !b || PARTS[a].tier !== PARTS[b].tier || PARTS[a].tier >= 4) return false;
    return a === b ? s.inv[a] >= 2 : s.inv[a] >= 1 && s.inv[b] >= 1;
  }
  function merge(s, a, b, target) {
    if (!canMerge(s, a, b) || PARTS[target].tier !== PARTS[a].tier + 1) return false;
    s.inv[a]--; s.inv[b]--; s.inv[target]++;
    return true;
  }

  function place(s, i) {
    const t = castleOf(s).slots[i];
    if (s.filled[i] || s.inv[t] < 1) return false;
    s.inv[t]--; s.filled[i] = true;
    return true;
  }
  const isComplete = (s) => s.filled.every(Boolean);

  // Какой цвет блоков даёт деталь (для подсказки на поле). Украшения только собираются.
  const PART_COLOR = { brick: 0, wall: 1, floor: 2, window: 3, door: 3, roof: 4, tiles: 4 };

  // Чего не хватает замку с учётом склада: [{ type, n }].
  function missing(s) {
    const out = [];
    PART_KEYS.forEach((k) => { const n = need(s, k); if (n > 0) out.push({ type: k, n }); });
    return out;
  }

  function newRun(s, rng) {
    const castle = castleOf(s);
    s.run = { board: genBoard(castle, rng), moves: s.opts.moves, got: [], extraUsed: false };
    s.runs++;
    return s.run;
  }

  // Конец забега. По документу сундуки, оставшиеся на поле, сгорают;
  // опция keepOnEnd вместо этого отдаёт их игроку (для сравнения на плейтесте).
  function endRun(s) {
    const left = [];
    s.run.board.forEach((row) => row.forEach((x) => { if (x && x.k === 'c') left.push({ color: x.color, stars: x.stars, series: 1 }); }));
    if (s.opts.keepOnEnd) { s.chests.push(...left); s.run.got.push(...left); }
    const summary = { got: s.run.got, left: left.length, kept: s.opts.keepOnEnd };
    s.run = null;
    return summary;
  }

  // После загрузки сохранения: счётчик id должен быть больше любого id на поле.
  function syncIds(s) {
    let max = 0;
    if (s.run) s.run.board.forEach((row) => row.forEach((x) => { if (x && x.id > max) max = x.id; }));
    ids.next = Math.max(ids.next, max + 1);
  }

  root.Core = {
    ROWS, COLS, COLORS, PARTS, PART_KEYS, CASTLES, partsOfTier, starsFor,
    genBoard, group, allGroups, gravity, collectBottom, refill, hasMove, shuffle, doMove,
    newGame, castleOf, need, missing, PART_COLOR, openChest, canMerge, merge, place, isComplete, newRun, endRun, syncIds,
  };
})(typeof window !== 'undefined' ? window : globalThis);
