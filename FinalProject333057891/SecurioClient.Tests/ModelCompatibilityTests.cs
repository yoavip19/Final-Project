using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SecurioModels;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioClient.Tests
{
    // Model compatibility tests: verify that the DTOs serialise and deserialise
    // correctly and that the same JSON produced by the client can be consumed by
    // the server (and vice-versa).  Uses Newtonsoft.Json — the serialiser shared
    // by both the client (BaseService) and the server (Azure Functions).
    public class ModelCompatibilityTests
    {
        // ── VaultItem ─────────────────────────────────────────────────────────────

        [Fact]
        public void VaultItem_SerialiseDeserialise_PreservesAllFields()
        {
            var original = new VaultItem
            {
                Id               = 7,
                UserId           = 3,
                AccountName      = "GitHub",
                AccountUsername  = "devuser",
                IV               = "aabbcc==",
                Tag              = "ddeeff==",
                CipherText       = "encrypted==",
                Notes            = "work account",
                Sha1Hash         = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709",
                IsLeaked         = true,
                PasswordChanged  = false,
                LastUpdate       = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc)
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<VaultItem>(json);

            Assert.NotNull(restored);
            Assert.Equal(original.Id,              restored!.Id);
            Assert.Equal(original.UserId,          restored.UserId);
            Assert.Equal(original.AccountName,     restored.AccountName);
            Assert.Equal(original.AccountUsername, restored.AccountUsername);
            Assert.Equal(original.IV,              restored.IV);
            Assert.Equal(original.Tag,             restored.Tag);
            Assert.Equal(original.CipherText,      restored.CipherText);
            Assert.Equal(original.Notes,           restored.Notes);
            Assert.Equal(original.Sha1Hash,        restored.Sha1Hash);
            Assert.Equal(original.IsLeaked,        restored.IsLeaked);
            Assert.Equal(original.LastUpdate,      restored.LastUpdate);
        }

        [Fact]
        public void VaultItem_JsonNaming_UsesCorrectPropertyNames()
        {
            var item = new VaultItem { AccountName = "test", CipherText = "ct==" };
            var json = JsonConvert.SerializeObject(item);
            Assert.Contains("AccountName", json);
            Assert.Contains("CipherText", json);
        }

        [Fact]
        public void VaultItem_DeserialiseFromAnonymous_ClientRequestCompatible()
        {
            // Simulate a JSON body like the one sent by the client to AddVaultItem.
            var json = JsonConvert.SerializeObject(new
            {
                UserId          = 1,
                AccountName     = "MyApp",
                AccountUsername = "user@test.com",
                IV              = "iv==",
                Tag             = "tag==",
                CipherText      = "ct==",
                Sha1Hash        = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709"
            });

            var item = JsonConvert.DeserializeObject<VaultItem>(json);
            Assert.NotNull(item);
            Assert.Equal("MyApp", item!.AccountName);
            Assert.Equal(1, item.UserId);
        }

        // ── User (registration / login payload) ──────────────────────────────────

        [Fact]
        public void User_SerialiseDeserialise_PreservesAllFields()
        {
            var original = new User
            {
                Id                 = 5,
                Username           = "alice",
                Email              = "alice@example.com",
                MasterPasswordKey  = "hashed-key==",
                AuthSalt           = "auth-salt==",
                EncryptionSalt     = "enc-salt==",
                PasswordSha1Hash   = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709",
                LastPasswordUpdate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var json     = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<User>(json);

            Assert.NotNull(restored);
            Assert.Equal(original.Id,                 restored!.Id);
            Assert.Equal(original.Username,           restored.Username);
            Assert.Equal(original.Email,              restored.Email);
            Assert.Equal(original.MasterPasswordKey,  restored.MasterPasswordKey);
            Assert.Equal(original.AuthSalt,           restored.AuthSalt);
            Assert.Equal(original.EncryptionSalt,     restored.EncryptionSalt);
            Assert.Equal(original.PasswordSha1Hash,   restored.PasswordSha1Hash);
            Assert.Equal(original.LastPasswordUpdate, restored.LastPasswordUpdate);
        }

        // ── UpdateAccountRequest ──────────────────────────────────────────────────

        [Fact]
        public void UpdateAccountRequest_NoPasswordChange_SerialiseDeserialise()
        {
            var request = new UpdateAccountRequest
            {
                Username        = "newname",
                Email           = "new@example.com",
                PasswordChanged = false
            };

            var json     = JsonConvert.SerializeObject(request);
            var restored = JsonConvert.DeserializeObject<UpdateAccountRequest>(json);

            Assert.NotNull(restored);
            Assert.Equal("newname",         restored!.Username);
            Assert.Equal("new@example.com", restored.Email);
            Assert.False(restored.PasswordChanged);
            Assert.Null(restored.MasterPasswordKey);
        }

        [Fact]
        public void UpdateAccountRequest_WithPasswordChange_PreservesVaultItems()
        {
            var request = new UpdateAccountRequest
            {
                Username          = "alice",
                Email             = "alice@example.com",
                PasswordChanged   = true,
                MasterPasswordKey = "newkey==",
                AuthSalt          = "newsalt==",
                EncryptionSalt    = "newencsalt==",
                VaultItems        = new List<VaultItem>
                {
                    new VaultItem { Id = 1, CipherText = "ct1==" },
                    new VaultItem { Id = 2, CipherText = "ct2==" }
                }
            };

            var json     = JsonConvert.SerializeObject(request);
            var restored = JsonConvert.DeserializeObject<UpdateAccountRequest>(json);

            Assert.NotNull(restored);
            Assert.True(restored!.PasswordChanged);
            Assert.Equal(2, restored.VaultItems!.Count);
            Assert.Equal("ct1==", restored.VaultItems[0].CipherText);
        }

        // ── AuthData ──────────────────────────────────────────────────────────────

        [Fact]
        public void AuthData_SerialiseDeserialise_PreservesAllFields()
        {
            var original = new AuthData
            {
                UserId   = 42,
                Username = "alice",
                Token    = "eyJhbGciOiJIUzI1NiJ9.payload.signature"
            };

            var json     = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<AuthData>(json);

            Assert.NotNull(restored);
            Assert.Equal(42,       restored!.UserId);
            Assert.Equal("alice",  restored.Username);
            Assert.Equal(original.Token, restored.Token);
        }

        // ── SaltData ─────────────────────────────────────────────────────────────

        [Fact]
        public void SaltData_SerialiseDeserialise_PreservesBothSalts()
        {
            var original = new SaltData
            {
                AuthSalt       = "auth-salt==",
                EncryptionSalt = "enc-salt=="
            };

            var json     = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<SaltData>(json);

            Assert.NotNull(restored);
            Assert.Equal("auth-salt==", restored!.AuthSalt);
            Assert.Equal("enc-salt==",  restored.EncryptionSalt);
        }

        // ── ServerResponse<T> wrapper ────────────────────────────────────────────

        [Fact]
        public void ServerResponse_Success_SerialiseDeserialise()
        {
            var response = new ServerResponse<string>
            {
                Success = true,
                Message = "All good",
                Data    = "result"
            };

            var json     = JsonConvert.SerializeObject(response);
            var restored = JsonConvert.DeserializeObject<ServerResponse<string>>(json);

            Assert.NotNull(restored);
            Assert.True(restored!.Success);
            Assert.Equal("All good", restored.Message);
            Assert.Equal("result",   restored.Data);
        }

        [Fact]
        public void ServerResponse_Failure_SerialiseDeserialise()
        {
            var response = new ServerResponse<object>
            {
                Success = false,
                Message = "Something went wrong",
                Data    = null
            };

            var json     = JsonConvert.SerializeObject(response);
            var restored = JsonConvert.DeserializeObject<ServerResponse<object>>(json);

            Assert.NotNull(restored);
            Assert.False(restored!.Success);
            Assert.Equal("Something went wrong", restored.Message);
        }

        [Fact]
        public void ServerResponse_OfVaultItemList_SerialiseDeserialise()
        {
            var response = new ServerResponse<List<VaultItem>>
            {
                Success = true,
                Data    = new List<VaultItem>
                {
                    new VaultItem { Id = 1, AccountName = "A" },
                    new VaultItem { Id = 2, AccountName = "B" }
                }
            };

            var json     = JsonConvert.SerializeObject(response);
            var restored = JsonConvert.DeserializeObject<ServerResponse<List<VaultItem>>>(json);

            Assert.NotNull(restored);
            Assert.True(restored!.Success);
            Assert.Equal(2, restored.Data!.Count);
        }

        // ── PasswordCheckResult ───────────────────────────────────────────────────

        [Fact]
        public void PasswordCheckResult_SerialiseDeserialise_PreservesAllCounters()
        {
            var original = new PasswordCheckResult
            {
                BreachedCount    = 3,
                OldCount         = 2,
                MasterPasswordOld = true
            };

            var json     = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<PasswordCheckResult>(json);

            Assert.NotNull(restored);
            Assert.Equal(3,    restored!.BreachedCount);
            Assert.Equal(2,    restored.OldCount);
            Assert.True(restored.MasterPasswordOld);
        }

        // ── MasterPasswordHistory ────────────────────────────────────────────────

        [Fact]
        public void MasterPasswordHistory_SerialiseDeserialise_PreservesAllFields()
        {
            var original = new MasterPasswordHistory
            {
                Id          = 1,
                UserId      = 5,
                PasswordKey = "key==",
                AuthSalt    = "salt==",
                CreatedAt   = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var json     = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<MasterPasswordHistory>(json);

            Assert.NotNull(restored);
            Assert.Equal(original.Id,          restored!.Id);
            Assert.Equal(original.PasswordKey, restored.PasswordKey);
            Assert.Equal(original.AuthSalt,    restored.AuthSalt);
            Assert.Equal(original.CreatedAt,   restored.CreatedAt);
        }
    }
}
