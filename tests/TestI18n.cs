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

        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
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

        // 1. Test Simplified Chinese default
        GameManager.SetLanguage("zh_CN");
        menu.UpdateAllTexts();

        AssertThat(menu.StartBtn?.Text.Contains("开始免疫行动") ?? false).IsTrue();
        AssertThat(menu.ClassHeaderLbl?.Text.Contains("选择你的免疫防御细胞") ?? false).IsTrue();
        AssertThat(menu.AchievementsBtn?.Text.Contains("成就收藏") ?? false).IsTrue();
        AssertThat(menu.GlobalBackBtn?.Text.Contains("返回") ?? false).IsTrue();
        GD.Print("[PASS] Simplified Chinese (zh_CN) text rendering verified.");

        // 2. Test English switching
        GameManager.SetLanguage("en");
        menu.UpdateAllTexts();

        AssertThat(menu.StartBtn?.Text.Contains("Begin Immune Action") ?? false).IsTrue();
        AssertThat(menu.ClassHeaderLbl?.Text.Contains("Select Your Immune Defense Cell") ?? false).IsTrue();
        AssertThat(menu.MapHeaderLbl?.Text.Contains("Select Pathological Stage") ?? false).IsTrue();
        AssertThat(menu.AchievementsBtn?.Text.Contains("Achievement Gallery") ?? false).IsTrue();
        AssertThat(menu.AchievementView?.HeaderLabel?.Text.Contains("Achievement Gallery") ?? false).IsTrue();
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
        AssertThat(menu.StartBtn?.Text.Contains("開始免疫行動") ?? false).IsTrue();
        GD.Print("[PASS] Traditional Chinese (zh_TW) text rendering verified.");

        string[] restCycle = { "ja", "de", "fr", "ru", "es", "en" };
        foreach (string expected in restCycle)
        {
            newLang = GameManager.ToggleLanguage();
            AssertThat(newLang).IsEqual(expected);
            AssertThat(GameManager.CurrentLanguage).IsEqual(expected);
        }
        menu.UpdateAllTexts();
        AssertThat(menu.StartBtn?.Text.Contains("Begin Immune Action") ?? false).IsTrue();
        GD.Print("[PASS] GameManager.toggle_language() 8-locale cycle verified.");

        // 5. Test In-Game HUD Localization
        menu.QueueFree();

        var hudScene = GD.Load<PackedScene>("res://scenes/ui/hud.tscn");
        AssertThat(hudScene).IsNotNull();
        var hud = hudScene!.Instantiate<Hud>();
        Root.AddChild(hud);

        // Test in English
        GameManager.SetLanguage("en");
        hud.UpdateLocalizedTexts();
        AssertThat(hud.TitleLabel?.Text.Contains("Status") ?? false).IsTrue();
        AssertThat(hud.ResumeBtn?.Text.Contains("Resume") ?? false).IsTrue();

        // Test in Chinese
        GameManager.SetLanguage("zh_CN");
        hud.UpdateLocalizedTexts();
        AssertThat(hud.TitleLabel?.Text.Contains("状态") ?? false).IsTrue();
        AssertThat(hud.ResumeBtn?.Text.Contains("继续战斗") ?? false).IsTrue();
        GD.Print("[PASS] In-game HUD & Pause Menu dynamic localization verified.");

        hud.QueueFree();
        GD.Print("--- ALL BILINGUAL I18N TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
