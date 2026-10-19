// Симулятор баланса: «бот» играет забеги и строит замок 1.
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
  const seen = new Set();
  for (let r = 0; r < C.N; r++)
    for (let c = 0; c < C.N; c++) {
      if (seen.has(r * C.N + c)) continue;
      const g = C.group(b, r, c);
      g.forEach(([y, x]) => seen.add(y * C.N + x));
      if (g.length < 2) continue;
      const top = {};
      g.forEach(([y, x]) => { top[x] = Math.min(top[x] ?? C.N, y); });
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

function autoBuild(s) {
  let progress = true;
  while (progress) {
    progress = false;
    C.castleOf(s).slots.forEach((t, i) => { if (C.place(s, i)) progress = true; });
    for (let tier = 2; tier <= 4; tier++) {
      for (const t of C.partsOfTier(tier)) {
        if (surplus(s, t) >= 0) continue;
        const src = C.partsOfTier(tier - 1).filter((p) => surplus(s, p) > 0);
        const pool = [];
        src.forEach((p) => { for (let k = 0; k < surplus(s, p); k++) pool.push(p); });
        if (pool.length >= 2 && C.merge(s, pool[0], pool[1], t)) progress = true;
      }
    }
  }
}

const KEEP = process.argv.includes('--keep');

function playGame() {
  const s = C.newGame();
  s.opts.keepOnEnd = KEEP;
  let chests = 0, burned = 0, sizes = [];
  while (!C.isComplete(s) && s.runs < 100) {
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

const games = +process.argv.find((a) => /^\d+$/.test(a)) || 500;
const res = [];
for (let i = 0; i < games; i++) res.push(playGame());
const runs = res.map((r) => r.runs).sort((a, b) => a - b);
const avg = (k) => (res.reduce((a, r) => a + r[k], 0) / res.length).toFixed(1);
console.log(`Игр: ${games}, сундуки в конце забега: ${KEEP ? 'достаются игроку' : 'сгорают'}`);
console.log(`Забегов на замок 1: медиана ${runs[runs.length >> 1]}, среднее ${avg('runs')}, мин ${runs[0]}, макс ${runs[runs.length - 1]}`);
console.log(`Сундуков за забег: ${(res.reduce((a, r) => a + r.chests / r.runs, 0) / res.length).toFixed(1)}`);
console.log(`Сгорело сундуков на поле за забег: ${(res.reduce((a, r) => a + r.burned / r.runs, 0) / res.length).toFixed(1)}`);
console.log(`Средний размер группы: ${avg('avgSize')}`);
