using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class JacekBrain : Agent
{
    public NegotiationAgent myBody;

    public override void Initialize()
    {
        myBody = GetComponent<NegotiationAgent>();
        myBody.myBrain = this;
    }

    public override void OnEpisodeBegin()
    {
        myBody.ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        myBody.UpdateNearestNeighbors();

        sensor.AddObservation(myBody.food);
        sensor.AddObservation(myBody.energy);
        
        //jakby nie znalazł sobie sąsiada to ma 0 zebrać
        if (myBody.agent2 != null)
        {
            sensor.AddObservation(myBody.agent2.food);
            sensor.AddObservation(myBody.agent2.energy);
            sensor.AddObservation((float)myBody.agent2.currentIntent);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f); 
        }

        if (myBody.agent3 != null)
        {
            sensor.AddObservation(myBody.agent3.food);
            sensor.AddObservation(myBody.agent3.energy);
            sensor.AddObservation((float)myBody.agent3.currentIntent);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        myBody.ProcessAction(action);
    }
}