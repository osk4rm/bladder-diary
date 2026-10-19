using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwarmApp.Api.Contracts;
using SwarmApp.Api.Data;

namespace SwarmApp.Api.Tests;

public sealed class DiaryTenancyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private sealed record CsrfResponse(string Token);

    /// <summary>
    /// Registers a fresh account, which also signs it in. The antiforgery token rotates on
    /// sign-in, so the one used for registration cannot be reused afterwards.
    /// </summary>
    private async Task<HttpClient> RegisterAsync()
    {
        var client = factory.CreateDefaultClient(new CookieAppender(new CookieContainer()));
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"diary-{Guid.NewGuid():N}@example.test",
            password = "Sup3rSecret!",
            displayName = "Diary Tester",
        });
        register.EnsureSuccessStatusCode();

        var fresh = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", fresh!.Token);
        return client;
    }

    [Fact]
    public async Task Two_households_cannot_see_or_write_each_others_data()
    {
        var first = await RegisterAsync();
        var second = await RegisterAsync();

        var childResponse = await first.PostAsJsonAsync("/api/diary/children", new
        {
            name = "Ada",
            birthDate = "2024-01-15",
        });
        Assert.Equal(HttpStatusCode.Created, childResponse.StatusCode);
        var child = await childResponse.Content.ReadFromJsonAsync<ChildResponse>();
        Assert.NotNull(child);

        var entryResponse = await first.PostAsJsonAsync("/api/diary/entries", new
        {
            childId = child!.Id,
            kind = "Water",
            occurredAt = "2026-10-19T08:30:00Z",
            notes = "100 ml",
        });
        Assert.Equal(HttpStatusCode.Created, entryResponse.StatusCode);
        var entry = await entryResponse.Content.ReadFromJsonAsync<EntryResponse>();
        Assert.NotNull(entry);
        Assert.Equal(child.Id, entry!.ChildId);

        var firstChildren = await first.GetFromJsonAsync<List<ChildResponse>>("/api/diary/children");
        Assert.NotNull(firstChildren);
        Assert.Single(firstChildren!);

        var firstEntries = await first.GetFromJsonAsync<List<EntryResponse>>("/api/diary/entries");
        Assert.NotNull(firstEntries);
        Assert.Single(firstEntries!);

        // The second household sees neither row, even when it knows the child id.
        var secondChildren = await second.GetFromJsonAsync<List<ChildResponse>>("/api/diary/children");
        Assert.NotNull(secondChildren);
        Assert.Empty(secondChildren!);

        var secondEntries = await second.GetFromJsonAsync<List<EntryResponse>>(
            $"/api/diary/entries?childId={child.Id}");
        Assert.NotNull(secondEntries);
        Assert.Empty(secondEntries!);

        // Writing with a foreign child id is a 404, the same answer as an unknown id:
        // it must not confirm that the row exists.
        var foreignWrite = await second.PostAsJsonAsync("/api/diary/entries", new
        {
            childId = child.Id,
            kind = "Water",
        });
        Assert.Equal(HttpStatusCode.NotFound, foreignWrite.StatusCode);
    }

    [Fact]
    public async Task A_user_without_a_household_gets_no_household_not_a_crash()
    {
        var client = await RegisterAsync();
        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me");
        Assert.NotNull(me);

        // Simulate an account created before households existed: authenticated, no tenant.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var household = await db.Households.SingleAsync(h => h.OwnerUserId == me!.Id);
            db.Households.Remove(household);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/diary/children");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("no_household", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
