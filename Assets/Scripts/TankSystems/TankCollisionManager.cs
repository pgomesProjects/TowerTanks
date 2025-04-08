using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankCollisionManager : MonoBehaviour
    {
        public CompositeCollider2D tankCompCollider;
        public Transform colliderCellParent;
        public GameObject colliderCellPrefab;
        private TankController tank;

        private Cell[] cells;
        public List<GameObject> colliderCells = new List<GameObject>();

        private float knockbackForce = 10f;
        private float collisionCooldownTimer = 0f;

        public bool UpdateCollider = false;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            //UpdateCells();
        }

        public void UpdateCells()
        {
            //Empty Old List
            foreach (GameObject _cell in colliderCells)
            {
                Destroy(_cell);
            }

            //Generate New List
            cells = tank.GetComponentsInChildren<Cell>();
            colliderCells.Clear();

            foreach (Cell cell in cells)
            {
                GameObject newCell = Instantiate(colliderCellPrefab, colliderCellParent);
                newCell.transform.position = cell.transform.position;
                newCell.transform.rotation = cell.transform.rotation;

                colliderCells.Add(newCell);
            }

            //Update Collider
            tankCompCollider.GenerateGeometry();
        }

        private void FixedUpdate()
        {
            CheckCollisions();

            if (UpdateCollider) { UpdateCells(); UpdateCollider = false; }
        }

        private void CheckCollisions()
        {
            if (collisionCooldownTimer > 0)
            {
                collisionCooldownTimer -= Time.deltaTime;
            }
            else collisionCooldownTimer = 0;
        }

        public void CellCollision(Vector2 direction)
        {
            if (collisionCooldownTimer == 0)
            {
                //Determine Direction
                float _direction = direction.x;

                //Apply Knockback Force
                if (knockbackForce > 0)
                {
                    float knockBackTime = knockbackForce * 0.05f;

                    //knockbackForce *= Mathf.Sign(velocity.x);
                    //tank.treadSystem.ApplyForce(transform.position, knockbackForce * _direction, knockBackTime);
                }

                //Effects
                GameManager.Instance.AudioManager.Play("TankImpact", gameObject);

                //Reset Timer
                collisionCooldownTimer = 0.05f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 direction = Vector2.left;

            if (collision.transform.position.x > transform.position.x) { direction = Vector2.left; }
            if (collision.transform.position.x < transform.position.x) { direction = Vector2.right; }

            CellCollision(direction);
        }

        private void OnDrawGizmos()
        {
            /*
            Gizmos.color = Color.red;
            foreach (Cell cell in cells)
            {
                Vector3 size = new Vector3(hitboxSize, hitboxSize, hitboxSize);
                Gizmos.DrawWireCube(cell.transform.position, size);
            }*/
        }
    }
}
