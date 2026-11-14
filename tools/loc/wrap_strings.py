# Wraps the game's Russian string literals for translation: "текст" -> Loc.T("текст"), $"текст {x}" ->
# Loc.F("текст {0}", x). Arguments of Plural(...) stay as they are (Loc.Plural translates them as a set).
# Literals already inside Loc.T/Loc.F/Loc.Plural are left alone, so running it again changes nothing.
# Usage: python3 wrap_strings.py file.cs ...   |   --list file.cs ...  (prints the keys, one per line, escaped)
import re, sys

CYR = re.compile('[А-Яа-яЁё]')


def lex_string(src, i):
    """src[i] starts a literal ("..", $"..", @".."). Returns (end, kind, raw content, holes)."""
    interp = verbatim = False
    j = i
    while src[j] in '$@':
        interp |= src[j] == '$'; verbatim |= src[j] == '@'; j += 1
    assert src[j] == '"'
    j += 1
    parts, holes, buf = [], [], []
    while True:
        c = src[j]
        if c == '\\' and not verbatim:
            buf.append(src[j:j + 2]); j += 2; continue
        if c == '"':
            if verbatim and src[j + 1] == '"':
                buf.append('""'); j += 2; continue
            j += 1; break
        if interp and c == '{':
            if src[j + 1] == '{':
                buf.append('{{'); j += 2; continue
            # a hole: read the expression up to its closing brace, skipping nested strings
            k, depth = j + 1, 0
            expr_start = k
            fmt = None
            while True:
                ch = src[k]
                if ch in '"$@' and (ch == '"' or src[k + 1] in '"$@'):
                    k = lex_string(src, k)[0]; continue
                if ch in '([{': depth += 1
                elif ch in ')]': depth -= 1
                elif ch == '}':
                    if depth == 0: break
                    depth -= 1
                elif ch == ':' and depth == 0 and fmt is None and src[k - 1] != '?' and not re.search(r'\?[^:]*$', src[expr_start:k]):
                    fmt = k
                k += 1
            expr = src[expr_start:fmt if fmt else k]
            spec = src[fmt:k] if fmt else ''
            buf.append('{%d%s}' % (len(holes), spec))
            holes.append(expr)
            j = k + 1; continue
        if interp and c == '}' and src[j + 1] == '}':
            buf.append('}}'); j += 2; continue
        buf.append(c); j += 1
    return j, ('interp' if interp else 'plain'), ''.join(buf), holes


def convert(src, keys):
    out, i, n = [], 0, len(src)
    skip_depth = []      # paren depths inside Plural(...) / Loc.*(...) / Debug.Log(...)
    depth = 0
    while i < n:
        c = src[i]
        if src.startswith('//', i):
            e = src.find('\n', i); e = n if e < 0 else e
            out.append(src[i:e]); i = e; continue
        if src.startswith('/*', i):
            e = src.find('*/', i) + 2
            out.append(src[i:e]); i = e; continue
        if c == "'":
            e = i + 1
            while src[e] != "'":
                e += 2 if src[e] == '\\' else 1
            out.append(src[i:e + 1]); i = e + 1; continue
        m = re.match(r'(Loc\.T|Loc\.F|Loc\.Plural|Plural|Debug\.Log\w*|ToString)\s*\(', src[i:])
        if m and (i == 0 or not (src[i - 1].isalnum() or src[i - 1] in '_.') or m.group(1).startswith(('Loc', 'Debug'))):
            out.append(m.group(0)); i += m.end(); depth += 1; skip_depth.append(depth); continue
        if c == '(':
            depth += 1
        elif c == ')':
            if skip_depth and skip_depth[-1] == depth: skip_depth.pop()
            depth -= 1
        if c == '"' or (c in '$@' and i + 1 < n and src[i + 1] in '"$@'):
            end, kind, raw, holes = lex_string(src, i)
            lit = src[i:end]
            if skip_depth or not CYR.search(raw):
                # nested Russian literals inside holes of an English-free string still get wrapped
                if kind == 'interp' and not skip_depth and holes:
                    lit = '$"' + rebuild(raw, holes, keys) + '"' if any(CYR.search(h) for h in holes) else lit
                out.append(lit); i = end; continue
            if kind == 'plain':
                keys.append(raw)
                out.append(f'Loc.T({lit})')
            else:
                hs = [convert(h, keys) for h in holes]
                keys.append(raw)
                out.append(f'Loc.F("{raw}", {", ".join(hs)})' if hs else f'Loc.T("{raw}")')
            i = end; continue
        out.append(c); i += 1
    return ''.join(out)


def rebuild(raw, holes, keys):
    hs = [convert(h, keys) for h in holes]
    return re.sub(r'\{(\d+)((?::[^}]*)?)\}', lambda m: '{' + hs[int(m.group(1))] + m.group(2) + '}', raw)


if __name__ == '__main__':
    args = sys.argv[1:]
    listing = args and args[0] == '--list'
    files = args[1:] if listing else args
    allkeys = []
    for f in files:
        src = open(f, encoding='utf-8-sig').read()
        keys = []
        dst = convert(src, keys)
        allkeys += keys
        if not listing and dst != src:
            open(f, 'w', encoding='utf-8').write(dst)
            print(f, len(keys))
    if listing:
        for k in dict.fromkeys(allkeys): print(k)
