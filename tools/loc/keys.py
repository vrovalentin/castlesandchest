# Lists every Russian text the game shows (the keys of Resources/Lang/en.txt), in C# escaped form, one per line.
# Usage: python3 keys.py <Scripts folder>
import re, sys, glob, os
root = sys.argv[1]
src = {f: open(f, encoding='utf-8-sig').read() for f in glob.glob(os.path.join(root, '**', '*.cs'), recursive=True)}
S = r'"((?:[^"\\]|\\.)*)"'
keys = []
for f, s in sorted(src.items()):
    s = re.sub(r'^\s*//.*$', '', s, flags=re.M)
    for m in re.finditer(r'Loc\.(?:T|F)\(\s*' + S, s): keys.append(m.group(1))
    for m in re.finditer(r'Plural\([^,()]+,\s*' + S + r',\s*' + S + r',\s*' + S, s): keys.append('|'.join(m.groups()))
    if f.endswith(('DemoContent.cs', 'Residents.cs', 'Palette.cs')):
        for m in re.finditer(S, s):
            if re.search('[А-Яа-яЁё]', m.group(1)): keys.append(m.group(1))
for k in dict.fromkeys(keys):
    if re.search('[А-Яа-яЁё]', k): print(k)
