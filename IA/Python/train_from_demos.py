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
    """Parse command-line arguments for training configuration.
    
    Called by: main()
    Returns: argparse.Namespace with training hyperparameters (epochs, lr, layers, etc.)
    """
    p = argparse.ArgumentParser()
    p.add_argument("--data", help="Directory containing demos (default: this script folder)")
    p.add_argument("--out", help="Output native lib path (trainer will write model_weights.txt in same folder)")
    p.add_argument("--epochs", type=int, default=2000)
    p.add_argument("--lr", type=float, default=0.02)
    p.add_argument("--layers", type=int, nargs='+', default=[16, 8], help="List of hidden layer sizes (e.g. 10 5)")
    p.add_argument("--l2", type=float, default=1e-5)
    p.add_argument("--seed", type=int, default=42)
    return p.parse_args()


def parse_csv_line(line: str, expected_cols: int) -> List[str]:
    """Parse a semicolon-separated CSV line into fields.
    
    Called by: load_all()
    Args:
        line: Raw CSV line string
        expected_cols: Expected number of columns (for validation)
    Returns: List of string fields
    """
    return line.split(';')

def to_float(s: str) -> float:
    """Convert string to float, handling both comma and dot decimal separators.
    
    Called by: load_all()
    Args:
        s: String representation of a number
    Returns: Float value
    """
    return float(s.replace(',', '.'))

def load_all() -> Tuple[List[List[float]], List[int]]:
    """Load all demo CSV files and extract features and labels.
    
    Called by: main()
    Reads all demos_*.csv files from the PATTERN directory.
    Parses each row to extract:
    - Original features: fh, fx, vs, dr, dx, oy, passes
    - Derived features: dist_bottom, dist_top, tti
    - Action label: 0 (no jump) or 1 (jump)
    
    Currently uses 7 features (excluding fh, fx, oy) for training.
    
    Returns:
        Tuple of (X, y) where:
        - X: List of feature vectors (each is 7 floats)
        - y: List of action labels (0 or 1)
    """
    X: List[List[float]] = []
    y: List[int] = []
    
    for path in glob.glob(PATTERN):
        print(f"Loading {path}...")
        with open(path, 'r') as f:
            header_line = f.readline().lower()
            header_fields = [f.strip() for f in header_line.split(';')]
            expected_fields = 9  # time + 7 features + action
            
            # Validate header column count
            if len(header_fields) != expected_fields:
                raise ValueError(
                    f"ERROR: CSV format mismatch in {path}!\n"
                    f"Expected {expected_fields} columns, found {len(header_fields)}.\n"
                    f"Header: {header_line.strip()}\n"
                    f"Please delete outdated CSV files and generate new training data."
                )
            
            for line in f:
                line = line.strip()
                if not line:
                    continue
                
                parts = parse_csv_line(line, expected_fields)
                
                try:
                    if len(parts) < expected_fields:
                        raise ValueError(
                            f"ERROR: Data row has {len(parts)} columns, expected {expected_fields}.\n"
                            f"File: {path}\n"
                            f"Line: {line[:100]}...\n"
                            f"Please delete outdated CSV files."
                        )
                    
                    fh = to_float(parts[1]); fx = to_float(parts[2]); vs = to_float(parts[3])
                    dr = to_float(parts[4]); dx = to_float(parts[5]); oy = to_float(parts[6]); passes = to_float(parts[7]); act = int(parts[8])
                    
                    # Derived features
                    # Gap ~ 30, Speed ~ 40 (hardcoded here based on Godot project)
                    gap = 30.0
                    speed = 40.0
                    
                    dist_bottom = fh - oy
                    dist_top = (oy + gap) - fh
                    tti = dx / speed if speed > 0 else 0
                    
                    # Total 7 inputs: vs, dr, dx, passes, dist_bottom, dist_top, tti
                    # X.append([fh, fx, vs, dr, dx, oy, passes, dist_bottom, dist_top, tti])  # Old 10-feature version
                    X.append([vs, dr, dx, passes, dist_bottom, dist_top, tti])
                    y.append(act)
                except (ValueError, IndexError) as e:
                    # Re-raise ValueError for format errors, skip other parsing errors
                    if isinstance(e, ValueError) and "ERROR:" in str(e):
                        raise
                    pass
    
    return X, y


