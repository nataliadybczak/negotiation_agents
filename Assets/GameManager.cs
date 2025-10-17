using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public NegotiationAgent[] agents; // lista agentów przypisana w Inspectorze
    public TextMeshProUGUI totalText; // tekst do wyświetlania sumy

    private float updateInterval = 1.0f; // co ile sekund aktualizować
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdateTotalResources();
            timer = 0f;
        }
    }

    void UpdateTotalResources()
    {
        // int totalFood = 0;
        // int totalEnergy = 0;

        // foreach (var agent in agents)
        // {
        //     if (agent != null)
        //     {
        //         totalFood += agent.food;
        //         totalEnergy += agent.energy;
        //     }
        // }

        // totalText.text = $"TOTAL RESOURCES\n Food: {totalFood}\n Energy: {totalEnergy}";
    }
}
