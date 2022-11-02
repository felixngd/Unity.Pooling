using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Pooling;
using UnityEngine;

namespace Project.Runtime
{
    public class HealthBarPrefab : IPrefab<HealthBar>
    {
        public int PrepoolAmount { get; set; }
        public UniTask<HealthBar> Instantiate()
        {
            var healthBar = Object.Instantiate(Resources.Load<HealthBar>("HealthBar"), Parent);
            return UniTask.FromResult(healthBar);
        }

        public UniTask<HealthBar> Instantiate(CancellationToken cancelToken)
        {
            return Instantiate();
        }

        public void Release(HealthBar instance)
        {
            Object.Destroy(instance.gameObject);
        }

        public Transform Parent { get; set; }
    }
}