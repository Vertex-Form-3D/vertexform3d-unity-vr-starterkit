// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoHubManager.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Demos
// </copyright>
// <summary>
//  Used as starting point to let developer choose amongst all demos available.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Collections.Generic;

using Photon.Pun.Demo.Cockpit;

namespace Photon.Pun.Demo.Hub
{
	public class DemoHubManager : MonoBehaviour {


		public Text TitleText;
		public Text DescriptionText;
		public GameObject OpenSceneButton;
		public GameObject OpenTutorialLinkButton;
		public GameObject OpenDocLinkButton;

        string MainDemoWebLink = "https://doc.photonengine.com/en-us/pun/v2/getting-started/pun-intro";

		struct DemoData
		{
			public string Title;
			public string Description;
			public string Scene;
			public string TutorialLink;
			public string DocLink;
		}

		Dictionary<string,DemoData> _data = new Dictionary<string, DemoData>();

		string currentSelection;

		// Use this for initialization
		void Awake () {

			PunCockpit.Embedded = false;

			OpenSceneButton.SetActive(false);
			
			OpenTutorialLinkButton.SetActive(false);
			OpenDocLinkButton.SetActive(false);

			// Setup data

			_data.Add(
				"BasicTutorial", 
				new DemoData()
				{
				Title = "Basic Tutorial",
				Description = "All custom code for connection, player and scene management.\n" +
					"Auto synchronization of room levels.\n" +
						"Uses PhotonAnimatoView for Animator synch.\n" +
						"New Unity UI all around, for Menus and player health HUD.\n" +
						"Full step by step tutorial available online.",
				Scene = "PunBasics-Launcher" ,
				TutorialLink = "https://doc.photonengine.com/en-us/pun/v2/demos-and-tutorials/pun-basics-tutorial/intro"
                }
			);
			
			_data.Add(
				"Chat", 
				new DemoData()
				{
				Title = "Chat",
				Description = "Uses the Chat API.\n" +
					"Simple UI.\n" +
					"You can enter any User ID.\n" +
					"Automatically subscribes some channels.\n" +
					"Allows simple commands via text.\n" +
					"\n" +
					"Requires configuration of Chat App ID in ServerSettings.",
						Scene = "DemoChat-Scene",
						DocLink = "http://j.mp/2iwQkPJ" 
				}
			);
            
			_data.Add(
				"Asteroids", 
				new DemoData()
				{
					Title = "Asteroids",
					Description = "Simple asteroid game based on the Unity learning asset.\n",
					Scene = "DemoAsteroids-LobbyScene",
                    TutorialLink = "https://doc.photonengine.com/pun/current/demos-and-tutorials/package-demos/asteroidsdemo"
                }
			);

			_data.Add(
				"SlotRacer", 
				new DemoData()
				{
					Title = "Slot Racer",
					Description = "Simple SlotRacing game.\n",
					Scene = "SlotCar-Scene"
				}
			);


			_data.Add(
				"Procedural", 
				new DemoData()
				{
					Title = "Procedural World",
					Description = "Shows how to synchronize the seed of a procedural world with deterministic generation.\n" +
                                    "Simple modifications to the world are possible." +
						            "\n" +
						            "This is a simple test scene to connect and join a random room, without using PUN but the actual LoadBalancing api only",
					Scene = "Procedural-Scene",
                    TutorialLink = "https://doc.photonengine.com/pun/current/demos-and-tutorials/package-demos/proceduraldemo"
                }
			);

			_data.Add(
				"PunCockpit", 
					new DemoData()
					{
						Title = "Cockpit",
						Description = "Controls most aspect of PUN.\n" +
							"Connection, Lobby, Room access, Player control",
					Scene = "PunCockpit-Scene"
					}
			);
        }

		public void SelectDemo(string Reference)
		{
			currentSelection = Reference;

			TitleText.text = _data[currentSelection].Title;
			DescriptionText.text = _data[currentSelection].Description;

			OpenSceneButton.SetActive(!string.IsNullOrEmpty(_data[currentSelection].Scene));

			OpenTutorialLinkButton.SetActive(!string.IsNullOrEmpty(_data[currentSelection].TutorialLink));
			OpenDocLinkButton.SetActive(!string.IsNullOrEmpty(_data[currentSelection].DocLink));
		}

		public void OpenScene()
		{
			if (string.IsNullOrEmpty(currentSelection))
		    {
				Debug.LogError("Bad setup, a CurrentSelection is expected at this point");
				return;
			}

			SceneManager.LoadScene(_data[currentSelection].Scene);
		}

		public void OpenTutorialLink()
		{
			if (string.IsNullOrEmpty(currentSelection))
			{
				Debug.LogError("Bad setup, a CurrentSelection is expected at this point");
				return;
			}
			
			Application.OpenURL(_data[currentSelection].TutorialLink);
		}

		public void OpenDocLink()
		{
			if (string.IsNullOrEmpty(currentSelection))
			{
				Debug.LogError("Bad setup, a CurrentSelection is expected at this point");
				return;
			}

			Application.OpenURL(_data[currentSelection].DocLink);
		}

		public void OpenMainWebLink()
		{
			Application.OpenURL(MainDemoWebLink);
		}
	}
}