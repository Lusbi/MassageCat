using System;
using System.Linq;
using UnityEngine;

public class NormalHit : MonoBehaviour
{
    private GameObject[] gameObjects;

    private void Awake()
    {
        gameObjects = transform.GetComponentsInChildren<Transform>(true)
            .Where(t => t != this.transform)
            .Select(t => t.gameObject)
            .ToArray();
    }
    public void OnEnable()
    {
        // 隨機啟用其中一個物件
        // 並隨機旋轉
        int index = UnityEngine.Random.Range(0, gameObjects.Length);
        for (int i = 0; i < gameObjects.Length; i++)
        {
            bool isActive = i == index;
            if (isActive)
            {
                float randomZ = UnityEngine.Random.Range(0f, 360f);
                gameObjects[i].transform.localRotation = Quaternion.Euler(0f, 0f, randomZ);
            }
            gameObjects[i].SetActive(i == index);
        }
    }

    public void OnDisable()
    {
        foreach (var go in gameObjects)
        {
            go.SetActive(false);
        }
    }

    public void Play(System.Action callBack = null)
    {
        this.gameObject.SetActive(true);
        PrimeTween.Tween.Delay(0.25f, () =>
        {
            callBack?.Invoke();
            this.gameObject.SetActive(false);
        });
    }
}
