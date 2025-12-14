# AI / MLP Guide

This guide explains how to train the Flappy Bird AI, modify the model architecture, and add new features.

## 1. Quick Start (Using Makefile)

The easiest way to update the AI is using the **root Makefile**.

### Train and Build (Recommended)
Run this command from the project root to train a new model from your recorded demos and compile the native library:
```bash
make update-model
```

### Debugging & Utilities
* **Debug Build**: `make build-debug` (Compiles with detailed logging enabled)
* **Check Weights**: `make check-weights` (Displays header of model file)
* **Run Game**: `make run` (Launches Godot with console output; adjust `GODOT_BIN` in Makefile if needed)

### Individual Steps
* **Train only**: `make train` (Generates `IA/SoftmaxC/model_weights.txt`)
* **Build only**: `make build-native` (Generates `IA/SoftmaxC/libsoftmodel.dylib`)

## 2. Customizing Training

You can customize the training parameters by overriding `TRAIN_ARGS`.

### Change Number of Neurons or Layers
You can now specify any number of hidden layers using the `--layers` argument.

To train with **one hidden layer** of 15 neurons:
```bash
make train TRAIN_ARGS="--epochs 3000 --layers 15"
make build-native
```

To train with **two hidden layers** (e.g., 10 neurons then 5 neurons):
```bash
make train TRAIN_ARGS="--epochs 2000 --layers 10 5"
make build-native
```

To train with **three hidden layers**:
```bash
make train TRAIN_ARGS="--epochs 2000 --layers 10 8 4"
```
*Note: The system automatically adds the final output neuron (1) to your topology.*

## 3. Adding New Features

If you want to add new inputs (e.g., "distance to next pipe"), you must update the entire pipeline:

1.  **Godot (`Root.cs`)**:
    *   Update `LoadNativeLibrary`: The delegate signature must match your new input count.
    *   Update `StartFlappy`: Add the new feature to the CSV header writing.
    *   Update `_Process`:
        *   Calculate the new feature value.
        *   Pass it to `_nativePredictFunc`.
        *   Write it to the `_recorder` (CSV file).

2.  **Python (`IA/Python/train_from_demos.py`)**:
    *   Update `load_all` to read the new column from the CSV.
    *   Normalized training will handle the new feature automatically.

3.  **C Model (`IA/SoftmaxC/model.c`)**:
    *   Update `predict` function signature to accept the new argument.
    *   Update the input array logic: `invals[...] = new_arg;`.

4.  **Rebuild**:
    *   Run `make update-model`.
