// using UnityEngine;
// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;

// public class NegotiationAgent : Agent
// {
//     public float moveSpeed = 2f;
//     public override void OnActionReceived(ActionBuffers actions)
//     {
//         float moveX = actions.ContinuousActions[0];
//         float moveZ = actions.ContinuousActions[1];

//         Vector3 move = new Vector3(moveX, 0, moveZ);
//         transform.position += move * moveSpeed * Time.deltaTime;
//     }

//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var continuousActions = actionsOut.ContinuousActions;
//         continuousActions[0] = Input.GetAxis("Horizontal");
//         continuousActions[1] = Input.GetAxis("Vertical");
//     }
// }

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class NegotiationAgent : Agent
{
    public float moveSpeed = 2f;

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        Vector3 move = Vector3.zero;

        // proste akcje
        switch (action)
        {
            case 1: move = Vector3.right; break;
            case 2: move = Vector3.left; break;
            case 3: move = Vector3.forward; break;
            case 4: move = Vector3.back; break;
            default: move = Vector3.zero; break;
        }

        transform.position += move * moveSpeed * Time.deltaTime;

        // ograniczenie pozycji w granicach planszy
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -4.5f, 4.5f),
            transform.position.y,
            Mathf.Clamp(transform.position.z, -4.5f, 4.5f)
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;

        if (Input.GetKey(KeyCode.RightArrow)) discrete[0] = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) discrete[0] = 2;
        if (Input.GetKey(KeyCode.UpArrow)) discrete[0] = 3;
        if (Input.GetKey(KeyCode.DownArrow)) discrete[0] = 4;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition); // 3 obserwacje (x, y, z)
    }
}

