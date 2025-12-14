This folder contains a small C wrapper to run the trained logistic model as a native library for the Godot project.

Usage:
- Ensure `IA/Python/train_from_demos.py` writes `model_weights.txt` into this folder (it does by default).
- From this folder run:

```bash
./build.sh
```

- That will produce `libsoftmodel.dylib` which the Godot game will attempt to load when you press "Play like me".

Notes:
- The C `predict` function expects the features in the same (original) scale as the weights written by the Python trainer.
- If you change feature ordering or normalization, update both trainer and `softmax.c` accordingly.
