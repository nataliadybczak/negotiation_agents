using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Globalization;

public enum StrategyType {RL, Egoist, Cooperative, Random};

public class NegotiationAgent : MonoBehaviour
{
    private Rigidbody rb;
    public float food;
    public float energy;    
    public TextMeshProUGUI statusText;
    public float maxEpisodeTime = 10f;
    private float episodeTimer;

    private string logFilePath;
    private float logTimer = 0f;
    private int stepCounter = 0;

    private NegotiationAgent[] allAgentsCache;

    private Vector3 targetPosition; 
    public float moveSpeed = 1.0f;
    
    public NegotiationAgent agent2;
    public NegotiationAgent agent3;
    [Header("Ustawienia Strategii")]
    public StrategyType currentStrategy = StrategyType.RL;

    [Header("Ekonomia (Preferencje)")]
    [Tooltip("Ile warta jest 1 jednostka jedzenia dla tego agenta?")]
    public float foodUtility = 1.0f; 
    
    [Tooltip("Ile warta jest 1 jednostka energii dla tego agenta?")]
    public float energyUtility = 1.0f;

    [Header("Ekonomia (Fizjologia)")]
    [Tooltip("Ile zasobów (Food ORAZ Energy) agent zużywa na sekundę.")]
    public float metabolismRate = 2.0f; 

    [Tooltip("Ile JEDZENIA agent produkuje na sekundę (Regeneracja).")]
    public float foodProductionRate = 0.0f;

    [Tooltip("Ile ENERGII agent produkuje na sekundę (Regeneracja).")]
    public float energyProductionRate = 0.0f;

    public enum TradeType {FoodForEnergy, EnergyForFood};
    public float currentImbalanceDebug;
    public Renderer robotRenderer; 

    public enum TradeIntention {None, OfferFoodForEnergy, OfferEnergyForFood};

    [Header("Stan Negocjacji")]
    [Tooltip("Co ten agent sygnalizuje innym w tej klatce?")]
    public TradeIntention currentIntent = TradeIntention.None;

    [Header("Ograniczenia Handlu")]
    public float tradeCooldownDuration = 2.0f;
    private float currentTradeCooldown = 0f;

    [Header("Stabilizacja Wizualna")]
    public float minFlagTime = 0.5f; 
    private float flagTimer = 0f;    

    public GameObject iconFoodOffer;   
    public GameObject iconEnergyOffer;

    [Header("Ustawienia Środowiska")]
    public float spawnRange = 12f;


    //jeśli ten agent jest sterowany przez PPO/DQN, tutaj wpinamy jemu mózg
    [HideInInspector] public JacekBrain myBrain; 

    void Start() 
    {
        rb = GetComponent<Rigidbody>();
        targetPosition = transform.localPosition;

        logFilePath = "Log_" + gameObject.name + ".csv";
        if (!File.Exists(logFilePath)) {
            File.WriteAllText(logFilePath, "Step,Food,Energy,Imbalance\n");
        }
    }

    public void ProcessAction(int action)
    {
        if (flagTimer > 0)
        {
            flagTimer -= Time.fixedDeltaTime;
        }
        
        switch (action)
        {
            case 0: 
                if (flagTimer <= 0)
                {
                    currentIntent = TradeIntention.None;
                }
                break;
            case 1: 
                currentIntent = TradeIntention.OfferFoodForEnergy; 
                flagTimer = minFlagTime;
                break;
            case 2: 
                currentIntent = TradeIntention.OfferEnergyForFood; 
                flagTimer = minFlagTime;
                break;
        }

        UpdateVisuals();

        if (currentIntent != TradeIntention.None)
        {
            TryExecuteHandshake();
        }
    }

    private void TryExecuteHandshake()
    {
        if (currentTradeCooldown > 0) return;

        NegotiationAgent partner = FindWillingPartner();
        if (partner != null)
        {
            if (partner.currentTradeCooldown <= 0)
            {
                PerformTrade(partner);
                
                this.currentTradeCooldown = tradeCooldownDuration;
                partner.currentTradeCooldown = tradeCooldownDuration;
                
                this.currentIntent = TradeIntention.None;
                partner.currentIntent = TradeIntention.None;
            }
        }
    }

