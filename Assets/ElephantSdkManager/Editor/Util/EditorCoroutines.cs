using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ElephantSdkManager.Util
{
    public class EditorCoroutines
    {
        public class EditorCoroutine
		{
			public EditorCoroutines.ICoroutineYield currentYield = new EditorCoroutines.YieldDefault();
			public IEnumerator routine;
			public string routineUniqueHash;
			public string ownerUniqueHash;
			public string MethodName = "";

			public int ownerHash;
			public string ownerType;

			public bool finished = false;

			public EditorCoroutine(IEnumerator routine, int ownerHash, string ownerType)
			{
				this.routine = routine;
				this.ownerHash = ownerHash;
				this.ownerType = ownerType;
				ownerUniqueHash = ownerHash + "_" + ownerType;

				if (routine != null)
				{
					string[] split = routine.ToString().Split('<', '>');
					if (split.Length == 3)
					{
						this.MethodName = split[1];
					}
				}

				routineUniqueHash = ownerHash + "_" + ownerType + "_" + MethodName;
			}

			public EditorCoroutine(string methodName, int ownerHash, string ownerType)
			{
				MethodName = methodName;
				this.ownerHash = ownerHash;
				this.ownerType = ownerType;
				ownerUniqueHash = ownerHash + "_" + ownerType;
				routineUniqueHash = ownerHash + "_" + ownerType + "_" + MethodName;
			}
		}

		public interface ICoroutineYield
		{
			bool IsDone(float deltaTime);
		}

		struct YieldDefault : EditorCoroutines.ICoroutineYield
		{
			public bool IsDone(float deltaTime)
			{
				return true;
			}
		}

		struct YieldWaitForSeconds : EditorCoroutines.ICoroutineYield
		{
			public float timeLeft;

			public bool IsDone(float deltaTime)
			{
				timeLeft -= deltaTime;
				return timeLeft < 0;
			}
		}

		struct YieldCustomYieldInstruction : EditorCoroutines.ICoroutineYield
		{
			public CustomYieldInstruction customYield;

			public bool IsDone(float deltaTime)
			{
				return !customYield.keepWaiting;
			}
		}

		struct YieldWWW : EditorCoroutines.ICoroutineYield
		{
			public WWW Www;

			public bool IsDone(float deltaTime)
			{
				return Www.isDone;
			}
		}

		struct YieldAsync : EditorCoroutines.ICoroutineYield
		{
			public AsyncOperation asyncOperation;

			public bool IsDone(float deltaTime)
			{
				return asyncOperation.isDone;
			}
		}

		struct YieldNestedCoroutine : EditorCoroutines.ICoroutineYield
		{
			public EditorCoroutines.EditorCoroutine coroutine;

			public bool IsDone(float deltaTime)
			{
				return coroutine.finished;
			}
		}

		static EditorCoroutines instance = null;

		Dictionary<string, List<EditorCoroutines.EditorCoroutine>> coroutineDict = new Dictionary<string, List<EditorCoroutines.EditorCoroutine>>();
		List<List<EditorCoroutines.EditorCoroutine>> tempCoroutineList = new List<List<EditorCoroutines.EditorCoroutine>>();

		Dictionary<string, Dictionary<string, EditorCoroutines.EditorCoroutine>> coroutineOwnerDict =
			new Dictionary<string, Dictionary<string, EditorCoroutines.EditorCoroutine>>();

		DateTime previousTimeSinceStartup;

		/// <summary>Starts a coroutine.</summary>
		/// <param name="routine">The coroutine to start.</param>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static EditorCoroutines.EditorCoroutine StartCoroutine(IEnumerator routine, object thisReference)
		{
			CreateInstanceIfNeeded();
			return instance.GoStartCoroutine(routine, thisReference);
		}

		/// <summary>Starts a coroutine.</summary>
		/// <param name="methodName">The name of the coroutine method to start.</param>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static EditorCoroutines.EditorCoroutine StartCoroutine(string methodName, object thisReference)
		{
			return StartCoroutine(methodName, null, thisReference);
		}

		/// <summary>Starts a coroutine.</summary>
		/// <param name="methodName">The name of the coroutine method to start.</param>
		/// <param name="value">The parameter to pass to the coroutine.</param>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static EditorCoroutines.EditorCoroutine StartCoroutine(string methodName, object value, object thisReference)
		{
			MethodInfo methodInfo = thisReference.GetType()
				.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (methodInfo == null)
			{
				Debug.LogError("Coroutine '" + methodName + "' couldn't be started, the method doesn't exist!");
			}
			object returnValue;

			if (value == null)
			{
				returnValue = methodInfo.Invoke(thisReference, null);
			}
			else
			{
				returnValue = methodInfo.Invoke(thisReference, new object[] {value});
			}

			if (returnValue is IEnumerator)
			{
				CreateInstanceIfNeeded();
				return instance.GoStartCoroutine((IEnumerator) returnValue, thisReference);
			}
			else
			{
				Debug.LogError("Coroutine '" + methodName + "' couldn't be started, the method doesn't return an IEnumerator!");
			}

			return null;
		}

		/// <summary>Stops all coroutines being the routine running on the passed instance.</summary>
		/// <param name="routine"> The coroutine to stop.</param>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static void StopCoroutine(IEnumerator routine, object thisReference)
		{
			CreateInstanceIfNeeded();
			instance.GoStopCoroutine(routine, thisReference);
		}

		/// <summary>
		/// Stops all coroutines named methodName running on the passed instance.</summary>
		/// <param name="methodName"> The name of the coroutine method to stop.</param>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static void StopCoroutine(string methodName, object thisReference)
		{
			CreateInstanceIfNeeded();
			instance.GoStopCoroutine(methodName, thisReference);
		}

		/// <summary>
		/// Stops all coroutines running on the passed instance.</summary>
		/// <param name="thisReference">Reference to the instance of the class containing the method.</param>
		public static void StopAllCoroutines(object thisReference)
		{
			CreateInstanceIfNeeded();
			instance.GoStopAllCoroutines(thisReference);
		}

		static void CreateInstanceIfNeeded()
		{
			if (instance == null)
			{
				instance = new EditorCoroutines();
				instance.Initialize();
			}
		}

		void Initialize()
		{
			previousTimeSinceStartup = DateTime.Now;
			EditorApplication.update += OnUpdate;
		}

		void GoStopCoroutine(IEnumerator routine, object thisReference)
		{
			GoStopActualRoutine(CreateCoroutine(routine, thisReference));
		}

		void GoStopCoroutine(string methodName, object thisReference)
		{
			GoStopActualRoutine(CreateCoroutineFromString(methodName, thisReference));
		}

		void GoStopActualRoutine(EditorCoroutines.EditorCoroutine routine)
		{
			if (coroutineDict.ContainsKey(routine.routineUniqueHash))
			{
				coroutineOwnerDict[routine.ownerUniqueHash].Remove(routine.routineUniqueHash);
				coroutineDict.Remove(routine.routineUniqueHash);
			}
		}

		void GoStopAllCoroutines(object thisReference)
		{
			EditorCoroutines.EditorCoroutine coroutine = CreateCoroutine(null, thisReference);
			if (coroutineOwnerDict.ContainsKey(coroutine.ownerUniqueHash))
			{
				foreach (var couple in coroutineOwnerDict[coroutine.ownerUniqueHash])
				{
					coroutineDict.Remove(couple.Value.routineUniqueHash);
				}
				coroutineOwnerDict.Remove(coroutine.ownerUniqueHash);
			}
		}

		EditorCoroutines.EditorCoroutine GoStartCoroutine(IEnumerator routine, object thisReference)
		{
			if (routine == null)
			{
				Debug.LogException(new Exception("IEnumerator is null!"), null);
			}
			EditorCoroutines.EditorCoroutine coroutine = CreateCoroutine(routine, thisReference);
			GoStartCoroutine(coroutine);
			return coroutine;
		}

		void GoStartCoroutine(EditorCoroutines.EditorCoroutine coroutine)
		{
			if (!coroutineDict.ContainsKey(coroutine.routineUniqueHash))
			{
				List<EditorCoroutines.EditorCoroutine> newCoroutineList = new List<EditorCoroutines.EditorCoroutine>();
				coroutineDict.Add(coroutine.routineUniqueHash, newCoroutineList);
			}
			coroutineDict[coroutine.routineUniqueHash].Add(coroutine);

			if (!coroutineOwnerDict.ContainsKey(coroutine.ownerUniqueHash))
			{
				Dictionary<string, EditorCoroutines.EditorCoroutine> newCoroutineDict = new Dictionary<string, EditorCoroutines.EditorCoroutine>();
				coroutineOwnerDict.Add(coroutine.ownerUniqueHash, newCoroutineDict);
			}

			// If the method from the same owner has been stored before, it doesn't have to be stored anymore,
			// One reference is enough in order for "StopAllCoroutines" to work
			if (!coroutineOwnerDict[coroutine.ownerUniqueHash].ContainsKey(coroutine.routineUniqueHash))
			{
				coroutineOwnerDict[coroutine.ownerUniqueHash].Add(coroutine.routineUniqueHash, coroutine);
			}

			MoveNext(coroutine);
		}

		EditorCoroutines.EditorCoroutine CreateCoroutine(IEnumerator routine, object thisReference)
		{
			return new EditorCoroutines.EditorCoroutine(routine, thisReference.GetHashCode(), thisReference.GetType().ToString());
		}

		EditorCoroutines.EditorCoroutine CreateCoroutineFromString(string methodName, object thisReference)
		{
			return new EditorCoroutines.EditorCoroutine(methodName, thisReference.GetHashCode(), thisReference.GetType().ToString());
		}

		void OnUpdate()
		{
			float deltaTime = (float) (DateTime.Now.Subtract(previousTimeSinceStartup).TotalMilliseconds / 1000.0f);

			previousTimeSinceStartup = DateTime.Now;
			if (coroutineDict.Count == 0)
			{
				return;
			}

			tempCoroutineList.Clear();
			foreach (var pair in coroutineDict)
				tempCoroutineList.Add(pair.Value);

			for (var i = tempCoroutineList.Count - 1; i >= 0; i--)
			{
				List<EditorCoroutines.EditorCoroutine> coroutines = tempCoroutineList[i];

				for (int j = coroutines.Count - 1; j >= 0; j--)
				{
					EditorCoroutines.EditorCoroutine coroutine = coroutines[j];

					if (!coroutine.currentYield.IsDone(deltaTime))
					{
						continue;
					}

					if (!MoveNext(coroutine))
					{
						coroutines.RemoveAt(j);
						coroutine.currentYield = null;
						coroutine.finished = true;
					}

					if (coroutines.Count == 0)
					{
						coroutineDict.Remove(coroutine.ownerUniqueHash);
					}
				}
			}
		}

		static bool MoveNext(EditorCoroutines.EditorCoroutine coroutine)
		{
			if (coroutine.routine.MoveNext())
			{
				return Process(coroutine);
			}

			return false;
		}

		// returns false if no next, returns true if OK
		static bool Process(EditorCoroutines.EditorCoroutine coroutine)
		{
			object current = coroutine.routine.Current;
			if (current == null)
			{
				coroutine.currentYield = new EditorCoroutines.YieldDefault();
			}
			else if (current is WaitForSeconds)
			{
				float seconds = float.Parse(GetInstanceField(typeof(WaitForSeconds), current, "m_Seconds").ToString());
				coroutine.currentYield = new EditorCoroutines.YieldWaitForSeconds() {timeLeft = seconds};
			}
			else if (current is CustomYieldInstruction)
			{
				coroutine.currentYield = new EditorCoroutines.YieldCustomYieldInstruction()
				{
					customYield = current as CustomYieldInstruction
				};
			}
			else if (current is WWW)
			{
				coroutine.currentYield = new EditorCoroutines.YieldWWW {Www = (WWW) current};
			}
			else if (current is WaitForFixedUpdate || current is WaitForEndOfFrame)
			{
				coroutine.currentYield = new EditorCoroutines.YieldDefault();
			}
			else if (current is AsyncOperation)
			{
				coroutine.currentYield = new EditorCoroutines.YieldAsync {asyncOperation = (AsyncOperation) current};
			}
			else if (current is EditorCoroutines.EditorCoroutine)
			{
				coroutine.currentYield = new EditorCoroutines.YieldNestedCoroutine { coroutine= (EditorCoroutines.EditorCoroutine) current};
			}
			else
			{
				Debug.LogException(
					new Exception("<" + coroutine.MethodName + "> yielded an unknown or unsupported type! (" + current.GetType() + ")"),
					null);
				coroutine.currentYield = new EditorCoroutines.YieldDefault();
			}
			return true;
		}

		static object GetInstanceField(Type type, object instance, string fieldName)
		{
			BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			FieldInfo field = type.GetField(fieldName, bindFlags);
			return field.GetValue(instance);
		}
    }
}