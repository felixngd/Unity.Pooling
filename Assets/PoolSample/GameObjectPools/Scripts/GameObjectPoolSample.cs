using UnityEngine;
using ZBase.Collections.Pooled.Generic;
using ZBase.Foundation.Pooling;
using ZBase.Foundation.Pooling.UnityPools;
using Grid = Sample.Environment.Grid;

namespace Pooling.Sample
{
    public class GameObjectPoolSample : MonoBehaviour
    {
        public Transform poolParent1;
        public Transform poolParent2;
        public GameObject prefab1;
        public GameObject prefab2;

        //you can access the spawned objects in your own list
        public List<GameObject> myGameObjects = new();

        //Manage lifetime of the pool yourself (in this case, the pool4 will not be registered in the SharedPool)
        public GameObjectPool pool4;
        public List<GameObject> pool4Objects = new();

        private readonly Grid _grid = new(20, 15, true);

        private void Start()
        {
            var pool2 = SharedPool.Of<CustomGameObjectPool>();
            pool2.Prefab = new GameObjectPrefab { Parent = poolParent1, PrePoolAmount = 10, Source = this.prefab1 };
            pool2.PrePool();
            pool4 = new GameObjectPool(new GameObjectPrefab {
                Parent = poolParent2, PrePoolAmount = 100, Source = this.prefab2
            });
        }

        private async void OnGUI()
        {
            //manually get the CustomGameObjectPool then rent and spawn some objects
            if (GUI.Button(new Rect(0, 0, 300, 50), "Spawn in Shared Pool"))
            {
                var pool = SharedPool.Of<CustomGameObjectPool>();
                for (int i = 0; i < 50; i++)
                {
                    var go = await pool.Rent();
                    go.transform.position = this._grid.GetAvailableSlot().position;
                    go.SetActive(true);
                    this.myGameObjects.Add(go);
                }
            }

            //manually get the CustomGameObjectPool then return and despawn some objects
            if (GUI.Button(new Rect(0, 100, 300, 50), "Despawn in SharedPool"))
            {
                var pool = SharedPool.Of<CustomGameObjectPool>();
                for (int i = myGameObjects.Count - 1; i >= 0; i++)
                {
                    var go = this.myGameObjects[i];
                    pool.Return(go);
                    this._grid.FreeSlot(go.transform.position);
                }
            }

            if (GUI.Button(new Rect(0, 200, 150, 50), "Spawn in Pool 4"))
            {
                for (int i = 0; i < 100; i++)
                {
                    if (!this._grid.TryGetAvailableSlot(out var slot))
                        continue;
                    var go = await this.pool4.Rent();
                    go.transform.position = slot.position;
                    go.SetActive(true);
                    this.pool4Objects.Add(go);
                }
            }

            if (!GUI.Button(new Rect(0, 300, 150, 50), "Despawn in Pool 4"))
                return;

            //return random 50 times in pool4, may some objects will be returned multiple times
            for (int i = 0; i < 50; i++)
            {
                var index = Random.Range(0, this.pool4Objects.Count);
                if (this.pool4Objects.Count <= index)
                    continue;
                var go = this.pool4Objects[index];
                this.pool4.Return(go);
                this._grid.FreeSlot(go.transform.position);
                this.pool4Objects.RemoveAt(index);
            }
        }

        private void OnDestroy() => this.pool4.Dispose();
    }
}