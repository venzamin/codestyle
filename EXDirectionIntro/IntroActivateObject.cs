using System.Collections;
using UnityEngine;

namespace CocoonGames.Dolphin.Client
{
	public class IntroActivateObject : IntroFindObject
	{
		[SerializeField]
		private bool isActive = false;
		[SerializeField]
		private bool isTargetChild = false;

		protected override IEnumerator Play()
		{
			yield return StartCoroutine(base.Play());

			if (Target)
			{
				if (isTargetChild)
				{
					for (int i = 0; i < Target.childCount; ++i)
					{
						Target.GetChild(i).SetActive(isActive);
					}
				}
				else
				{
					Target.SetActive(isActive);
				}
			}
		}
	}
}

