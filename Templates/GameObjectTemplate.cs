﻿using System.Collections.Generic;
using UnityEngine;

namespace SRMLExtras.Templates
{
	/// <summary>
	/// Represents a template game object, used to contain templates to turn into
	/// Runtime Prefabs
	/// </summary>
	public class GameObjectTemplate
	{
		private readonly List<Component> components = new List<Component>();

		public readonly List<GameObjectTemplate> children = new List<GameObjectTemplate>();

		public string Name { get; set; } = "Template Object";
		public string Tag { get; set; } = null;
		public LayerMask Layer { get; set; } = LayerMask.NameToLayer("Default");

		public Vector3 Position { get; set; } = Vector3.zero;
		public Vector3 Rotation { get; set; } = Vector3.zero;
		public Vector3 Scale { get; set; } = Vector3.one;

		public MarkerType DebugMarker { get; private set; }

		private event System.Action<GameObject> afterChildren;

		public GameObjectTemplate(string name, params Component[] comps)
		{
			Name = name;
			components.AddRange(comps);
		}

		public GameObjectTemplate SetTransform(Vector3 position, Vector3 rotation, Vector3 scale)
		{
			Position = position;
			Rotation = rotation;
			Scale = scale;
			return this;
		}

		public GameObjectTemplate SetTransform(BaseObjects.ObjectTransformValues trans)
		{
			Position = trans.position;
			Rotation = trans.rotation;
			Scale = trans.scale;
			return this;
		}

		public GameObjectTemplate SetDebugMarker(MarkerType type, float scaleMult = 1)
		{
			DebugMarker = type;

			if (BaseObjects.markerMaterials.ContainsKey(type))
			{
				AddChild(new GameObjectTemplate("DebugMarker", 
					new Create<MeshFilter>((filter) => filter.sharedMesh = BaseObjects.cubeMesh),
					new Create<MeshRenderer>((render) => render.sharedMaterial = BaseObjects.markerMaterials[type]),
					new DebugMarker()
					{
						scaleMult = scaleMult
					}
				).SetTransform(Vector3.zero, Vector3.zero, Vector3.one));
			}

			return this;
		}

		public GameObjectTemplate SetAfterChildren(System.Action<GameObject> action)
		{
			afterChildren += action;
			return this;
		}

		public GameObjectTemplate AddChild(GameObjectTemplate template)
		{
			children.Add(template);
			return this;
		}

		public GameObjectTemplate AddComponents(params Component[] comps)
		{
			components.AddRange(comps);
			return this;
		}

		public Component[] GetComponents()
		{
			return components.ToArray();
		}

		public GameObject ToGameObject(GameObject parent)
		{
			GameObject obj;
			if (parent == null)
			{
				obj = new GameObject(Name);
				SRML.Utils.GameObjectUtils.Prefabitize(obj);
			}
			else
			{
				obj = new GameObject(Name);
				obj.transform.parent = parent.transform;
			}

			obj.transform.localPosition = Position;
			obj.transform.localEulerAngles = Rotation;
			obj.transform.localScale = Scale;

			foreach (Component comp in components)
			{
				if (comp is ICreateComponent)
				{
					((ICreateComponent)comp).AddComponent(obj);
				}
				else
				{
					Component newComp = obj.AddComponent(comp.GetType());
					comp.CopyAllTo(newComp);
				}
			}

			if (Tag != null) obj.tag = Tag;
			if (Layer != LayerMask.NameToLayer("Default")) obj.layer = Layer;

			foreach (GameObjectTemplate child in children)
				child.ToGameObject(obj);

			afterChildren?.Invoke(obj);

			return obj;
		}
	}
}