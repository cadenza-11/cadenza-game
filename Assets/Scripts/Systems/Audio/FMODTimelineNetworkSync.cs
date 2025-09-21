using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace Cadenza
{
    /// <summary>
    /// Network message containing timeline sync data from host to clients
    /// </summary>
    public struct TimelineSyncData : INetworkSerializable
    {
        public int timelinePosition; // Position in milliseconds
        public double timestamp; // Network time when this position was captured

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref timelinePosition);
            serializer.SerializeValue(ref timestamp);
        }
    }

    /// <summary>
    /// Main synchronization component that handles FMOD timeline sync across network
    /// </summary>
    public class FMODTimelineNetworkSync : NetworkBehaviour
    {
        private static FMODTimelineNetworkSync singleton;

        [Header("FMOD Configuration")]
        [SerializeField] private EventReference timelineEventRef;
        [SerializeField] private float driftTolerance = 30f; // milliseconds
        [SerializeField] private float networkAnticipation = 25f; // milliseconds - how early host sends

        [Header("Runtime Info (Read Only)")]
        [SerializeField] private bool isSynced = false;
        [SerializeField] private float lastDriftAmount = 0f;
        [SerializeField] private int currentBar = 0;
        [SerializeField] private int currentBeat = 0;

        private EventInstance timelineInstance;
        private EVENT_CALLBACK beatCallback;

        // Sync state
        private bool pendingSync = false;
        private int targetSyncPosition = 0;
        private bool isAwaitingBarBoundary = false;
        private Coroutine syncCoroutine;

        // Network messaging
        private const string SYNC_REQUEST_RPC = "RequestTimelineSync";
        private const string SYNC_BROADCAST_RPC = "ReceiveTimelineSync";

        public override void OnNetworkSpawn()
        {
            if (singleton == null)
                singleton = this;
            else
                Destroy(this);

            // Initialize FMOD timeline instance
            InitializeTimeline();

            Debug.Log($"[FMODSync] Sync initialized for player {this.OwnerClientId} [host={this.IsHost}, client={this.IsClient}]");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                    StartHostBroadcasting();
                else if (IsClient)
                    RequestSyncFromHost();
            }
        }

        public override void OnDestroy()
        {
            if (timelineInstance.isValid())
            {
                timelineInstance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
                timelineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                timelineInstance.release();
            }
        }

        private void InitializeTimeline()
        {
            // Create the timeline instance (assuming it's already playing from game start)
            timelineInstance = RuntimeManager.CreateInstance(timelineEventRef);

            // Set up beat callback
            beatCallback = new EVENT_CALLBACK(BeatCallbackHandler);
            timelineInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

            // Play timeline if not already playing
            timelineInstance.getPlaybackState(out PLAYBACK_STATE state);
            if (state != PLAYBACK_STATE.PLAYING)
                timelineInstance.start();

            Debug.Log($"[FMODSync] Started global track.");
        }

        /// <summary>
        /// FMOD beat callback - triggered on every beat
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static FMOD.RESULT BeatCallbackHandler(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            // Get the instance that triggered this callback
            EventInstance instance = new EventInstance(instancePtr);

            // Extract beat properties
            if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
            {
                var param = (TIMELINE_BEAT_PROPERTIES)System.Runtime.InteropServices.Marshal.PtrToStructure(
                    parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));

                if (singleton.timelineInstance.handle == instance.handle)
                    singleton.OnBeatCallback(param.bar, param.beat, param.position);
            }

            return FMOD.RESULT.OK;
        }

        /// <summary>
        /// Called on every beat from FMOD callback
        /// </summary>
        private void OnBeatCallback(int bar, int beat, float position)
        {
            currentBar = bar;
            currentBeat = beat;

            // Check if we're at a bar boundary (beat == 0)
            if (beat == 0)
            {
                OnBarBoundary();
            }
        }

        /// <summary>
        /// Called when we hit a bar boundary (beat 0)
        /// </summary>
        private void OnBarBoundary()
        {
            Debug.Log($"[FMODSync] Bar boundary reached. Bar: {currentBar}");

            // Host broadcasts its position slightly before the actual boundary
            if (IsHost)
            {
                BroadcastTimelinePosition();
            }

            // Client applies pending sync if waiting
            if (IsClient && pendingSync)
            {
                ApplyTimelineSync();
            }
        }

        /// <summary>
        /// Host-only: Start broadcasting timeline position every bar
        /// </summary>
        private void StartHostBroadcasting()
        {
            if (!IsHost) return;

            // Host immediately broadcasts when a client might be joining
            BroadcastTimelinePosition();

            Debug.Log("[FMODSync] Host broadcasting started");
        }

        /// <summary>
        /// Host broadcasts current timeline position to all clients
        /// </summary>
        private void BroadcastTimelinePosition()
        {
            if (!IsHost) return;

            // Get current timeline position
            timelineInstance.getTimelinePosition(out int currentPosition);

            // Adjust for network anticipation (send slightly early)
            currentPosition += (int)networkAnticipation;

            // Create sync data
            TimelineSyncData syncData = new TimelineSyncData
            {
                timelinePosition = currentPosition,
                timestamp = NetworkManager.Singleton.NetworkTimeSystem.ServerTime
            };

            // Send to all clients
            BroadcastSyncDataClientRpc(syncData);

            Debug.Log($"[FMODSync] Host broadcast position: {currentPosition}ms");
        }

        /// <summary>
        /// Client requests initial sync when joining lobby
        /// </summary>
        private void RequestSyncFromHost()
        {
            if (!IsClient || IsHost) return;

            Debug.Log("[FMODSync] Requesting sync from host.");

            RequestSyncServerRpc();
        }

        /// <summary>
        /// Server RPC - Client requests sync data
        /// </summary>
        [ServerRpc]
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestSyncServerRpc()
        {
            // Get current position and send to requesting client
            timelineInstance.getTimelinePosition(out int currentPosition);

            TimelineSyncData syncData = new TimelineSyncData
            {
                timelinePosition = currentPosition,
                timestamp = NetworkManager.Singleton.NetworkTimeSystem.ServerTime
            };

            ReceiveSyncDataClientRpc(syncData);

            Debug.Log($"[FMODSync] Host sent sync data to client");
        }

        /// <summary>
        /// Client RPC - Receive sync data (for initial sync)
        /// </summary>
        [Rpc(SendTo.NotMe)]
        private void ReceiveSyncDataClientRpc(TimelineSyncData syncData)
        {
            if (IsHost) return; // Host doesn't need to sync to itself

            ProcessReceivedSyncData(syncData, true);
        }

        /// <summary>
        /// Client RPC - Broadcast sync data (for ongoing sync)
        /// </summary>
        [Rpc(SendTo.NotMe)]
        private void BroadcastSyncDataClientRpc(TimelineSyncData syncData)
        {
            if (IsHost) return; // Host doesn't need to sync to itself

            ProcessReceivedSyncData(syncData, false);
        }

        /// <summary>
        /// Process received sync data and determine if sync is needed
        /// </summary>
        private void ProcessReceivedSyncData(TimelineSyncData syncData, bool isInitialSync)
        {
            // Calculate elapsed time since host captured position
            double currentNetworkTime = NetworkManager.Singleton.NetworkTimeSystem.ServerTime;
            double elapsedTicks = currentNetworkTime - syncData.timestamp;
            float elapsedMs = (float)(elapsedTicks / NetworkManager.Singleton.NetworkTimeSystem.TickLatency) * 1000f;

            // Extrapolate host's current position
            int extrapolatedHostPosition = syncData.timelinePosition + (int)elapsedMs;

            // Get our current position
            timelineInstance.getTimelinePosition(out int currentLocalPosition);

            // Calculate drift
            float drift = Mathf.Abs(extrapolatedHostPosition - currentLocalPosition);
            lastDriftAmount = drift;

            Debug.Log($"[FMODSync] Sync data received. Drift: {drift}ms, Tolerance: {driftTolerance}ms");

            // Determine if we need to sync
            bool needsSync = isInitialSync || (drift > driftTolerance);

            if (needsSync)
            {
                // Schedule sync for next bar boundary
                ScheduleSync(extrapolatedHostPosition);
            }
            else if (!isSynced)
            {
                isSynced = true;
                Debug.Log("[FMODSync] Client is synchronized within tolerance");
            }
        }

        /// <summary>
        /// Schedule a timeline sync to occur at the next bar boundary
        /// </summary>
        private void ScheduleSync(int targetPosition)
        {
            pendingSync = true;
            targetSyncPosition = targetPosition;

            // Calculate how long until next bar for better position extrapolation
            if (syncCoroutine != null)
                StopCoroutine(syncCoroutine);
            syncCoroutine = StartCoroutine(TrackSyncTarget());

            Debug.Log($"[FMODSync] Sync scheduled for next bar boundary. Target: {targetPosition}ms");
        }

        /// <summary>
        /// Coroutine to track and update sync target position over time
        /// </summary>
        private IEnumerator TrackSyncTarget()
        {
            float startTime = Time.realtimeSinceStartup;
            int startPosition = targetSyncPosition;

            while (pendingSync)
            {
                // Update target position based on elapsed time
                float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                targetSyncPosition = startPosition + (int)elapsed;

                yield return null;
            }
        }

        /// <summary>
        /// Apply the timeline sync by jumping to target position
        /// </summary>
        private void ApplyTimelineSync()
        {
            if (!pendingSync) return;

            // Set timeline to target position
            timelineInstance.setTimelinePosition(targetSyncPosition);

            pendingSync = false;
            isSynced = true;

            if (syncCoroutine != null)
            {
                StopCoroutine(syncCoroutine);
                syncCoroutine = null;
            }

            timelineInstance.getTimelinePosition(out int newPosition);
            Debug.Log($"[FMODSync] Sync applied! Jumped to position: {newPosition}ms");
        }

        /// <summary>
        /// Public method to manually trigger sync request (useful for debugging)
        /// </summary>
        [ContextMenu("Force Sync Request")]
        public void ForceRequestSync()
        {
            if (IsClient && !IsHost)
            {
                RequestSyncFromHost();
            }
        }

        /// <summary>
        /// Get current sync status
        /// </summary>
        public bool IsSynchronized => isSynced;

        /// <summary>
        /// Get last measured drift in milliseconds
        /// </summary>
        public float GetLastDrift => lastDriftAmount;
    }
}