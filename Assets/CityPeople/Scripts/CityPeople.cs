using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.AI;
 
namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        [Header("Character Type")]
        [SerializeField]
        private bool isMale = false;   // tick this in Inspector for male NPCs
 
        [Header("Animation Clip Names")]
        [SerializeField] private string femaleIdleClip      = "idle_f_2_190f";
        [SerializeField] private string femaleWaitClip      = "idle_f_1_150f";
        [SerializeField] private string femaleWalkClip      = "locom_f_basicWalk_30f";
 
        [SerializeField] private string maleIdleClip        = "idle_m_2_220f";
        [SerializeField] private string maleWaitClip        = "idle_m_1_200f";
        [SerializeField] private string maleWalkClip        = "locom_m_basicWalk_30f";
 
        // ====== NAVMESH / PATH FOLLOWING ======
        private NavMeshAgent _agent;
 
        [Header("Path Targets (waypoints in order)")]
        [SerializeField]
        private Transform[] _targets;
 
        private int _currentTargetIndex = 0;
 
        // ====== STATE & BEHAVIOUR DATA ======
        private enum State
        {
            Idle,
            Walking,
            Waiting
        }
 
        [SerializeField]
        private State currentState = State.Idle;
 
        private State _lastState;
 
        [Header("Behaviour Data (CSV)")]
        public BehaviourDataLoader behaviourDataLoader;  // assign in Inspector if possible
 
        // triggers that we have already waited at and should ignore next time
        private HashSet<Collider> _ignoredTriggers = new HashSet<Collider>();
 
        // ====== ANIMATION & PALETTE ======
        [Tooltip("Autoplay random animation clips (NOT recommended for walking NPCs)")]
        [SerializeField]
        private bool AutoPlayAnimations = false;   // set to false for path NPCs
 
        [SerializeField]
        [Tooltip("Overrides palette materials, skips other objects")]
        private Material PaletteOverride;
 
        public string CurrentPaletteName { get; private set; }
 
        private AnimationClip[] myClips;
        private Animator animator;
        public const string people_pal_prefix = "people_pal";
        private List<Renderer> _paletteMeshes;
 
        private void Awake()
        {
            // Cache animator
            animator = GetComponent<Animator>();
 
            // Palette handling
            var AllRenderers = gameObject.GetComponentsInChildren<Renderer>();
            _paletteMeshes = new List<Renderer>();
            foreach (Renderer r in AllRenderers)
            {
                var matName = r.sharedMaterial != null ? r.sharedMaterial.name : string.Empty;
                var len = Math.Min(people_pal_prefix.Length, matName.Length);
                if (len > 0 && matName.Substring(0, len) == CityPeople.people_pal_prefix)
                {
                    _paletteMeshes.Add(r);
                }
            }
            if (_paletteMeshes.Count > 0)
            {
                CurrentPaletteName = _paletteMeshes[0].sharedMaterial.name;
            }
 
            if (PaletteOverride != null)
            {
                SetPalette(PaletteOverride);
            }
        }
 
        void Start()
        {
            // ====== NavMeshAgent setup ======
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                Debug.LogError("NavMeshAgent is NULL on " + gameObject.name);
                return;
            }
 
            if (!_agent.isOnNavMesh)
            {
                Debug.LogError("NavMeshAgent is NOT on NavMesh at start on " + gameObject.name);
            }
 
            // --- FIX: als er geen targets zijn, niet crashen maar idle blijven ---
            if (_targets == null || _targets.Length == 0)
            {
                Debug.LogWarning("No targets assigned to CityPeople on " + gameObject.name +
                                 ". NPC will stay idle and not follow a path.");
                _agent.isStopped = true;
                currentState = State.Idle;
            }
            else
            {
                // Begin at the first target
                _currentTargetIndex = 0;
                _agent.SetDestination(_targets[_currentTargetIndex].position);
                currentState = State.Walking;
            }
 
            // ====== Animation clips ======
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                myClips = animator.runtimeAnimatorController.animationClips;
            }
 
            // Optional old behaviour: random clip shuffling
            if (AutoPlayAnimations && myClips != null && myClips.Length > 0)
            {
                StartCoroutine(ShuffleClips());
            }
 
            _lastState = currentState;
            ApplyAnimationForState(currentState);
 
            // Collider for click detection (optional)
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.8f, 0f);
            collider.radius = 0.3f;
            collider.height = 1.77f;
            collider.direction = 1;
 
            // --- FIX: automatische fallback voor BehaviourDataLoader ---
            if (behaviourDataLoader == null)
            {
                behaviourDataLoader = FindObjectOfType<BehaviourDataLoader>();
                if (behaviourDataLoader == null)
                {
                    Debug.LogWarning("No BehaviourDataLoader found in scene for " + gameObject.name +
                                     ". NPC will not use CSV-based stop behaviour.");
                }
            }
        }
 
        void Update()
        {
            if (_agent == null)
                return;
 
            // Als er geen targets zijn, dan alleen animatiestaat bijwerken
            if (_targets == null || _targets.Length == 0)
            {
                // eventueel kun je hier nog logica toevoegen voor random rondkijken etc.
                return;
            }
 
            // If we're waiting because of a stop trigger, do not change destination/state here.
            if (currentState == State.Waiting)
                return;
 
            // Check if we've reached current target
            if (!_agent.pathPending &&
                _agent.remainingDistance <= _agent.stoppingDistance &&
                !_agent.hasPath)
            {
                GoToNextTarget();
            }
 
            // Update state based on movement (only if not waiting)
            UpdateStateFromAgent();
 
            // Only change animation when state actually changes
            if (currentState != _lastState)
            {
                ApplyAnimationForState(currentState);
                _lastState = currentState;
            }
        }
 
        // ====== WAYPOINT LOGIC ======
        private void GoToNextTarget()
        {
            if (_targets == null || _targets.Length == 0)
                return;
 
            _currentTargetIndex++;
 
            // Stop after the last target
            if (_currentTargetIndex >= _targets.Length)
            {
                _agent.isStopped = true;
                currentState = State.Idle;
                ApplyAnimationForState(currentState);
                return;
            }
 
            _agent.isStopped = false;
            _agent.SetDestination(_targets[_currentTargetIndex].position);
            currentState = State.Walking;
            ApplyAnimationForState(currentState);
        }
 
        private void UpdateStateFromAgent()
        {
            if (_agent == null)
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
 
        // ====== PALETTE ======
        public void SetPalette(Material mat)
        {
            if (mat != null)
            {
                if (mat.name.Length >= people_pal_prefix.Length &&
                    mat.name.Substring(0, people_pal_prefix.Length) == CityPeople.people_pal_prefix)
                {
                    CurrentPaletteName = mat.name;
                    foreach (Renderer r in _paletteMeshes)
                    {
                        if (r != null)
                        {
                            r.material = mat;
                        }
                    }
                }
                else
                {
                    Debug.Log("Material name should start with 'people_pal...' by convention.");
                }
            }
        }
 
        // ====== ANIMATIONS ======
        public void PlayAnyClip()
        {
            if (myClips != null && myClips.Length > 0)
            {
                var cl = myClips[Random.Range(0, myClips.Length)];
                animator.CrossFadeInFixedTime(cl.name, 1.0f, -1, Random.value * cl.length);
            }
            else Debug.LogWarning("Missing animation clips on " + gameObject.name);
        }
 
        IEnumerator ShuffleClips()
        {
            while (true)
            {
                yield return new WaitForSeconds(15.0f + Random.value * 5.0f);
                PlayAnyClip();
            }
        }
 
        // Play a specific animation clip by name
        public void PlayClipByName(string clipName, float transitionTime = 0.1f)
        {
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on " + gameObject.name);
                return;
            }
 
            if (string.IsNullOrEmpty(clipName))
            {
                Debug.LogWarning("PlayClipByName called with empty clipName on " + gameObject.name);
                return;
            }
 
            animator.CrossFadeInFixedTime(clipName, transitionTime);
        }
 
        // Map states to specific clips
        private void ApplyAnimationForState(State state)
        {
            if (animator == null)
                return;
        
            // Choose which set of clips to use
            string idleClip, waitClip, walkClip;
            if (isMale)
            {
                idleClip = maleIdleClip;
                waitClip = maleWaitClip;
                walkClip = maleWalkClip;
            }
            else
            {
                idleClip = femaleIdleClip;
                waitClip = femaleWaitClip;
                walkClip = femaleWalkClip;
            }
        
            switch (state)
            {
                case State.Idle:
                    PlayClipByName(idleClip);
                    break;
        
                case State.Walking:
                    PlayClipByName(walkClip);
                    break;
        
                case State.Waiting:
                    PlayClipByName(waitClip);
                    break;
            }
        }
 
        // ====== COLLISION WITH EXHIBIT TRIGGERS (CSV‑DRIVEN STOPS) ======
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
 
            // --- FIX: lege objectId's negeren om ruis in logs te voorkomen ---
            if (string.IsNullOrWhiteSpace(objectId))
            {
                Debug.LogWarning("BehaviourObjectId has empty objectId on trigger: " + other.gameObject.name);
                return;
            }
 
            if (behaviourDataLoader == null)
            {
                Debug.LogWarning("No BehaviourDataLoader assigned on " + gameObject.name);
                return;
            }
 
            // Ask data loader if we should stop here, and how long
            if (!behaviourDataLoader.TryGetStopDecision(objectId, out float waitSeconds))
            {
                // Random choice or missing rule: walk past
                return;
            }
 
            Debug.Log("Entered WAITING trigger with " + other.gameObject.name +
                      " | objectId: " + objectId +
                      " | waitSeconds: " + waitSeconds);
 
            // Switch to waiting state
            currentState = State.Waiting;
            ApplyAnimationForState(currentState);
 
            if (_agent != null)
            {
                _agent.isStopped = true;
            }
 
            // wait for the given time, then resume and ignore this trigger next time
            StartCoroutine(WaitAndResume(other, waitSeconds));
        }
 
        private IEnumerator WaitAndResume(Collider trigger, float waitSeconds)
        {
            // wait according to CSV
            yield return new WaitForSeconds(waitSeconds);
 
            // add this trigger to ignore list
            _ignoredTriggers.Add(trigger);
 
            if (_agent != null)
            {
                _agent.isStopped = false;
            }
 
            // After waiting, we go back to Walking;
            // Update() will continue sending us along the path.
            currentState = State.Walking;
            ApplyAnimationForState(currentState);
        }
    }
}