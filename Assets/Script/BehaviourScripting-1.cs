using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;  // NEW input system
using CityPeople;               // <-- needed to see CityPeople.CityPeople

public class BehaviourScripting : MonoBehaviour
{
    private NavMeshAgent _agent;
    private CityPeople.CityPeople _cityPeople;   // <-- reference to CityPeople

    [Header("Behaviour Data")]
    public BehaviourDataLoader behaviourDataLoader;  // assign in Inspector

    private enum State
    {
        Idle,
        Walking,
        Waiting
    }

    [SerializeField]
    private State currentState;

    private State _lastState;   // <-- to detect state changes

    // triggers that we have already waited at and should ignore next time
    private HashSet<Collider> _ignoredTriggers = new HashSet<Collider>();

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            enabled = false;
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("NavMeshAgent is NOT on the NavMesh at start!");
        }

        // get CityPeople script on the same GameObject
        _cityPeople = GetComponent<CityPeople.CityPeople>();
        if (_cityPeople == null)
        {
            Debug.LogWarning("CityPeople component not found on " + gameObject.name);
        }

        currentState = State.Idle;
        _lastState = currentState;

        // play initial animation
        ApplyAnimationForState(currentState);
    }

    void Update()
    {
        // NEW INPUT SYSTEM: left mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            MoveToMousePosition();
        }

        UpdateStateFromAgent();

        // Only change animation when state actually changes
        if (currentState != _lastState)
        {
            ApplyAnimationForState(currentState);
            _lastState = currentState;
        }
    }

    void MoveToMousePosition()
    {
        if (_agent == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("No camera with tag 'MainCamera' found in the scene.");
            return;
        }

        if (Mouse.current == null)
        {
            Debug.LogError("Mouse.current is null – check new Input System setup.");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name + " at " + hit.point);

            bool set = _agent.SetDestination(hit.point);  // Unity 6 returns bool
            Debug.Log("SetDestination called, success = " + set);
        }
        else
        {
            Debug.Log("Raycast did NOT hit anything.");
        }
    }

    void UpdateStateFromAgent()
    {
        if (_agent == null) return;

        // IMPORTANT: if we’re Waiting, do not override the state
        if (currentState == State.Waiting)
            return;

        if (_agent.pathPending)
            return;

        if (_agent.remainingDistance > _agent.stoppingDistance)
        {
            currentState = State.Walking;
        }
        else
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.001f)
            {
                currentState = State.Idle;
            }
        }
    }

    // Map states to your specific clips
    void ApplyAnimationForState(State state)
    {
        if (_cityPeople == null) return;

        switch (state)
        {
            case State.Idle:
                _cityPeople.PlayClipByName("idle_f_2_190f");
                break;

            case State.Walking:
                _cityPeople.PlayClipByName("locom_f_basicWalk_30f");
                break;

            case State.Waiting:
                _cityPeople.PlayClipByName("idle_f_1_150f");
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter with " + other.gameObject.name + " (tag: " + other.tag + ")");

        // Only react to objects tagged "Collider"
        if (!other.CompareTag("Collider"))
            return;

        // Ignore triggers that we already waited at
        if (_ignoredTriggers.Contains(other))
            return;

        // If we're already waiting, do nothing
        if (currentState == State.Waiting)
            return;

        // Get objectId from the trigger (matches '#' column in CSV)
        BehaviourObjectId idComponent = other.GetComponent<BehaviourObjectId>();
        if (idComponent == null)
        {
            Debug.LogWarning("Trigger has no BehaviourObjectId: " + other.gameObject.name);
            return;
        }

        string objectId = idComponent.objectId;

        if (behaviourDataLoader == null)
        {
            Debug.LogWarning("No BehaviourDataLoader assigned on NPC.");
            return;
        }

        // Ask data loader if we should stop here, and how long
        if (!behaviourDataLoader.TryGetStopDecision(objectId, out float waitSeconds))
        {
            // Random choice said: walk past
            return;
        }

        Debug.Log("Entered WAITING trigger with " + other.gameObject.name +
                  " | objectId: " + objectId +
                  " | waitSeconds: " + waitSeconds);

        currentState = State.Waiting;

        if (_agent != null)
        {
            // Stop but keep the path so we can resume
            _agent.isStopped = true;
        }

        // wait for the given time, then resume and ignore this trigger next time
        StartCoroutine(WaitAndResume(other, waitSeconds));
    }

    private System.Collections.IEnumerator WaitAndResume(Collider trigger, float waitSeconds)
    {
        // wait according to CSV
        yield return new WaitForSeconds(waitSeconds);

        // add this trigger to ignore list
        _ignoredTriggers.Add(trigger);

        if (_agent != null)
        {
            _agent.isStopped = false;   // resume movement along the same path
        }

        // set state to Walking so the walking animation plays again
        currentState = State.Walking;
        ApplyAnimationForState(currentState);
    }
}