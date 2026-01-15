using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class JacekBrain : Agent
{
    // Referencja do ciała (logiki gry)
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
        // 1. NAJWAŻNIEJSZE: Wymuś aktualizację sąsiadów w TYM MOMENCIE.
        // Dzięki temu sieć widzi stan faktyczny z klatki decyzyjnej, a nie z ostatniego FixedUpdate.
        myBody.UpdateNearestNeighbors();

        // 2. Obserwacje własne (zawsze bezpieczne)
        sensor.AddObservation(myBody.food);
        sensor.AddObservation(myBody.energy);
        
        // 3. Obserwacje Agenta 2 (Najbliższego) - ZABEZPIECZENIE
        if (myBody.agent2 != null)
        {
            sensor.AddObservation(myBody.agent2.food);
            sensor.AddObservation(myBody.agent2.energy);
            sensor.AddObservation((float)myBody.agent2.currentIntent);
        }
        else
        {
            // Jeśli nikogo nie ma w pobliżu, dajemy zera (brak sygnału)
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f); // Intencja None
        }

        // 4. Obserwacje Agenta 3 (Drugiego najbliższego) - ZABEZPIECZENIE
        if (myBody.agent3 != null)
        {
            sensor.AddObservation(myBody.agent3.food);
            sensor.AddObservation(myBody.agent3.energy);
            sensor.AddObservation((float)myBody.agent3.currentIntent);
        }
        else
        {
            // Brak drugiego sąsiada -> zera
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