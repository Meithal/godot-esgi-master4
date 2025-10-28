using UnityEngine;

namespace RedVSBlue.WarriorManagerV1
{
    public class SpawnerManagerScript : MonoBehaviour
    {
        public GameObject redWarriorPrefab;
        public GameObject blueWarriorPrefab;
        public Transform[] redSpawners;
        public Transform[] blueSpawners;
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
                foreach (var redSpawner in redSpawners)
                {
                    Instantiate(redWarriorPrefab, 
                        redSpawner.position + Vector3.up, 
                        Quaternion.identity);
                }
                
                foreach (var blueSpawner in blueSpawners)
                {
                    Instantiate(blueWarriorPrefab, 
                        blueSpawner.position + Vector3.up, 
                        Quaternion.identity);
                }

                _lastSpawnTime = currentTime;
            }
        }
    }
}