def normalize(X: List[List[float]], eps=1e-6):
    """Normalize features using z-score normalization (mean=0, std=1).
    
    Called by: main()
    Computes mean and standard deviation for each feature across all samples,
    then normalizes each feature to have zero mean and unit variance.
    
    Args:
        X: List of feature vectors
        eps: Minimum std to avoid division by zero (default: 1e-6)
    
    Returns:
        Tuple of (Xn, means, stds) where:
        - Xn: Normalized feature vectors
        - means: Mean of each feature (needed for C model normalization)
        - stds: Std dev of each feature (needed for C model normalization)
    """
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


def balance_data(X: List[List[float]], y: List[int], seed: int=42) -> Tuple[List[List[float]], List[int]]:
    """Balance the dataset by oversampling the minority class (jumps).
    
    Called by: main()
    Since the Flappy Bird dataset typically has many more 'no jump' (0) samples
    than 'jump' (1) samples, this function oversamples the positive class to
    achieve roughly equal representation.
    
    Args:
        X: Feature vectors
        y: Labels (0 or 1)
        seed: Random seed for reproducibility
    
    Returns:
        Tuple of (X_balanced, y_balanced) with equal class representation
    """
    if not X:
        return X, y
    
    pos_indices = [i for i, label in enumerate(y) if label == 1]
    neg_indices = [i for i, label in enumerate(y) if label == 0]
    
    n_pos = len(pos_indices)
    n_neg = len(neg_indices)
    

    
    if n_pos == 0 or n_neg == 0:
        return X, y # Cannot balance
    
    # Target: roughly equal counts
    target = n_neg
    if n_pos < target:
        # Oversample positives
        rnd = random.Random(seed)
        while len(pos_indices) < target:
            pos_indices.append(rnd.choice(pos_indices[:n_pos]))
            
    # Combine indices
    all_indices = pos_indices + neg_indices
    random.Random(seed).shuffle(all_indices)
    
    X_bal = [X[i] for i in all_indices]
    y_bal = [y[i] for i in all_indices]
    

    return X_bal, y_bal

def make_mlp(n_in: int, layer_sizes: List[int], seed: int=42):
    """Initialize a Multi-Layer Perceptron with random weights.
    
    Called by: train_mlp()
    Creates the network architecture with:
    - Input layer: n_in neurons
    - Hidden layers: sizes specified in layer_sizes (e.g., [16, 8])
    - Output layer: 1 neuron (binary classification)
    
    Activation functions:
    - Hidden layers: tanh
    - Output layer: sigmoid (for binary classification)
    
    Args:
        n_in: Number of input features
        layer_sizes: List of hidden layer sizes (e.g., [16, 8] for two hidden layers)
        seed: Random seed for weight initialization
    
    Returns:
        Tuple of (W, b) where:
        - W: List of weight matrices, W[i] connects layer i to layer i+1
        - b: List of bias vectors, b[i] for layer i+1
    """
    rnd = random.Random(seed)
    def randw():
        return rnd.uniform(-0.1, 0.1)
    
    # Structure:
    # Layers: [n_in, h1, h2, ..., hn, 1] (binary classification output is 1)
    # We store weights W[i] connecting layer i to i+1
    # Biases b[i] for layer i+1
    
    # Full topology including input and output
    # Input dim is n_in. Output dim is 1.
    topology = [n_in] + layer_sizes + [1]
    
    W = []
    b = []
    
    for i in range(len(topology) - 1):
        din = topology[i]
        dout = topology[i+1]
        # Weights: dout rows x din columns
        w_mat = [[randw() for _ in range(din)] for _ in range(dout)]
        b_vec = [0.0] * dout
        W.append(w_mat)
        b.append(b_vec)
        
    return W, b


def sigmoid(x):
    """Sigmoid activation function with numerical stability.
    
    Called by: forward_sample()
    Used for the output layer in binary classification.
    Handles large positive/negative values to avoid overflow.
    
    Args:
        x: Input value
    
    Returns:
        Value in range (0, 1)
    """
    if x >= 0:
        return 1.0 / (1.0 + math.exp(-x))
    else:
        expx = math.exp(x)
        return expx / (1.0 + expx)


