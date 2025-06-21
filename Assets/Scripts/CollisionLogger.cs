using UnityEngine;

public class CollisionLogger : MonoBehaviour
{
    // accumulates all contact points from every OnCollisionStay this frame
    public static int globalCollisionCount = 0;

    void OnCollisionStay(Collision collisionInfo)
    {
        // collisionInfo.contactCount is the number of contact points in this collision :contentReference[oaicite:0]{index=0}
        globalCollisionCount += collisionInfo.contactCount;
    }
}
