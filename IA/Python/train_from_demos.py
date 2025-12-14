#!/usr/bin/env python3
"""
Train a small MLP (two hidden layers) on all demos_*.csv files.
Saves a normalized model file to ../SoftmaxC/model_weights.txt with the following text format:

First line: MLP n_input h1 h2
Second line: means (space separated)
Third line: stds (space separated)
Then W1 rows (h1 rows, each n_input values), then b1 (h1 values)
Then W2 rows (h2 rows, each h1 values), then b2 (h2 values)
Then W3 row (1 row, h2 values), then b3 (single value)

The native model expects inputs in original units; the model file contains means/stds so the native loader will normalize inputs before forward.

No external deps. Uses simple batch gradient descent with L2 regularization.
"""
import os
import glob
import math
import sys
import argparse
import subprocess
import shutil
import random
from typing import List, Tuple

HERE = os.path.dirname(__file__)
PATTERN = os.path.join(HERE, "demos_*.csv")
# Default output weights path
OUT = os.path.join(HERE, "..", "SoftmaxC", "model_weights.txt")


def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--data", help="Directory containing demos (default: this script folder)")
    p.add_argument("--out", help="Output native lib path (trainer will write model_weights.txt in same folder)")
    p.add_argument("--epochs", type=int, default=1000)
    p.add_argument("--lr", type=float, default=0.1)
    p.add_argument("--h1", type=int, default=16)
    p.add_argument("--h2", type=int, default=8)
    p.add_argument("--l2", type=float, default=1e-4)
    p.add_argument("--seed", type=int, default=42)
    return p.parse_args()


def load_all() -> Tuple[List[List[float]], List[int]]:
    X: List[List[float]] = []
    y: List[int] = []
    for path in glob.glob(PATTERN):
        with open(path, 'r') as f:
            header = f.readline().lower()
            has_passes = 'passes' in header
            for line in f:
                line = line.strip()
                if not line:
                    continue
                parts = line.split(',')
                if has_passes:
                    if len(parts) < 9:
                        continue
                    fh = float(parts[1]); fx = float(parts[2]); vs = float(parts[3]); dr = float(parts[4]); dx = float(parts[5]); oy = float(parts[6]); passes = float(parts[7]); act = int(parts[8])
                    X.append([fh, fx, vs, dr, dx, oy, passes])
                    y.append(act)
                else:
                    if len(parts) < 8:
                        continue
                    fh = float(parts[1]); fx = float(parts[2]); vs = float(parts[3]); dr = float(parts[4]); dx = float(parts[5]); oy = float(parts[6]); act = int(parts[7])
                    X.append([fh, fx, vs, dr, dx, oy])
                    y.append(act)
    return X, y


def normalize(X: List[List[float]], eps=1e-6):
    if not X:
        return X, [], []
    n = len(X[0])
    m = len(X)
    means = [0.0]*n
    for row in X:
        for i in range(n):
            means[i] += row[i]
    means = [x/m for x in means]
    stds = [0.0]*n
    for row in X:
        for i in range(n):
            stds[i] += (row[i]-means[i])**2
    stds = [math.sqrt(s/m) for s in stds]
    # clamp stds to avoid divide-by-zero
    stds = [s if s > eps else 1.0 for s in stds]
    Xn = []
    for row in X:
        Xn.append([(row[i]-means[i])/stds[i] for i in range(n)])
    return Xn, means, stds


def make_mlp(n_in: int, h1: int, h2: int, seed: int=42):
    rnd = random.Random(seed)
    # small random init
    def randw():
        return rnd.uniform(-0.1, 0.1)
    W1 = [[randw() for _ in range(n_in)] for _ in range(h1)]
    b1 = [0.0]*h1
    W2 = [[randw() for _ in range(h1)] for _ in range(h2)]
    b2 = [0.0]*h2
    W3 = [[randw() for _ in range(h2)]]
    b3 = [0.0]
    return W1, b1, W2, b2, W3, b3


def sigmoid(x):
    return 1.0/(1.0+math.exp(-x))


def tanh_vec(x):
    return math.tanh(x)


def forward_sample(x, W1, b1, W2, b2, W3, b3):
    # x: list n_in
    # layer1
    h1 = [0.0]*len(b1)
    for i in range(len(b1)):
        s = b1[i]
        row = W1[i]
        for j,v in enumerate(x): s += row[j]*v
        h1[i] = math.tanh(s)
    # layer2
    h2 = [0.0]*len(b2)
    for i in range(len(b2)):
        s = b2[i]
        row = W2[i]
        for j,v in enumerate(h1): s += row[j]*v
        h2[i] = math.tanh(s)
    # out
    s = b3[0]
    for j,v in enumerate(h2): s += W3[0][j]*v
    p = sigmoid(s)
    return p, h1, h2