def forward_sample(x, W, b):
    """Perform forward pass through the MLP for a single sample.
    
    Called by: train_mlp() during training
    Computes the network output and stores all layer activations for backpropagation.
    
    Activation functions:
    - Hidden layers: tanh
    - Output layer: sigmoid
    
    Args:
        x: Input feature vector
        W: List of weight matrices
        b: List of bias vectors
    
    Returns:
        Tuple of (p, activations) where:
        - p: Predicted probability (0 to 1)
        - activations: List of activation vectors for each layer (needed for backprop)
    """
    # x: input vector (list)
    # W: list of weight matrices
    # b: list of bias vectors
    
    # activations[0] = input x
    # activations[i] = output of layer i (after activation)
    activations = [x]
    
    # raw_outputs[i] = W[i]*a[i] + b[i] (before activation)
    # We strictly don't need to store raw outputs for standard backprop if we compute gradients directly,
    # but for clarity usually we need them or just activations.
    # The standard backprop uses 'delta' which relates to activation derivative.
    
    current_act = x
    
    # Iterate through all layers
    # Last layer is Sigmoid, others are Tanh
    num_layers = len(W)
    
    for i in range(num_layers):
        w_mat = W[i]
        b_vec = b[i]
        next_act = [0.0] * len(b_vec)
        
        for r in range(len(b_vec)):
            s = b_vec[r]
            row = w_mat[r]
            for c, val in enumerate(current_act):
                s += row[c] * val
            
            # Activation function
            if i == num_layers - 1:
                # Output layer: Sigmoid
                next_act[r] = sigmoid(s)
            else:
                # Hidden layer: Tanh
                next_act[r] = math.tanh(s)
        
        activations.append(next_act)
        current_act = next_act
        
    # Final output is the single element in the last activation vector
    p = current_act[0]
    return p, activations


