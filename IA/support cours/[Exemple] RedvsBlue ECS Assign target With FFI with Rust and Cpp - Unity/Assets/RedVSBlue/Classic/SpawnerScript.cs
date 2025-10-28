using UnityEngine;

namespace RedVSBlue.Classic
{
    public class SpawnerScript : MonoBehaviour
    {
        public GameObject prefabToSpawn;
        public float spawnDelay;

        private float _lastSpawnTime;

        private void Start()
        {
            _lastSpawnTime = Time.time;
        }

        private void Update()
        {
            var currentTime = Time.time;

            var delta = currentTime - _lastSpawnTime;

            if (delta >= spawnDelay)
            {
                Instantiate(prefabToSpawn, transform.position + Vector3.up, Quaternion.identity);
                _lastSpawnTime = currentTime;
            }
        }
    }
}