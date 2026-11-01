using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameCore.Resources
{
    /// <summary>
    /// Addressable in base
    /// </summary>
    public static class CoreResoucesService 
    {
        /// <summary>
        /// Task-based 載入資源
        /// </summary>
        /// <typeparam name="T">若屬於<see cref="Component"/>則須先載入<see cref="GameObject"/>再取得<typeparamref name="T"/></typeparam>
        /// <param name="address">資源地址</param>
        public static async Task<AsyncOperationHandle<T>> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("address cannot be null or empty.");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle;
            }
            else
            {
                Debug.LogError($"Failed to load {address}");
                Addressables.Release(handle);
                return default;
            }
        }

        public static AsyncOperationHandle<T> LoadAssetAsync<T>(string address, Action<T> callback) where T : UnityEngine.Object
        {
            if (callback == null) return default;

            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    callback(handle.Result);
                }
                else
                {
                    Debug.LogError($"Failed to load {address}");
                    Addressables.Release(handle);
                }
            };

            return handle;
        }

        /// <summary>
        /// 引用後立即釋放
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<GameObject> LoadAssetComponentAsync<T>(string address, Action<T> callback) where T : Component
        {
            if (callback == null) return default;

            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            handle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var result = handle.Result;
                    if (result.TryGetComponent(out T component))
                    {
                        callback(component);
                    }
                    else
                    {
                        Debug.LogError($"Failed to get Component {typeof(T).Name} on Prefab {result.name}");
                        Addressables.Release(handle);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load {address}");
                    Addressables.Release(handle);
                }
            };

            return handle;
        }
        

        /// <summary>
        /// 使用<see cref="UnityEngine.Coroutine"/>載入資源
        /// </summary>
        /// <typeparam name="T">若屬於<see cref="Component"/>則須先載入<see cref="GameObject"/>再取得<typeparamref name="T"/></typeparam>
        /// <param name="address">資源地址</param>
        /// <param name="onComplete">完成callback</param>
        /// <param name="onProgress">進度callback</param>
        /// <returns>完成時callback將傳遞<see cref="AsyncOperationHandle{TObject}"/></returns>
        public static IEnumerator LoadAssetAsync<T>(string address, Action<AsyncOperationHandle<T>> onComplete, Action<float> onProgress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("address cannot be null or empty.");
                return null;
            }

            if (onComplete == null)
            {
                Debug.LogError($"onComplete cannot be null. address: {address}");
                return null;
            }

            return LoadAssetRoutine(address, onComplete, onProgress);
        }

        private static IEnumerator LoadAssetRoutine<T>(string address, Action<AsyncOperationHandle<T>> onComplete, Action<float> onProgress)
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            if (onProgress != null)
            {
                while (!handle.IsDone)
                {
                    onProgress(handle.PercentComplete);
                    yield return null;
                }

                onProgress(1f);
            }
            else
            {
                yield return handle;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete(handle);
            }
            else
            {
                Debug.LogError($"Failed to load {address}");
                Addressables.Release(handle);
            }
        }

        public static void Release(GameObject gameObject)
        {
            Addressables.Release(gameObject);
        }
    }
}