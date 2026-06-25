using UnityEngine;
using UnityEngine.AI;

public class bezoekerslopen : MonoBehaviour
{
    // Reference to the NavMeshAgent component
    private NavMeshAgent _agent;

    // GameObject to move to
    [SerializeField]
    private GameObject _target;

    void Start()
    {
        // Get the NavMeshAgent on this GameObject
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("Nav Mesh Agent is Null");
        }
    }

    void Update()
    {
        // Set destination to the target
        if (_agent != null && _target != null)
        {
            _agent.SetDestination(_target.transform.position);
        }
    }
}