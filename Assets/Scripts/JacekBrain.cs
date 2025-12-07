using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// Ten skrypt jest "tyczką" do Pythona.
// Wpinamy go TYLKO do agenta, który ma się uczyć (Jacka).
public class JacekBrain : Agent
{
    // Referencja do ciała (logiki gry)
    public NegotiationAgent myBody;

    public override void Initialize()
    {
        // Znajdź skrypt "Ciała" na tym samym obiekcie
        myBody = GetComponent<NegotiationAgent>();
        // Powiedz Ciału, że ma Mózg (żeby wiedziało gdzie słać nagrody)
        myBody.myBrain = this;
    }

    public override void OnEpisodeBegin()
    {
        // Mózg nakazuje Ciału reset
        myBody.ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Mózg patrzy na Ciało i zbiera dane
        sensor.AddObservation(myBody.food);
        sensor.AddObservation(myBody.energy);
        sensor.AddObservation(myBody.agent2.food);
        sensor.AddObservation(myBody.agent2.energy);
        sensor.AddObservation(myBody.agent3.food);
        sensor.AddObservation(myBody.agent3.energy);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Mózg dostaje decyzję z Pythona i każe Ciału ją wykonać
        int action = actions.DiscreteActions[0];
        myBody.ProcessAction(action);
    }
}