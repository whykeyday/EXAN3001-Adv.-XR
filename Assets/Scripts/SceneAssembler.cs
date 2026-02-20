using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages scene transitions in a VR meditation app.
/// When user touches a "Memory Fragment", particles assemble to form the full scene.
/// </summary>
public class SceneAssembler : MonoBehaviour
{
    // ============ STATE MANAGEMENT ============
    public enum AppState
    {
        Hub,           // User is in hub world
        Transitioning, // Assembly animation is playing
        InScene        // User is inside a memory scene
    }

    [Header("State")]
    public AppState currentState = AppState.Hub;

    // ============ SCENE REFERENCES ============
    [Header("Hub Fragments (Small particle clusters in hub)")]
    [Tooltip("The 3 floating fragments in the Hub world")]
    public GameObject[] hubFragments;

    [Header("Full Scenes (The complete memory scenes: Tree, Ocean, Cat)")]
    [Tooltip("Index 0 = Forest, 1 = Ocean, 2 = Cat")]
    public GameObject[] fullScenes;

    [Header("Gathering Effect Particle Systems")]
    [Tooltip("One gathering effect per scene, or use a single shared one")]
    public ParticleSystem[] gatheringEffects;

    // ============ TRANSITION SETTINGS ============
    [Header("Transition Settings")]
    [Tooltip("Duration of the assembly animation in seconds")]
    public float transitionDuration = 2.0f;

    [Tooltip("Delay before starting to fade in the target scene")]
    public float fadeInDelay = 0.5f;

    // ============ PRIVATE VARS ============
    private int currentSceneIndex = -1;
    private List<ParticleSystem> currentSceneParticles = new List<ParticleSystem>();

    // ============ PUBLIC API ============

    /// <summary>
    /// Call this to select and transition to a scene.
    /// Can be called from UI, hand interaction, or other scripts.
    /// </summary>
    /// <param name="sceneIndex">0 = Forest, 1 = Ocean, 2 = Cat</param>
    public void SelectScene(int sceneIndex)
    {
        if (currentState != AppState.Hub)
        {
            Debug.LogWarning("SceneAssembler: Cannot select scene while not in Hub state.");
            return;
        }

        if (sceneIndex < 0 || sceneIndex >= fullScenes.Length)
        {
            Debug.LogError($"SceneAssembler: Invalid scene index {sceneIndex}");
            return;
        }

        StartCoroutine(AssembleSceneCoroutine(sceneIndex));
    }

    /// <summary>
    /// Return to the Hub world from any scene.
    /// </summary>
    public void ReturnToHub()
    {
        if (currentState == AppState.Hub)
        {
            Debug.LogWarning("SceneAssembler: Already in Hub.");
            return;
        }

        StartCoroutine(DisassembleSceneCoroutine());
    }

    // ============ COLLISION DETECTION ============

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player's hand
        if (other.CompareTag("PlayerHand"))
        {
            // Determine which fragment was touched
            // This script can be attached to SceneAssembler OR to each fragment
            // If attached to fragments, use GetComponent pattern below
            Debug.Log("SceneAssembler: Hand detected on fragment!");
        }
    }

    // ============ ASSEMBLY COROUTINE ============

    private IEnumerator AssembleSceneCoroutine(int sceneIndex)
    {
        currentState = AppState.Transitioning;
        currentSceneIndex = sceneIndex;
        Debug.Log($"SceneAssembler: Starting assembly for scene {sceneIndex}");

        // Step 1: Hide all Hub Fragments
        foreach (var fragment in hubFragments)
        {
            if (fragment != null)
                fragment.SetActive(false);
        }

        // Step 2: Enable target scene but set particles to invisible (size 0)
        GameObject targetScene = fullScenes[sceneIndex];
        if (targetScene != null)
        {
            targetScene.SetActive(true);
            
            // Get all particle systems in the target scene
            currentSceneParticles.Clear();
            currentSceneParticles.AddRange(targetScene.GetComponentsInChildren<ParticleSystem>());

            // Set all particles to size 0 initially
            foreach (var ps in currentSceneParticles)
            {
                var main = ps.main;
                main.startSize = 0f;
            }
        }

        // Step 3: Play the gathering effect (particles flying inward)
        if (gatheringEffects != null && sceneIndex < gatheringEffects.Length && gatheringEffects[sceneIndex] != null)
        {
            ParticleSystem gatherPS = gatheringEffects[sceneIndex];
            gatherPS.gameObject.SetActive(true);
            gatherPS.Clear();
            gatherPS.Play();
        }

        // Step 4: Wait for fade-in delay, then gradually increase particle size
        yield return new WaitForSeconds(fadeInDelay);

        float elapsed = 0f;
        float fadeDuration = transitionDuration - fadeInDelay;
        float targetSize = 0.05f; // Final particle size (adjust as needed)

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Ease-out curve for smoother appearance
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float currentSize = Mathf.Lerp(0f, targetSize, easedT);

            // Apply size to all particles in the scene
            foreach (var ps in currentSceneParticles)
            {
                var main = ps.main;
                main.startSize = currentSize;
            }

            yield return null;
        }

        // Ensure final size is set
        foreach (var ps in currentSceneParticles)
        {
            var main = ps.main;
            main.startSize = targetSize;
        }

        // Step 5: Stop gathering effect
        if (gatheringEffects != null && sceneIndex < gatheringEffects.Length && gatheringEffects[sceneIndex] != null)
        {
            gatheringEffects[sceneIndex].Stop();
        }

        currentState = AppState.InScene;
        Debug.Log($"SceneAssembler: Assembly complete. Now in scene {sceneIndex}");
    }

    // ============ DISASSEMBLY COROUTINE ============

    private IEnumerator DisassembleSceneCoroutine()
    {
        currentState = AppState.Transitioning;
        Debug.Log("SceneAssembler: Disassembling scene, returning to Hub...");

        float elapsed = 0f;
        float fadeDuration = transitionDuration * 0.5f; // Faster exit
        float startSize = 0.05f;

        // Fade out current scene particles
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float currentSize = Mathf.Lerp(startSize, 0f, t);

            foreach (var ps in currentSceneParticles)
            {
                var main = ps.main;
                main.startSize = currentSize;
            }

            yield return null;
        }

        // Hide current scene
        if (currentSceneIndex >= 0 && currentSceneIndex < fullScenes.Length)
        {
            fullScenes[currentSceneIndex].SetActive(false);
        }

        // Show hub fragments
        foreach (var fragment in hubFragments)
        {
            if (fragment != null)
                fragment.SetActive(true);
        }

        currentSceneIndex = -1;
        currentSceneParticles.Clear();
        currentState = AppState.Hub;
        Debug.Log("SceneAssembler: Returned to Hub.");
    }
}
