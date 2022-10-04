using OWML.Common;
using OWML.ModHelper;
using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using QSB.Player;

namespace HideAndSeek
{
    public class HideAndSeek : ModBehaviour
    {
        public Sprite hideIcon;
        public Sprite seekIcon;

        public enum PlayerState { WAITING, HIDE, SEEK };
        public PlayerState playerState = PlayerState.WAITING;
        private Image stateIcon;
        private Text hideTime;
        private float hideSeconds;

        private void Start()
        {
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;

                var parentCanvas = GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas");
                hideIcon = Load<Sprite>("hideandseekicons", "Assets/HideNSeek/Decal_NOM_Symbols_EYE_512.png", this);
                seekIcon = Load<Sprite>("hideandseekicons", "Assets/HideNSeek/Decal_NOM_Symbols_NOM.png", this);

                // make the hider/seeker icon
                var icon = GameObject.Instantiate(GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/GaugeGroup/BackgroundFuelO2"));
                icon.name = "Player Status Display";
                icon.transform.parent = parentCanvas.transform;
                icon.transform.localScale = Vector3.one;
                icon.transform.localPosition = new Vector3(-263.3876f, 360, 0);
                icon.transform.localEulerAngles = new Vector3(0, 0, 360);
                stateIcon = icon.GetComponent<Image>();
                
                // timer
                var timer = GameObject.Instantiate(GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/GaugeGroup/FixedLabels/Fuel/FuelLabel"));
                timer.name = "Hide Time Display";
                timer.transform.parent = icon.transform;
                timer.transform.localPosition = new Vector3(60, -53, 0);
                timer.transform.localEulerAngles = new Vector3(0, 0, 50);
                timer.transform.localScale = Vector3.one;
                hideTime = timer.GetComponent<Text>();
                hideTime.text = "00:00";

                // finish set up
                SetPlayerState(PlayerState.SEEK);

                // done :)
                ModHelper.Console.WriteLine($"Hide and seek setup complete!", MessageType.Success);
            };

            QSBPlayerManager.OnAddPlayer += (player) =>
            {
                ModHelper.Events.Unity.RunWhen(
                    () => player.ProbeBody != null, 
                    () => { 
                        GameObject deathZone = new GameObject("Tag, you're it");
                        var collider = deathZone.AddComponent<SphereCollider>();
                        collider.isTrigger = true;
                        collider.radius = 1;
                        var hazard = deathZone.AddComponent<HazardVolume>();
                        hazard._firstContactDamage = 100;
                        hazard._firstContactDamageType = InstantDamageType.Electrical;
                
                        deathZone.transform.parent = player.ProbeBody.transform;
                        deathZone.transform.localPosition = Vector3.zero;
                    });
            };
        }

        public void SetPlayerState(PlayerState state)
        {
            playerState = state;
            switch(state)
            {
                case PlayerState.SEEK: stateIcon.sprite = seekIcon; break;
                case PlayerState.HIDE: stateIcon.sprite = hideIcon; break;
            }
        }

        public void Update()
        {
            if (playerState == PlayerState.HIDE)
            {
                hideSeconds += Time.deltaTime;

                int hideSecondsInt = (int)hideSeconds;
                hideTime.text = (hideSecondsInt < 600 ? "0" : "") + (hideSecondsInt/60) + 
                                ":" + 
                                (hideSecondsInt < 10 ? "0" : "") + (hideSecondsInt%60);
            }
            
            if (Keyboard.current[Key.H].wasReleasedThisFrame)
            {
                if      (playerState == PlayerState.HIDE) SetPlayerState(PlayerState.SEEK);
                else if (playerState == PlayerState.SEEK) SetPlayerState(PlayerState.HIDE);
            }

            if (Keyboard.current[Key.H].isPressed && Keyboard.current[Key.T].wasReleasedThisFrame)
            {
                hideSeconds = 0;
                hideTime.text = "00:00";
            }
        }

        // blatantly stolen from NH
        public static Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
        public static T Load<T>(string assetBundleRelativeDir, string pathInBundle, IModBehaviour mod) where T : UnityEngine.Object
        {
            string key = Path.GetFileName(assetBundleRelativeDir);
            T obj;

            try
            {
                AssetBundle bundle;

                if (AssetBundles.ContainsKey(key))
                {
                    bundle = AssetBundles[key];
                }
                else
                {
                    var completePath = Path.Combine(mod.ModHelper.Manifest.ModFolderPath, assetBundleRelativeDir);
                    bundle = AssetBundle.LoadFromFile(completePath);
                    if (bundle == null)
                    {
                        //ModHelper.Console.LogError($"Couldn't load AssetBundle at [{completePath}] for [{mod.ModHelper.Manifest.Name}]");
                        return null;
                    }

                    AssetBundles[key] = bundle;
                }

                obj = bundle.LoadAsset<T>(pathInBundle);
            }
            catch (Exception e)
            {
                //Logger.LogError($"Couldn't load asset {pathInBundle} from AssetBundle {assetBundleRelativeDir}:\n{e}");
                return null;
            }

            return obj;
        }
    }
}