    private NegotiationAgent FindWillingPartner()
    {
        NegotiationAgent[] candidates = new NegotiationAgent[] { agent2, agent3 };
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            if (candidate.currentIntent == TradeIntention.None) continue;

            if (this.currentIntent == TradeIntention.OfferFoodForEnergy && candidate.currentIntent == TradeIntention.OfferEnergyForFood)
            {
                return candidate;
            }
            else if (this.currentIntent == TradeIntention.OfferEnergyForFood && candidate.currentIntent == TradeIntention.OfferFoodForEnergy)
            {
                return candidate;
            }
        }
        return null;
    }

    private void PerformTrade(NegotiationAgent partner)
    {
        if (currentIntent == TradeIntention.OfferFoodForEnergy)
        {
            if (this.food >= 1f && partner.energy >= 1f)
            {
                this.food -= 1f;
                partner.energy -= 1f;
                this.energy += 1f;
                partner.food += 1f;

                if (myBrain != null) myBrain.AddReward(0.2f);
            }
        }
        else
        {
            if (this.energy >= 1 && partner.food >= 1)
            {
                this.energy -= 1; this.food += 1;
                partner.energy -= 1; partner.food += 1;
                if (myBrain != null) myBrain.AddReward(0.2f);
            }
        }
    }

    void UpdateVisuals()
    {
        if (iconFoodOffer) iconFoodOffer.SetActive(currentIntent == TradeIntention.OfferFoodForEnergy);
        if (iconEnergyOffer) iconEnergyOffer.SetActive(currentIntent == TradeIntention.OfferEnergyForFood);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        UpdateNearestNeighbors();

        food += (foodProductionRate - metabolismRate) * dt;
        energy += (energyProductionRate - metabolismRate) * dt;

        food = Mathf.Clamp(food, 0f, 100f);
        energy = Mathf.Clamp(energy, 0f, 100f);

        //? czy śmierc z głodu
        if (food <= 0f && energy <= 0f)
        {
            if (myBrain != null) 
            {
                myBrain.AddReward(-10f); 
                myBrain.EndEpisode();   
            }
            else 
            {
                ResetAgent();
            }
            return; 
        }
        
        if (currentTradeCooldown > 0)
        {
            currentTradeCooldown -= dt;
        }
        if (currentStrategy != StrategyType.RL)
        {
            if (Time.frameCount % 5 == 0)
            {
                int scriptedAction = GetScriptedAction();
                ProcessAction(scriptedAction);
            }
        }


        float perceivedFood = food * foodUtility;
        float perceivedEnergy = energy * energyUtility;


        float imbalance = Mathf.Abs(perceivedFood - perceivedEnergy);
        currentImbalanceDebug = -imbalance;

        float wealth = (perceivedFood + perceivedEnergy) / 200.0f;

        float reward = (wealth * 1.0f) - (imbalance / 100.0f * 2.0f);
        
        float scaledPenalty = reward * dt;

        if (myBrain != null)
        {
            myBrain.AddReward(-scaledPenalty);
        }

        episodeTimer += dt;
        if (episodeTimer >= maxEpisodeTime)
        {
            ResetAgent(); 
            if (myBrain != null) myBrain.EndEpisode();
        }

        if (robotRenderer == null) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * moveSpeed);
        
        UpdateColor();
        UpdateText();

        logTimer += Time.fixedDeltaTime;
    
        if (logTimer > 0.1f) 
        {
            logTimer = 0f; 

            float currentImbalance = Mathf.Abs((food * foodUtility) - (energy * energyUtility));
            
            stepCounter++;

            //KROPKA (ma byc dla tych google Sheets)
            string sFood = food.ToString("F2", CultureInfo.InvariantCulture);
            string sEnergy = energy.ToString("F2", CultureInfo.InvariantCulture);
            string sImbalance = currentImbalance.ToString("F2", CultureInfo.InvariantCulture);

            string line = $"{stepCounter},{sFood},{sEnergy},{sImbalance}\n";

            File.AppendAllText(logFilePath, line);
        }
    }

    public void ResetAgent()
    {
        float randomX = UnityEngine.Random.Range(-spawnRange, spawnRange);
        float randomZ = UnityEngine.Random.Range(-spawnRange, spawnRange);

        targetPosition = new Vector3(randomX, 0.5f, randomZ);
        transform.localPosition = targetPosition;

        food = UnityEngine.Random.Range(20f, 80f);
        energy = UnityEngine.Random.Range(40f, 100f);
        episodeTimer = 0f;

        //ich zawody: rolnik elektryk biedak

        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll < 0.33f)
        {
            foodProductionRate = 8f;
            energyProductionRate = 0f;
        }
        else if (roll < 0.66f)
        {
            foodProductionRate = 0f;
            energyProductionRate = 8f;
        }
        else
        {
            foodProductionRate = 2f;
            energyProductionRate = 2f;
        }
    }

    private int GetScriptedAction()
    {
        switch (currentStrategy)
        {
            case StrategyType.Random: return UnityEngine.Random.Range(0, 3);


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

    public void UpdateNearestNeighbors()
    {
        if (allAgentsCache == null || allAgentsCache.Length != 6) 
        {
            allAgentsCache = FindObjectsOfType<NegotiationAgent>();
        }

        var potentialPartners = new List<NegotiationAgent>();
        foreach (var a in allAgentsCache)
        {
            if (a != null && a != this && a.gameObject.activeSelf)
            {
                potentialPartners.Add(a);
            }
        }

        potentialPartners.Sort((a, b) => 
        {
            float distA = Vector3.Distance(transform.position, a.transform.position);
            float distB = Vector3.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });

        agent2 = (potentialPartners.Count >= 1) ? potentialPartners[0] : null;
        agent3 = (potentialPartners.Count >= 2) ? potentialPartners[1] : null;


        if (agent2 != null) 
            Debug.DrawLine(transform.position, agent2.transform.position, Color.green); //pierwszy najblizszy
        if (agent3 != null) 
            Debug.DrawLine(transform.position, agent3.transform.position, Color.yellow); //drugi
    }

    void UpdateText() { if (statusText != null) statusText.text = $"F: {food:F0} (x{foodUtility})\nE: {energy:F0} (x{energyUtility})"; }
    void UpdateColor() {
        if (robotRenderer == null) return;
        if (food <= 1f && energy <= 1f)
        {
            robotRenderer.material.color = Color.Lerp(robotRenderer.material.color, Color.black, Time.deltaTime * 5f);
            return; 
        }

        Color good = Color.green; Color bad = Color.red;

        float perceivedFood = food * foodUtility;
        float perceivedEnergy = energy * energyUtility;

        float imbalance = Mathf.Abs(perceivedFood - perceivedEnergy);
        float norm = Mathf.Clamp01(imbalance / 100.0f);

        Color targetColor = Color.Lerp(good, bad, norm);
        robotRenderer.material.color = Color.Lerp(robotRenderer.material.color, targetColor, Time.deltaTime * 5f);
    }
}