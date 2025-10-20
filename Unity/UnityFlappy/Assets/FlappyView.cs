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

    [Header("View Parameters")]
    public float BirdOffsetX = 0f;
    public float BirdSpeed = 1.4f;
    public float MinY = 0f;
    public float MaxY = 10f;

    private FlappyEntry flappy;
    private OutputData output;
    private GameObject bird;
    private GameObject[] obstacles;
    private float worldScrollX = 0f;

    void Start()
    {
        if (Origin == null) Origin = transform;

        flappy = new FlappyEntry();
        output = flappy.Init(100f, MaxY - MinY);

        bird = Instantiate(BirdPrefab, Origin);
        bird.transform.localPosition = new Vector3(BirdOffsetX, MapY(output.FlappyHeight), 0f);

        obstacles = new GameObject[FlappyEntry.MAX_VISIBLE_OBSTACLES];
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i] = Instantiate(ObstaclePrefab, Origin);
        }
    }

    void Update()
    {
        var input = new InputData
        {
            JumpPressed = Input.GetKeyDown(KeyCode.Space) || CheckTouchJump(),
            DeltaTime = Time.deltaTime
        };

        try { flappy.Update(in input, ref output); }
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

        // Obstacles bougent à gauche selon le scroll du monde
        for (int i = 0; i < output.Obstacles.Length; i++)
        {
            var o = output.Obstacles[i];
            var go = obstacles[i];
            if (go == null) continue;

            float screenX = (o.X - worldScrollX) * UnitsPerMeter;
            float screenY = MapY(o.Y);

            go.SetActive(true);
            go.transform.localPosition = new Vector3(screenX, screenY, 0f);
        }
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
}
