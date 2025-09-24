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
        /// <summary>
        /// The position in the track timeline, in milliseconds.
        /// This value may be anticipated before sending over the network.
        /// </summary>
        public int timelinePosition;

        /// <summary>
        /// Network time when the timeline position was captured.
        /// </summary>
        public double timestamp;

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

        [Tooltip("How early the host should send data (in milliseconds)")]
        [SerializeField] private int networkAncitipationMs = 25;

        [Header("Runtime Info (Read Only)")]
        [SerializeField] private bool isSynced = false;
        [SerializeField] private float lastDriftAmount = 0f;
        [SerializeField] private int currentBar = 0;
        [SerializeField] private int currentBeat = 0;

        public static int NetworkCompensationTimeMs
        {
            get => singleton.networkAncitipationMs;
            set => singleton.networkAncitipationMs = value;
        }

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
        private const int FIRST_BEAT_INDEX = 1;

        void Start()
        {
            if (singleton == null)
            {
                singleton = this;
            }
            else
            {
                Destroy(this);
                return;
            }

            // Initialize FMOD timeline instance
            InitializeTimeline();

            Debug.Log($"[FMODSync] Joined session as player #{this.OwnerClientId} [host={this.IsHost}]");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                if (IsHost)
                    SendSyncDataToClients();
                else if (IsClient)
                    RequestSyncToHost();
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

            // Explicitly cache the delegate object so it doesn't
            // get garbage collected as it's being used.
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
            // (Assume the beat callback came from the global track, as
            // only the global track should have registered this callback.)

            // Extract beat properties
            if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
            {
                var param = (TIMELINE_BEAT_PROPERTIES)System.Runtime.InteropServices.Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));
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

            // Check if we're at a bar boundary (first beat)
            if (beat == FIRST_BEAT_INDEX)
                OnBarBoundary();
        }

        /// <summary>
        /// Called when we hit a bar boundary (beat 0)
        /// </summary>
        private void OnBarBoundary()
        {
            // Host broadcasts its position slightly before the actual boundary.
            // Client applies pending sync if waiting.

            if (IsHost)
                SendSyncDataToClients();
            else if (IsClient && pendingSync)
                ApplyTimelineSync();
        }

        /// <summary>
        /// If currently the host, broadcast current timeline position to all clients.
        /// </summary>
        private void SendSyncDataToClients()
        {
            if (!IsHost)
                return;

            // Get current timeline position in milliseconds
            timelineInstance.getTimelinePosition(out int currentPosition);

            // Adjust for network anticipation (sync to a slightly later time)
            TimelineSyncData syncData = new()
            {
                timelinePosition = currentPosition + networkAncitipationMs,
                timestamp = NetworkManager.Singleton.NetworkTimeSystem.ServerTime
            };

            // Send to all clients
            BroadcastSyncDataClientRpc(syncData);

            Debug.Log($"[FMODSync] Host broadcasted at time {syncData.timestamp}: sync to timeline position {syncData.timelinePosition}");
        }

        /// <summary>
        /// Client requests initial sync when joining lobby
        /// </summary>
        private void RequestSyncToHost()
        {
            if (!IsClient || IsHost) return;

            Debug.Log($"[FMODSync] Player {this.OwnerClientId} requested a sync.");

            RequestSyncServerRpc();
        }

        /// <summary>
        /// Server RPC - Tell host to send sync data to client
        /// </summary>
        [ServerRpc]
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestSyncServerRpc()
        {
            // Get current position and send to requesting client
            timelineInstance.getTimelinePosition(out int currentPosition);

            TimelineSyncData syncData = new()
            {
                timelinePosition = currentPosition + this.networkAncitipationMs,
                timestamp = NetworkManager.Singleton.NetworkTimeSystem.ServerTime
            };

            ReceiveSyncDataClientRpc(syncData);

            Debug.Log($"[FMODSync] Host broadcasted at time {syncData.timestamp}: sync to timeline position {syncData.timelinePosition}");
        }

        /// <summary>
        /// Client RPC - Receive sync data (for initial sync)
        /// </summary>
        [Rpc(SendTo.NotMe)]
        private void ReceiveSyncDataClientRpc(TimelineSyncData syncData)
        {
            if (IsHost)
                return; // Host doesn't need to sync to itself

            ProcessReceivedSyncData(syncData, true);
        }

        /// <summary>
        /// Client RPC - Broadcast sync data (for ongoing sync)
        /// </summary>
        [Rpc(SendTo.NotMe)]
        private void BroadcastSyncDataClientRpc(TimelineSyncData syncData)
        {
            if (IsHost)
                return; // Host doesn't need to sync to itself

            ProcessReceivedSyncData(syncData, false);
        }

        /// <summary>
        /// Process received sync data and determine if sync is needed
        /// </summary>
        private void ProcessReceivedSyncData(TimelineSyncData syncData, bool immediate)
        {
            // Calculate elapsed time since host captured position
            double currentNetworkTime = NetworkManager.Singleton.NetworkTimeSystem.ServerTime;
            double elapsedTicks = currentNetworkTime - syncData.timestamp;
            float elapsedMs = (float)(elapsedTicks / NetworkManager.Singleton.NetworkTimeSystem.TickLatency) * 1000f;

            // Calculate drift
            int extrapolatedHostPosition = syncData.timelinePosition + (int)elapsedMs;
            timelineInstance.getTimelinePosition(out int currentLocalPosition);

            float drift = Mathf.Abs(extrapolatedHostPosition - currentLocalPosition);
            lastDriftAmount = drift;

            Debug.Log($"[FMODSync] Sync data received. Current timeline position: {currentLocalPosition} Drift: {drift}ms, Tolerance: {driftTolerance}ms");

            // Schedule sync for next bar boundary if needed
            if (immediate || (drift > driftTolerance))
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
                RequestSyncToHost();
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
