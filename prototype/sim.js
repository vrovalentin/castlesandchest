// Симулятор баланса: «бот» играет забеги и строит замки 1 и 2.
// Запуск: node prototype/sim.js [число_игр] [--keep]
// --keep: сундуки, оставшиеся на поле в конце забега, не сгорают, а достаются игроку.
// Бот тапает самую большую группу на поле, открывает все сундуки,
// собирает недостающие детали из лишних и ставит их на замок.
require('./core.js');
const C = globalThis.Core;

let seed = 12345;
const rng = () => {
  // xorshift32
  seed ^= seed << 13; seed >>>= 0;
  seed ^= seed >>> 17;
  seed ^= seed << 5; seed >>>= 0;
  return seed / 4294967296;
};

// Бот выбирает группу по очкам: размер группы + бонус за каждый сундук,
// который после этого хода опустится ниже (под ним освободится клетка).
function bestTap(b) {
  let best = null, bestScore = -1;
  for (const g of C.allGroups(b)) {
    if (g.length < 2) continue;
    const top = {};
    g.forEach(([y, x]) => { top[x] = Math.min(top[x] ?? C.ROWS, y); });
    let chestsAbove = 0;
    for (const x in top) for (let y = 0; y < top[x]; y++) if (b[y][x] && b[y][x].k === 'c') chestsAbove++;
    const score = g.length + 3 * chestsAbove;
    if (score > bestScore) { bestScore = score; best = g; }
  }
  // Тап по нижней клетке группы: новый сундук ближе к краю.
  return best.reduce((a, p) => (p[0] > a[0] ? p : a));
}

function surplus(s, p) {
  const slots = C.castleOf(s).slots;
  const open = slots.filter((t, i) => t === p && !s.filled[i]).length;
  return s.inv[p] - open;
}

// Бот: ставит всё, что может, и сливает лишнее вверх: 2 лишние детали уровня t → 1 деталь уровня t+1,
// пока на более высоких уровнях замку чего-то не хватает.
function autoBuild(s) {
  const placeAll = () => C.castleOf(s).slots.forEach((_, i) => C.place(s, i));
  placeAll();
  for (let tier = 1; tier < C.MAX_TIER; tier++) {
    const higherNeed = C.PART_KEYS.some((k) => C.PARTS[k].tier > tier && C.need(s, k) > 0);
    if (!higherNeed) break;
    for (;;) {
      const pool = [];
      C.partsOfTier(tier).forEach((p) => { for (let k = 0; k < surplus(s, p); k++) pool.push(p); });
      if (pool.length < 2) break;
      const up = C.partsOfTier(tier + 1).sort((a, b) => C.need(s, b) - C.need(s, a))[0];
      if (!C.merge(s, pool[0], pool[1], up)) break;
      placeAll();
    }
  }
}

const KEEP = process.argv.includes('--keep');

function playCastle(s) {
  let chests = 0, burned = 0, sizes = [];
  while (!C.isComplete(s) && s.runs < 300) {
    C.newRun(s, rng);
    const castle = C.castleOf(s);
    while (s.run.moves > 0) {
      const t = bestTap(s.run.board);
      const res = C.doMove(s.run.board, t[0], t[1], castle, rng);
      sizes.push(res.size);
      s.chests.push(...res.chests);
      chests += res.chests.length;
      s.run.moves--;
    }
    const sum = C.endRun(s);
    if (!sum.kept) burned += sum.left;
    while (s.chests.length) C.openChest(s, s.chests.shift(), rng);
    autoBuild(s);
  }
  return { runs: s.runs, chests, burned, avgSize: sizes.reduce((a, b) => a + b, 0) / sizes.length };
}

function playGame() {
  const s = C.newGame();
  s.tickets = Infinity; // бот играет без ограничения билетов
  s.opts.keepOnEnd = KEEP;
  const c1 = playCastle(s);
  C.nextCastle(s);
  const c2 = playCastle(s);
  return { runs: c1.runs, runs2: c2.runs, chests: c1.chests, burned: c1.burned, avgSize: c1.avgSize };
}

const games = +process.argv.find((a) => /^\d+$/.test(a)) || 500;
const res = [];
for (let i = 0; i < games; i++) res.push(playGame());
const runs = res.map((r) => r.runs).sort((a, b) => a - b);
const runs2 = res.map((r) => r.runs2).sort((a, b) => a - b);
const avg = (k) => (res.reduce((a, r) => a + r[k], 0) / res.length).toFixed(1);
console.log(`Игр: ${games}, сундуки в конце забега: ${KEEP ? 'достаются игроку' : 'сгорают'}`);
console.log(`Забегов на замок 1: медиана ${runs[runs.length >> 1]}, среднее ${avg('runs')}, мин ${runs[0]}, макс ${runs[runs.length - 1]}`);
console.log(`Забегов на замок 2 (склад при переходе сгорает, кроме фигурок): медиана ${runs2[runs2.length >> 1]}, среднее ${avg('runs2')}, мин ${runs2[0]}, макс ${runs2[runs2.length - 1]}`);
console.log(`Сундуков за забег: ${(res.reduce((a, r) => a + r.chests / r.runs, 0) / res.length).toFixed(1)}`);
console.log(`Сгорело сундуков на поле за забег: ${(res.reduce((a, r) => a + r.burned / r.runs, 0) / res.length).toFixed(1)}`);
console.log(`Средний размер группы: ${avg('avgSize')}`);
