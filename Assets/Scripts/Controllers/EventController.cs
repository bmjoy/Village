using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Village.Scriptables;
using Village.Views;
using static Village.Controllers.GameController;

namespace Village.Controllers
{
	[SelectionBase]
	public class EventController : MonoBehaviour
	{
		[SerializeField]
		private Transform contentParent;

		[SerializeField]
		private EventView eventPrefab;

		[SerializeField]
		private EventBase merchantEvent;

		[SerializeField]
		private int _predictionFactor;

		[SerializeField]
		private List<EventView> currentEvents;

		[SerializeField]
		private List<GameEvent> chapterEvents;

		[SerializeField]
		private AudioClip newEventsSound;

		public List<GameEvent.SaveData> SaveCurrentEvents()
		{
			return currentEvents.Select(x => x.Event.Save()).ToList();
		}

		public List<GameEvent.SaveData> SaveChapterEvents()
		{
			return chapterEvents.Select(x => x.Save()).ToList();
		}

		public int PredictionFactor { get => _predictionFactor; set => _predictionFactor = value; }

		public EventView AddEvent(GameEvent gameEvent, bool saveLoad = false)
		{
			var view = Instantiate(eventPrefab, contentParent);
			view.Load(gameEvent);
			currentEvents.Add(view);
			if (gameEvent.eventBase.name == merchantEvent.name && !saveLoad)
			{
				instance.LoadNewMerchantTrades();
			}
			return view;
		}

		public bool MerchantAvailable()
		{
			return currentEvents.Select(x => x.Event.eventBase).Any(x=> x.name==merchantEvent.name);
		}

		public void LoadChapterEvents()
		{
			GameChapter chapter = instance.Chapter;
			chapterEvents = chapter.GenerateEventList()
				.OrderBy(x => x.turn)
				.ThenByDescending(x=> x.eventBase.eventPriority)
				.ToList();
		}

		public void LoadChapterEvents(List<GameEvent.SaveData> events)
		{
			chapterEvents = new List<GameEvent>();
			foreach (var ev in events)
			{
				EventBase eventBase = AssetManager.instance.GetAsset<EventBase>(ev.eventName);
				chapterEvents.Add(new GameEvent(eventBase, ev.turn));
			}
		}

		public void LoadCurrentEvents(List<GameEvent.SaveData> events)
		{
			foreach (var ev in events)
			{
				EventBase evBase = AssetManager.instance.GetAsset<EventBase>(ev.eventName);
				AddEvent(new GameEvent(evBase, ev.turn), saveLoad: true);
			}
		}

		public void RefreshGUI()
		{
			SortEvents();
			foreach (var ev in currentEvents)
			{
				ev.transform.SetSiblingIndex(currentEvents.IndexOf(ev));
				ev.RefreshData();
			}
		}

		public void SortEvents()
		{
			currentEvents = currentEvents
				.OrderBy(x => x.TurnsLeft)
				.ThenByDescending(x => x.Event.eventBase.eventPriority)
				.ToList();
		}

		public void EventUpdate()
		{
			int turnToLoad = instance.GetCurrentTurn() + PredictionFactor;
			var chapter = instance.Chapter;

			var toRemove = new List<EventView>();
			foreach (var ev in currentEvents)
			{ 
				ev.RefreshData();
				if (ev.TurnsLeft == 0)
				{
					bool eventSuccess = true;
					foreach (var req in ev.Event.eventBase.requirements)
					{
						if (instance.GetResourceAmount(req.resource) < req.Amount)
						{
							eventSuccess = false;
						}
					}
					if (eventSuccess)
					{
						ev.Event.eventBase.requirements.ForEach(x => x.resource.Apply(-x.Amount));
						ev.Event.eventBase.ApplySuccess();
					}
					else
					{
						ev.Event.eventBase.ApplyFailure();
					}
					toRemove.Add(ev);
				}
			}
			foreach (var ev in toRemove)
			{
				currentEvents.Remove(ev);
				Destroy(ev.gameObject);
			}

			var newEvents = chapterEvents.Where(x => x.turn == turnToLoad).ToList();

			if(newEvents.Count > 0)
			{
				AudioController.instance.PlaySound(newEventsSound);
			}

			foreach (var newEvent in newEvents)
			{
				AddEvent(newEvent);
			}
		}
	}
}
