using System;
using UnityEngine;

namespace Cadenza
{
    public class OrchestraInstrumentAssigner : MonoBehaviour
    {
        private static OrchestraInstrumentAssigner singleton;

        [Serializable]
        public struct InstrumentType
        {
            public string FMODParameterName;
            public GameObject InstrumentPrefab;
        }

        [SerializeField] private InstrumentType[] instrumentTypes;

        void Awake()
        {
            singleton = this;
        }

        public static void AssignInstrument(EnemyGruntFunk grunt)
        {
            // Get random instrument.
            var instrumentType = singleton.instrumentTypes[UnityEngine.Random.Range(0, singleton.instrumentTypes.Length)];

            // Place instrument.
            if (instrumentType.InstrumentPrefab != null && grunt.handBone != null)
                Instantiate(instrumentType.InstrumentPrefab, grunt.handBone.transform, worldPositionStays: false);

            // Associate FMOD parameter.
            if (grunt.TryGetComponent<EnemyHealthParameterBinding>(out var paramController))
                paramController.parameterName = instrumentType.FMODParameterName;
        }
    }
}
