using System;
using System.CodeDom;
using System.Collections;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon;
using Modding;
using UnityEngine;
using static BossSequenceController;

namespace Bindings
{
    [UsedImplicitly]
    public class Bindings : Mod
    {
        private static readonly FieldInfo DATA_FI = typeof(BossSequenceController).GetField("currentData", FLAGS);

        private static readonly FieldInfo SEQUENCE_FI =
            typeof(BossSequenceController).GetField("currentSequence", FLAGS);

        private const ChallengeBindings BINDINGS = (ChallengeBindings) 15;
        private const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Static;

        public override void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (a, b) =>
            {
                if (GameManager.instance == null) return;
                GameManager.instance.StartCoroutine(SetBindings());
            };

            ModHooks.Instance.SoulGainHook += OnSoulGain;

            On.GetNailDamage.OnEnter += NailBinding;
            On.HeroController.MaxHealth += MaxHealth;
        }

        private int OnSoulGain(int num)
        {
            Log(PlayerData.instance.MPCharge + "?: " + num);
            return PlayerData.instance.MPCharge + num > 33 ? 0 : num;
        }

        private static void MaxHealth(On.HeroController.orig_MaxHealth orig, HeroController self)
        {
            self.playerData.prevHealth = 4;
            self.playerData.health = 4;
            self.playerData.blockerHits = 4;
            self.playerData.UpdateBlueHealth();
            self.proxyFSM.SendEvent("HeroCtrl-MaxHealth");
        }

        private static void NailBinding(On.GetNailDamage.orig_OnEnter orig, GetNailDamage self)
        {
            if (!self.storeValue.IsNone)
            {
                self.storeValue.Value = 13;
            }

            self.Finish();
        }

        private IEnumerator SetBindings()
        {
            if (HeroController.instance == null || GameManager.instance == null)
            {
                yield break;
            }

            if (DATA_FI == null || SEQUENCE_FI == null)
            {
                Log("wtf");
                yield break;
            }

            BossSequenceData data = (BossSequenceData) DATA_FI.GetValue(null);
            if (data == null)
            {
                DATA_FI.SetValue(null, new BossSequenceData
                {
                    bindings = BINDINGS
                });
            }
            else if (string.IsNullOrEmpty(data.bossSequenceName))
            {
                data.bindings = BINDINGS;
            }

            BossSequence seq = (BossSequence) SEQUENCE_FI.GetValue(null);

            if (seq == null)
            {
                yield return new WaitForSeconds(.1f);
                seq = (BossSequence) SEQUENCE_FI.GetValue(null);
                if (seq == null)
                {
                    BossSequence bs = ScriptableObject.CreateInstance<BossSequence>();
                    bs.maxHealth = 4;
                    bs.nailDamage = 13;
                    bs.lowerNailDamagePercentage = 1f;

                    SEQUENCE_FI.SetValue(null, bs);
                }
            }

            if (seq != null && seq.Count == 1)
            {
                seq.maxHealth = 4;
                seq.nailDamage = 13;
            }

            EventRegister.SendEvent("SHOW BOUND NAIL");
            GameManager.instance.playerData.equippedCharms.Clear();
            GameManager.instance.playerData.overcharmed = false;
            for (int i = 0; i < 50; i++)
            {
                PlayerData.instance.SetBoolInternal("equippedCharm_" + i, false);
            }

            PlayerData.instance.equippedCharms.Clear();
            EventRegister.SendEvent("SHOW BOUND CHARMS");
            HeroController.instance.CharmUpdate();
            PlayMakerFSM.BroadcastEvent("CHARM EQUIP CHECK");
            EventRegister.SendEvent("UPDATE BLUE HEALTH");
            PlayMakerFSM.BroadcastEvent("HUD IN");
            EventRegister.SendEvent("BIND VESSEL ORB");

            while (GameManager.instance == null ||
                   GameManager.instance.soulOrb_fsm == null ||
                   GameManager.instance.soulVessel_fsm == null ||
                   GameCameras.instance.soulOrbFSM == null ||
                   GameCameras.instance.soulVesselFSM == null ||
                   GameObject.Find("Health 11") == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(.2f);

            PlayerData.instance.ClearMP();
            GameManager.instance.soulOrb_fsm.SendEvent("MP LOSE");
            GameManager.instance.soulVessel_fsm.SendEvent("MP RESERVE DOWN");
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");

            yield return new WaitForSeconds(2f);

            if (seq == null)
            {
                SEQUENCE_FI.SetValue(null, null);
            }

            Log("fin");
        }
    }
}