def train_mlp(Xn, y, h1, h2, epochs=500, lr=0.01, l2=1e-4):
    n_in = len(Xn[0])
    W1, b1, W2, b2, W3, b3 = make_mlp(n_in, h1, h2)
    m = len(Xn)
    for epoch in range(epochs):
        # batch gradient descent
        # zero grads
        dW1 = [[0.0]*n_in for _ in range(h1)]
        db1 = [0.0]*h1
        dW2 = [[0.0]*h1 for _ in range(h2)]
        db2 = [0.0]*h2
        dW3 = [[0.0]*h2]
        db3 = [0.0]
        loss = 0.0
        for xi, yi in zip(Xn, y):
            p, a1, a2 = forward_sample(xi, W1, b1, W2, b2, W3, b3)
            diff = p - yi
            loss += - (yi*math.log(max(p,1e-12)) + (1-yi)*math.log(max(1-p,1e-12)))
            # output layer grads
            for j in range(len(a2)):
                dW3[0][j] += diff * a2[j]
            db3[0] += diff
            # backprop to layer2
            delta2 = [0.0]*h2
            for i in range(h2):
                # sum over output weights
                delta2[i] = diff * W3[0][i] * (1.0 - a2[i]*a2[i])
            for i in range(h2):
                for j in range(h1):
                    dW2[i][j] += delta2[i] * a1[j]
                db2[i] += delta2[i]
            # backprop to layer1
            for i in range(h1):
                s = 0.0
                for k in range(h2):
                    s += W2[k][i] * delta2[k]
                delta1 = s * (1.0 - a1[i]*a1[i])
                for j in range(n_in):
                    dW1[i][j] += delta1 * xi[j]
                db1[i] += delta1
        # apply gradients (average and L2)
        for i in range(h1):
            for j in range(n_in):
                W1[i][j] -= lr * (dW1[i][j]/m + l2*W1[i][j])
            b1[i] -= lr * (db1[i]/m)
        for i in range(h2):
            for j in range(h1):
                W2[i][j] -= lr * (dW2[i][j]/m + l2*W2[i][j])
            b2[i] -= lr * (db2[i]/m)
        for j in range(h2):
            W3[0][j] -= lr * (dW3[0][j]/m + l2*W3[0][j])
        b3[0] -= lr * (db3[0]/m)
    return W1, b1, W2, b2, W3, b3


def write_model(W1, b1, W2, b2, W3, b3, means, stds, outpath):
    d = os.path.dirname(outpath)
    if not os.path.exists(d): os.makedirs(d)
    with open(outpath, 'w') as f:
        n_in = len(W1[0])
        h1 = len(W1)
        h2 = len(W2)
        f.write(f"MLP {n_in} {h1} {h2}\n")
        f.write(' '.join(str(x) for x in means) + "\n")
        f.write(' '.join(str(x) for x in stds) + "\n")
        # W1
        for i in range(h1):
            f.write(' '.join(str(x) for x in W1[i]) + "\n")
        f.write(' '.join(str(x) for x in b1) + "\n")
        # W2
        for i in range(h2):
            f.write(' '.join(str(x) for x in W2[i]) + "\n")
        f.write(' '.join(str(x) for x in b2) + "\n")
        # W3
        f.write(' '.join(str(x) for x in W3[0]) + "\n")
        f.write(str(b3[0]) + "\n")


def main():
    args = parse_args()
    if args.data:
        data_dir = os.path.abspath(args.data)
        global PATTERN
        PATTERN = os.path.join(data_dir, "demos_*.csv")
    lib_out = None
    global OUT
    if args.out:
        lib_out = os.path.abspath(args.out)
        soft_dir = os.path.dirname(lib_out)
        OUT = os.path.join(soft_dir, "model_weights.txt")

    X, y = load_all()
    if not X:
        print("No demo files found (pattern:", PATTERN, ")")
        return
    Xn, means, stds = normalize(X)
    print("Data shapes:", len(Xn), "samples x", len(Xn[0]), "features")
    print("means:", means)
    print("stds:", stds)
    W1, b1, W2, b2, W3, b3 = train_mlp(Xn, y, args.h1, args.h2, epochs=args.epochs, lr=args.lr, l2=args.l2)
    write_model(W1, b1, W2, b2, W3, b3, means, stds, OUT)
    print(f"Wrote MLP model to {OUT}")

    # build native lib if requested
    if lib_out:
        soft_dir = os.path.dirname(lib_out)
        build_sh = os.path.join(soft_dir, 'build.sh')
        makefile = os.path.join(soft_dir, 'Makefile')
        built = False
        try:
            if os.path.exists(build_sh):
                print("Running build.sh to build native library...")
                subprocess.check_call(["/bin/bash", build_sh], cwd=soft_dir)
                built = True
            elif os.path.exists(makefile):
                print("Running make to build native library...")
                subprocess.check_call(["make"], cwd=soft_dir)
                built = True
            else:
                print("No build.sh or Makefile in SoftmaxC; please build libsoftmodel.dylib manually or add a Makefile/build.sh.")
        except subprocess.CalledProcessError as e:
            print("Native build failed:", e)

        expected_lib = os.path.join(soft_dir, 'libsoftmodel.dylib')
        if os.path.exists(expected_lib):
            if os.path.abspath(expected_lib) != os.path.abspath(lib_out):
                try:
                    shutil.copyfile(expected_lib, lib_out)
                    print(f"Copied {expected_lib} -> {lib_out}")
                    built = True
                except Exception as e:
                    print("Failed to copy built library:", e)
        else:
            if built:
                print("Build attempted but libsoftmodel.dylib not found in SoftmaxC")
        if built:
            print("Native library build step completed (check libsoftmodel.dylib)")


if __name__ == '__main__':
    main()
