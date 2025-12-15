#!/usr/bin/env python3
import subprocess
import os
import sys
import webbrowser
import json

# Ensure we are in the script directory
HERE = os.path.dirname(os.path.abspath(__file__))
os.chdir(HERE)

def run_viz_test(lr=0.5, epochs=2000, seed=42, snapshot_interval=50, mode='xor'):
    # Ensure binary exists
    target = 'viz' if mode == 'xor' else 'viz_flappy'
    binary = './viz_test' if mode == 'xor' else './viz_flappy'
    
    if not os.path.exists(binary):
        print(f"Building {target}...")
        subprocess.check_call(['make', target])

    print(f"Running {binary} (lr={lr}, epochs={epochs})...")
    # snapshot interval passed as arg 4, data file as arg 5 (for flappy)
    if mode == 'xor':
        cmd = [binary, str(lr), str(epochs), str(seed), str(snapshot_interval)]
    else:
        cmd = [binary, str(lr), str(epochs), str(seed), str(snapshot_interval), 'viz_data.txt']
    
    # Run and capture output
    # Use PIPE and communicate() to handle large output properly
    process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    stdout, stderr = process.communicate()
    
    if process.returncode != 0:
        print(f"Error running {binary}:")
        print(stderr)
        sys.exit(1)
    
    # Print stderr (contains progress messages) to show what happened
    if stderr:
        print(stderr, file=sys.stderr)
        
    return stdout

def generate_advanced_html(json_output, template_path, output_path='network_learning.html'):
    try:
        # The C output is a stream of JSON lines wrapped in [ ... ]?
        # Actually I made it output a valid JSON array.
        snapshots = json.loads(json_output)
    except json.JSONDecodeError as e:
        print("Failed to parse JSON output from C:", e)
        print("Raw output start:", json_output[:200])
        sys.exit(1)

    if not os.path.exists(template_path):
        print(f"Template not found: {template_path}")
        sys.exit(1)

    with open(template_path, 'r') as f:
        html = f.read()

    # Inject data
    # JSON dump to ensure valid JS syntax
    data_js = json.dumps(snapshots)
    html = html.replace('__SNAPSHOTS__', data_js)
    
    with open(output_path, 'w') as f:
        f.write(html)
    
    print(f"Generated report: {os.path.abspath(output_path)}")
    return output_path

import argparse
# Add parent dir to path to import train_from_demos
sys.path.append(os.path.join(HERE, "..", "Python"))
import train_from_demos

def prepare_flappy_data():
    print("Loading demos...")
    # Use the logic from train_from_demos
    # We need to temporarily hijack the PATTERN globally or just rely on it finding files in ../Python
    # train_from_demos.PATTERN is absolute path, so it should be fine if we set it?
    # Actually train_from_demos.PATTERN uses os.dirname(__file__) which is ../Python.
    # So it should find the csvs there.
    
    X, y = train_from_demos.load_all()
    if not X:
        print("No demo data found!")
        sys.exit(1)
        
    # Balance
    X, y = train_from_demos.balance_data(X, y)
    
    # Normalize
    Xn, means, stds = train_from_demos.normalize(X)
    
    # Write to viz_data.txt
    out_path = os.path.join(HERE, "viz_data.txt")
    print(f"Writing {len(Xn)} samples to {out_path}...")
    with open(out_path, 'w') as f:
        for i in range(len(Xn)):
            # join inputs with space
            line = " ".join(str(v) for v in Xn[i])
            # append target
            line += f" {y[i]}\n"
            f.write(line)
            
    return out_path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--flappy', action='store_true', help='Visualize Flappy Bird training')
    parser.add_argument('--lr', type=float, default=0.5)
    parser.add_argument('--epochs', type=int, default=2000)
    args = parser.parse_args()

    mode = 'flappy' if args.flappy else 'xor'
    
    if mode == 'flappy':
        prepare_flappy_data()
        # Lower default LR for flappy typically? Or high? C impl uses 0.05 default.
        if args.lr == 0.5: args.lr = 0.05 
        
    output = run_viz_test(lr=args.lr, epochs=args.epochs, snapshot_interval=50, mode=mode)
    
    if mode == 'xor':
        template_name = 'xor_viewer.html'
        output_name = 'network_learning_xor.html'
    else:
        template_name = 'flappy_viewer.html'
        output_name = 'network_learning_flappy.html'

    template = os.path.join(HERE, 'templates', template_name)
    if not os.path.exists(template):
        print(f"Template missing: {template}")
        return

    report_file = generate_advanced_html(output, template, output_name)
    
    print("Opening report in browser...")
    webbrowser.open('file://' + os.path.abspath(report_file))

if __name__ == '__main__':
    main()
