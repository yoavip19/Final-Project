using System;
using System.Collections.Generic;
using SecurioClient.Helpers;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioClient.Tests
{
    // Unit tests for SessionHelper — the in-memory session state manager.
    // SessionHelper is a static class so each test must ensure clean state.
    // Tests cover: session lifecycle, vault cache CRUD, and warnings cache.
    public class SessionHelperTests : IDisposable
    {
        // Restore clean state before and after every test.
        public SessionHelperTests() => SessionHelper.EndSession();
        public void Dispose() => SessionHelper.EndSession();

        // ── IsAuthenticated ──────────────────────────────────────────────────────

        [Fact]
        public void IsAuthenticated_BeforeStart_IsFalse()
        {
            Assert.False(SessionHelper.IsAuthenticated);
        }

        [Fact]
        public void IsAuthenticated_AfterStart_IsTrue()
        {
            SessionHelper.StartSession("some-aes-key==");
            Assert.True(SessionHelper.IsAuthenticated);
        }

        [Fact]
        public void IsAuthenticated_AfterEnd_IsFalse()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.EndSession();
            Assert.False(SessionHelper.IsAuthenticated);
        }

        // ── StartSession ─────────────────────────────────────────────────────────

        [Fact]
        public void StartSession_SetsSessionVaultKey()
        {
            SessionHelper.StartSession("test-vault-key==");
            Assert.Equal("test-vault-key==", SessionHelper.SessionVaultKey);
        }

        [Fact]
        public void StartSession_Overwrites_PreviousKey()
        {
            SessionHelper.StartSession("old-key==");
            SessionHelper.StartSession("new-key==");
            Assert.Equal("new-key==", SessionHelper.SessionVaultKey);
        }

        // ── EndSession ───────────────────────────────────────────────────────────

        [Fact]
        public void EndSession_NullsOutSessionVaultKey()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.EndSession();
            Assert.Null(SessionHelper.SessionVaultKey);
        }

        [Fact]
        public void EndSession_ClearsCachedVault()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 1, AccountName = "site" });
            SessionHelper.EndSession();
            Assert.Empty(SessionHelper.CachedVault);
        }

        [Fact]
        public void EndSession_NullsCachedWarnings()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedWarnings = new WarningsData { LeakedCount = 3 };
            SessionHelper.EndSession();
            Assert.Null(SessionHelper.CachedWarnings);
        }

        [Fact]
        public void EndSession_CalledTwice_NoException()
        {
            SessionHelper.EndSession();
            SessionHelper.EndSession();
        }

        // ── InvalidateWarnings ───────────────────────────────────────────────────

        [Fact]
        public void InvalidateWarnings_NullsCachedWarnings()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedWarnings = new WarningsData { WeakCount = 2 };
            SessionHelper.InvalidateWarnings();
            Assert.Null(SessionHelper.CachedWarnings);
        }

        [Fact]
        public void InvalidateWarnings_DoesNotAffectVaultOrKey()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 1 });
            SessionHelper.CachedWarnings = new WarningsData();
            SessionHelper.InvalidateWarnings();
            Assert.True(SessionHelper.IsAuthenticated);
            Assert.Single(SessionHelper.CachedVault);
        }

        // ── AddVaultItem ─────────────────────────────────────────────────────────

        [Fact]
        public void AddVaultItem_ItemAddedToCachedVault()
        {
            SessionHelper.StartSession("key==");
            var item = new VaultItem { Id = 42, AccountName = "GitHub" };
            SessionHelper.AddVaultItem(item);
            Assert.Contains(item, SessionHelper.CachedVault);
        }

        [Fact]
        public void AddVaultItem_NullItem_DoesNotThrow()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.AddVaultItem(null!);
            Assert.Empty(SessionHelper.CachedVault);
        }

        [Fact]
        public void AddVaultItem_MultipleItems_AllPresent()
        {
            SessionHelper.StartSession("key==");
            var a = new VaultItem { Id = 1 };
            var b = new VaultItem { Id = 2 };
            SessionHelper.AddVaultItem(a);
            SessionHelper.AddVaultItem(b);
            Assert.Equal(2, SessionHelper.CachedVault.Count);
        }

        // ── UpdateVaultItem ──────────────────────────────────────────────────────

        [Fact]
        public void UpdateVaultItem_ExistingItem_UpdatesInPlace()
        {
            SessionHelper.StartSession("key==");
            var original = new VaultItem { Id = 5, AccountName = "old", Sha1Hash = "abc" };
            SessionHelper.CachedVault.Add(original);

            SessionHelper.UpdateVaultItem(new VaultItem
            {
                Id          = 5,
                AccountName = "new",
                Sha1Hash    = "xyz",
                IsLeaked    = true
            });

            Assert.Single(SessionHelper.CachedVault);
            Assert.Equal("new", SessionHelper.CachedVault[0].AccountName);
            Assert.Equal("xyz", SessionHelper.CachedVault[0].Sha1Hash);
            Assert.True(SessionHelper.CachedVault[0].IsLeaked);
        }

        [Fact]
        public void UpdateVaultItem_UnknownId_ListCountUnchanged()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 1, AccountName = "a" });

            SessionHelper.UpdateVaultItem(new VaultItem { Id = 999, AccountName = "ghost" });

            Assert.Single(SessionHelper.CachedVault);
            Assert.Equal("a", SessionHelper.CachedVault[0].AccountName);
        }

        [Fact]
        public void UpdateVaultItem_NullItem_DoesNotThrow()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 1 });
            SessionHelper.UpdateVaultItem(null!);
            Assert.Single(SessionHelper.CachedVault);
        }

        [Fact]
        public void UpdateVaultItem_UpdatesAllEditableFields()
        {
            var ts = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 3 });

            SessionHelper.UpdateVaultItem(new VaultItem
            {
                Id               = 3,
                AccountName      = "site",
                AccountUsername  = "user",
                IV               = "iv==",
                Tag              = "tag==",
                CipherText       = "ct==",
                Notes            = "my note",
                Sha1Hash         = "hash",
                IsLeaked         = true,
                LastUpdate       = ts
            });

            var v = SessionHelper.CachedVault[0];
            Assert.Equal("site",    v.AccountName);
            Assert.Equal("user",    v.AccountUsername);
            Assert.Equal("iv==",    v.IV);
            Assert.Equal("tag==",   v.Tag);
            Assert.Equal("ct==",    v.CipherText);
            Assert.Equal("my note", v.Notes);
            Assert.Equal("hash",    v.Sha1Hash);
            Assert.True(v.IsLeaked);
            Assert.Equal(ts,        v.LastUpdate);
        }

        // ── RemoveVaultItem ──────────────────────────────────────────────────────

        [Fact]
        public void RemoveVaultItem_ExistingItem_RemovesIt()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.Add(new VaultItem { Id = 10 });
            SessionHelper.RemoveVaultItem(10);
            Assert.Empty(SessionHelper.CachedVault);
        }

        [Fact]
        public void RemoveVaultItem_NonExistingId_NoException()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.RemoveVaultItem(999);
            Assert.Empty(SessionHelper.CachedVault);
        }

        [Fact]
        public void RemoveVaultItem_RemovesOnlyMatchingItem()
        {
            SessionHelper.StartSession("key==");
            SessionHelper.CachedVault.AddRange(new[]
            {
                new VaultItem { Id = 1 },
                new VaultItem { Id = 2 },
                new VaultItem { Id = 3 }
            });
            SessionHelper.RemoveVaultItem(2);
            Assert.Equal(2, SessionHelper.CachedVault.Count);
            Assert.DoesNotContain(SessionHelper.CachedVault, v => v.Id == 2);
        }

        // ── CachedWarnings ───────────────────────────────────────────────────────

        [Fact]
        public void CachedWarnings_SetAndGet_ReturnsSameObject()
        {
            var data = new WarningsData { LeakedCount = 1, WeakCount = 2, ReusedCount = 3, OldCount = 4 };
            SessionHelper.StartSession("key==");
            SessionHelper.CachedWarnings = data;
            Assert.Same(data, SessionHelper.CachedWarnings);
        }

        [Fact]
        public void CachedWarnings_InitiallyNull()
        {
            Assert.Null(SessionHelper.CachedWarnings);
        }
    }
}
