using UnityEngine;

public class AutoMoveAgent : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float arenaLimit = 4.5f;
    private Vector3 moveDir; //current direction              

    void Start()
    {
        // random direction for start 
        float moveX = Random.Range(-1f, 1f);
        float moveZ = Random.Range(-1f, 1f);
        moveDir = new Vector3(moveX, 0, moveZ).normalized;
    }

    void Update()
    {
        // moving current direction 
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        Vector3 pos = transform.position;

        // bouncing to the walls 
        if (pos.x > arenaLimit || pos.x < -arenaLimit)
        {
            moveDir.x = -moveDir.x;
            pos.x = Mathf.Clamp(pos.x, -arenaLimit, arenaLimit);
        }

        if (pos.z > arenaLimit || pos.z < -arenaLimit)
        {
            moveDir.z = -moveDir.z;
            pos.z = Mathf.Clamp(pos.z, -arenaLimit, arenaLimit);
        }

        transform.position = pos;

        // prevent falling from the scene 
        if (transform.position.y < 0.3f)
        {
            transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
        }
    }
}
