# AI Coding Agent Instructions

## Project Overview
This repository implements a **Hybrid Flappy Bird Simulation** using a shared C# logic core integrated into both **Godot 4 (.NET)** and **Unity**. It also features an AI training and inference pipeline using Python and C.

## Architecture & Code Organization
- **Shared Logic (`core-dotnet/FlappyCore`)**: 
  - Contains all game logic (physics, collision, state). **Engine-agnostic**: Must NOT reference Godot or Unity APIs.
  - **Key File**: `FlappyEntry.cs` handles the update loop (`Init`, `Update`, `Reset`).
  - **Data**: Exchanges `InputData` and `OutputData` structs with the engines.
- **Godot Project (`godot/`)**:
  - `Root.cs`: Main coordinator. Handles input, UI, rendering (via `_Draw`), and AI integration.
  - Uses `FlappyCore.dll` for logic.
- **Unity Project (`Unity/`)**:
  - Uses `FlappyCore.dll` (in `Assets/Plugins`) for logic.
- **AI Module (`IA/`)**:
  - `IA/Python/`: scripts for training (`train_from_demos.py`) and recording demos.
  - `IA/SoftmaxC/`: C code for the native inference library (`libsoftmodel.dylib`).

## Critical Workflows
### 1. Modifying Game Logic
1.  Edit `core-dotnet/FlappyCore/*.cs`.
2.  **Build**: `dotnet build core-dotnet/FlappyCore`.
3.  **Deploy**: Copy `FlappyCore.dll` to:
    - `godot/bin/` (or wherever Godot scripts expect it).
    - `Unity/Assets/Plugins/`.

### 2. AI Training & Inference
1.  **Record**: Play the game in Godot (demos saved to `IA/Python/demos_*.csv`).
2.  **Train**: Run `python IA/Python/train_from_demos.py` -> produces `model_weights.txt`.
3.  **Build Native Lib**:
    - `cd IA/SoftmaxC`
    - `make` (produces `libsoftmodel.dylib`)
    - Godot loads this dylib dynamically for "Play like me" features.

## Conventions & Patterns
- **Path Handling**: The Godot `Root.cs` assumes relative paths to `../IA/` for AI integration. Maintain this directory structure.
- **Native Interop**: `Root.cs` uses `NativeLibrary.Load` to load the C model. Ensure the dylib path candidates in `LoadNativeLibrary()` match the actual file location.
- **Godot Signals**: UI events (Play, Train) are wired manually in `_Ready` or via Godot editor signals. Prefer C# wiring for clarity in `Root.cs`.
- **Performance**: The core simulation must remain lightweight. Avoid heavy allocations in `FlappyEntry.Update`.

## Key Commands
- **Build C Lib**: `make` (inside `IA/SoftmaxC`)
- **Run Python Tests**: `make test_ia_lib` (from root)
