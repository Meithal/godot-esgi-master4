using Godot;
using System;
using FlappyCore;
using System.IO;
using System.Runtime.InteropServices;

/**
  script godot du nouveau flappy qui utilise le nouveau core (Flappy2)
*/
public partial class Root : Node2D
{
    [Export] private float _num_obstacles = 10;
    [Export] private float _gap = 30f;
    [Export] private float _obstacle_width = 10f;
    [Export] private float _max_y = 100f;
    [Export] private float _pipeExcess = 100f; // Dépassement des obstacles

    private FlappyEntry _core;
    private OutputData _output;

    private Node2D _bird;
    private Rect2[] _obstaclesBottom;
    private Rect2[] _obstaclesTop;

    private float _screenWidth;
    private float _screenHeight;

    private float birdOffsetX = 0f;
    private float unitsPerPixel = 0;

    // === UI ===
    private Control _menuPanel;
    private Control _gameOverPanel;

    private bool _isPlaying = false;

    // Recording / model
    private StreamWriter _recorder = null;
    [Export] private string _demoPath = "../IA/Python/demos.csv";
    [Export] private bool _useNativeModel = false; // set true to use native C model if available
    [Export] private bool _forceLoseNearRoof = false; // if true, recorded action will be forced to 0 near roof
    private const float NEAR_ROOF_THRESHOLD = 8f; // units

    // Native model interop (optional) - dynamic loader
    private IntPtr _nativeLibHandle = IntPtr.Zero;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // signature now includes 10 arguments
    // private delegate int PredictDelegate(double flappyHeight, double flappyX, double verticalSpeed, double distToRoof, double obs_dx, double obs_y, double passes, double distBottom, double distTop, double tti);
    private delegate int PredictDelegate(double verticalSpeed, double distToRoof, double obs_dx, double passes, double distBottom, double distTop, double tti);
    private PredictDelegate _nativePredictFunc = null;

    private bool LoadNativeLibrary()
    {
        string[] candidates = new string[] { "libsoftmodel.dylib", "../IA/SoftmaxC/libsoftmodel.dylib", "../../IA/SoftmaxC/libsoftmodel.dylib" };
        foreach (var p in candidates)
        {
            try
            {
                if (!File.Exists(p)) continue;
                _nativeLibHandle = NativeLibrary.Load(p);
                IntPtr fp = NativeLibrary.GetExport(_nativeLibHandle, "predict");
                _nativePredictFunc = Marshal.GetDelegateForFunctionPointer<PredictDelegate>(fp);
                GD.Print("Loaded native model from: " + p);
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr("Failed to load native lib '" + p + "': " + ex.Message);
                _nativePredictFunc = null;
                _nativeLibHandle = IntPtr.Zero;
            }
        }
        return false;
    }