def train_mlp(Xn, y, layer_sizes: List[int], epochs=500, lr=0.01, l2=1e-4):
    """Train the MLP using batch gradient descent with backpropagation.
    
    Called by: main()
    Implements:
    - Binary cross-entropy loss
    - Class weighting to handle imbalanced data
    - L2 regularization to prevent overfitting
    - Gradient clipping for stability
    - Backpropagation through time for all layers
    
    Args:
        Xn: Normalized feature vectors
        y: Labels (0 or 1)
        layer_sizes: Hidden layer sizes (e.g., [16, 8])
        epochs: Number of training iterations
        lr: Learning rate
        l2: L2 regularization coefficient
    
    Returns:
        Tuple of (W, b) - trained weights and biases
    """
    n_in = len(Xn[0]) if Xn else 0
    if not Xn or not y or len(Xn) != len(y):
        print(f"ERROR: Data mismatch: X={len(Xn)}, y={len(y)}")
        return None, None
    
    W, b = make_mlp(n_in, layer_sizes)
    m = len(Xn)
    num_layers = len(W)
    
    # Compute class weights
    pos_count = sum(y)
    neg_count = m - pos_count
    weight_pos = neg_count / pos_count if pos_count > 0 else 1.0
    weight_neg = 1.0
    

    
    for epoch in range(epochs):
        # Accumulate gradients
        dW = []
        db = []
        # Initialize zero gradients matching shapes
        for k in range(num_layers):
            rows = len(W[k])
            cols = len(W[k][0])
            dW.append([[0.0]*cols for _ in range(rows)])
            db.append([0.0]*rows)
            
        loss = 0.0
        
        for xi, yi in zip(Xn, y):
            p, activations = forward_sample(xi, W, b)
            
            # Loss and initial error (delta) at output
            # Binary Cross Entropy with Logits/Sigmoid simplification
            # dL/dz = p - y (for sigmoid + BCE)
            diff = p - yi
            
            # Apply class weight
            weight = weight_pos if yi == 1 else weight_neg
            loss += weight * (- (yi*math.log(max(p,1e-12)) + (1-yi)*math.log(max(1-p,1e-12))))
            diff = diff * weight
            
            # Backpropagation
            # Delta for output layer (index num_layers-1)
            # Since output is scalar for this problem (1 neuron), delta is just [diff]
            delta = [diff]
            
            # We iterate backwards from last layer to first
            # Layer index k goes: num_layers-1, ..., 0
            for k in range(num_layers - 1, -1, -1):
                # Current layer k connects activations[k] to activations[k+1]
                # activations[k] is input to this layer
                # delta is dL/dz_{k+1} (error at output of this layer)
                
                # Computed dW[k] and db[k]
                prev_a = activations[k] # Input to this layer
                
                rows = len(W[k])
                cols = len(W[k][0])
                
                # Gradients for W[k], b[k]
                # dW = delta * prev_a.T
                for r in range(rows):
                    d_val = delta[r]
                    db[k][r] += d_val
                    for c in range(cols):
                        dW[k][r][c] += d_val * prev_a[c]
                
                # Compute delta for previous layer (k-1) if k > 0
                if k > 0:
                    # delta_prev = (W[k].T @ delta) * sigma'(z_{k})
                    # since activations are Tanh, sigma'(z) = 1 - a^2
                    # activations[k] is the output of layer k-1 (the input to k)
                    # Wait, activations indices:
                    # a[0] = inputs
                    # a[1] = output of layer 0
                    # ...
                    # a[k] = output of layer k-1 -> This is what we need derivative for
                    
                    next_delta = [0.0] * cols # size of input to this layer
                    
                    # Backpropagate through weights
                    for r in range(rows):
                        d_val = delta[r]
                        row_w = W[k][r]
                        for c in range(cols):
                            next_delta[c] += row_w[c] * d_val
                    
                    # Multiply by derivative of activation function of layer (k-1)
                    # which produced activations[k]
                    # Since it's hidden layer (k>0 implies layer k-1 was hidden or input.. wait)
                    # if k=1, layer 0 is hidden? YES.
                    # Layer 0 connects Input -> Hidden1. Output is Tanh.
                    # So derivative is 1 - a^2
                    current_layer_input_act = activations[k]
                    for c in range(len(next_delta)):
                        val = current_layer_input_act[c]
                        next_delta[c] *= (1.0 - val*val)
                        
                    delta = next_delta

        # Apply gradients with clipping and L2
        clip = 5.0
        for k in range(num_layers):
            rows = len(W[k])
            cols = len(W[k][0])
            for r in range(rows):
                grad_b = db[k][r] / m
                if grad_b > clip: grad_b = clip
                elif grad_b < -clip: grad_b = -clip
                b[k][r] -= lr * grad_b
                
                for c in range(cols):
                    grad_w = dW[k][r][c] / m + l2 * W[k][r][c]
                    if grad_w > clip: grad_w = clip
                    elif grad_w < -clip: grad_w = -clip
                    W[k][r][c] -= lr * grad_w
                    
        if (epoch + 1) % max(1, epochs // 10) == 0:
            print(f"  Epoch {epoch+1}/{epochs}, loss={loss/m:.4f}")
            
    return W, b


def write_model(W, b, means, stds, outpath):
    """Write trained model to text file in format readable by C model loader.
    
    Called by: main()
    Writes to ../SoftmaxC/model_weights.txt in the following format:
    - Line 1: MLP <n_in> <n_layers> <layer_sizes...>
    - Line 2: means (space-separated)
    - Line 3: stds (space-separated)
    - Then for each layer: weight matrix rows, then bias vector
    
    The C model (model.c) reads this file to perform inference in Godot.
    
    Args:
        W: List of weight matrices
        b: List of bias vectors
        means: Feature means (for normalization)
        stds: Feature standard deviations (for normalization)
        outpath: Output file path
    """
    d = os.path.dirname(outpath)
    if not os.path.exists(d): os.makedirs(d)
    
    n_in = len(W[0][0])
    num_layers = len(W)
    # Output layer sizes
    layer_sizes = [len(w) for w in W]
    
    with open(outpath, 'w') as f:
        # Header: MLP <n_in> <n_layers> <layer_size_1> <layer_size_2> ... <layer_size_out>
        # Note: layer_sizes includes the output layer size (which is 1)
        header_sizes = " ".join(str(s) for s in layer_sizes)
        f.write(f"MLP {n_in} {num_layers} {header_sizes}\n")
        
        f.write(' '.join(str(x) for x in means) + "\n")
        f.write(' '.join(str(x) for x in stds) + "\n")
        
        for k in range(num_layers):
            # Write Weights (rows)
            for r in range(len(W[k])):
                f.write(' '.join(str(x) for x in W[k][r]) + "\n")
            # Write Bias
            f.write(' '.join(str(x) for x in b[k]) + "\n")


def main():
    """Main training pipeline.
    
    Orchestrates the complete training workflow:
    1. Load CSV demo files
    2. Balance the dataset (oversample jumps)
    3. Normalize features
    4. Train MLP with backpropagation
    5. Write model weights to file
    6. Optionally build native C library
    
    The trained model is used by the C inference code (model.c) in Godot
    to enable the "Play like me" AI mode.
    """
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
        print("No data found!")
        sys.exit(1)
        
    # Balance data (oversample jumps)
    X, y = balance_data(X, y, args.seed)
    
    # Normalize
    Xn, means, stds = normalize(X)
    print(f"Training on {len(Xn)} samples with {len(Xn[0])} features")
    
    # Argparse default was 0.
    W, b = train_mlp(Xn, y, args.layers, epochs=args.epochs, lr=args.lr, l2=args.l2)
    write_model(W, b, means, stds, OUT)
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
