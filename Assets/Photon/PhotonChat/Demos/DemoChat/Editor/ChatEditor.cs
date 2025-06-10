// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH"/>
// <summary>Demo code for Photon Chat in Unity.</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


#if !PHOTON_UNITY_NETWORKING
using System;
using Photon.Chat;
using Photon.Chat.Demo;
using Photon.Realtime;
using UnityEditor;
using UnityEngine;


namespace Photon.Chat.Editor
{
    [InitializeOnLoad]
    public class ChatEditor : EditorWindow
    {
        static ChatEditor()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            EditorApplication.update -= OnEditorUpdate;

            //ChatSettings settings = ChatSettings.Load();
            //if (settings != null && !settings.WizardDone && string.IsNullOrEmpty(settings.AppId))
            //{
            //    OpenWizard();
            //}
        }


        [MenuItem("Window/Photon Chat/Setup")]
        public static void OpenWizard()
        {
            //currentSettings = ChatSettings.Load();
            //currentSettings.WizardDone = true;
            //EditorUtility.SetDirty(currentSettings);

            ChatEditor editor = (ChatEditor)EditorWindow.GetWindow(typeof (ChatEditor), false, "Photon Chat");
            editor.minSize = editor.preferredSize;
        }


        private ChatGui cGui;
        internal string mailOrAppId;
        internal bool showDashboardLink = false;
        internal bool showRegistrationDone = false;
        internal bool showRegistrationError = false;
        private readonly Vector2 preferredSize = new Vector2(350, 400);

        internal static string UrlCloudDashboard = "https://dashboard.photonengine.com/en-US/";

        public string WelcomeText = "Thanks for importing Photon Chat.\nThis window should set you up.\n\nYou will need a free Photon Account to setup a Photon Chat application.\nOpen the Photon Dashboard (webpage) to access your account (see button below).\n\nCopy and paste a Chat AppId into the field below and click \"Setup\".";
        //public string AlreadyRegisteredInfo = "The email is registered so we can't fetch your AppId (without password).\n\nPlease login online to get your AppId and paste it above.";
        //public string RegisteredNewAccountInfo = "We created a (free) account and fetched you an AppId.\nWelcome. Your Photon Chat project is setup.";
        //public string FailedToRegisterAccount = "This wizard failed to register an account right now. Please check your mail address or try via the Dashboard.";
        //public string AppliedToSettingsInfo = "Your AppId is now applied to this project.";
        public string SetupCompleteInfo = "<b>Done!</b>\nYour Chat AppId is now stored in the <b>Scripts</b> object, Chat App Settings.";
        public string CloseWindowButton = "Close";
        public string OpenCloudDashboardText = "Photon Dashboard Login";
        public string OpenCloudDashboardTooltip = "Review Cloud App information and statistics.";


