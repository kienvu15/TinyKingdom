    using System;
    using UnityEngine;

    public class Tool : MonoBehaviour
    {
        public GameObject owner;
        private void Start()
        {
            FindVillager();
        }

        private void Update()
        {
            if (owner != null)
            {
                return;
            }
            else
            {
                FindVillager();
            }
        }

        private void FindVillager()
        {
            if (owner != null) return;

            Villager[] villagers = FindObjectsOfType<Villager>();

            float minDistance = Mathf.Infinity;
            Villager nearestVillager = null;

            foreach (Villager villager in villagers)
            {
                // Nếu muốn bỏ qua Villager đã có tool
                if (villager.hasTool)
                    continue;

                float distance = Vector3.Distance(transform.position, villager.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestVillager = villager;
                }
            }

            if (nearestVillager != null)
            {
                owner = nearestVillager.gameObject;

                nearestVillager.TargetPosition = transform.position;
                nearestVillager.hasMoveCommand = true;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag == "Player")
            {
                var villager = other.gameObject.GetComponent<Villager>();
                if (villager != null)
                {
                    villager.hasTool = true;
                    villager.hasMoveCommand = false;
                    Destroy(gameObject);
                }
            }
        }
    }
