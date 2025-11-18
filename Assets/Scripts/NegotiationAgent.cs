using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TMPro;
using System.Collections.Generic;
// using System;
// using Unity.Mathematics;


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
    private List<NegotiationAgent> otherAgents;
    private enum TradeType {FoodForEnergy, EnergyForFood};
    [Tooltip("Pokazuje bieżącą, nieskalowaną karę. Cel to 0.")]
    public float currentImbalanceDebug;

    public Renderer robotRenderer; //żeby je kolorowac



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
        int tradeAction = actions.DiscreteActions[0];

        switch (tradeAction)
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

    void FixedUpdate()
    {
        float imbalance = Mathf.Abs(food - energy);
        currentImbalanceDebug = -imbalance;

        float scaledPenalty = (imbalance / 100.0f) * Time.fixedDeltaTime;

        AddReward(-scaledPenalty);


        episodeTimer += Time.fixedDeltaTime;

        // Sprawdzamy, czy czas epizodu został przekroczony
        if (episodeTimer >= maxEpisodeTime)
        {
            // Debug.Log("Czas minął! Reset epizodu.");
            EndEpisode(); // Kończymy epizod i wywołujemy OnEpisodeBegin()
        }
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