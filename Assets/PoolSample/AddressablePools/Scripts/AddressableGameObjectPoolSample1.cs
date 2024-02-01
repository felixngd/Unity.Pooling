using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.Collections.Pooled.Generic;
using ZBase.Foundation.Pooling;
using ZBase.Foundation.Pooling.AddressableAssets;
using ZBase.Foundation.Pooling.GameObject.LazyPool;

namespace Pooling.Sample
{
    public class AddressableGameObjectPoolSample1 : MonoBehaviour
    {
        [SerializeField] private AssetRefGameObjectPrefab _prefab;
        [SerializeField] private float _spawnRadius = 20f;
        [SerializeField] private int _spawnCount = 20;
        private readonly List<GameObject> _spawned = new();

        private async UniTask Spawn()
        {
            var pool = SharedPool.Of<GlobalAssetRefGameObjectPool>();
            var go = await pool.Rent(_prefab);
            var pos = Random.insideUnitCircle * _spawnRadius;
            go.transform.position = new Vector3 { x = pos.x, y = 0, z = pos.y };
            go.SetActive(true);
            _spawned.Add(go);
        }

        private void Return()
        {
            var pool = SharedPool.Of<GlobalAssetRefGameObjectPool>();
            foreach (var go in _spawned)
                pool.Return(go);
            _spawned.Clear();
        }

        private void ReleaseAll()
        {
            foreach (var go in _spawned)
                go.SetActive(false);
            var pool = SharedPool.Of<GlobalAssetRefGameObjectPool>();
            pool.ReleaseInstances(0);
            _spawned.Clear();
        }

        private void OnDisable() => ReleaseAll();

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 50), "Spawn"))
                for (int i = 0; i < _spawnCount; i++)
                    Spawn().Forget();
            if (GUI.Button(new Rect(10, 130, 150, 50), "Return"))
                Return();
            if (GUI.Button(new Rect(10, 190, 150, 50), "Release All"))
                ReleaseAll();
        }
    }
}