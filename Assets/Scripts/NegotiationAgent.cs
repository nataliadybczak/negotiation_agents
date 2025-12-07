using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum StrategyType {RL, Egoist, Cooperative, Random};

// To teraz zwykły MonoBehaviour - nie wymaga BehaviorParameters!
public class NegotiationAgent : MonoBehaviour
{
    private Rigidbody rb;
    public float food;
    public float energy;    
    public TextMeshProUGUI statusText;
    public float maxEpisodeTime = 10f;
    private float episodeTimer;
    
    // Teraz wszyscy są NegotiationAgent, więc typy pasują!
    public NegotiationAgent agent2;
    public NegotiationAgent agent3;

    [Header("Ekonomia (Preferencje)")]
    [Tooltip("Ile warta jest 1 jednostka jedzenia dla tego agenta?")]
    public float foodUtility = 1.0f; 
    
    [Tooltip("Ile warta jest 1 jednostka energii dla tego agenta?")]
    public float energyUtility = 1.0f;
    
    [Header("Ustawienia Strategii")]
    public StrategyType currentStrategy = StrategyType.RL;

    public enum TradeType {FoodForEnergy, EnergyForFood};
    public float currentImbalanceDebug;
    public Renderer robotRenderer; 

    public enum TradeIntention {None, OfferFoodForEnergy, OfferEnergyForFood};

    [Header("Stan Negocjacji")]
    [Tooltip("Co ten agent sygnalizuje innym w tej klatce?")]
    public TradeIntention currentIntent = TradeIntention.None;

    public GameObject iconFoodOffer;   // w Unity (np. czerwona kuleczka)
    public GameObject iconEnergyOffer; // w Unity (np. niebieska kuleczka)

    // --- REFERENCJA DO TRENERA (Dla Jacka) ---
    // Jeśli ten agent jest sterowany przez PPO/DQN, tutaj wpinamy "Mózg"
    [HideInInspector] public JacekBrain myBrain; 

    void Start() // Zamiast Initialize
    {
        rb = GetComponent<Rigidbody>();
    }

    // Ta funkcja jest wywoływana przez MÓZG (JacekBrain) lub przez Skrypt (FixedUpdate)
    public void ProcessAction(int action)
    {
        switch (action)
        {
            case 0: break;
            case 1: // Food -> Energy
                NegotiationAgent bestAgentFood = FindBestPartner(TradeType.FoodForEnergy);
                if (bestAgentFood != null) HandleTrade(TradeType.FoodForEnergy, bestAgentFood);
                break;
            case 2: // Energy -> Food
                NegotiationAgent bestAgentEnergy = FindBestPartner(TradeType.EnergyForFood);
                if (bestAgentEnergy != null) HandleTrade(TradeType.EnergyForFood, bestAgentEnergy);
                break;
        }
    }

    void FixedUpdate()
    {
        // 1. Logika dla Agentów SKRYPTOWYCH (Egoist/Coop/Random)
        if (currentStrategy != StrategyType.RL)
        {
            // Decyzja co 5 klatek (symulacja czasu reakcji)
            if (Time.frameCount % 5 == 0)
            {
                int scriptedAction = GetScriptedAction();
                ProcessAction(scriptedAction);
            }
        }

        // 2. Fizyka i Nagrody
        // dodajemy wagę jedzenia i energii
        float perceivedFood = food * foodUtility;
        float perceivedEnergy = energy * energyUtility;


        float imbalance = Mathf.Abs(perceivedFood - perceivedEnergy);
        currentImbalanceDebug = -imbalance;
        
        // Obliczamy karę
        float scaledPenalty = (imbalance / 100.0f) * Time.fixedDeltaTime;

        // JEŚLI mam podpięty mózg ML (Jacek), wysyłam mu nagrodę
        if (myBrain != null)
        {
            myBrain.AddReward(-scaledPenalty);
        }

        // 3. Zarządzanie czasem
        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            ResetAgent(); // Resetuje tylko siebie
            // Jeśli mam mózg, mówię mu, że to koniec epizodu
            if (myBrain != null) myBrain.EndEpisode();
        }
        
