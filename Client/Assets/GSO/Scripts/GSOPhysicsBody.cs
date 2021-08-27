using UnityEngine;

namespace GSO
{
    public class GSOPhysicsBody : MonoBehaviour
    {
        public float radius = 1f;
        public float mass = 1f;
        public Vector2 velocity = Vector2.zero;

        private void OnDrawGizmos() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + new Vector3(velocity.x, velocity.y, 0f));
        }

        public void Absorb(GSOPhysicsBody other) {
            radius += other.radius * 0.5f;
            mass += other.mass * 0.5f;
            transform.localScale = new Vector3(radius, radius, 1f);

            velocity += other.velocity * 0.5f;
        }
    }
}