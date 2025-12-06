using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TMPro;
using System.Collections.Generic;
// using System;
// using Unity.Mathematics;

public enum StrategyType
{
    RL,
    Egoist,
    Cooperative,
    Random
}


public class NegotiationAgent : Agent
{
    private Rigidbody rb;
    // public float moveSpeed = 10f; 
    public float food;
    public float energy;    
    public TextMeshProUGUI statusText;
    public float maxEpisodeTime = 10f;
    private float episodeTimer;
    public NegotiationAgent agent2;
    public NegotiationAgent agent3;
    [Header("Ustawienia Strategii")]
    [Tooltip("RL = Model sieci. Inne = Skrypt.")]
    public StrategyType currentStrategy = StrategyType.RL;
    private List<NegotiationAgent> otherAgents;
    private enum TradeType {FoodForEnergy, EnergyForFood};
    [Tooltip("Pokazuje bieżącą, nieskalowaną karę. Cel to 0.")]
    public float currentImbalanceDebug;

    public Renderer robotRenderer; //żeby je kolorowac

    private int stepCounter = 0; 
    public int decisionPeriod = 5;



    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        //Szukamy innych agentów
        if (agent2 != null && agent3 != null)
        {
            otherAgents = new List<NegotiationAgent> {agent2, agent3};
        }
        else
        {
            Debug.LogError($"Agenci 'agent2' i 'agent3' nie są przypisani w {gameObject.name}!");

        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (currentStrategy == StrategyType.RL)
        {
            int action = actions.DiscreteActions[0];
            ExecuteAction(action); // Wywołujemy nową funkcję
        }
    }

