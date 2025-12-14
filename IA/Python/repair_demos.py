#!/usr/bin/env python3
"""
Repair demos_*.csv files where decimal comma characters were inserted as CSV separators.
Produces backups with `.bak` and overwrites originals with repaired CSV using dot decimals.
"""
import glob
import os

PATTERN = os.path.join(os.path.dirname(__file__), "demos_*.csv")


def repair_line(line):
    line = line.strip()
    if not line:
        return line
    # header
    if line.startswith("time"):
        return line
    parts = line.split(',')
    # if already 8 fields, assume ok
    if len(parts) == 8:
        return line
    # try to merge tokens in pairs: int,frac -> int.frac
    repaired = []
    i = 0
    n = len(parts)
    while i < n and len(repaired) < 8:
        a = parts[i]
        if i + 1 < n:
            b = parts[i+1]
            # if both are numeric-like, merge as a.b
            if a.replace('.', '', 1).isdigit() and b.replace('.', '', 1).isdigit():
                # join using dot
                repaired.append(a + '.' + b)
                i += 2
                continue
        # fallback: take single token
        repaired.append(a)
        i += 1
    # if we didn't reach 8 fields, try appending remaining tokens joined
    if len(repaired) < 8 and i < n:
        rest = ''.join(parts[i:])
        repaired.append(rest)
    # if still not 8, just return original line
    if len(repaired) != 8:
        return line
    return ','.join(repaired)


def repair_file(path):
    print('Repairing', path)
    bak = path + '.bak'
    if not os.path.exists(bak):
        os.rename(path, bak)
    else:
        # original already backed up; operate on current file
        pass
    with open(bak, 'r') as f_in, open(path, 'w') as f_out:
        for line in f_in:
            f_out.write(repair_line(line) + '\n')


def main():
    files = glob.glob(PATTERN)
    if not files:
        print('No demo files found to repair (pattern:', PATTERN, ')')
        return
    for p in files:
        try:
            repair_file(p)
        except Exception as e:
            print('Failed to repair', p, e)

if __name__ == '__main__':
    main()
