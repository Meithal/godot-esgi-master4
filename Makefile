.PHONY: test_ia_lib clean run_godot train build-native build-debug build-godot

test_ia_lib:
	rm -f *.gv
	rm -f *.gv.png
	./IA/Python/venv/bin/python -m unittest ./IA/Python/test.py

clean:
	rm *.gv
	rm *.gv.png

# AI / MLP Workflow
PYTHON ?= ./IA/Python/venv/bin/python
TRAIN_ARGS ?= --epochs 2000 --layers 10

# Train the model from demos
train:
	$(PYTHON) IA/Python/train_from_demos.py $(TRAIN_ARGS)

# Build the native C library (requires make and gcc)
# Build the native C library (requires make and gcc)
build-native:
	$(MAKE) -C IA/SoftmaxC

# Build the native C library with DEBUG logs enabled
build-debug:
	$(MAKE) -C IA/SoftmaxC CFLAGS="-std=c11 -O2 -DDEBUG_PRINTS"

clean-native:
	$(MAKE) -C IA/SoftmaxC clean

# Full update: train then build
update-model: train build-native

# Utility: Check the first few lines of the weights file
check-weights:
	head -n 5 IA/SoftmaxC/model_weights.txt

# Build Godot C# project
build-godot:
	@echo "Building Godot C# project..."
	@$(GODOT_BIN) --headless --path ./godot --build-solutions --quit || true

# Run Godot (assumes standard Mac path, override if different)
GODOT_BIN ?= /Applications/Godot_mono.app/Contents/MacOS/Godot
run-godot: build-godot
	$(GODOT_BIN) --path ./godot

viz-flappy:
	$(MAKE) -C IA/SoftmaxC run_viz_flappy