    // Overload: try to load a native library at a specific path first
    private bool LoadNativeLibrary(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                if (File.Exists(path))
                {
                    _nativeLibHandle = NativeLibrary.Load(path);
                    IntPtr fp = NativeLibrary.GetExport(_nativeLibHandle, "predict");
                    _nativePredictFunc = Marshal.GetDelegateForFunctionPointer<PredictDelegate>(fp);
                    GD.Print("Loaded native model from: " + path);
                    return true;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("Failed to load native lib '" + path + "': " + ex.Message);
                _nativePredictFunc = null;
                _nativeLibHandle = IntPtr.Zero;
            }
        }
        // fallback to original search
        return LoadNativeLibrary();
    }

    private void UnloadNativeLibrary()
    {
        try
        {
            if (_nativeLibHandle != IntPtr.Zero)
            {
                NativeLibrary.Free(_nativeLibHandle);
                _nativeLibHandle = IntPtr.Zero;
                _nativePredictFunc = null;
            }
        }
        catch (Exception ex) { GD.PrintErr("Error unloading native lib: " + ex.Message); }
    }

    private Label _score_label;
    private long _lastNativeLogMs = 0;

    public override void _Ready()
    {
        base._Ready();

        _menuPanel = GetNode<Control>("CanvasLayer/MainMenu");
        _gameOverPanel = GetNode<Control>("CanvasLayer/Gameover");

        _menuPanel.Visible = true;
        _gameOverPanel.Visible = false;

        _menuPanel.GetNode<Button>("Play").Pressed += OnPlayPressed;
        _menuPanel.GetNode<Button>("Quit").Pressed += OnQuitPressed;
        _gameOverPanel.GetNode<Button>("replay").Pressed += OnReplayPressed;
        _gameOverPanel.GetNode<Button>("Mainmenu").Pressed += OnMainMenuPressed;

        // add UI buttons: native toggle, train, and play-like-me
        try
        {
            var nativeBtn = new Button();
            nativeBtn.Text = _useNativeModel ? "Native model: ON" : "Native model: OFF";
            nativeBtn.Pressed += () =>
            {
                _useNativeModel = !_useNativeModel;
                nativeBtn.Text = _useNativeModel ? "Native model: ON" : "Native model: OFF";
                if (_useNativeModel)
                {
                    if (!LoadNativeLibrary())
                    {
                        GD.PrintErr("Native model could not be loaded; disabling.");
                        _useNativeModel = false;
                        nativeBtn.Text = "Native model: OFF";
                    }
                }
                else
                {
                    UnloadNativeLibrary();
                }
            };
            nativeBtn.SetPosition(new Vector2(10, 200));
            _menuPanel.AddChild(nativeBtn);

            var trainBtn = new Button();
            trainBtn.Text = "Train from demos";
            trainBtn.SetPosition(new Vector2(10, 240));
            trainBtn.Pressed += () => { OnTrainPressed(); };
            _menuPanel.AddChild(trainBtn);

            var playBtn = new Button();
            playBtn.Text = "Play like me";
            playBtn.SetPosition(new Vector2(10, 280));
            playBtn.Pressed += () => { OnPlayLikeMePressed(); };
            _menuPanel.AddChild(playBtn);
        }
        catch (Exception) { /* ignore UI creation errors */ }

        GD.Print("[Root] En attente de lancement...");
    }

    // ================= GAMEPLAY =================
    private void StartFlappy()
    {
        GD.Print("[Root] Initialisation FlappyEntry...");
        _core = new FlappyEntry();
        _output = _core.Init(
            numObstacles: (int)_num_obstacles,
            gravity: 100f,
            obstacleSpeed: 40f,
            birdRadius: 1f,
            seed: 42
        );

        _bird = GetNode<Node2D>("%Bird");
        var sprite = _bird.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        sprite.Scale = new Vector2(2f, 2f);
        sprite.Centered = true;
        sprite.Position = Vector2.Zero;

        _screenWidth = GetViewport().GetVisibleRect().Size.X;
        _screenHeight = GetViewport().GetVisibleRect().Size.Y;
        unitsPerPixel = _screenWidth / _max_y;

        _obstaclesBottom = new Rect2[_output.Obstacles.Length];
        _obstaclesTop = new Rect2[_output.Obstacles.Length];

        _isPlaying = true;
        _menuPanel.Visible = false;
        _gameOverPanel.Visible = false;

        // create unique demo filename per play (skip if using native model)
        if (!_useNativeModel)
        {
            try
            {
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _demoPath = Path.Combine(Path.GetFullPath(".."), "IA", "Python", $"demos_{ts}.csv");
                var dir = Path.GetDirectoryName(_demoPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                bool writeHeader = !File.Exists(_demoPath);
                _recorder = new StreamWriter(_demoPath, append: true);
                if (writeHeader)
                {
                    _recorder.WriteLine("time;flappyHeight;flappyX;verticalSpeed;distToRoof;nearest_dx;nearest_y;passes;action");
                    _recorder.Flush();
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("Could not open demo file for writing: " + ex.Message);
                _recorder = null;
            }
        }
        else
        {
            GD.Print("[Root] Native model active — skipping CSV recording.");
            _recorder = null; // Ensure recorder is null
        }

        _score_label = GetNode<Label>("%LabelScore");

        GD.Print("[Root] Jeu démarré !");
    }

    private void ResetFlappy()
    {
        GD.Print("[Root] Réinitialisation du jeu...");
        _core.Reset();
        _isPlaying = true;
        _menuPanel.Visible = false;
        _gameOverPanel.Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_isPlaying || _core == null) return;

        // compute features from last output (previous state) to let model decide current input
        double nearest_dx = 9999.0;
        double nearest_y = 0.0;
        for (int i = 0; i < _output.Obstacles.Length; i++)
        {
            var o = _output.Obstacles[i];
            if (o.X <= _output.FlappyX) continue;
            double dx = o.X - _output.FlappyX;
            if (dx < nearest_dx) { nearest_dx = dx; nearest_y = o.Y; }
        }
        double distToRoof = _max_y - _output.FlappyHeight;
        double verticalSpeed = _output.FlappyVerticalSpeed;

        // Derived features
        double gap = _gap;
        double speed = 40.0; // Assume constant speed from Init
        double distBottom = _output.FlappyHeight - nearest_y;
        double distTop = (nearest_y + gap) - _output.FlappyHeight;
        double tti = speed > 0 ? nearest_dx / speed : 0.0;

        // Normalise features to comparable ranges (rough, using game parameters)
        // distances/positions scaled by _max_y (world height), speeds by a max-speed constant
        double denomDist = Math.Max(1.0, (double)_max_y);
        double maxVSpeed = 200.0; // heuristic cap for vertical speed normalization
        double flappyHeightNorm = _output.FlappyHeight / denomDist; // 0..1
        double flappyXNorm = _output.FlappyX / denomDist; // relative horizontal position
        double verticalSpeedNorm = verticalSpeed / maxVSpeed; // approx -1..1
        double distToRoofNorm = distToRoof / denomDist; // 0..1
        double nearestDxNorm = nearest_dx / denomDist; // scaled distance to next obstacle
        double nearestYNorm = nearest_y / denomDist; // obstacle vertical pos 0..1

        bool humanJump = Input.IsActionJustPressed("Flap");

        // if native model is enabled, use it to override human input
        if (_useNativeModel)
        {
            if (_nativePredictFunc == null) LoadNativeLibrary();
            if (_nativePredictFunc != null)
            {
                int pred = 0;
                try
                {
                    int passesArg = 0;
                    try { passesArg = _core.GetObstaclesPasses(); } catch { passesArg = 0; }
                    // Pass new arguments: distBottom, distTop, tti
                    // pred = _nativePredictFunc(_output.FlappyHeight, _output.FlappyX, verticalSpeed, distToRoof, nearest_dx, nearest_y, (double)passesArg, distBottom, distTop, tti);
                    // Call with 7 features: vs, dr, dx, passes, distBottom, distTop, tti
                    pred = _nativePredictFunc(verticalSpeed, distToRoof, nearest_dx, (double)passesArg, distBottom, distTop, tti);
                    humanJump = pred == 1;
                    // throttle logging to avoid spamming output
                    try
                    {
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                        if (now - _lastNativeLogMs > 500)
                        {
                            GD.Print($"[native] pred={pred} fh={_output.FlappyHeight:F3} distB={distBottom:F3} distT={distTop:F3} tti={tti:F3}");
                            _lastNativeLogMs = now;
                        }
                    }
                    catch { /* ignore logging errors */ }
                }
                catch (Exception ex) { GD.PrintErr("Native predict failed: " + ex.Message); _useNativeModel = false; }

                // Combine native model decision with a heuristic based on obstacles passed.
                // We don't force stop; instead we blend the native vote (0/1) with a "no-jump"
                // preference that becomes strong once 4 passes are reached.
                try
                {
                    int passes = 0;
                    if (_core != null) passes = _core.GetObstaclesPasses();
                    // heuristic_no_jump: 0.0 means no heuristic, 1.0 means strong no-jump preference
                    double heuristic_no_jump = 0.0;
                    if (passes >= 4) heuristic_no_jump = 0.9; // very strong no-jump preference at 4+
                    else if (passes == 3) heuristic_no_jump = 0.5; // moderate influence
                    else heuristic_no_jump = 0.0;
                    // blending weights: when heuristic is strong, favor it more
                    double heuristic_weight = heuristic_no_jump > 0.0 ? 0.7 : 0.2;
                    double native_weight = 1.0 - heuristic_weight;
                    // native vote is 0/1
                    double native_vote = pred == 1 ? 1.0 : 0.0;
                    // final probability of jump = native_weight * native_vote + heuristic_weight * (1 - heuristic_no_jump)
                    double final_jump_prob = native_weight * native_vote + heuristic_weight * (1.0 - heuristic_no_jump);
                    bool blendedJump = final_jump_prob >= 0.5;
                    // log when heuristic meaningfully changed decision
                    if ((blendedJump != (pred == 1)))
                    {
                        long now3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                        if (now3 - _lastNativeLogMs > 500)
                        {
                            GD.Print($"[native+heuristic] passes={passes} pred={pred} heuristic_no_jump={heuristic_no_jump:F2} final_prob={final_jump_prob:F2} => jump={(blendedJump ? 1 : 0)}");
                            _lastNativeLogMs = now3;
                        }
                    }
                    humanJump = blendedJump;
                }
                catch { }
            }
        }

        // Note: native model expects features in original game units (not normalized), so do not pass normalized values.

        var input = new InputData { JumpPressed = humanJump, DeltaTime = (float)delta };

        try
        {
            _core.Update(in input, ref _output);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[Root] FlappyEntry.Update failed: " + ex.Message);
            _recorder = null;
            _gameOverPanel.Visible = true;
            return;
        }

        // Check if bird hit obstacle or boundary
        if (_output.GameOver)
        {
            GD.Print("[Root] Game Over - bird hit obstacle!");
            _isPlaying = false;
            if (_recorder != null)
            {
                _recorder.Close();
                _recorder = null;
            }
            _gameOverPanel.Visible = true;
            return;
        }

        _bird.Position = new Vector2(100, MapY(_output.FlappyHeight));

        UpdateObstacles();
        DrawScore();

        // Record normalized features + action
        if (_recorder != null)
        {
            int actionLabel = humanJump ? 1 : 0;
            // use raw game units for the near-roof check and recorded features
            if (_forceLoseNearRoof && distToRoof < NEAR_ROOF_THRESHOLD) actionLabel = 0;

            int passes = 0;
            try { passes = _core.GetObstaclesPasses(); } catch { passes = 0; }

            // Write raw (unnormalized) features so the trainer's normalization is correct
            _recorder.WriteLine($"{_core.GameTime};{_output.FlappyHeight:F6};{_output.FlappyX:F6};{verticalSpeed:F6};{distToRoof:F6};{nearest_dx:F6};{nearest_y:F6};{passes};{actionLabel}");
            _recorder.Flush();
        }
    }

    private void UpdateObstacles()
    {
        float obstacleWidthPixels = _obstacle_width / _max_y * _screenWidth;

        for (int i = 0; i < _output.Obstacles.Length; i++)
        {
            var o = _output.Obstacles[i];
            float xPos = birdOffsetX + (o.X - _output.FlappyX) * unitsPerPixel;

            // --- OBSTACLE BAS ---
            float bottomHeight = o.Y;
            float bottomHeightPixels = bottomHeight / _max_y * _screenHeight;
            float rectHeightBottom = bottomHeightPixels + _pipeExcess;
            float bottomTopY = _screenHeight - bottomHeightPixels - (_pipeExcess / 2f); // centrer l'excess
            _obstaclesBottom[i] = new Rect2(
                xPos,
                bottomTopY,
                obstacleWidthPixels,
                rectHeightBottom
            );

            // --- OBSTACLE HAUT ---
            float gap = _gap;
            float topHeight = _max_y - bottomHeight - gap;
            float topHeightPixels = topHeight / _max_y * _screenHeight;
            float rectHeightTop = topHeightPixels + _pipeExcess;
            float topBottomY = bottomTopY - (gap / _max_y * _screenHeight) - topHeightPixels - (_pipeExcess / 2f); // centrer l'excess
            _obstaclesTop[i] = new Rect2(
                xPos,
                topBottomY,
                obstacleWidthPixels,
                rectHeightTop
            );
        }

        QueueRedraw();
    }

    private void DrawScore()
    {
        // if (_isPlaying)
        // 	_score_label.Visible = false;
        // else
        // 	_score_label.Visible = true;

        _score_label.Text = "Score : " + _core.GetObstaclesPasses();
    }

    public override void _Draw()
    {
        base._Draw();
        if (!_isPlaying) return;

        for (int i = 0; i < _obstaclesBottom.Length; i++)
        {
            DrawRect(_obstaclesBottom[i], Colors.Green);
            DrawRect(_obstaclesTop[i], Colors.Green);
        }

        //float borderHeight = 0f; // hauteur des rects de délimitation

        // Rect en bas du monde

        float borderHeight = 500f; // par exemple, étendue verticale

        //float borderHeight = 20f; // hauteur des bords en unités de jeu (ou pixels si tu veux fixe)

        // Rect en bas du monde (s'étend vers le haut)
        float bottomY = MapY(0); // position verticale du bas du monde
        Rect2 bottomBorder = new Rect2(-1000, bottomY, _screenWidth * 2, borderHeight);
        DrawRect(bottomBorder, Colors.Green);

        // Rect en haut du monde (s'étend vers le bas)
        float topY = _max_y; // position verticale du haut du monde
        Rect2 topBorder = new Rect2(-1000, -topY - borderHeight, _screenWidth * 2, borderHeight);
        DrawRect(topBorder, Colors.Green);

        DrawScore();
    }

    private float MapY(float worldY)
    {
        return (_max_y - worldY) / _max_y * _screenHeight;
    }

    // ================= UI CALLBACKS =================
    private void OnTrainPressed()
    {
        GD.Print("[Root] Train requested: searching for demos and training script...");

        string pythonDir = Path.Combine(Path.GetFullPath(".."), "IA", "Python");
        if (!Directory.Exists(pythonDir))
        {
            GD.PrintErr("No IA/Python folder found at: " + pythonDir);
            return;
        }

        // find demo files
        var demos = Directory.GetFiles(pythonDir, "demos*.csv");
        if (demos.Length == 0)
        {
            GD.PrintErr("No demo files found in " + pythonDir + ", generate some play sessions first.");
            return;
        }

        // find a trainer script
        string[] candidates = new string[] { Path.Combine(pythonDir, "train.py"), Path.Combine(pythonDir, "train_model.py"), Path.Combine(pythonDir, "train_from_demos.py") };
        string trainer = null;
        foreach (var c in candidates) if (File.Exists(c)) { trainer = c; break; }
        if (trainer == null)
        {
            GD.PrintErr("No training script (train.py/train_model.py) found in " + pythonDir);
            GD.Print("If you have a custom trainer, place it in IA/Python and name it train.py");
            return;
        }

        // build args: pass folder or files; trainer should accept --data <folder> --out <outpath>
        string outLib = Path.Combine(Path.GetFullPath(".."), "IA", "SoftmaxC", "libsoftmodel.dylib");
        string args = $"\"{trainer}\" --data \"{pythonDir}\" --out \"{outLib}\"";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "python3";
            psi.Arguments = args;
            psi.WorkingDirectory = pythonDir;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            var proc = System.Diagnostics.Process.Start(psi);
            proc.OutputDataReceived += (s, e) => { if (e.Data != null) GD.Print("[trainer] " + e.Data); };
            proc.BeginOutputReadLine();
            proc.ErrorDataReceived += (s, e) => { if (e.Data != null) GD.PrintErr("[trainer] " + e.Data); };
            proc.BeginErrorReadLine();
            GD.Print("Training started...");
            proc.EnableRaisingEvents = true;
            proc.Exited += (s, e) =>
            {
                System.Threading.Tasks.Task.Run(() => OnTrainerFinished(new object[] { pythonDir, outLib }));
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr("Failed to start trainer: " + ex.Message);
        }
    }

    private void OnTrainerFinished(object obj)
    {
        // obj is an object[] { pythonDir, outLib }
        if (!(obj is object[] arr && arr.Length >= 2)) return;
        string pythonDir = arr[0] as string;
        string outLib = arr[1] as string;
        string weightsPath = Path.Combine(Path.GetFullPath(".."), "IA", "SoftmaxC", "model_weights.txt");
        if (File.Exists(weightsPath))
        {
            GD.Print($"Trainer finished — weights written to: {weightsPath}");

            // Dump weights to Godot console for debugging
            try
            {
                var lines = File.ReadAllLines(weightsPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    GD.Print($"[weights] {i}: {lines[i]}");
                }
                if (lines.Length >= 7)
                {
                    double[] w = new double[7];
                    for (int i = 0; i < 7; i++) double.TryParse(lines[i], out w[i]);
                    GD.Print($"[weights] Parsed bias={w[6]:F6} firstWeight={w[0]:F6}");
                }
            }
            catch (Exception ex) { GD.PrintErr("Failed to read weights: " + ex.Message); }
            // try to auto-build native lib if Makefile or build.sh present
            string softDir = Path.Combine(Path.GetFullPath(".."), "IA", "SoftmaxC");
            if (File.Exists(Path.Combine(softDir, "Makefile")) || File.Exists(Path.Combine(softDir, "makefile")))
            {
                GD.Print("Found Makefile in SoftmaxC — attempting to run 'make' to build libsoftmodel.dylib");
                try
                {
                    var psi2 = new System.Diagnostics.ProcessStartInfo();
                    psi2.FileName = "make";
                    psi2.WorkingDirectory = softDir;
                    psi2.RedirectStandardOutput = true;
                    psi2.RedirectStandardError = true;
                    psi2.UseShellExecute = false;
                    var proc2 = System.Diagnostics.Process.Start(psi2);
                    proc2.WaitForExit(10000);
                    GD.Print("Make finished, check SoftmaxC for libsoftmodel.dylib");
                }
                catch (Exception ex) { GD.PrintErr("Failed to run make: " + ex.Message); }
            }
            else if (File.Exists(Path.Combine(softDir, "build.sh")))
            {
                GD.Print("Found build.sh in SoftmaxC — attempting to run it");
                try
                {
                    var psi2 = new System.Diagnostics.ProcessStartInfo();
                    psi2.FileName = "/bin/bash";
                    psi2.Arguments = "build.sh";
                    psi2.WorkingDirectory = softDir;
                    psi2.RedirectStandardOutput = true;
                    psi2.RedirectStandardError = true;
                    psi2.UseShellExecute = false;
                    var proc2 = System.Diagnostics.Process.Start(psi2);
                    proc2.WaitForExit(10000);
                    GD.Print("build.sh finished, check SoftmaxC for libsoftmodel.dylib");
                }
                catch (Exception ex) { GD.PrintErr("Failed to run build.sh: " + ex.Message); }
            }
            else
            {
                GD.Print("No Makefile/build.sh found in SoftmaxC. To get a native library, compile the C sources in IA/SoftmaxC into libsoftmodel.dylib or place the library at IA/SoftmaxC/libsoftmodel.dylib");
            }

            // If the requested output library exists or default library exists, try loading it into the game
            string candidateLib = outLib;
            if (string.IsNullOrEmpty(candidateLib) || !File.Exists(candidateLib))
            {
                string defLib = Path.Combine(softDir, "libsoftmodel.dylib");
                if (File.Exists(defLib)) candidateLib = defLib;
            }
            if (!string.IsNullOrEmpty(candidateLib) && File.Exists(candidateLib))
            {
                GD.Print($"Attempting to load trained native library: {candidateLib}");
                if (LoadNativeLibrary(candidateLib))
                {
                    _useNativeModel = true;
                    GD.Print("Native model loaded and enabled automatically after training.");
                }
                else
                {
                    GD.PrintErr("Failed to load the native library at: " + candidateLib);
                }
            }
        }
        else
        {
            GD.PrintErr($"Trainer finished but no weights found at {weightsPath}");
        }
    }

    private void OnPlayLikeMePressed()
    {
        // Try to load the native model and let the bird play using it.
        if (LoadNativeLibrary())
        {
            _useNativeModel = true;
            GD.Print("Native model loaded — Play like me enabled.");
        }
        else
        {
            _useNativeModel = false;
            GD.PrintErr("Native model not found; falling back to human control.");
        }

        StartFlappy();
    }
    private void OnPlayPressed()
    {
        _useNativeModel = false; // Ensure CSV recording for human play
        StartFlappy();
    }
    private void OnQuitPressed() => GetTree().Quit();
    private void OnReplayPressed() => ResetFlappy();
    private void OnMainMenuPressed()
    {
        _isPlaying = false;
        _menuPanel.Visible = true;
        _gameOverPanel.Visible = false;
    }
}