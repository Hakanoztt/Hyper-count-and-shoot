using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Mobge.Threading {
    public static class ThreadSystem
    {
	    /// <summary>
	    /// Call this function on main thread before using ThreadSystem.DoOnMainThread function
	    /// </summary>
	    public static void InitializeMainThreadHandler() {
		    MainThreadEventQueueHandler.EnsureShared();
	    }
	    
        /// <summary>
        /// calls the function on each update until the function returns true
        /// call ThreadSystem.InitializeMainThreadHandler on main thread before calling this function
        /// </summary>
        /// <param name="updateFunc"></param>
        public static void DoOnMainThread(Func<bool> updateFunc) {
            MainThreadEventQueueHandler.AddFunction(updateFunc);
        }
        
        /// <summary>
        /// calls the action function on secondary (not main) thread
        /// </summary>
        /// <param name="action"></param>
        public static void DoOnSecondaryThread(Action action) {
			SecondaryThreadEventQueueHandler.AddAction(action);
        }
		
        /// <summary>
        /// creates a new thread and do specified action on that new thread
        /// </summary>
        /// <param name="action"></param>
        public static void DoOnNewThread(Action action) {
	        NewThreadEventQueueHandler.DoAction(action);
        }
        
        private class MainThreadEventQueueHandler : MonoBehaviour {
            private static MainThreadEventQueueHandler _shared;
            private static MainThreadEventQueueHandler Shared {
                get {
                    EnsureShared();
                    return _shared;
                }
            }
            public static void EnsureShared() {
	            if (_shared != null) return;
	            try {
	                _shared = new GameObject(nameof(MainThreadEventQueueHandler)).AddComponent<MainThreadEventQueueHandler>();
                }
                catch (Exception e) {
	                Debug.LogError($"ERROR: Call {nameof(ThreadSystem)}.{nameof(InitializeMainThreadHandler)} on main thread before calling any other {nameof(ThreadSystem)} function. \n" +
	                               $"Gameobject required for {nameof(MainThreadEventQueueHandler)} cannot be created on secondary threads...");
	                Debug.LogError(e);
	                throw;
                }
                _shared._queue = new EventQueue();
                DontDestroyOnLoad(_shared.gameObject);
            }
            private EventQueue _queue;
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
            [UnityEditor.InitializeOnEnterPlayMode]
            [UsedImplicitly]
            private static void ResetStatic() {
                _shared = null;
            }
#endif
	        public static void AddFunction(Func<bool> updateFunc) {
		        Shared._queue.AddAction(updateFunc);
	        }
            private void Start() {
                if(_shared != this) {
                    throw new Exception(nameof(MainThreadEventQueueHandler) + " behaviour should not be used directly. Use " + nameof(EventQueue) + ".MainEventQueue instead.");
                }
            }
            private void Update() {
                _queue.TriggerEvents();
            }
            private class EventQueue {
	            private readonly object _subroutineLock = new object();
	            private readonly List<Func<bool>> _tempSubroutines = new List<Func<bool>>();
	            private readonly List<Func<bool>> _subroutines = new List<Func<bool>>();
	            /// <summary>
	            /// calls the function on each update until the function returns true
	            /// </summary>
	            /// <param name="update"></param>
	            public void AddAction(Func<bool> update) {
		            lock (_subroutineLock) {
			            _tempSubroutines.Add(update);
		            }
	            }
	            public void TriggerEvents() {
		            lock (_subroutineLock) {
			            _subroutines.AddRange(_tempSubroutines);
			            _tempSubroutines.Clear();
		            }
		            for (int i = 0; i < _subroutines.Count; i++) {
			            var s = _subroutines[i];
			            bool finished = true;
			            try {
				            finished = s();
			            }
			            catch (Exception e) {
				            Debug.LogError($"ERROR: Exception at {nameof(ThreadSystem)}, a function queued " +
				                           $"on {nameof(MainThreadEventQueueHandler)} " +
				                           "has thrown an exception, removing function from queue...");
				            Debug.LogError(e);
			            }
			            finally {
				            if (finished) {
					            var lastIndex = _subroutines.Count - 1;
					            _subroutines[i] = _subroutines[lastIndex];
					            _subroutines.RemoveAt(lastIndex);
					            i--;
				            }
			            }
			            
		            }
	            }
            }
        }
        private class SecondaryThreadEventQueueHandler : MonoBehaviour {
	        internal static void AddAction(Action a) {
		        Instance.InternalAddAction(a);
	        }
	        private readonly Queue<Action> _queue = new Queue<Action>();
	        private Semaphore _sem;
	        private static SecondaryThreadEventQueueHandler _instance;
	        private static SecondaryThreadEventQueueHandler Instance {
		        get {
#if UNITY_EDITOR
			        if (!Application.isPlaying) {
				        return _instance;
			        }
#endif
			        if (_instance == null) {
				        new GameObject(nameof(SecondaryThreadEventQueueHandler))
					        .AddComponent<SecondaryThreadEventQueueHandler>();
			        }
			        return _instance;
		        }
	        }
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
	        [UnityEditor.InitializeOnEnterPlayMode]
	        [UsedImplicitly]
	        private static void ResetStatic() {
		        _instance = null;
	        }
#endif
	        private void Awake() {
		        if (_instance != null) {
			        const string typeName = nameof(SecondaryThreadEventQueueHandler);
			        gameObject.DestroySelf();
			        throw new Exception(
				        $"Multiple instances of {typeName} detected. There cannot be more than one instance of {typeName}.");
		        }

		        _instance = this;
		        DontDestroyOnLoad(gameObject);
		        InitThread();
	        }
	        private void OnDestroy() {
		        if (_instance != this) return;
		        _instance = null;
		        ReleaseThread();
	        }
	        private void InitThread() {
		        _sem = new Semaphore(0, 200);
		        var t = new Thread(Run) {
			        Name = $"{nameof(SecondaryThreadEventQueueHandler)} thread",
			        Priority = System.Threading.ThreadPriority.Lowest,
		        };
		        t.Start();
	        }
	        private void Run() {
		        while (true) {
			        _sem.WaitOne();
			        Action a;
			        lock (_queue) {
				        if (_queue.Count == 0) {
					        break;
				        }
				        a = _queue.Dequeue();
			        }
			        try {
				        a();
			        }
			        catch (Exception e) {
				        const string typeName = nameof(SecondaryThreadEventQueueHandler);
				        Debug.LogError($"{typeName} Error! there is an exception while executing an action on {typeName}!");
				        Debug.LogError(e);
			        }
		        }

		        _sem.Close();
	        }
	        private void ReleaseThread() {
		        _sem.Release();
	        }
	        private void InternalAddAction(Action a) {
		        lock (_queue) {
			        _queue.Enqueue(a);
		        }
		        _sem.Release();
	        }
        }
        private static class NewThreadEventQueueHandler {
	        private static readonly BasicPool<Worker> Pool = new BasicPool<Worker>();
	        internal static void DoAction(Action action) {
		        lock (Pool) {
			        Pool.Get().Do(action);
		        }
	        }
	        private class Worker {
		        private Action _action;
		        private readonly Semaphore _semaphore = new Semaphore(0, 1);
		        public Worker() {
			        var thread = new Thread(ThreadAction) {
				        Name = $"{nameof(NewThreadEventQueueHandler)} worker thread",
				        Priority = System.Threading.ThreadPriority.BelowNormal,
			        };
			        thread.Start();
		        }
		        public void Do(Action action) {
			        if (_action != null) {
				        return;
			        }
			        _action = action;
			        _semaphore.Release(1);
		        }
		        private void ThreadAction() {
			        while (true) {
				        _semaphore.WaitOne();
				        try {
					        _action?.Invoke();
				        }
				        catch (Exception e) {
					        const string typeName = nameof(NewThreadEventQueueHandler);
					        Debug.LogError($"{typeName} Error! there is an exception while executing an action on {typeName}!");
					        Debug.LogError(e);
				        }
				        _action = null;
				        lock (Pool) {
					        Pool.Release(this);
				        }
			        }
			        // ReSharper disable once FunctionNeverReturns
		        }
	        }
        }
    }
}


