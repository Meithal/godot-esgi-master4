using UnityEngine;
using FlappyCore;

public class FlappyView : MonoBehaviour
{
    [Header("Scene Alignment")]
    public Transform Origin;
    public float UnitsPerMeter = 1f;

    [Header("Prefabs")]
    public GameObject BirdPrefab;
    public GameObject ObstaclePrefab;
    public GameObject GroundPrefab;

    [Header("View Parameters")]
    public float BirdOffsetX = 0f;
    public float BirdSpeed = 1.4f;
    public float MinY = 0f;
    public float MaxY = 100f;

    private FlappyEntry flappy;
    private OutputData output;
    private GameObject bird;
    private GameObject[] obstaclesBottom;
    private GameObject[] obstaclesTop;
    private float worldScrollX = 0f;
    private float pipeExcess = 30f;

    private float _prevFlappyHeight;
    private float _birdAngleSmoothed = 0f;
    [Tooltip("Facteur appliqué à la vitesse verticale pour obtenir l'angle")]
    public float RotationFactor = 0.5f;
    [Tooltip("Vitesse de lissage de la rotation (plus grand = moins de jitter)")]
    public float RotationSmooth = 8f;


    public void StartFlappy()
    {
        if (Origin == null) Origin = transform;

        // Initialise Flappy2 via FlappyEntry
        flappy = new FlappyEntry();
        output = flappy.Init(
            numObstacles: 20,
            gravity: 100f,
            obstacleSpeed: 40f,
            birdRadius: 2f,
            seed: 42
        );

        // Crée l’oiseau
        bird = Instantiate(BirdPrefab, Origin);
        bird.transform.localPosition = new Vector3(BirdOffsetX, MapY(output.FlappyHeight), 0f);

        var g = Instantiate(GroundPrefab, Origin);
        g.transform.localPosition = new Vector3(75f, MapY(0), 20f);

        // Crée un GameObject pour chaque obstacle bas
        obstaclesBottom = new GameObject[output.Obstacles.Length];
        obstaclesTop = new GameObject[output.Obstacles.Length];
        for (int i = 0; i < output.Obstacles.Length; i++)
        {
            obstaclesBottom[i] = Instantiate(ObstaclePrefab, Origin);
            obstaclesTop[i] = Instantiate(ObstaclePrefab, Origin);
        }

        _prevFlappyHeight = output.FlappyHeight;
    }

    public void UpdateFlappy()
    {
        var input = new InputData
        {
            JumpPressed = Input.GetKeyDown(KeyCode.Space) || CheckTouchJump(),
            DeltaTime = Time.deltaTime
        };

        try
        {
            flappy.Update(in input, ref output);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FlappyView] FlappyEntry.Update failed: " + ex.Message);
            return;
        }

        // Le monde "défile" en fonction de la position logique du bird
        worldScrollX = output.FlappyX;

        // Bird reste fixe à l’écran
        if (bird != null)
            bird.transform.localPosition = new Vector3(BirdOffsetX, MapY(output.FlappyHeight), 0f);
        //float angle = Mathf.Clamp(output.FlappyVerticalSpeed * 0.5f, -45f, 45f);
        //bird.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        float obstacleWidth = 10f;
        float gap = 30f; // espace entre le bas et le haut

        for (int i = 0; i < output.Obstacles.Length; i++)
        {
            var o = output.Obstacles[i];
            float xPos = (o.X - worldScrollX) * UnitsPerMeter;

            // --- TUBE BAS ---
            float bottomHeight = o.Y;
            float bottomCenterY = bottomHeight / 2f;
            obstaclesBottom[i].transform.localScale = new Vector3(obstacleWidth, MapY(bottomHeight) / 2f + pipeExcess, obstacleWidth);
            obstaclesBottom[i].transform.localPosition = new Vector3(xPos, MapY(bottomCenterY) - pipeExcess, 0f);

            // --- TUBE HAUT ---
            float topHeight = bottomHeight + gap;              // Hauteur du tube haut
            float topCenterY = bottomHeight + bottomCenterY + gap;    // Centre du cylindre

            obstaclesTop[i].transform.localScale = new Vector3(obstacleWidth, MapY(bottomHeight) / 2f + pipeExcess, obstacleWidth);
            obstaclesTop[i].transform.localPosition = new Vector3(
                xPos,
                MapY(topCenterY) + pipeExcess,
                0f
            );

        }

        // calculer la vitesse verticale en unités monde / seconde
        float verticalSpeed = 0f;
        if (Time.deltaTime > 0f)
            verticalSpeed = (output.FlappyHeight - _prevFlappyHeight) / Time.deltaTime;

        // stocker pour la frame suivante
        _prevFlappyHeight = output.FlappyHeight;

        // convertir en angle, contraindre et lisser
        float targetAngle = Mathf.Clamp(verticalSpeed * RotationFactor, -45f, 45f);
        _birdAngleSmoothed = Mathf.Lerp(_birdAngleSmoothed, targetAngle, RotationSmooth * Time.deltaTime);

        // appliquer la rotation (Z en degrés)
        if (bird != null)
            bird.transform.localRotation = Quaternion.Euler(-_birdAngleSmoothed - 20, 90f, 0);

    }

    private float MapY(float worldY)
    {
        return (worldY - MinY) * UnitsPerMeter;
    }

    private bool CheckTouchJump()
    {
        if (Input.touchCount == 0) return false;
        foreach (var t in Input.touches)
            if (t.phase == TouchPhase.Began) return true;
        return false;
    }

    public void Reset() { flappy.Reset(); }

    public bool GameOver() { return output.GameOver; }
}