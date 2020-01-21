using CocoonGames.Dolphin.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CocoonGames.Dolphin.Client
{
	public class IntroEmcee : MonoObject
	{
		IntroPhase FinalPhase { get; set; }
		Queue<IntroPhase> intros;

		Transform PeekedPhase { get; set; }
		public UIIntro UI { get; private set; }
		public TouchCamera Camera { get; set; }

		public bool IsComplete { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			intros = new Queue<IntroPhase>();

			UI = UIManager.Instance.LoadUI<UIIntro>("UI_Prefabs/Introduction/Intro", FindChild("UI", transform));
			Transform phasesTransform = FindChild("Phase", transform);

			for (int cnt = 0; cnt < phasesTransform.childCount; ++cnt)
			{
				IntroPhase phase = FindComponent<IntroPhase>(phasesTransform.GetChild(cnt));
				if (phase == null)
					continue;

				phase.Emcee = this;
				intros.Enqueue(phase);

				if (phase.name.Equals("Final"))
					FinalPhase = phase;
			}

			UI.Skip += (sender, e) => Skip();
		}

		public void StartProcess()
		{
			StartCoroutine(Process());
		}

		IEnumerator Process()
		{
			IsComplete = false;
			while (intros.Count > 0)
			{
				yield return StartCoroutine(intros.Dequeue().Process());
			}
			IsComplete = true;
		}

		void Skip()
		{
			StartCoroutine(SkipEnumerator());
		}

		IEnumerator SkipEnumerator()
		{
			yield return StartCoroutine(FinalPhase.Process());

			foreach (IntroPhase phase in intros)
			{
				phase.StopPhase();
			}

			StopAllCoroutines();
			IsComplete = true;
		}

		public void Release()
		{
			Destroy(gameObject);
		}
	}
}

