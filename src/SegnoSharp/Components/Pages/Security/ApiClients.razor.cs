using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.ViewModels.Security;

namespace Whitestone.SegnoSharp.Components.Pages.Security;

public partial class ApiClients
{
    [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
    [Inject] private PermissionRegistry Registry { get; set; }
    [Inject] private ApiKeyGenerator KeyFactory { get; set; }
    [Inject] private ApiKeyCache KeyCache { get; set; }
    // The grant cache rather than the grant store: this page only evicts, and the store is
    // scoped, which would hold a DbContext for the lifetime of the circuit.
    [Inject] private ApiClientGrantCache GrantCache { get; set; }
    [Inject] private ISystemClock Clock { get; set; }
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private ILogger<ApiClients> Logger { get; set; }

    private List<ApiClientListItem> _clients = [];
    private ApiClientEdit _edit;
    private List<ApiKeyListItem> _keys = [];

    private string _newKeyDescription;
    private DateTime? _newKeyExpiry;

    private string _issuedKey;
    private int _issuedKeyId;
    private bool _issuedKeyAcknowledged;
    private bool _issuedKeyFocused;
    private ElementReference _issuedKeyInput;

    private bool _dirty;
    private bool _busy;
    private string _message;
    private string _messageClass = "alert-info";

    protected override async Task OnInitializedAsync() => await LoadListAsync();

    private async Task LoadListAsync()
    {
        DateTime now = Clock.UtcNow;

        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        _clients = await db.SecurityApiClients
            .AsNoTracking()
            .OrderBy(client => client.Name)
            .Select(client => new ApiClientListItem
            {
                Id = client.Id,
                Name = client.Name,
                Description = client.Description,
                Enabled = client.Enabled,
                Created = client.Created,
                PermissionCount = client.Permissions.Count,
                ActiveKeyCount = client.Keys.Count(key =>
                    key.Revoked == null && (key.Expires == null || key.Expires > now))
            })
            .ToListAsync();
    }

    private void StartCreate()
    {
        _edit = new ApiClientEdit();
        _keys = [];
        _newKeyExpiry = Clock.UtcNow.AddYears(1).Date;
        _dirty = true;
        ClearMessage();
    }

    private async Task SelectAsync(int clientId)
    {
        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        SecurityApiClient client = await db.SecurityApiClients
            .AsNoTracking()
            .Include(c => c.Permissions)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client is null)
        {
            await LoadListAsync();
            return;
        }

        _edit = new ApiClientEdit
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            Enabled = client.Enabled,
            Permissions = client.Permissions.Select(p => p.Permission).ToHashSet(StringComparer.Ordinal),
            OriginalPermissions = client.Permissions.Select(p => p.Permission).ToHashSet(StringComparer.Ordinal)
        };

        _newKeyDescription = null;
        _newKeyExpiry = Clock.UtcNow.AddYears(1).Date;
        _dirty = false;

        DismissIssuedKey();

