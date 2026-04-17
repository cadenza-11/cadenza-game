using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class AudioToUIShader : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string backgroundElementName;
        private MaterialDefinition visualizerMaterial;
        private float bass;
        private float mids;
        private float treble;

        void Start()
        {
            this.visualizerMaterial = this.uiDocument.rootVisualElement.Q<VisualElement>(this.backgroundElementName).resolvedStyle.unityMaterial;
            Debug.Log($"Visualizer material: {this.visualizerMaterial}, UI Document: {this.uiDocument}, Background element: {this.uiDocument.rootVisualElement.Q<VisualElement>(this.backgroundElementName)}");
        }
        
        void Update()
        {
            float[] spectrum = AudioSystem.FFTSpectrum;

            if (spectrum == null || spectrum.Length == 0) return;

            // Split spectrum into bands and average each band to get a single value for bass, mids, and treble.
            float[] bands = this.BuildBands(spectrum);
            float targetBass = bands[0] + bands[1];
            float targetMids = bands[2] + bands[3] + bands[4];
            float targetTreble = bands[5] + bands[6] + bands[7];

            // Smoothing
            this.bass = Mathf.Lerp(this.bass, targetBass, Time.deltaTime * 8f);
            this.mids = Mathf.Lerp(this.mids, targetMids, Time.deltaTime * 8f);
            this.treble = Mathf.Lerp(this.treble, targetTreble, Time.deltaTime * 8f);

            // this.visualizerMaterial.SetFloat("_Bass", this.bass);
            // this.visualizerMaterial.SetFloat("_Mids", this.mids);
            // this.visualizerMaterial.SetFloat("_Treble", this.treble);

            // float volume = this.GetRMSVolume(spectrum) * 50f;
            // this.visualizerMaterial.SetFloat("_Volume", volume);
            this.visualizerMaterial.SetFloat("_InputSound", this.mids);
            Debug.Log($"Bass: {this.bass}, Mids: {this.mids}, Treble: {this.treble}");
        }

        /// <summary>
        /// Splits the spectrum into 8 logarithmic bands and averages each band to get a single value.
        /// </summary>
        /// <param name="spectrum">The audio spectrum array.</param>
        /// <returns>An array of average values for each frequency band.</returns>
        private float[] BuildBands(float[] spectrum)
        {
            int bandCount = 8;
            float[] bands = new float[bandCount];

            int index = 0;

            for (int i = 0; i < bandCount; i++)
            {
                int sampleCount = (int)Mathf.Pow(2, i) * 2;

                if (index + sampleCount > spectrum.Length)
                    sampleCount = spectrum.Length - index;

                float sum = 0f;

                for (int j = 0; j < sampleCount; j++)
                {
                    sum += spectrum[index];
                    index++;
                }

                float average = (sampleCount > 0) ? sum / sampleCount : 0f;

                bands[i] = average * 50f;
            }

            return bands;
        }

        float GetRMSVolume(float[] spectrum)
        {
            float sum = 0f;

            for (int i = 0; i < spectrum.Length; i++)
            {
                float v = spectrum[i];
                sum += v * v;
            }

            return Mathf.Sqrt(sum / spectrum.Length);
        }
    }
}