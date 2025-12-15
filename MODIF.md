Manual Changes Required
---
1. Root.cs - Stop recording these fields in CSV
- Remove flappyHeight and flappyX from the CSV header (line ~221)
- Remove them from the data writing line (line ~366)
2. train_from_demos.py - Update parsing logic
- Update expected_fields from 9 to 7 (since you're removing 2 columns)
- Remove parsing of fh and fx (currently at parts[1] and parts[2])
- Adjust the indices for remaining fields
- Update the feature list to exclude fh and fx when appending to X
3. model.c - Update prediction function signature
- Remove fh and fx parameters from the predict() function
- Update mlp_nin expectation (will be 8 instead of 10)
- Remove them from the raw_in array population
4. Root.cs - Update native model call
- Remove _output.FlappyHeight and _output.FlappyX from the _nativePredictFunc() call
- Update the delegate signature to match

Visualizer Changes
---
1. viz_flappy.c
- Update the network topology from 10-16-8-2 to 8-16-8-2 (8 inputs instead of 10)
- Remove fh and fx from the input neuron creation
- Update the data loading to skip the fh and fx columns from viz_data.txt
2. viz_learning.py
- In prepare_flappy_data(): Update the data writing to exclude fh and fx when writing to viz_data.txt
- The script already uses train_from_demos.load_all(), so once you update that function, it should automatically get the new 8-feature format
3. flappy_viewer.html
- Update the input legend to remove the fh and fx entries
- Change from 10 inputs to 8 inputs in the legend display
The visualizer will automatically adapt to the new network size since it uses dynamic layout, but you need to:
- Update the input neuron names in viz_flappy.c
- Update the legend in flappy_viewer.html to match the new 8 features