        await LoadKeysAsync(clientId);
        ClearMessage();
    }

    private async Task LoadKeysAsync(int clientId)
    {
        DateTime now = Clock.UtcNow;

        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        List<SecurityApiKey> keys = await db.SecurityApiKeys
            .AsNoTracking()
            .Where(key => key.SecurityApiClientId == clientId)
            .OrderByDescending(key => key.Created)
            .ToListAsync();

        _keys = keys
            .Select(key =>
            {
                bool revoked = key.Revoked is not null;
                bool expired = key.Expires is not null && key.Expires <= now;

                return new ApiKeyListItem
                {
                    Id = key.Id,
                    Prefix = key.Prefix,
                    Description = key.Description,
                    Created = key.Created,
                    Expires = key.Expires,
                    LastUsed = key.LastUsed,
                    IsActive = !revoked && !expired,
                    ExpiresSoon = !revoked && !expired
                                  && key.Expires is not null && key.Expires <= now.AddDays(30),
                    Status = revoked ? "Revoked" : expired ? "Expired" : "Active"
                };
            })
            .ToList();
    }

    private void MarkDirty() => _dirty = true;

    private async Task SaveAsync()
    {
        if (_edit is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_edit.Name))
        {
            ShowMessage("Give the client a name.", "alert-error");
            return;
        }

        // Orphaned grants may be saved back, but nothing unknown or user-only may be newly added.
        foreach (string added in _edit.Permissions.Except(_edit.OriginalPermissions, StringComparer.Ordinal))
        {
            RegisteredPermission known = Registry.Find(added);

            if (known is null)
            {
                ShowMessage($"'{added}' is not a permission any loaded plugin declares.", "alert-error");
                return;
            }

            if (!known.Permission.AllowForApiClients)
            {
                ShowMessage($"'{known.Permission.DisplayName}' cannot be granted to an API client.", "alert-error");
                return;
            }
        }

        _busy = true;

        try
        {
            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

            SecurityApiClient client;
            var enabledChanged = false;

            if (_edit.Id == 0)
            {
                client = new SecurityApiClient
                {
                    Name = _edit.Name.Trim(),
                    Description = _edit.Description,
                    Enabled = _edit.Enabled,
                    Created = Clock.UtcNow
                };

                db.SecurityApiClients.Add(client);
            }
            else
            {
                client = await db.SecurityApiClients
                    .Include(c => c.Permissions)
                    .FirstAsync(c => c.Id == _edit.Id);

                enabledChanged = client.Enabled != _edit.Enabled;

                client.Name = _edit.Name.Trim();
                client.Description = _edit.Description;
                client.Enabled = _edit.Enabled;

                foreach (SecurityApiClientPermission removed in client.Permissions
                             .Where(permission => !_edit.Permissions.Contains(permission.Permission))
                             .ToList())
                {
                    client.Permissions.Remove(removed);
                    db.Remove(removed);
                }
            }

            HashSet<string> current = client.Permissions
                .Select(permission => permission.Permission)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string added in _edit.Permissions.Where(permission => !current.Contains(permission)))
            {
                client.Permissions.Add(new SecurityApiClientPermission { Permission = added });
            }

            await db.SaveChangesAsync();

            GrantCache.Evict(client.Id);

            // Enabled state is denormalised into the cached key record, so disabling
            // has to invalidate the key cache as well.
            if (enabledChanged)
            {
                await EvictKeyCacheAsync(db, client.Id);
            }

            Logger.LogInformation("API client {ClientId} '{DisplayName}' saved by {Subject}.",
                client.Id,
                client.Name,
                await CurrentSubjectAsync());

            int savedId = client.Id;

            await LoadListAsync();
            await SelectAsync(savedId);

            ShowMessage("Saved.", "alert-success");
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to save API client {ClientId}.", _edit.Id);
            ShowMessage("The client could not be saved.", "alert-error");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (_edit is null || _edit.Id == 0)
        {
            return;
        }

        int activeKeys = _keys.Count(key => key.IsActive);

        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Delete the client '{_edit.Name}'? Its {activeKeys} active " +
            $"{(activeKeys == 1 ? "key" : "keys")} will stop working immediately.");

        if (!confirmed)
        {
            return;
        }

        _busy = true;

        try
        {
            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

            SecurityApiClient client = await db.SecurityApiClients
                .Include(c => c.Keys)
                .FirstOrDefaultAsync(c => c.Id == _edit.Id);

            if (client is null)
            {
                await LoadListAsync();
                return;
            }

            List<string> prefixes = client.Keys.Select(key => key.Prefix).ToList();
            string name = client.Name;
            int clientId = client.Id;

            db.SecurityApiClients.Remove(client);
            await db.SaveChangesAsync();

            foreach (string prefix in prefixes)
            {
                KeyCache.Evict(prefix);
            }

            GrantCache.Evict(clientId);

            Logger.LogWarning("API client {ClientId} '{DisplayName}' deleted by {Subject}.",
                clientId,
                name,
                await CurrentSubjectAsync());

            Cancel();
            await LoadListAsync();

            ShowMessage("Client deleted. Its keys no longer work.", "alert-success");
        }
        finally
        {
            _busy = false;
        }
    }

    private void Cancel()
    {
        _edit = null;
        _keys = [];
        _newKeyDescription = null;
        _newKeyExpiry = null;
        _dirty = false;

        DismissIssuedKey();
        ClearMessage();
    }

    private async Task CreateKeyAsync()
    {
        if (_edit is null || _edit.Id == 0)
        {
            return;
        }

        DateTime now = Clock.UtcNow;
        DateTime expires = ResolveExpiry(now);

        if (expires <= now)
        {
            ShowMessage("Choose an expiry date in the future.", "alert-error");
            return;
        }

        _busy = true;

        try
        {
            ApiKeyCreationResult created = KeyFactory.Create();

            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

            db.SecurityApiKeys.Add(new SecurityApiKey
            {
                SecurityApiClientId = _edit.Id,
                Prefix = created.Prefix,
                Hash = created.Hash,
                Description = _newKeyDescription,
                Created = now,
                Expires = expires
            });

            await db.SaveChangesAsync();

            // A negative cache entry could exist for a freshly generated prefix.
            KeyCache.Evict(created.Prefix);

            Logger.LogInformation("API key {Prefix} issued for client {ClientId} by {Subject}, expires {Expires}.",
                created.Prefix,
                _edit.Id,
                await CurrentSubjectAsync(),
                expires);

            ShowIssuedKey(created.PlainText);
            _newKeyDescription = null;

            await LoadKeysAsync(_edit.Id);
            await LoadListAsync();

            // The row is only known after the reload; the new key is the newest for this client.
            _issuedKeyId = _keys.Count > 0 ? _keys[0].Id : 0;

            ClearMessage();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RevokeKeyAsync(int keyId)
    {
        if (_edit is null)
        {
            return;
        }

        ApiKeyListItem listItem = _keys.FirstOrDefault(key => key.Id == keyId);

        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Revoke key {listItem?.Prefix}? Anything using it stops working immediately, " +
            "and the key cannot be restored.");

        if (!confirmed)
        {
            return;
        }

        _busy = true;

        try
        {
            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

            SecurityApiKey key = await db.SecurityApiKeys.FirstOrDefaultAsync(k => k.Id == keyId);

            if (key is null || key.Revoked is not null)
            {
                await LoadKeysAsync(_edit.Id);
                return;
            }

            key.Revoked = Clock.UtcNow;
            await db.SaveChangesAsync();

            KeyCache.Evict(key.Prefix);

            Logger.LogWarning("API key {Prefix} (client {ClientId}) revoked by {Subject}.",
                key.Prefix,
                key.SecurityApiClientId,
                await CurrentSubjectAsync());

            if (_issuedKeyId == keyId)
            {
                DismissIssuedKey();
            }

            await LoadKeysAsync(_edit.Id);
            await LoadListAsync();

            ShowMessage($"Key {key.Prefix} revoked.", "alert-success");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ReplaceKeyAsync(int keyId)
    {
        if (_edit is null)
        {
            return;
        }

        ApiKeyListItem listItem = _keys.FirstOrDefault(key => key.Id == keyId);

        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Replace key {listItem?.Prefix}? A new key is issued and the current one is revoked " +
            "immediately, so anything still using it stops working.");

        if (!confirmed)
        {
            return;
        }

        _busy = true;

        try
        {
            DateTime now = Clock.UtcNow;

            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

            SecurityApiKey existing = await db.SecurityApiKeys.FirstOrDefaultAsync(k => k.Id == keyId);

            if (existing is null)
            {
                await LoadKeysAsync(_edit.Id);
                return;
            }

            ApiKeyCreationResult created = KeyFactory.Create();

            db.SecurityApiKeys.Add(new SecurityApiKey
            {
                SecurityApiClientId = existing.SecurityApiClientId,
                Prefix = created.Prefix,
                Hash = created.Hash,
                Description = existing.Description,
                Created = now,
                // Preserve the original expiry rather than silently extending it.
                Expires = existing.Expires is { } expiry && expiry > now ? expiry : now.AddYears(1)
            });

            existing.Revoked = now;

            await db.SaveChangesAsync();

            KeyCache.Evict(existing.Prefix);
            KeyCache.Evict(created.Prefix);

            Logger.LogWarning("API key {OldPrefix} replaced by {NewPrefix} for client {ClientId} by {Subject}.",
                existing.Prefix,
                created.Prefix,
                existing.SecurityApiClientId,
                await CurrentSubjectAsync());

            ShowIssuedKey(created.PlainText);

            await LoadKeysAsync(_edit.Id);
            await LoadListAsync();

            _issuedKeyId = _keys.Count > 0 ? _keys[0].Id : 0;

            ClearMessage();
        }
        finally
        {
            _busy = false;
        }
    }

    private DateTime ResolveExpiry(DateTime now)
    {
        if (_newKeyExpiry is not { } date)
        {
            return now.AddYears(1);
        }

        // The date input carries no time component; expire at the end of the chosen UTC day.
        return DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }

    private async Task EvictKeyCacheAsync(SegnoSharpDbContext db, int clientId)
    {
        List<string> prefixes = await db.SecurityApiKeys
            .Where(key => key.SecurityApiClientId == clientId)
            .Select(key => key.Prefix)
            .ToListAsync();

        foreach (string prefix in prefixes)
        {
            KeyCache.Evict(prefix);
        }
    }

    private void ShowIssuedKey(string plainText)
    {
        _issuedKey = plainText;
        _issuedKeyId = 0;
        _issuedKeyAcknowledged = false;
        _issuedKeyFocused = false;
    }

    private void DismissIssuedKey()
    {
        _issuedKey = null;
        _issuedKeyId = 0;
        _issuedKeyAcknowledged = false;
        _issuedKeyFocused = false;
    }

    /// <summary>
    /// Focusing the field brings the revealed key into view without a JS scroll helper,
    /// and leaves the value ready to select.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_issuedKey is null || _issuedKeyFocused || _issuedKeyId == 0)
        {
            return;
        }

        _issuedKeyFocused = true;
        await _issuedKeyInput.FocusAsync();
    }

    private async Task CopyIssuedKeyAsync()
    {
        if (_issuedKey is null)
        {
            return;
        }

        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _issuedKey);
            _issuedKeyAcknowledged = true;
        }
        catch (JSException)
        {
            // The clipboard API needs a secure context; the value stays selectable in the field.
            ShowMessage("Copy the key from the field manually. The clipboard is unavailable here.", "alert-warning");
        }
    }

    /// <summary>
    /// Stored values are UTC but may come back from the database with an unspecified kind,
    /// so the kind is set before converting for display.
    /// </summary>
    private static string Format(DateTime? value) =>
        value is null
            ? "Never"
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToLocalTime().ToString("g");

    private async Task<string> CurrentSubjectAsync()
    {
        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        return state.User.FindFirst("sub")?.Value
               ?? state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? "unknown";
    }

    /// <summary>
    /// The marker is the same on every key, so it carries no information in a list.
    /// The full prefix stays available via the title attribute for support tickets.
    /// </summary>
    private static string ShortPrefix(string prefix) =>
        prefix.StartsWith(ApiKeyFormat.PrefixMarker, StringComparison.Ordinal)
            ? prefix[ApiKeyFormat.PrefixMarker.Length..]
            : prefix;

    private void ShowMessage(string message, string cssClass)
    {
        _message = message;
        _messageClass = cssClass;
    }

    private void ClearMessage() => _message = null;
}
