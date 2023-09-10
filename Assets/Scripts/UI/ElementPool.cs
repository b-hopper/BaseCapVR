using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementPool<T> where T : MonoBehaviour
{
    private List<T> pool = new();
    private int usedElements = 0;
    private T prefab;
    private Transform layout;

    public ElementPool(T poolPrefab, Transform parentLayout)
    {
        prefab = poolPrefab;
        layout = parentLayout;
    }

    public T GetElement()
    {
        T item;
        if (usedElements == pool.Count)
        {
            item = GameObject.Instantiate(prefab, layout);
            pool.Add(item);
            item.gameObject.SetActive(true);
            usedElements++;
            return item;
        }

        item = pool[usedElements];
        usedElements++;
        item.gameObject.SetActive(true);
        return item;
    }

    public void ReturnElement(T element)
    {
        if (pool.Contains(element))
        {
            element.gameObject.SetActive(false);
            usedElements--;
            if (usedElements < 0) usedElements = 0;
        }
    }

}
