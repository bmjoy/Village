﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Village.Controllers;
using UnityEngine.Serialization;
using static Village.Controllers.GameController;

namespace Village.Scriptables
{
	[CreateAssetMenu(fileName = "Resource", menuName = "Village/Resource")]
	public class Resource : Effect
	{
		[SerializeField]
		private string localeResourceName;
		public int baseCost;
		public bool tradable = true;

		public string ResourceName => Lean.Localization.LeanLocalization.GetTranslationText(localeResourceName);

		public override void Apply(int value, Villager villager = null)
		{
			instance.AddRemoveResource(this, value);
		}

		[Serializable]
		public class ResourceAmount
		{
			public Resource resource;

			[SerializeField]
			private int amount;

			public int Amount 
			{ 
				get
				{
					return amount;
				}
				set
				{
					amount = Mathf.Clamp(value, 0, RESOURCES_MAX); 
				}
			}

			public int DifficultyAmount => Mathf.RoundToInt(Amount * instance.GetDifficultyMultiplier());

			public ResourceAmount(Resource resource, int amount)
			{
				this.resource = resource;
				this.amount = amount;
			}
			public SaveData Save()
			{
				SaveData data = new SaveData();
				data.resourceName = resource.name;
				data.amount = amount;
				return data;
			}

			public override string ToString()
			{
				return $"{resource.name} ({amount})";
			}

			[Serializable]
			public class SaveData
			{
				public string resourceName;
				public int amount;
			}
		}
	}
}