    void FixedUpdate()
    {
        // Dla agentów skryptowych (eoisgt i kooperanci), wykonaj akcję co określoną liczbę kroków
        if (currentStrategy != StrategyType.RL)
        {
            stepCounter++;
            if (stepCounter >= decisionPeriod)
            {
                int action = GetScriptedAction();
                ExecuteAction(action);
                stepCounter = 0;
            }
        }


        float imbalance = Mathf.Abs(food - energy);
        currentImbalanceDebug = -imbalance;
        float scaledPenalty = (imbalance / 100.0f) * Time.fixedDeltaTime;
        AddReward(-scaledPenalty);

        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            EndEpisode();
        }
    }

    private void ExecuteAction(int action)
    {
        switch (action)
        {
            case 0:  //nic ne rób
                break;
            case 1:  //jedzenie -> energa
                NegotiationAgent bestAgentFood = FindBestPartner(TradeType.FoodForEnergy);
                if (bestAgentFood != null)
                {
                    HandleTrade(TradeType.FoodForEnergy, bestAgentFood);
                }
                break;
            case 2: //energia -> jedzenie
                NegotiationAgent bestAgentEnergy = FindBestPartner(TradeType.EnergyForFood);
                if (bestAgentEnergy != null)
                {
                    HandleTrade(TradeType.EnergyForFood, bestAgentEnergy);
                }
                break;
        }
    }

    private int GetScriptedAction()
    {
        switch(currentStrategy)
        {
            case StrategyType.Egoist:
                float imbalance = food - energy;
                if (imbalance > 5f)
                {
                    return 2; // Wymień energię za jedzenie
                }
                else if (imbalance < -5f)
                {
                    return 1; // Wymień jedzenie za energię
                }
                else
                {
                    return 0; // Nic nie rób
                }
            
            case StrategyType.Cooperative:
                return CalculateCooperativeAction();
            
            case StrategyType.Random:
                return Random.Range(0, 3); // Losowa akcja: 0, 1 lub 2
            default:
                return 0; // Domyślnie nic nie rób
        }
    }

    private int CalculateCooperativeAction()
    {
        float currentGlobalImbalance = GetGlobalImbalance();

        float imbalanceIfAction1 = currentGlobalImbalance;
        NegotiationAgent bestAgent1 = FindBestPartner(TradeType.FoodForEnergy);
        if (bestAgent1 != null)
        {
            float myOldImbalance = Mathf.Abs(food - energy);
            float myNewImbalance = Mathf.Abs((food - 1f) - (energy + 1f));

            float targetOldImbalance = Mathf.Abs(bestAgent1.food - bestAgent1.energy);
            float targetNewImbalance = Mathf.Abs((bestAgent1.food + 1f) - (bestAgent1.energy - 1f));

            imbalanceIfAction1 = (myNewImbalance + targetNewImbalance) - (myOldImbalance + targetOldImbalance);
        }

        float imbalanceIfAction2 = currentGlobalImbalance;
        NegotiationAgent bestAgent2 = FindBestPartner(TradeType.EnergyForFood);
        if (bestAgent2 != null)
        {
            float myOldImbalance = Mathf.Abs(food - energy);
            float myNewImbalance = Mathf.Abs((food + 1f) - (energy - 1f));

            float targetOldImbalance = Mathf.Abs(bestAgent2.food - bestAgent2.energy);
            float targetNewImbalance = Mathf.Abs((bestAgent2.food - 1f) - (bestAgent2.energy + 1f));

            imbalanceIfAction2 = (myNewImbalance + targetNewImbalance) - (myOldImbalance + targetOldImbalance);
        }

        if (imbalanceIfAction1 < currentGlobalImbalance && imbalanceIfAction1 <= imbalanceIfAction2)
        {
            return 1; // Wymień jedzenie za energię
        }
        else if (imbalanceIfAction2 < currentGlobalImbalance && imbalanceIfAction2 < imbalanceIfAction1)
        {
            return 2; // Wymień energię za jedzenie
        }
        else
        {
            return 0; // Nic nie rób
        }
    }

    private float GetGlobalImbalance()
    {
        float sum = Mathf.Abs(food - energy);
        if (agent2 != null)
        {
            sum += Mathf.Abs(agent2.food - agent2.energy);
        }
        if (agent3 != null)
        {
            sum += Mathf.Abs(agent3.food - agent3.energy);
        }
        return sum;
    }

    
    private NegotiationAgent FindBestPartner(TradeType tradeType)
    {
        NegotiationAgent[] candidates = new NegotiationAgent[] { agent2, agent3 };
        NegotiationAgent bestAgent = null;
        float bestValue = Mathf.Infinity;

        foreach (var candidate in candidates)
        {
            float candidateValue = 0f;

            if (tradeType == TradeType.FoodForEnergy)
            {
                candidateValue = candidate.food;
            }
            else if (tradeType == TradeType.EnergyForFood)
            {
                candidateValue = candidate.energy;
            }

            if (candidateValue < bestValue)
            {
                bestValue = candidateValue;
                bestAgent = candidate;
            }
        }

        return bestAgent;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // // Ta linia jest kluczowa, aby naprawić błąd z konsoli
        // sensor.AddObservation(0f);

        //Obserwacja siebie
        sensor.AddObservation(this.food);
        sensor.AddObservation(this.energy);

        //Obserwacja agenta2
        sensor.AddObservation(agent2.food);
        sensor.AddObservation(agent2.energy);

        //Obserwacja agenta3
        sensor.AddObservation(agent3.food);
        sensor.AddObservation(agent3.energy);

        // Debug.Log($"Agent {gameObject.name} obserwuje: Self({food:F1}, {energy:F1}), " +
        //           $"A2({agent2.food:F1}, {agent2.energy:F1}), " +
        //           $"A3({agent3.food:F1}, {agent3.energy:F1})");
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));

        food = Random.Range(20f, 80f);
        energy = Random.Range(40f, 100f);

        Debug.Log($"Nowy epizod! Agent: {gameObject.name}, Food: {food}, Energy: {energy}");

        episodeTimer = 0f;
        currentImbalanceDebug = -Mathf.Abs(food - energy);
    }

    public void Update()
    {
        if (statusText != null)
        {
            statusText.text = $"Food: {food.ToString("F1")}\nEnergy: {energy.ToString("F1")}";
        }
        if (robotRenderer == null) return;

        Color goodBalanceColor = Color.green;
        Color badBalanceColor = Color.red;

        float maxImbalance = 100f;
        float currentImbalance = Mathf.Abs(food - energy);

        float normalizedImbalance = Mathf.Clamp01(currentImbalance / maxImbalance);

        Color NewColor = Color.Lerp(goodBalanceColor, badBalanceColor, normalizedImbalance);

        robotRenderer.material.color = NewColor;
        
    }

    private void HandleTrade(TradeType tradeType, NegotiationAgent targetAgent)
    {
        if (otherAgents == null || otherAgents.Count == 0) return;

        if (tradeType == TradeType.FoodForEnergy)
        {
            if (this.food >= 1f && targetAgent.energy >= 1f)
            {
                this.food -= 1f;
                targetAgent.energy -= 1f;
                this.energy += 1f;
                targetAgent.food += 1f;

                Debug.Log($"[{gameObject.name}] wymienił 1 FOOD za 1 ENERGY z [{targetAgent.gameObject.name}]");
            }
        } else if (tradeType == TradeType.EnergyForFood)
        {
            if (this.energy >= 1f && targetAgent.food >= 1f)
            {
                this.energy -= 1f;
                targetAgent.food -= 1f;
                this.food += 1f;
                targetAgent.energy += 1f;

                Debug.Log($"[{gameObject.name}] wymienił 1 ENERGY za 1 FOOD z [{targetAgent.gameObject.name}]");
            }
        }
    }

}