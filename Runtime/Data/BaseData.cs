using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DataForge {
    public abstract class BaseData<T, TConf> where T : class, new() where TConf : Config, new() {
        private static LocalData<T> _ld;
        private static LocalData<T> LD => _ld ??= new LocalData<T>(new TConf());

        private static T cache;
        /// <summary>
        /// Only use cache to display, to update data to call Load, avoid hacking
        /// </summary>
        public static T Cache => cache ?? Load();

        public virtual void Save() => _ = SaveWithExceptionHandlingAsync();

        async Task SaveWithExceptionHandlingAsync() {
            try {
                cache = this as T;
                await LD.SaveAsync(cache);
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        public static T Load() {
            try {
                cache = LD.Load(cache);
            } catch (Exception ex) {
                LogEx(ex);
            }
            cache ??= new T();
            return cache;
        }

        public static void Delete() => LD.Delete();

        public static void LogEx(Exception ex) {
            Debug.LogException(ex);
#if ENABLE_FIREBASE
            Firebase.Crashlytics.Crashlytics.LogException(ex);
#endif
        }
    }
}