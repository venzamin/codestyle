using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CocoonGames.Dolphin.Client
{
	public class IntroPhase : MonoObject
	{
		[SerializeField]
		private bool isSequencer;

		Queue<IntroPhase> subPhase;

		public IntroEmcee Emcee { get; set; }

		protected override void Awake()
		{
			subPhase = new Queue<IntroPhase>();
			base.Awake();

			for (int i = 0; i < transform.childCount; ++i)
			{
				IntroPhase phase = FindComponent<IntroPhase>(transform.GetChild(i));

				if (phase == null)
					continue;

				
				subPhase.Enqueue(phase);
			}
		}

		protected virtual IEnumerator Play() { yield break; }

		public IEnumerator Process()
		{
			Coroutine coroutine = StartCoroutine(Play());
			Coroutine subCoroutine = StartCoroutine(PlaySubPhase());

			yield return isSequencer ? subCoroutine : null;
			yield return coroutine;

			OnCompleProcessing();
		}
		
		IEnumerator PlaySubPhase()
		{
			while (subPhase.Count > 0)
			{
				IntroPhase phase = subPhase.Dequeue();
				phase.Emcee = Emcee;
				yield return StartCoroutine(phase.Process());
			}
		}

		public void StopPhase()
		{
			foreach (IntroPhase phase in subPhase)
			{
				phase.StopPhase();
			}

			StopAllCoroutines();
		}

		protected virtual void OnCompleProcessing() { }
	}
}

