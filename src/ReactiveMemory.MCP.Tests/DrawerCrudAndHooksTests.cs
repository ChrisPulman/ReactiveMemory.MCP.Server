using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class DrawerCrudAndHooksTests
{
    [Test]
    public async Task Drawer_Crud_And_Hook_Settings_Work_End_To_End()
    {
        var harness = await TestHarness.CreateAsync();
        var added = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector_alpha", "vault_one", "Alpha content", "alpha.md", "tester");

        var fetched = await ReactiveMemoryTools.GetDrawerAsync(harness.Service, added.DrawerId);
        var listed = await ReactiveMemoryTools.ListDrawersAsync(harness.Service, "sector_alpha", "vault_one", 10, 0);
        var updated = await ReactiveMemoryTools.UpdateDrawerAsync(harness.Service, added.DrawerId, "Updated alpha content", "sector_beta", "vault_two");
        var settings = await ReactiveMemoryTools.HookSettingsAsync(harness.Service, silentSave: false, desktopToast: true);
        var reconnect = await ReactiveMemoryTools.ReconnectAsync(harness.Service);
        var deleted = await ReactiveMemoryTools.DeleteDrawerAsync(harness.Service, added.DrawerId);

        await Assert.That(fetched.Found).IsTrue();
        await Assert.That(listed.Total).IsEqualTo(1);
        await Assert.That(updated.Success).IsTrue();
        await Assert.That(updated.Drawer!.Sector).IsEqualTo("sector_beta");
        await Assert.That(updated.Drawer!.Vault).IsEqualTo("vault_two");
        await Assert.That(settings.Updated).IsTrue();
        await Assert.That(settings.SilentSave).IsFalse();
        await Assert.That(settings.DesktopToast).IsTrue();
        await Assert.That(reconnect.Success).IsTrue();
        await Assert.That(deleted.Success).IsTrue();
    }
}
