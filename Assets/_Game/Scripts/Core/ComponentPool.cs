using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Simple stack pool. Prewarm at load so gameplay never instantiates; it only grows
    /// (with a warning in the Editor) if the prewarm count was too small.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly Stack<T> free;
        int created;

        public int CountCreated => created;

        public ComponentPool(T prefab, Transform parent, int prewarm)
        {
            this.prefab = prefab;
            this.parent = parent;
            free = new Stack<T>(Mathf.Max(4, prewarm));
            for (var i = 0; i < prewarm; i++)
            {
                var item = Create();
                item.gameObject.SetActive(false);
                free.Push(item);
            }
        }

        T Create()
        {
            created++;
            var item = Object.Instantiate(prefab, parent);
            item.gameObject.name = prefab.name;
            return item;
        }

        public T Get(Vector3 position)
        {
            var item = free.Count > 0 ? free.Pop() : Create();
            item.transform.SetPositionAndRotation(position, Quaternion.identity);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf) return;
            item.gameObject.SetActive(false);
            free.Push(item);
        }
    }
}
