using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Modding;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace Bindings
{
    [UsedImplicitly]
    public class Bindings : Mod, ITogglableMod
    {
        [UsedImplicitly]
        public static bool True() => true;

        [UsedImplicitly]
        public static int BoundNailDamage()
        {
            int @base = PlayerData.instance.nailDamage;

            return @base < 13 ? Mathf.RoundToInt(@base * .8f) : 13;
        }

        [UsedImplicitly]
        public static int BoundMaxHealth() => 5;

        private readonly string[] BindingProperties =
        {
            nameof(BossSequenceController.IsInSequence),
            nameof(BossSequenceController.BoundNail),
            nameof(BossSequenceController.BoundCharms),
            nameof(BossSequenceController.BoundShell),
            nameof(BossSequenceController.BoundSoul)
        };

        private readonly List<Detour> _detours = new List<Detour>();

        public override void Initialize()
        {
            foreach (string property in BindingProperties)
            {
                _detours.Add
                (
                    new Detour
                    (
                        typeof(BossSequenceController).GetProperty(property)?.GetGetMethod(),
                        typeof(Bindings).GetMethod(nameof(True))
                    )
                );
            }

            _detours.Add
            (
                new Detour
                (
                    typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundNailDamage))?.GetGetMethod(),
                    typeof(Bindings).GetMethod(nameof(BoundNailDamage))
                )
            );
            
            _detours.Add
            (
                new Detour
                (
                    typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundMaxHealth))?.GetGetMethod(),
                    typeof(Bindings).GetMethod(nameof(BoundMaxHealth))
                )
            );

            ModHooks.Instance.SavegameLoadHook += OnLoad;
            ModHooks.Instance.NewGameHook += NewGame;
            On.BossSceneController.RestoreBindings += NoOp;
        }
        
        private static void NoOp(On.BossSceneController.orig_RestoreBindings orig, BossSceneController self) {}

        private static void NewGame() => OnLoad();

        private static void OnLoad(int id = -1)
        {
            GameManager.instance.OnFinishedEnteringScene -= ShowIcons;
            GameManager.instance.OnFinishedEnteringScene += ShowIcons;
        }

        private static void ShowIcons()
        {
            GameManager.instance.StartCoroutine(ShowIconsCoroutine());
        }

        private static IEnumerator ShowIconsCoroutine()
        {
            yield return new WaitWhile(() => HeroController.instance == null);
            
            yield return null;
            
            EventRegister.SendEvent("SHOW BOUND NAIL");
            EventRegister.SendEvent("SHOW BOUND CHARMS");

            if (PlayerData.instance.equippedCharms.Count == 0) yield break;
            
            foreach (int charm in PlayerData.instance.equippedCharms)
            {
                GameManager.instance.SetPlayerDataBool($"equippedCharm_{charm}", false);
            }
            
            PlayerData.instance.equippedCharms.Clear();
        }

        public void Unload()
        {
            foreach (Detour d in _detours)
            {
                d.Dispose();
            }
            
            _detours.Clear();
            
            ModHooks.Instance.SavegameLoadHook -= OnLoad;
            ModHooks.Instance.NewGameHook -= NewGame;
            On.BossSceneController.RestoreBindings -= NoOp;
        }
    }
}