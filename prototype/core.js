// Логика игры без отрисовки: поле, сундуки, склад, замок.
// Работает и в браузере (window.Core), и в Node (симулятор sim.js).
// Правила взяты из design/game-design.md v0.6; при переносе в Unity это пойдёт в Game.Core.
(function (root) {
  'use strict';

  // Поле: 10 × 10 (решение Валентина 20.10: на 12 столбцах клетки на телефоне слишком мелкие).
  const ROWS = 10;
  const COLS = 10;

  // Цвет блоков = уровень детали из сундука: индекс 0 → уровень 1 и т.д.
  const COLORS = [
    { key: 'purple', name: 'Фиолетовый', gives: 'глина' },
    { key: 'brown', name: 'Коричневый', gives: 'кирпич' },
    { key: 'gray', name: 'Серый', gives: 'стенка' },
    { key: 'blue', name: 'Синий', gives: 'перекрытие, кровля' },
    { key: 'green', name: 'Зелёный', gives: 'окно, дверь' },
  ];

  // 7 уровней деталей (решение Валентина 19.10).
  const PARTS = {
    clay: { name: 'Глина', tier: 1 },
    brick: { name: 'Кирпич', tier: 2 },
    wall: { name: 'Стенка', tier: 3 },
    floor: { name: 'Перекрытие', tier: 4 },
    roof: { name: 'Крыша', tier: 4 },
    tiles: { name: 'Черепица', tier: 4 },
    window: { name: 'Окно', tier: 5 },
    door: { name: 'Дверь', tier: 5 },
    spire: { name: 'Шпиль', tier: 6 },
    porch: { name: 'Крыльцо', tier: 6 },
    lantern: { name: 'Фонарик', tier: 6 },
    princess: { name: 'Принцесса', tier: 7 },
    knight: { name: 'Рыцарь', tier: 7 },
    dragon: { name: 'Дракон', tier: 7 },
    unicorn: { name: 'Единорог', tier: 7 },
  };
  const MAX_TIER = 7;
  const COLOR_TIERS = 5; // уровни 1–5 падают из сундуков своего цвета
  const PART_KEYS = Object.keys(PARTS);
  const partsOfTier = (t) => PART_KEYS.filter((k) => PARTS[k].tier === t);

  // Награда по звёздам (решение Валентина 19.10). Премиум: у зелёного сундука декор или фигурка,
  // у остальных деталь на уровень выше [по умолчанию].
  const PREMIUM_CHANCE = { 4: 0.2, 5: 0.4 };
  const GREEN_FIGURE_SHARE = 0.25; // доля фигурок среди премиума зелёного сундука [по умолчанию]

  // Замки (раздел 5). Порядок слотов совпадает с картинкой в game.js.
  const CASTLES = [
    {
      id: 1,
      name: 'Башня в 2 этажа',
      moves: 30,
      trashShare: 0.25,
      helpChance: 0.2,
      slots: [
        'clay', 'clay', 'clay', 'clay',
        'wall', 'wall', 'wall', 'wall',
        'floor', 'floor',
        'roof', 'tiles',
        'window', 'window',
        'porch', 'spire', 'lantern',
      ],
    },
    {
      id: 2,
      name: 'Замок с башнями',
      moves: 30,
      trashShare: 0.25,
      helpChance: 0.2,
      slots: [
        ...Array(10).fill('clay'),
        ...Array(8).fill('wall'),
        'floor', 'floor', 'floor',
        ...Array(6).fill('window'),
        'porch', 'porch',
        'roof', 'roof',
        'spire', 'spire',
        'lantern',
        'princess', 'knight',
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
    return 1 + Math.floor(rng() * (COLORS.length - 1));
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
    const chests = settle(b, castle, rng, onPhase);
    return { size: g.length, color, chests };
  }

  // После удаления клеток: падение, сбор сундуков из нижнего ряда (каскадом), досыпка, перемешивание.
  function settle(b, castle, rng, onPhase) {
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

    return collected.map((x) => ({ color: x.color, stars: x.stars }));
  }

  // ---------- Помощники: метла и молоток (решение Валентина 20.10) ----------
  // Метла убирает все блоки ряда, молоток — один блок. Сундуки не трогают, сундуков не создают, ход не тратят.
  const TOOLS = {
    broom: { name: 'Метла', hint: 'Тапни ряд: все блоки в нём исчезнут' },
    hammer: { name: 'Молоток', hint: 'Тапни блок: он исчезнет' },
  };
  function useTool(b, tool, r, c, castle, rng, onPhase) {
    const cells = tool === 'broom'
      ? Array.from({ length: COLS }, (_, x) => [r, x]).filter(([y, x]) => b[y][x] && b[y][x].k === 'b')
      : (b[r][c] && b[r][c].k === 'b' ? [[r, c]] : []);
    if (!cells.length) return null;
    const removed = cells.map(([y, x]) => b[y][x].id);
    cells.forEach(([y, x]) => { b[y][x] = null; });
    onPhase && onPhase('remove', { removed, chest: null, size: cells.length, r, c, tool });
    return { size: cells.length, chests: settle(b, castle, rng, onPhase) };
  }

  // ---------- Ежедневная выдача (решение Валентина 20.10) ----------
  // Раз в сутки помощники и билеты пополняются до этих значений [по умолчанию: «до», а не «плюс»].
  // Билет — вход в один забег на поле.
  const DAILY = { broom: 3, hammer: 3, tickets: 15 };
  function ensureMeta(s) {
    if (!s.tools) s.tools = { broom: 0, hammer: 0 };
    if (typeof s.tickets !== 'number') s.tickets = 0;
    if (!('daily' in s)) s.daily = null;
    return s;
  }
  // day — строка даты вида 2026-10-20. Возвращает, сколько выдали, или null, если сегодня уже выдавали.
  function dailyGrant(s, day) {
    ensureMeta(s);
    if (s.daily === day) return null;
    const got = {
      broom: Math.max(0, DAILY.broom - s.tools.broom),
      hammer: Math.max(0, DAILY.hammer - s.tools.hammer),
      tickets: Math.max(0, DAILY.tickets - s.tickets),
    };
    s.tools.broom += got.broom;
    s.tools.hammer += got.hammer;
    s.tickets += got.tickets;
    s.daily = day;
    return got;
  }

  // ---------- Мета: склад, сундуки, замок ----------
  function newGame() {
    const inv = {};
    PART_KEYS.forEach((k) => { inv[k] = 0; });
    return {
      v: 2,
      castle: 0,
      inv,
      chests: [
        // Подарок на старт, чтобы «Стройка» сразу была не пустой.
        { color: 0, stars: 2 },
        { color: 1, stars: 1 },
      ],
      filled: CASTLES[0].slots.map(() => false),
      run: null,
      runs: 0,
      tools: { broom: 0, hammer: 0 },
      tickets: 0,
      daily: null,
      built: 0,
      // Настройки баланса для плейтеста (раздел 3.1): ходы на забег и судьба сундуков в конце забега.
      opts: { moves: CASTLES[0].moves, keepOnEnd: false },
    };
  }

  const castleOf = (s) => CASTLES[Math.min(s.castle, CASTLES.length - 1)];

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

  // Сундук цвета T (уровень T = color + 1):
  // ★1 — 1 деталь T; ★2 — 1 деталь T + 1 деталь T−1; ★3 — 2 детали T;
  // ★4 — 2 детали T + 20% премиум; ★5 — 2 детали T + 40% премиум.
  function chestTiers(chest, rng) {
    const T = chest.color + 1;
    const tiers = chest.stars <= 1 ? [T] : chest.stars === 2 ? [T, Math.max(1, T - 1)] : [T, T];
    const p = PREMIUM_CHANCE[chest.stars] || 0;
    if (p && rng() < p) {
      if (T >= COLOR_TIERS) tiers.push(rng() < GREEN_FIGURE_SHARE ? 7 : 6);
      else tiers.push(T + 1);
    }
    return tiers;
  }

  function openChest(s, chest, rng) {
    const out = [];
    for (const t of chestTiers(chest, rng)) {
      const k = pickNeeded(s, partsOfTier(t), rng);
      s.inv[k]++;
      out.push(k);
    }
    return out;
  }

  // Сборка: любые 2 детали одной ступени → 1 деталь следующей [по умолчанию].
  function canMerge(s, a, b) {
    if (!a || !b || PARTS[a].tier !== PARTS[b].tier || PARTS[a].tier >= MAX_TIER) return false;
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
  const isLastCastle = (s) => s.castle >= CASTLES.length - 1;

  // Что переходит на следующий замок: только фигурки (уровень 7) [по умолчанию].
  // Остальной склад и закрытые сундуки сгорают (решение Валентина 19.10: «почти всё сгорает»).
  const KEEP_TIER = 7;
  function burnPreview(s) {
    let parts = 0;
    PART_KEYS.forEach((k) => { if (PARTS[k].tier < KEEP_TIER) parts += s.inv[k]; });
    const kept = PART_KEYS.filter((k) => PARTS[k].tier >= KEEP_TIER && s.inv[k] > 0).map((k) => ({ type: k, n: s.inv[k] }));
    return { parts, chests: s.chests.length, kept };
  }
  function nextCastle(s) {
    PART_KEYS.forEach((k) => { if (PARTS[k].tier < KEEP_TIER) s.inv[k] = 0; });
    s.chests = [];
    s.castle = Math.min(s.castle + 1, CASTLES.length - 1);
    s.filled = castleOf(s).slots.map(() => false);
    s.runs = 0;
  }

  // Какой цвет блоков даёт деталь (для подсказки на поле). Украшения только собираются.
  const PART_COLOR = {};
  PART_KEYS.forEach((k) => { if (PARTS[k].tier <= COLOR_TIERS) PART_COLOR[k] = PARTS[k].tier - 1; });

  // Чего не хватает замку с учётом склада: [{ type, n }].
  function missing(s) {
    const out = [];
    PART_KEYS.forEach((k) => { const n = need(s, k); if (n > 0) out.push({ type: k, n }); });
    return out;
  }

  // Новый забег стоит 1 билет. Без билетов забег не начинается.
  function newRun(s, rng) {
    ensureMeta(s);
    if (s.tickets < 1) return null;
    s.tickets--;
    const castle = castleOf(s);
    s.run = { board: genBoard(castle, rng), moves: s.opts.moves, got: [], extraUsed: false };
    s.runs++;
    return s.run;
  }

  // Конец забега. По документу сундуки, оставшиеся на поле, сгорают;
  // опция keepOnEnd вместо этого отдаёт их игроку (для сравнения на плейтесте).
  function endRun(s) {
    const left = [];
    s.run.board.forEach((row) => row.forEach((x) => { if (x && x.k === 'c') left.push({ color: x.color, stars: x.stars }); }));
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
    ROWS, COLS, COLORS, PARTS, PART_KEYS, CASTLES, MAX_TIER, partsOfTier, starsFor, chestTiers,
    genBoard, group, allGroups, gravity, collectBottom, refill, hasMove, shuffle, doMove, settle,
    TOOLS, useTool, DAILY, ensureMeta, dailyGrant,
    newGame, castleOf, need, missing, PART_COLOR, openChest, canMerge, merge, place, isComplete, isLastCastle, nextCastle, burnPreview, newRun, endRun, syncIds,
  };
})(typeof window !== 'undefined' ? window : globalThis);