        public void OnGUI()
        {
            if (this.cGui == null)
            {
                #if UNITY_6000_0_OR_NEWER
                cGui = FindFirstObjectByType<ChatGui>();
                #else
                cGui = FindObjectOfType<ChatGui>();
                #endif
            }

            GUI.skin.label.wordWrap = true;
            GUI.skin.label.richText = true;
            if (string.IsNullOrEmpty(mailOrAppId))
            {
                mailOrAppId = string.Empty;
            }

            GUILayout.Label("Chat Settings", EditorStyles.boldLabel);
            GUILayout.Label(this.WelcomeText);
            GUILayout.Space(15);


            GUILayout.Label("Chat AppId");
            string input = EditorGUILayout.TextField(this.mailOrAppId);


            if (GUI.changed)
            {
                this.mailOrAppId = input.Trim();
            }

            //bool isMail = false;
            bool minimumInput = false;
            bool isAppId = false;

            if (IsValidEmail(this.mailOrAppId))
            {
                // this should be a mail address
                minimumInput = true;
                //isMail = true;
            }
            else if (IsAppId(this.mailOrAppId))
            {
                // this should be an appId
                minimumInput = true;
                isAppId = true;
            }


            EditorGUI.BeginDisabledGroup(!minimumInput);


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool setupBtn = GUILayout.Button("Setup", GUILayout.Width(205));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            if (setupBtn)
            {
                this.showDashboardLink = false;
                this.showRegistrationDone = false;
                this.showRegistrationError = false;

                //if (isMail)
                //{
                //    EditorUtility.DisplayProgressBar("Fetching Account", "Trying to register a Photon Cloud Account.", 0.5f);
                //    AccountService service = new AccountService();
                //    //service.RegisterByEmail(this.mailOrAppId, AccountService.Origin.Pun);
                //    //EditorUtility.ClearProgressBar();

                //    //if (service.ReturnCode == 0)
                //    //{
                //    //    currentSettings.AppId = service.AppId;
                //    //    EditorUtility.SetDirty(currentSettings);
                //    //    this.showRegistrationDone = true;

                //    //    Selection.objects = new UnityEngine.Object[] { currentSettings };
                //    //}
                //    //else
                //    //{
                //    //    if (service.Message.Contains("registered"))
                //    //    {
                //    //        this.showDashboardLink = true;
                //    //    }
                //    //    else
                //    //    {
                //    //        this.showRegistrationError = true;
                //    //    }
                //    //}
                //}
                //else 
                if (isAppId)
                {
                    //currentSettings.AppId = this.mailOrAppId;
                    //EditorUtility.SetDirty(currentSettings);
                    if (this.cGui != null)
                    {
                        this.cGui.ChatAppSettings.AppIdChat = this.mailOrAppId;
                        EditorUtility.SetDirty(this.cGui);
                    }

                    showRegistrationDone = true;
                }

                //EditorGUIUtility.PingObject(currentSettings);
            }
            EditorGUI.EndDisabledGroup();


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(OpenCloudDashboardText, OpenCloudDashboardTooltip), GUILayout.Width(205)))
            {
                EditorUtility.OpenWithDefaultApp(UrlCloudDashboard);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //if (this.showDashboardLink)
            //{
            //    // button to open dashboard and get the AppId
            //    GUILayout.Space(15);
            //    GUILayout.Label(AlreadyRegisteredInfo);


            //    GUILayout.BeginHorizontal();
            //    GUILayout.FlexibleSpace();
            //    if (GUILayout.Button(new GUIContent(OpenCloudDashboardText, OpenCloudDashboardTooltip), GUILayout.Width(205)))
            //    {
            //        EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.mailOrAppId));
            //        this.mailOrAppId = string.Empty;
            //        this.showDashboardLink = false;
            //    }
            //    GUILayout.FlexibleSpace();
            //    GUILayout.EndHorizontal();
            //}
            //if (this.showRegistrationError)
            //{
            //    GUILayout.Space(15);
            //    GUILayout.Label(FailedToRegisterAccount);

            //    GUILayout.BeginHorizontal();
            //    GUILayout.FlexibleSpace();
            //    if (GUILayout.Button(new GUIContent(OpenCloudDashboardText, OpenCloudDashboardTooltip), GUILayout.Width(205)))
            //    {
            //        EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.mailOrAppId));
            //        this.mailOrAppId = string.Empty;
            //        this.showDashboardLink = false;
            //    }
            //    GUILayout.FlexibleSpace();
            //    GUILayout.EndHorizontal();

            //}
            if (this.showRegistrationDone)
            {
                GUILayout.Space(15);
                //GUILayout.Label("Registration done");
                ////if (isMail)
                ////{
                ////    GUILayout.Label(RegisteredNewAccountInfo);
                ////}
                ////else
                ////{
                //    GUILayout.Label(AppliedToSettingsInfo);
                ////}

                // setup-complete info
                GUILayout.Space(15);
                GUILayout.Label(SetupCompleteInfo);


                // close window (done)
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(CloseWindowButton, GUILayout.Width(205)))
                {
                    this.Close();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        public static bool IsAppId(string val)
        {
            if (string.IsNullOrEmpty(val) || val.Length < 16)
            {
                return false;
            }

            try
            {
                new Guid(val);
            }
            catch
            {
                return false;
            }
            return true;
        }

        // https://stackoverflow.com/a/1374644/1449056
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return false;
            }
            try
            {
                System.Net.Mail.MailAddress addr = new System.Net.Mail.MailAddress(email);
                return email.Equals(addr.Address);
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif