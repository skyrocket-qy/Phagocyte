using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestI18n : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING BILINGUAL I18N AUTOMATED VERIFICATION ---");

        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        if (menuScene == null)
        {
            GD.PrintErr("Failed to load main_menu.tscn");
            Quit(1);
            return;
        }
        var menu = menuScene.Instantiate();
        Root.AddChild(menu);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        _testDone = true;
        var menu = Root.GetNodeOrNull<MainMenu>("MainMenu");
        if (menu == null)
        {
            GD.PrintErr("MainMenu node not found");
            Quit(1);
            return true;
        }

        // 0. Default language is English out of the box.
        AssertThat(GameManager.CurrentLanguage).IsEqual("en");

        // 1. Test Simplified Chinese default
        GameManager.SetLanguage("zh_CN");
        menu.UpdateAllTexts();

        AssertThat(menu.StartBtn?.Text.Contains("开始") ?? false).IsTrue();
        AssertThat(menu.ClassHeaderLbl?.Text.Contains("选择细胞") ?? false).IsTrue();
        AssertThat(menu.AchievementsBtn?.Text.Contains("成就") ?? false).IsTrue();
        AssertThat(menu.GlobalBackBtn?.Text.Contains("返回") ?? false).IsTrue();
        GD.Print("[PASS] Simplified Chinese (zh_CN) text rendering verified.");

        // 2. Test English switching
        GameManager.SetLanguage("en");
        menu.UpdateAllTexts();

        AssertThat(menu.StartBtn?.Text.Contains("Start") ?? false).IsTrue();
        AssertThat(menu.ClassHeaderLbl?.Text.Contains("Select Cell") ?? false).IsTrue();
        AssertThat(menu.MapHeaderLbl?.Text.Contains("Stage") ?? false).IsTrue();
        AssertThat(menu.AchievementsBtn?.Text.Contains("Achievements") ?? false).IsTrue();
        AssertThat(menu.AchievementView?.HeaderLabel?.Text.Contains("Achievements") ?? false).IsTrue();
        AssertThat(menu.GlobalBackBtn?.Text.Contains("Back") ?? false).IsTrue();
        GD.Print("[PASS] English (en) text rendering verified.");

        // 3. Test Metadata Translation
        var macroEn = GameManager.GetClassInfo("macrophage");
        AssertThat(macroEn["role"].AsString().Contains("Melee Heavy Tank")).IsTrue();

        var mapEn = GameManager.GetMapInfo("acute_wound");
        AssertThat(mapEn["name"].AsString().Contains("Acute Wound")).IsTrue();
        GD.Print("[PASS] Class and Map metadata translations verified.");

        // 4. Test Toggle Functionality (en -> zh_CN -> zh_TW -> ja -> de -> fr -> ru -> es -> en)
        string newLang = GameManager.ToggleLanguage();
        AssertThat(newLang).IsEqual("zh_CN");
        AssertThat(GameManager.CurrentLanguage).IsEqual("zh_CN");

        newLang = GameManager.ToggleLanguage();
        AssertThat(newLang).IsEqual("zh_TW");
        AssertThat(GameManager.CurrentLanguage).IsEqual("zh_TW");
        menu.UpdateAllTexts();
        AssertThat(menu.StartBtn?.Text.Contains("開始") ?? false).IsTrue();
        GD.Print("[PASS] Traditional Chinese (zh_TW) text rendering verified.");

        string[] restCycle = { "ja", "de", "fr", "ru", "es", "en" };
        foreach (string expected in restCycle)
        {
            newLang = GameManager.ToggleLanguage();
            AssertThat(newLang).IsEqual(expected);
            AssertThat(GameManager.CurrentLanguage).IsEqual(expected);
        }
        menu.UpdateAllTexts();
        AssertThat(menu.StartBtn?.Text.Contains("Start") ?? false).IsTrue();
        GD.Print("[PASS] GameManager.toggle_language() 8-locale cycle verified.");

        // 5. Test In-Game HUD Localization
        menu.QueueFree();

        var hudScene = AssetLoader.Load<PackedScene>("res://scenes/ui/hud.tscn");
        AssertThat(hudScene).IsNotNull();
        var hud = hudScene!.Instantiate<Hud>();
        Root.AddChild(hud);

        // Test in English
        GameManager.SetLanguage("en");
        hud.UpdateLocalizedTexts();
        AssertThat(hud.HpTitleLabel?.Text.Contains("HP") ?? false).IsTrue();
        AssertThat(hud.ResumeBtn?.Text.Contains("Resume") ?? false).IsTrue();

        // Test in Chinese
        GameManager.SetLanguage("zh_CN");
        hud.UpdateLocalizedTexts();
        AssertThat(hud.HpTitleLabel?.Text.Contains("HP") ?? false).IsTrue();
        AssertThat(hud.ResumeBtn?.Text.Contains("继续") ?? false).IsTrue();
        GD.Print("[PASS] In-game HUD & Pause Menu dynamic localization verified.");

        // 6. Stat preview rows re-translate on language switch (they used to
        // freeze in the build-time language).
        var statsScene = AssetLoader.Load<PackedScene>("res://scenes/ui/stat_preview.tscn");
        AssertThat(statsScene).IsNotNull();
        var panel = statsScene!.Instantiate<StatPreviewPanel>();
        Root.AddChild(panel);
        GameManager.SetLanguage("zh_CN");
        panel.Refresh("macrophage");
        var mightNameZh = panel.GetNodeOrNull<Label>("Margin/VBox/StatScroll/GroupsBox/Row_might/Name");
        AssertThat(mightNameZh).IsNotNull();
        AssertThat(mightNameZh!.Text.Contains("伤害")).IsTrue();
        GameManager.SetLanguage("en");
        panel.UpdateLocalizedTexts();
        var mightNameEn = panel.GetNodeOrNull<Label>("Margin/VBox/StatScroll/GroupsBox/Row_might/Name");
        AssertThat(mightNameEn).IsNotNull();
        AssertThat(mightNameEn!.Text.Contains("Might")).IsTrue();
        panel.QueueFree();
        GD.Print("[PASS] Stat preview rows re-translate on language switch.");

        hud.QueueFree();
        GD.Print("--- ALL BILINGUAL I18N TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
