using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TMPro;

public class NegotiationAgent : Agent
{
    private Rigidbody rb;
    public float moveSpeed = 10f; // Dajmy większą wartość na start
    public float food;
    public float energy;
    public TextMeshProUGUI statusText;
    public float maxEpisodeTime = 10f; // Ile sekund ma trwać epizod
    private float episodeTimer;        // Licznik czasu dla bieżącego epizodu

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Pobieramy akcje (0 lub 1) z obu gałęzi
        int moveXAction = actions.DiscreteActions[0];
        int moveZAction = actions.DiscreteActions[1];

        Vector3 controlSignal = Vector3.zero;

        // Logika dla osi X - mapujemy 0 i 1 na ruch
        if (moveXAction == 0)
        {
            controlSignal.x = 1f; // Akcja 0 -> Ruch w prawo
        }
        else // moveXAction == 1
        {
            controlSignal.x = -1f; // Akcja 1 -> Ruch w lewo
        }

        // Logika dla osi Z - mapujemy 0 i 1 na ruch
        if (moveZAction == 0)
        {
            controlSignal.z = 1f; // Akcja 0 -> Ruch do przodu
        }
        else // moveZAction == 1
        {
            controlSignal.z = -1f; // Akcja 1 -> Ruch do tyłu
        }

        Vector3 targetVelocity = controlSignal.normalized * moveSpeed;
        rb.velocity = targetVelocity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Ta linia jest kluczowa, aby naprawić błąd z konsoli
        sensor.AddObservation(0f);
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
    }

    public void Update()
    {
        if (statusText != null)
        {
            statusText.text = $"Name: {gameObject.name}\nFood: {food.ToString("F1")}\nEnergy: {energy.ToString("F1")}";
        }
    }

    void FixedUpdate()
    {
        // Dodajemy czas, który upłynął od ostatniej klatki fizyki
        episodeTimer += Time.fixedDeltaTime;

        // Sprawdzamy, czy czas epizodu został przekroczony
        if (episodeTimer >= maxEpisodeTime)
        {
            Debug.Log("Czas minął! Reset epizodu.");
            EndEpisode(); // Kończymy epizod i wywołujemy OnEpisodeBegin()
        }
    }
}
