using UnityEngine;

namespace Cadenza
{
    public abstract class ApplicationSystem : MonoBehaviour
    {
        public virtual void OnInitialize()
        {
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnApplicationStop()
        {
        }

        public virtual void OnGameStart()
        {
        }

        public virtual void OnGameStop()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnBeat()
        {
        }
    }
}
