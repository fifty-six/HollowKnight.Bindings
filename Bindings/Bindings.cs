using System.Collections;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using JetBrains.Annotations;
using Modding;
using UnityEngine;
using static BossSequenceController;

namespace Bindings
{
    [UsedImplicitly]
    public class Bindings : Mod
    {
        private static readonly FieldInfo DATA_FI = typeof(BossSequenceController).GetField("currentData", FLAGS);
        private static readonly FieldInfo SEQUENCE_FI = typeof(BossSequenceController).GetField("currentSequence", FLAGS);

        private const ChallengeBindings BINDINGS = (ChallengeBindings) 15;
        private const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Static;

        public override void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (a, b) =>
            {
                if (GameManager.instance == null) return;
                GameManager.instance.StartCoroutine(SetBindings());
            };

            On.GetNailDamage.OnEnter += NailBinding;
            On.HeroController.MaxHealth += MaxHealth;
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
            yield return null;
            yield return null;
            
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
                BossSequence bs = ScriptableObject.CreateInstance<BossSequence>();
                bs.maxHealth = 4;
                bs.nailDamage = 13;
                bs.lowerNailDamagePercentage = 1f;
                
                SEQUENCE_FI.SetValue(null, bs);
            }

            if (seq != null && seq.Count == 1)
            {
                seq.maxHealth = 4;
                seq.nailDamage = 13;
            }
            
            ApplyBindings();
            
            if (data == null)
            {
                DATA_FI.SetValue(null, null);
            }

            if (seq == null)
            {
                SEQUENCE_FI.SetValue(null, null);
            }

            for (int i = 5; i <= 11; i++)
            {
                GameObject go = GameObject.Find("Health " + i);
                
                if (go == null) continue;

                PlayMakerFSM fsm = go.LocateMyFSM("health_display");
                
                foreach (FsmStateAction action in fsm.FsmStates.Single(x => x.Name == "Bound").Actions)
                {
                    action.OnEnter();
                    action.Reset();
                }
            }
            
        }
    }
}