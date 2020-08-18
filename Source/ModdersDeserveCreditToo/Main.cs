using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdersDeserveCreditToo
{
    using HarmonyLib;
    using RimWorld;
    using UnityEngine;
    using Verse;
    using Verse.Noise;

    public static class Main
    {
        public static IEnumerable<Tuple<string, string>> GetMods()
        {

            List<ModMetaData> mods = ModsConfig.ActiveModsInLoadOrder.Where(mod => !mod.Official).ToList();

            if (ModdersDeserveCreditMod.settings.useCategories)
                return mods.GroupBy(mod => mod.Author).OrderBy(grp => grp.Key).SelectMany(grp => grp.Select(mmd => new Tuple<string, string>(mmd.Author, mmd.Name)));
            else
                return mods.Select(mmd => new Tuple<string, string>(mmd.Name, mmd.Author));
        }

        public static void AddTranslationKey(string key)
        {
            if (!LanguageDatabase.activeLanguage.HaveTextForKey(key))
                LanguageDatabase.activeLanguage.keyedReplacements.Add(key, new LoadedLanguage.KeyedReplacement() { fileSource = "CustomCredits", fileSourceFullPath = "CustomCreditLand", fileSourceLine = 0, isPlaceholder = false, key = key, value = key });
        }

        public static IEnumerable<CreditsEntry> Postfix(IEnumerable<CreditsEntry> __result)
        {
            foreach (CreditsEntry creditsEntry in __result)
            {
                yield return creditsEntry;
                if (creditsEntry is CreditRecord_Role crr && crr.creditee.Equals("Many other gracious volunteers!"))
                {
                    yield return new CreditRecord_Space(200f);
                    LoadedLanguage lang = LanguageDatabase.activeLanguage;
                    lang.LoadMetadata();
                    if (lang.info.credits.Count > 0)
                    {
                        yield return new CreditRecord_Title("Credits_TitleLanguage".Translate(lang.FriendlyNameEnglish));
                    }
                    foreach (CreditsEntry credit in lang.info.credits)
                    {
                        if (credit is CreditRecord_Role creditRecordRole)
                            creditRecordRole.compressed = true;
                        yield return credit;
                    }
                    yield return new CreditRecord_Space(150f);


                    yield return new CreditRecord_Title("Your mods' creators");

                    foreach ((string item1, string item2) in GetMods())
                    {
                        AddTranslationKey(item1);
                        yield return new CreditRecord_Role(item1, item2);
                    }

                    yield return new CreditRecord_Space(150f);

                    foreach (LoadedLanguage language in LanguageDatabase.AllLoadedLanguages.Except(lang))
                    {
                        language.LoadMetadata();
                        if (language.info.credits.Count > 0)
                        {
                            yield return new CreditRecord_Title("Credits_TitleLanguage".Translate(language.FriendlyNameEnglish));
                        }
                        foreach (CreditsEntry credit in language.info.credits)
                        {
                            if (credit is CreditRecord_Role creditRecordRole)
                                creditRecordRole.compressed = true;
                            yield return credit;
                        }
                    }
                }
            }
        }

        [DebugAction("Mods", "Show Endgame Credits", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void ActivateEnd() => 
            GameVictoryUtility.ShowCredits("You cheated not only the game, but yourself.\r\n\r\nYou didn't grow.\r\nYou didn't improve.\r\nYou took a shortcut and gained nothing.\r\n\r\nYou experienced a hollow victory.\r\nNothing was risked and nothing was gained.\r\n\r\nIt's sad that you don't know the difference. ");
    }

    public class ModdersDeserveCreditModSettings : ModSettings
    {
        public bool useCategories = true;


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.useCategories, "useCategories", true);
        }
    }

    public class ModdersDeserveCreditMod : Mod
    {
        public static ModdersDeserveCreditModSettings settings;

        public override string SettingsCategory() => "Modders Deserve Credit";

        public ModdersDeserveCreditMod(ModContentPack content) : base(content)
        {
            settings = this.GetSettings<ModdersDeserveCreditModSettings>();
            Harmony harmony = new Harmony("rimworld.erdelf.ModdersDeserveCreditToo");
            harmony.Patch(AccessTools.Method(typeof(CreditsAssembler), nameof(CreditsAssembler.AllCredits)), postfix: new HarmonyMethod(typeof(Main), nameof(Main.Postfix)));
        }

        private Vector2 scrollPos;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard options      = new Listing_Standard();
            Color defaultColor = GUI.color;
            options.Begin(inRect);
            GUI.color   = defaultColor;
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            options.Gap();
            options.CheckboxLabeled("Use Categories", ref settings.useCategories);
            options.Gap();
            options.Label("Preview:");

            List<Tuple<string, string>> list = Main.GetMods().ToList();
            
            Rect rect = new Rect(options.GetRect(inRect.height * 0.8f));
            Rect viewRect = new Rect(Vector2.zero, new Vector2(rect.width - 20f, (list.Count) * (Text.LineHeight + options.verticalSpacing) + 8f));

            Widgets.DrawBoxSolid(rect, new Color(0.25f, 0.25f, 0.25f, 0.25f));
            GUI.color = Color.cyan;
            Widgets.DrawBox(rect.ExpandedBy(3f), 3);
            GUI.color = defaultColor;
            options.BeginScrollView(rect, ref this.scrollPos, ref viewRect);

            string text = string.Empty;
            foreach ((string item1, string item2) in list)
            {
                Main.AddTranslationKey(item1);

                options.LabelDouble(item1 != text ? item1 : string.Empty, item2);
                text = item1;
            }

            options.EndScrollView(ref viewRect);

            options.End();
            settings.Write();
        }
    }
}
