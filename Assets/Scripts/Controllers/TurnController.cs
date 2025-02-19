﻿using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Village.Scriptables;
using Village.Views;
using static Village.Controllers.GameController;

namespace Village.Controllers
{
	[SelectionBase]
	public class TurnController : MonoBehaviour
	{
		[SerializeField]
		private GameChapter chapter;

		[SerializeField]
		private int turn = 1;

		[SerializeField]
		private UnityData references;

		public int Turn => turn;

		public GameChapter Chapter => chapter;

		public bool GameEnds
		{
			get
			{
				var lastTurn = chapter.nextChapter is null && Turn != Chapter.chapterTurnStart;
				var villagersCount = instance.GetVillagersCount();
				return villagersCount == 0 || lastTurn;
			}
		}

		public void ChapterUpdate()
		{
			GameChapter selected = chapter;
			while (selected != null)
			{
				// If new chapter begins
				if (turn == selected.chapterTurnStart)
				{
					LoadChapterDetails(selected);
					LoadChapterMessage(chapter.chapterStartMessage);
					instance.LoadChapterEvents();

					// load events skipped with prediction factor
					if(instance.GetPredictionFactor() == 1)
					{
						instance.LoadEventsFromTurn(turn);
					}

					instance.ApplyIntelligenceBonus();
					instance.PlayMusic();
					break;
				}
				else selected = selected.nextChapter;
			}
		}

		private void LoadChapterDetails(GameChapter selected)
		{
			chapter = selected;
			references.panel.color = selected.color;
			references.view.SetChapterName(selected.ChapterName);
			instance.SetMusic(selected.chapterMusic);
		}

		public void CheckIfGameEnds()
		{
			if (GameEnds)
			{
				instance.autoSave = false;
				instance.ClearSave();
				int reputationA = instance.GetResourceAmount(references.countryAReputation);
				int reputationB = instance.GetResourceAmount(references.countryBReputation);
				EndingLoader.ending = SelectEnding(reputationA, reputationB);
				SceneManager.LoadScene("EndingScene");
			}
		}

		public void LoadChapterMessage(Message message)
		{
			var view = Instantiate(references.messagePrefab, references.canvas);
			view.LoadMessage(message);
			view.MessagesEnded.AddListener(() => Destroy(view.gameObject));
		}

		private Message SelectEnding(int reputationA, int reputationB)
		{
			Message ending = default;

			if (instance.GetVillagersCount() == 0)
			{
				ending = references.ending1;
			}
			else
			{
				if (reputationA >= NEUTRAL_ENDING_REPUTATION
				&& reputationB >= NEUTRAL_ENDING_REPUTATION)
				{
					ending = references.ending5;
				}
				else if (reputationB >= COUNTRY_B_ENDING_REPUTATION)
				{
					ending = references.ending4;
				}
				else if (reputationA >= COUNTRY_A_ENDING_REPUTATION)
				{
					ending = references.ending3;
				}
				else
				{
					ending = references.ending2;
				}
			}
			return ending;
		}

		public void LoadTurnAndChapter(int turn)
		{
			this.turn = turn;
			LoadChapter();
		}

		public void LoadChapter()
		{
			GameChapter selected = chapter;
			while (turn < selected.chapterTurnStart || turn >= (selected.nextChapter?.chapterTurnStart ?? int.MaxValue))
			{
				selected = selected.nextChapter;
			}
			LoadChapterDetails(selected);
		}

		public void MoveToNextTurn()
		{
			turn++;
			if (!instance.GameEnds)
			{
				var sound = AudioController.instance.newTurnSound;
				AudioController.instance.PlaySound(sound);
			}
		}

		public void RefreshGUI()
		{
			references.view.SetTurn(turn);
		}

		[Serializable]
		public class UnityData
		{
			public TurnView view;

			public Image panel;

			public Message
				ending1,
				ending2,
				ending3,
				ending4,
				ending5;

			public Resource countryAReputation;

			public Resource countryBReputation;

			public MessageView messagePrefab;

			public Transform canvas;
		}
	}
}