        UpdateColor();
        UpdateText();
    }

    public void ResetAgent()
    {
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        food = Random.Range(20f, 80f);
        energy = Random.Range(40f, 100f);
        episodeTimer = 0f;
    }

    // --- LOGIKA POMOCNICZA---
    private int GetScriptedAction()
    {
        switch (currentStrategy)
        {
            case StrategyType.Random: return Random.Range(0, 3);


            case StrategyType.Egoist:
                float valFood = food * foodUtility;
                float valEnergy = energy * energyUtility;
                float myDiff = valFood - valEnergy;

                if (myDiff > 2.0f) return 1;
                if (myDiff < -2.0f) return 2;
                return 0;


            case StrategyType.Cooperative: return CalculateCooperativeAction();


            default: return 0;
        }
    }

    private int CalculateCooperativeAction()
    {
        float currentGlobalError = GetGlobalImbalance();
        float errorIfAction1 = currentGlobalError;
        NegotiationAgent partner1 = FindBestPartner(TradeType.FoodForEnergy);
        if (partner1 != null) {
            float myNewErr = Mathf.Abs((food - 1) - (energy + 1));
            float pNewErr = Mathf.Abs((partner1.food + 1) - (partner1.energy - 1));
            errorIfAction1 = currentGlobalError - Mathf.Abs(food - energy) - Mathf.Abs(partner1.food - partner1.energy) + myNewErr + pNewErr;
        }
        float errorIfAction2 = currentGlobalError;
        NegotiationAgent partner2 = FindBestPartner(TradeType.EnergyForFood);
        if (partner2 != null) {
            float myNewErr = Mathf.Abs((food + 1) - (energy - 1));
            float pNewErr = Mathf.Abs((partner2.food - 1) - (partner2.energy + 1));
            errorIfAction2 = currentGlobalError - Mathf.Abs(food - energy) - Mathf.Abs(partner2.food - partner2.energy) + myNewErr + pNewErr;
        }
        if (errorIfAction1 < currentGlobalError && errorIfAction1 <= errorIfAction2) return 1;
        if (errorIfAction2 < currentGlobalError && errorIfAction2 < errorIfAction1) return 2;
        return 0;
    }

    private float GetGlobalImbalance() {
        float sum = Mathf.Abs(food - energy);
        if (agent2 != null) sum += Mathf.Abs(agent2.food - agent2.energy);
        if (agent3 != null) sum += Mathf.Abs(agent3.food - agent3.energy);
        return sum;
    }

    private NegotiationAgent FindBestPartner(TradeType tradeType) {
        NegotiationAgent[] candidates = new NegotiationAgent[] { agent2, agent3 };
        NegotiationAgent bestAgent = null;
        float bestScore = -Mathf.Infinity; 
        foreach (var candidate in candidates) {
            if (candidate == null) continue;
            float score = 0f;
            if (tradeType == TradeType.FoodForEnergy) score = candidate.energy - candidate.food;
            else if (tradeType == TradeType.EnergyForFood) score = candidate.food - candidate.energy;
            if (score > bestScore) { bestScore = score; bestAgent = candidate; }
        }
        return bestAgent;
    }

    private void HandleTrade(TradeType tradeType, NegotiationAgent targetAgent) {
        if (tradeType == TradeType.FoodForEnergy) {
            if (this.food >= 1f && targetAgent.energy >= 1f) {
                this.food -= 1f; targetAgent.energy -= 1f; this.energy += 1f; targetAgent.food += 1f;
            }
        } else if (tradeType == TradeType.EnergyForFood) {
            if (this.energy >= 1f && targetAgent.food >= 1f) {
                this.energy -= 1f; targetAgent.food -= 1f; this.food += 1f; targetAgent.energy += 1f;
            }
        }
    }

    void UpdateText() { if (statusText != null) statusText.text = $"F: {food:F0} (x{foodUtility})\nE: {energy:F0} (x{energyUtility})"; }
    void UpdateColor() {
         if (robotRenderer == null) return;
         Color good = Color.green; Color bad = Color.red;
         float norm = Mathf.Clamp01(Mathf.Abs(food - energy) / 100f);
         robotRenderer.material.color = Color.Lerp(robotRenderer.material.color, Color.Lerp(good, bad, norm), Time.deltaTime * 5f);
    }
}