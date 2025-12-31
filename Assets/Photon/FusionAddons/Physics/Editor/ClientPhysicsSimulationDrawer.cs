namespace Fusion.Addons.Physics.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;

	[CustomPropertyDrawer(typeof(ClientPhysicsSimulation))]
	public sealed class ClientPhysicsSimulationDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			ClientPhysicsSimulation clientPhysicsSimulation = (ClientPhysicsSimulation)property.intValue;

			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			clientPhysicsSimulation = (ClientPhysicsSimulation)EditorGUI.EnumPopup(position, label, clientPhysicsSimulation);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = (int)clientPhysicsSimulation;
			}
			EditorGUI.EndProperty();

			switch (clientPhysicsSimulation)
			{
				case ClientPhysicsSimulation.Disabled:
					EditorGUILayout.HelpBox("Physics simulation is disabled on clients. It runs only on the server.", MessageType.Info);
					break;
				case ClientPhysicsSimulation.SyncTransforms:
					EditorGUILayout.HelpBox("Clients call Physics.SyncTransforms() every tick. This only synchronizes collider transforms with PhysX engine. Physics simulation runs only on the server.", MessageType.Info);
					break;
				case ClientPhysicsSimulation.SimulateForward:
					EditorGUILayout.HelpBox("Clients call Physics.SyncTransforms() in resimulation ticks and Physics.Simulate() in forward ticks. This effectively runs physics simulation for ticks which are being simulated for the first time.", MessageType.Warning);
					break;
				case ClientPhysicsSimulation.SimulateAlways:
					EditorGUILayout.HelpBox("Clients call Physics.Simulate() every tick including resimulations. This option allows full client side prediction for physics objects, but has a big impact on performance.", MessageType.Warning);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(clientPhysicsSimulation));
			}

			EditorGUILayout.Space(4);
		}
	}
}
