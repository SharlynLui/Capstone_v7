using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A utility class to safely execute actions on Unity's main thread.
/// Useful when background threads (e.g., MQTT callbacks) need to
/// modify Unity objects (like UI or GameObjects), which must
/// only be accessed on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    // Queue to hold actions that need to run on the main thread
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    /// <summary>
    /// Enqueue an action to be executed on the main thread.
    /// Thread-safe: can be called from any background thread.
    /// </summary>
    /// <param name="action">The action to execute on the main thread.</param>
    public static void Enqueue(Action action)
    {
        if (action == null) return;

        // Lock to ensure thread-safe access to the queue
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Called every frame by Unity.
    /// Executes all queued actions on the main thread.
    /// </summary>
    void Update()
    {
        // Lock queue to prevent race conditions
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                // Dequeue and invoke each action
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}