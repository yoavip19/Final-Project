-- ============================================================
-- Securio demo seed  –  generated 2026-05-02 10:17:43 UTC
-- ============================================================

-- Clear existing seed data (safe to re-run)
DELETE FROM MasterPasswordHistory;
DELETE FROM VaultItems;
DELETE FROM Users;

-- ── User 1: alice_smith ───────────────────────────────────────
DECLARE @uid1 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('alice_smith', 'alice@secure.com', 'V8gN2G8v9o0OG+C6NTFWpaovOirAyL1mOu1eVNtuMb8=', 'GgOMredb592VLynKfdwHLg5k4ZqaXIzdkda3oytx8IU=', 'dCOGGh8oOWlLrjwsXsCbCrql2RV0TjoRx6dyJ/R4Bzg=', GETDATE(), GETDATE(), DATEADD(day, -190, GETDATE()));
SET @uid1 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid1, 'Gmail', 'alice@gmail.com', 'hXLCg889blSU5Tf8', 'ZUCvZS6pkbeXeY/BLpokZg==', 's37aC7NOKW0DKDky6A==', 'Work email', 'EA379AE8AABBC1C2DDEE77A601C7150FC885B983', 0, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid1, 'Facebook', 'alice.smith', 'JTDbDL0mfSXSDEC5', '4hFFvnylQJ3KBz6KuihLAw==', '8jklT/VnFh/9Sj6Wfps=', NULL, '9773EA652EAD9DF0E66987A15DFB5B65373D4643', 0, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid1, 'Twitter', 'alice_s', 'iq4+3mxMKsMTf6NX', 'nLhpSB0RH7BMoHH/1uJUUQ==', 'K9VeUJho+yuSSln4vg==', NULL, '5FBF8909A5D55670427BD18740D26667E930AEE0', 0, DATEADD(day, -100, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid1, 'Amazon', 'alice@secure.com', 'Dc30gTIvO+RU36YQ', 'axioYM0MdbklGbMfKd6yCA==', 'tz1IYnMOZJfi2g040Q==', 'Prime account', 'EDFF2A23BCDCF54A336364E872B2D916B2DE586E', 0, DATEADD(day, -110, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid1, 'Netflix', 'alice@secure.com', 'wv1D1+rGqParFG5d', 'iCeAP4ym3NGHJcFo8+U0wA==', 'QFkfxbacKnKXTyBk82o=', NULL, '30FC9C7E6461915035845C28AA607FBBA1CC6927', 0, GETDATE());


-- ── User 2: bob_jones ─────────────────────────────────────────
DECLARE @uid2 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('bob_jones', 'bob@secure.com', '1G3Ajf6tOP6ixadjGzLc4kcnnmoiulGorhijh8IoX3g=', 'flI1BFa5e/eRJUkSjBDhxK+jaPv/li+PBxLpRfEvptY=', 'Gbf7iAMKVMnK/svZJOIH8kVHB2WLsA6fLV9wYinnCZU=', GETDATE(), GETDATE(), DATEADD(day, -200, GETDATE()));
SET @uid2 = SCOPE_IDENTITY();


-- ── User 3: carol_white ───────────────────────────────────────
DECLARE @uid3 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('carol_white', 'carol@secure.com', 'ro3KHjPfPGjYcmUD3mzluXMmCABp2AGWL64hEqYoi9Q=', 'gDALoEkLqBSt/yPjaOSjBh2s3BvD5a4wuW6Y3kR2nj0=', 'P4XOcj3vgANdAEyjy8QkNpBEh8S61Ho0ot1BrgSDUsk=', GETDATE(), DATEADD(day, -120, GETDATE()), DATEADD(day, -210, GETDATE()));
SET @uid3 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid3, 'LinkedIn', 'carol.white', 'FVVN/miGb4f0NWyI', '2q9bVIIwcqWZBiJ5I+QPrA==', 'aV5ejxwjSpe9HIek', NULL, '039DF0F3FF5DF13F790E22CBB47B4A2F3D82D362', 0, DATEADD(day, -95, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid3, 'Dropbox', 'carol@secure.com', 'Am3UPzMEBuWzQTBu', '/R5lBSmcSW8SvW7EP89BAw==', 'ThBByG1ntDJRt+Rx', NULL, 'A6F5E90A3EFCEA614670F743535D948BEF5A5A24', 0, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid3, 'Spotify', 'carol_music', 'sINC0ezuGHmFV+0Y', 'xjdDZDTz3WAaVwuH8wFZ5g==', 'sbfhNvFIodu46Fva', 'Family plan', '0D54A2C83BC28A4BB598754D7FAE0543E5BA1B8E', 0, DATEADD(day, -100, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid3, '1IqLx//4Doep4w6PdJS7QLlW3eYHwNC2h98CVR6kCKI=', 'U/1pFZWoVP7+L4FsKwJU92Thl9l+TXfxSXwFxBdfsFY=', DATEADD(day, -300, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid3, '0OsO3g15d7mKEauyZ4JtZpjGDGYjZChdKe6Ybi2ydLg=', 'o1vkI1NEqktql708SbTOUWe6wrNn/BxP2UxXvdgDxDM=', DATEADD(day, -200, GETDATE()));


-- ── User 4: dave_miller ───────────────────────────────────────
DECLARE @uid4 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('dave_miller', 'dave@secure.com', 'hxM7IWwULyR6IPl7txPbF76tCY8wk4W2XZA9ZR/rQFc=', '0kP49zcg4c/umNreeQ3V+/7aNOo+2QIxvsctdlJmoKM=', 'nQ578n2QFB0qIcCDCnLobH+NXPKOevD7cXfuEoSTCk8=', GETDATE(), GETDATE(), DATEADD(day, -220, GETDATE()));
SET @uid4 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid4, 'GitHub', 'davemiller', 'mLaotLLFYpFLwXUB', 'hAALTthru/gGwYMyM6bKzw==', 'kBoLTLE5hUk=', NULL, '5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8', 1, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid4, 'Reddit', 'dave_m', 'yalQzaX//aUUgwC7', 'Exb5UGERvop8+x7lbLTn4Q==', 'QcaY0OH4', NULL, '7C4A8D09CA3762AF61E59520943DC26494F8941B', 1, DATEADD(day, -25, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid4, 'Steam', 'dave_games', 'e+0bqVSBPcO1a0RF', 'pZ1MEui5s8fQJXsULF0GSA==', '60aPMUpy', NULL, 'B1B3773A05C0ED0176787A4F1574FF0075F7521E', 1, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid4, 'PayPal', 'dave@secure.com', 'l9sB7ydac0LXLKQW', 'p8+9kkBsdsL/Qj8cgkDocQ==', 'PMmfAwF17g==', NULL, 'B7A875FC1EA228B9061041B7CEC4BD3C52AB3CE3', 1, DATEADD(day, -30, GETDATE()));


-- ── User 5: eve_king ──────────────────────────────────────────
DECLARE @uid5 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('eve_king', 'eve@secure.com', 'cMgISPnNTxmDjpYald+UI4YtgpmMZevm7e9j38YYb/E=', 'KEuBEGpb43uAc0L7C0Y7n+MFX+2vRrVIr0Of4LWZYzQ=', 'EXn4fYNomUP612uovE2uqwViwM8QADgfYEcYashgtqU=', GETDATE(), GETDATE(), DATEADD(day, -230, GETDATE()));
SET @uid5 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'GitHub', 'eve_codes', '+cACophlLOS48YKk', 'tvP+zHh0S4kKQ6Q4KRfo0g==', 'UXvr1K6GDKWjMHYj', NULL, '08A5BDD09EC7044E4FBC16F90B0B79AA953BA174', 0, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'GitLab', 'eve_codes', 'HU6trR8q1mp+76XW', 'Y7klUvaTeX4hv23PJ3IH5w==', 'uMe36gqCvzSJYmHC', NULL, '08A5BDD09EC7044E4FBC16F90B0B79AA953BA174', 0, DATEADD(day, -25, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'Bitbucket', 'eve_codes', 'yfDKt4Fk+KEJYqZv', 'BOQOw1M/UEG0iVaIbFMR/A==', 'zzVQimJYNKrmLbhR', NULL, '08A5BDD09EC7044E4FBC16F90B0B79AA953BA174', 0, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'Jira', 'eve@secure.com', '5/2cw0yJIs4OyADb', 'fuywIOc9tdekdJxPEnVImw==', 'MtZZwtp6MnN5x0IY', NULL, '08A5BDD09EC7044E4FBC16F90B0B79AA953BA174', 0, DATEADD(day, -110, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'Slack', 'eve_king', 'zZhuvh29LAinXOkU', 'S1Z8HX1h304y9E++Ok/XGg==', 'D4vlmd0QE6C7Jnjn7g==', NULL, '497E006E2004844BA1BABCD52EE428E6E5480216', 0, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid5, 'Zoom', 'eve@secure.com', 'dfwdieELcUiy8vEP', 'HvGQxw26Hn5pxioKkCOdjQ==', 'ADTjNzA5j6FhlRU1qw==', NULL, '51B34ABF528117A22B290B7397DC51B485738B9F', 0, DATEADD(day, -30, GETDATE()));


-- ── User 6: frank_brown ───────────────────────────────────────
DECLARE @uid6 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('frank_brown', 'frank@secure.com', 'FyedmrB6Km43BhQmW4Gah21GfKu5gEXU6/R7YHwg3e0=', '6XbIeQqGjWmBpZb9ijM3ABTLndoraJKxRapf9FEwgRM=', 'YxmXGVZDtEg7igUjMvPYf6k2DjEyMiSP6SK4z3TLNKs=', GETDATE(), GETDATE(), DATEADD(day, -240, GETDATE()));
SET @uid6 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid6, 'Twitter', 'frank_b', 'K6bqzfP41Vsx/hrN', 'XdA7YpWEZ/D9tYAcIRS9Qg==', '9dIsjVIiDDQ=', NULL, 'EE8D8728F435FD550F83852AABAB5234CE1DA528', 1, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid6, 'Instagram', 'frank.brown', 'WVhKPSPwc3Qc8DPh', 'cEOS4sGCdSiYUiPqCwXalg==', 'Lq/frk4bkCnW46fCCA==', NULL, '309F0659CA4F00887389C6864EEA91ABB6B29718', 0, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid6, 'Pinterest', 'frank@secure.com', 'mXBI+VkQwlobAAQx', 'rrsOdca6Q68lTOgK4BC8mw==', '120s7q1K2Tx8ne0r0g==', NULL, 'B0A12BE98ABD6CFC8AA0BA109C9877DD2B2A57DB', 0, DATEADD(day, -115, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid6, 'TikTok', 'frank_tok', 'W7jPrBCd4AehPi/W', '38m39ZeuNrvxEDAKzSgdJA==', 'w/8R/vyk', NULL, 'AF8978B1797B72ACFFF9595A5A2A373EC3D9106D', 1, DATEADD(day, -100, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid6, 'YouTube', 'frank@secure.com', 'TuB2ESxHdvgaw/Cm', '/zTJwgJEAP7vY3kI5JE8Wg==', 'f5tgVwSLAsdGwVN3hGw=', NULL, 'DEEDE4684692471BD8A8D5DCDDA920D6CF36E356', 0, GETDATE());

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid6, 'cKabZ/zKbHtgOsM/PrxX7H04LIYmiSIHn+R+u3RCal4=', 'C0ZaPHLZqgd/1i/sEqnHyK+Y0cTilZTIeOXvP48qeA8=', DATEADD(day, -180, GETDATE()));


-- ── User 7: grace_hall ────────────────────────────────────────
DECLARE @uid7 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('grace_hall', 'grace@secure.com', 'YfGCSYEdpFgR/525ZDgqG0u6BvCfhpUVZwNSJ4CNIc4=', 'JqmHAZjiVMFE3ajXRFWutWWGptKrA9GdXgXv6SDl024=', 'duw8eEb3HhS6w1ijHfT19R84Jdmd2Nk3UnYtqPZkIZ0=', GETDATE(), GETDATE(), DATEADD(day, -250, GETDATE()));
SET @uid7 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'Gmail', 'grace@gmail.com', 'juf0BkIE1PniklZm', '65gT+09adFD8W7OvG4ia5w==', 'h4tYrwdxqrOnl83jc0o=', 'Personal', '480BDDF7CD32D5A729710C62090D15CC8C6B7008', 0, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'GitHub', 'grace_dev', 'YUJ2sgEM57Njd6Tl', 'R8MHkWgbebd0HV5EDRhTPA==', 'oFJANRmldb+ECUDS', NULL, '658F56E99C2E00582154BD7AE1A630FEF9CFBDB2', 0, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'Netflix', 'grace@secure.com', 'q//lIcfLRJLpIl5n', '7JNSsnWBTYjQH4d53iGfDQ==', 'i1knE6sGeIqfvLHmzg==', NULL, 'A3694587A3E7761B432D4BE305CD8303E732E074', 0, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'Amazon', 'grace@secure.com', 'ZiTybE4ZJnh4ZhJc', 'D0ZZXIejfv6S8EmPQOwEPg==', 'ZbgF1LQR5aRTP0N3oQ==', 'Wishlist', 'D1F7697AA2D801B6274336C61CCA6FFC2492F9E8', 0, DATEADD(day, -25, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'LinkedIn', 'grace.hall', 'xikJt6xuoue/MRI/', 'bpfIHuyTpPahuABl/Mf+iA==', '98S1G1QES/sSXE27GA==', NULL, 'A1A155DD85A1ADB4DCA0ADB95DDE38C70095845E', 0, DATEADD(day, -30, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'Slack', 'grace_hall', 'DWvIkcXV18rX9jAf', 'Zq0XMha8st9cORAQPEKiCw==', 'jYlrAuqF4gtuUesr', NULL, 'D947072E403D1B63CE26F353BBB28F3E57AB6F48', 0, DATEADD(day, -35, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid7, 'Dropbox', 'grace@secure.com', 'X+aZX1Vrq4RhybiP', 'a2PUyiG35qxg2YZbwnBd5Q==', '94VlftlctXbXn2LJkg==', NULL, '392AA5ED80F6131F2709E63417954FAF19B478A6', 0, DATEADD(day, -40, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid7, 'qSghYcM9hB8OARMjcix4vhrCBzjQawE4E2tpsxWpnmY=', 'ux9w/V3s06SQNbNZjhyy6Pl7x18iuhfkRZ5Vc4yc0OQ=', DATEADD(day, -270, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid7, 'JhnH4evejLDu5/D8vK4Y9X6P6OtfYyukvp01GfvyC1s=', 'OrEHw8+YHYSS0T171PA9iXckA4rqNGZHefhTSGNnagk=', DATEADD(day, -180, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid7, 'fIZ8qzuloaIzPsa+4K0zfGE5RhZleAeFwB39ZeaTCFU=', 'oNC2Vz7marE4cTgxD65LEBaypHn9CCKxc5OIOzJCKXw=', DATEADD(day, -90, GETDATE()));


-- ── User 8: henry_taylor ──────────────────────────────────────
DECLARE @uid8 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('henry_taylor', 'henry@secure.com', '2AYFuooCY+QNW65GOZWQjH04/68BAZ2PJGnBOlxASIw=', 'abtktrVi0D/e3i81xex+3PGL2zyYHDZItIOMI3FBAE4=', 'HgQqeAeoYVSZbigcBpM6MJpuUiODCAuxjEFZb6noJKk=', GETDATE(), DATEADD(day, -100, GETDATE()), DATEADD(day, -260, GETDATE()));
SET @uid8 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid8, 'eBay', 'henry_t', 'djyOVHf9Fi+6gGe4', '3Dl9v/Yn/hf/U5DgbdLJzA==', 'mNdGoVu7', NULL, 'AB87D24BDC7452E55738DEB5F868E1F16DEA5ACE', 1, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid8, 'PayPal', 'henry@secure.com', '3s0R2u01AswyfOUv', 'qXwE5OGpp9JkIicL2EL6nw==', 'CE2WNKkA', NULL, '4F26AEAFDB2367620A393C973EDDBE8F8B846EBD', 1, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid8, 'Reddit', 'henry_taylor', 'eC8WTdVxi90BRLDO', 'OjOAo01YiqufvNUBKYzlTQ==', 'imdvYo4=', NULL, 'D033E22AE348AEB5660FC2140AEC35850C4DA997', 1, DATEADD(day, -30, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid8, '7W5NNGJjt9omPkm86rp6LxBH2iu4qQx/THRirRFxscU=', 'oYHaH1qsdIzR6TOaNjbOwNJAhKdmeuKqDS6jeCAr9sw=', DATEADD(day, -200, GETDATE()));


-- ── User 9: iris_vance ────────────────────────────────────────
DECLARE @uid9 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('iris_vance', 'iris@secure.com', '2xtbmc6SC1g767NRbVknlfXcAXtKc2n5u9bBjyGvNKs=', 'Sqk4lithvo5tJY/Tdz7i3hvjYo67YwZtKwpHgaib5fI=', 'CMUwohVsQtL5wP/5hpW1OdxrZLbfklkefFVlDCXC6WA=', GETDATE(), GETDATE(), DATEADD(day, -270, GETDATE()));
SET @uid9 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid9, 'Gmail', 'iris@gmail.com', '8RrKLLmktS88+nYQ', 'pAxtCjLFVkLKATKBrcCSow==', '9+31LEymUAtOa39lug==', 'Backup email', 'AE7E8529A396CD9AFE4C07330F1EA9A10E3FD623', 0, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid9, 'OneDrive', 'iris@secure.com', 'E7O1OMNA4drR5ggm', 'DsjrLCodryJO39P4A/SfHA==', 'zKJw6DxMjOLnoiA=', NULL, '53B2465BC54DAB6E5E1A25B4890634122B698F4C', 0, DATEADD(day, -115, GETDATE()));


-- ── User 10: jack_oliver ───────────────────────────────────────
DECLARE @uid10 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('jack_oliver', 'jack@secure.com', 'rluOhIGXsvHq4Gv/6Xe/lE5+qHzgkTqGt6PO0kAjH2w=', 'AwnYmbQido3GUfQp8NwXLVOOIO3Xp4dgVu9KKGF4R+w=', 'HnC2/zh+LCz6yn71cneaey9pZ8/C5Ja44yAsnjQ390k=', GETDATE(), GETDATE(), DATEADD(day, -280, GETDATE()));
SET @uid10 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Facebook', 'jack_oliver', 'q3VsOcfAYH6MU2U5', 'S3cLGKvkmV3Tg1rx1s1O9w==', 'ydlCMrWHAyg=', NULL, '5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8', 1, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Twitter', 'jack_o', 'YIVSCD3jQ+ojcakS', 'AoekwewVbVBjzPHFYGB7KQ==', 'ZD9BwK34GSTV', NULL, 'F7C3BC1D808E04732ADF679965CCC34CA7AE3441', 1, DATEADD(day, -12, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Gmail', 'jack@gmail.com', 'wCQTvHPOVGXQyB6l', 'Q0rorkSiqkpkOBOz0j8cag==', 'Z2Hd6yqkOzKt', NULL, '5CEC175B165E3D5E62C9E13CE848EF6FEAC81BFF', 1, DATEADD(day, -14, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Steam', 'jack_games', '07GGrUFfVN2p5bN6', 'gKPbUJ5Ec4bPD9TjrSpGDg==', 'AGHKiPha', NULL, '6367C48DD193D56EA7B0BAAD25B19455E529F5EE', 1, DATEADD(day, -16, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Reddit', 'jack_o', 'OqJN4u06PAqRXZK2', 'zRdGeysJdo/Hp/rHZ4dMLw==', 'QG33Wg3V2LI=', NULL, 'EE8D8728F435FD550F83852AABAB5234CE1DA528', 1, DATEADD(day, -18, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'TikTok', 'jack_tok', 'rCipaZTm+JBwI5xQ', '4KpgEGBKg/CI654KLjpK0g==', 'w5wUBKfm', NULL, 'AF8978B1797B72ACFFF9595A5A2A373EC3D9106D', 1, DATEADD(day, -20, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid10, 'Snapchat', 'jack_snap', 'm3XYYycRxnW3LI7Y', '9YaGh/3lXbr3BU95mEbe0w==', 'RevozmE+ww==', NULL, 'B7A875FC1EA228B9061041B7CEC4BD3C52AB3CE3', 1, DATEADD(day, -22, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid10, 'jggE9nsAfbz/LN4SRkL51DLTIo44jUrPjGObYdBqldI=', 'Jp4PDgqS+RfUyf8vxY/qIDeoHCdruciE7IzQXCBKjik=', DATEADD(day, -120, GETDATE()));


-- ── User 11: kate_nash ─────────────────────────────────────────
DECLARE @uid11 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('kate_nash', 'kate@secure.com', 'aTwLaQewqB/YuYcRuw4/FIJa4YQoSJAxzU8v9PC9WGU=', 'BdGF0IoW7GNaYFjVgr7a1zvdtw1SrC8I3g6gKTQ+rzw=', 'o0RKG00EAMnp4NT29VEEz9OZAJjgHD/QPYGgpa1xQF8=', GETDATE(), GETDATE(), DATEADD(day, -290, GETDATE()));
SET @uid11 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid11, 'Spotify', 'kate_music', 'xnhpcakZZB3b9GB4', 'Qbc4ri27PcnZlP1CY/zs7A==', 'd42n9PxI', NULL, 'AB87D24BDC7452E55738DEB5F868E1F16DEA5ACE', 1, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid11, 'Instagram', 'kate.nash', 'NuaWhivUnG88qzI2', 'IBQuk9pTSLs6rBf6AXGFHQ==', 'OSUQSW0mH+a8XcGWPQ==', NULL, '2131D05FBFAC5556B4DE0C4920FBAF7C05565E32', 0, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid11, 'Pinterest', 'kate@secure.com', 'd+acrzppQ99ZpZPJ', 'b23SKmnIFkFV1curEpUoVw==', 'rYTKyINjDmFiNidiVg==', NULL, '3130168F4F436B300C07ED9F16A1081F5DAE8E51', 0, DATEADD(day, -115, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid11, 'Twitch', 'kate_streams', 'Hv+vYs4axdKeD2eN', 'e8J4gui4BuwCZ8zwcLZH8A==', 'mLyAckI=', NULL, 'D033E22AE348AEB5660FC2140AEC35850C4DA997', 1, DATEADD(day, -100, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid11, 'mCEZ/1bE3OXhDKAgnmt2pjgzYMzLznkT7NZ//XHOqVw=', '+v9h87lmR4NgtaXkQ/nx8Njg24W9P+/aBsHxD28YvTg=', DATEADD(day, -180, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid11, 'OOcKmC+EpF1zShpABw3UDyVS+1FVgaqRqcWHMbSqnVE=', 'rgs3xyDBn405kBFDsGiIkMmMk5Hdh1jWRZDiqj9kSKw=', DATEADD(day, -90, GETDATE()));


-- ── User 12: liam_parker ───────────────────────────────────────
DECLARE @uid12 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('liam_parker', 'liam@secure.com', '2HRPHlZ/eW8eQzSxd+beZbDMgGomwt13zLjXTwEWb6Y=', 'bSCZruGn/KHOe9zr8txmVtB3nW7cDHHf9PgA8HBPUJw=', 'A3SJ7sjNjHX3E7rjY7arFqLxHZ9I+cJ/7IK8CepOTqk=', GETDATE(), DATEADD(day, -95, GETDATE()), DATEADD(day, -300, GETDATE()));
SET @uid12 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid12, 'Xbox', 'liam_gamer', 'oApagscjkH542uQA', '8uXgDduSY+83Qy4PBqSsRQ==', 'WoYz6LWM3PbIs5b5', 'Gamepass', '4F727E08D25D9B6939FA9DD8567D4626BEA2A056', 0, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid12, 'Discord', 'liam_parker', 'aeGfyrSYZ64ahAWp', 'JOeDZf4yvU4QvIbaA4il3w==', 'ix5DI1nBYPKwUt24', NULL, '29004E91D113A25DE20431DF3BF0A0704484B4E1', 0, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid12, 'Steam', 'liam_gamer', 'JhNU8BuCmytbVDsb', '/WHlxGkKf24b5RaEP3Ysdg==', 'kmgBAlHabrjlSlId6Q==', NULL, '90C141DE1B46B2E8C323D58C6ADDDB817F40CFA6', 0, DATEADD(day, -20, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid12, 'o0/KAjvTql8Gvx/Qs9Seb2OYqjolagvGP5+mxStwygI=', 'LQ/7keF999NIbLsRrKytCGiCnGJDBDl5S1Dwkfzdqtk=', DATEADD(day, -365, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid12, 'Ue47gN+W1/nC7/EdTPrRtga+qdFV+pKNhos024cyapU=', 'vpSP0Rbn4zhuuiVikPMARI9cUtQ30ExZ8qZMbzqS0gU=', DATEADD(day, -270, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid12, '/8qFJ4ZIpmF4gtJa28AIvcyKFcIB/zMZGD9Lt7LxHc4=', 'p48GJHhsMJlSGDbKNd5Zdd3wKW3mbHEJVcAcJvNM+Qg=', DATEADD(day, -180, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid12, 'BBLfa2pndpdXmq+MnngI0D6n6qhT/Ik8ggaktPJj87E=', 'qAqtw1+AFAkey3MtPKlos2J1Z0tx2g8hG7JKnWMCUF4=', DATEADD(day, -95, GETDATE()));


-- ── User 13: mia_ross ──────────────────────────────────────────
DECLARE @uid13 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('mia_ross', 'mia@secure.com', 'q93xDUJUB+e+fW8HlZUDxkPJa0rgEsNvYXNUGlD0WXo=', 'HvEqejvCGXK/QCcG1oy15kob/IyLgGl/3ESveVzcX+w=', 'hbdvSf5cB164l9C4n417UT48JivuTMamoBfPqKnr91g=', GETDATE(), GETDATE(), DATEADD(day, -310, GETDATE()));
SET @uid13 = SCOPE_IDENTITY();


-- ── User 14: noah_cole ─────────────────────────────────────────
DECLARE @uid14 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('noah_cole', 'noah@secure.com', 'fwfyxq6IGwbhNA7PqFTjt+8K6K2Da1K2A4uAOKWxDT4=', 'DU+9MO6XXNNuFBjndzpfHyzvavFl9Y/ly3dl/JdLB6E=', 'mxkTrLVje0XPexTuhacrcyiqqIeWewaV/j3+YVlI3nU=', GETDATE(), GETDATE(), DATEADD(day, -320, GETDATE()));
SET @uid14 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'Gmail', 'noah@gmail.com', 'hHB9vO4Q745DASjn', 'UUHioouL0/l+xuRhlYVD/w==', 'VVXDatjg+ahN1jetLg==', 'Main email', 'F044D5AED0456E923A24D7DA7828D0F38D45114F', 0, DATEADD(day, -95, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'LinkedIn', 'noah.cole', 'TsFAVh0KB4cbXwKb', '2+Y2XZAURf3X2M+Yl5URLA==', 'IvPmh0a/qOKcIa4V28Q=', NULL, 'D7DFCC90F87A52E19EDB7C40CDDA4B76CF271841', 0, DATEADD(day, -100, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'GitHub', 'noah_dev', 'vb5SbNeuFfxTtSTO', '6soEP4Y7xBLkMRdmFkQkaw==', 'klVxThg7kqX+nS+2jQ==', NULL, 'B978AF0211BC517555F107679E41B141CC39776A', 0, DATEADD(day, -110, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'Dropbox', 'noah@secure.com', 'usZF3xOkrUGRAfVa', 'vZ+ZbqYdtX9ui9ZtiJupmg==', 'HHHdCzaH7n24BMoE', NULL, '5938B75678128A2BEAB046412DA350C2E6061781', 0, DATEADD(day, -120, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'Slack', 'noah_cole', 'gJLNwPB8m+YkSQfX', 'wLZ0uLY0m/BOQmsMNY0v3w==', 'R5UwNFzx8I54wu2TuQ==', NULL, '141B5AE6D7E55CE0227793495EE561DCDBC91357', 0, DATEADD(day, -130, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid14, 'Zoom', 'noah@secure.com', 'nhFIy9FzI42Bpctz', 'nO7zWGfGGPF6flfpu9HXTQ==', 'ekx4Nf//ADJoF4ih', NULL, 'BA4378B7903C54DC6D9A2AAB6D70A300B229DBD8', 0, DATEADD(day, -140, GETDATE()));

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid14, 'l185kTNPpw1GKmxi8HD0c6KtSkHlm7ytlmkgoHFtnZc=', 'GuAG13RdHe1auoUrmbhSaoc9tNgx+70GioBUlhgZVTE=', DATEADD(day, -200, GETDATE()));


-- ── User 15: olivia_fox ────────────────────────────────────────
DECLARE @uid15 INT;
INSERT INTO Users (Username, Email, MasterPasswordKey, AuthSalt, EncryptionSalt, LastLogin, LastPasswordUpdate, CreatedAt)
VALUES ('olivia_fox', 'olivia@secure.com', 'JenfND5LmbmRsgVxxKzY7NbQGACmp3XNZ3Hm11Nty+Q=', 'YE37a/NGGarYKhlg4p/gTbZTwuiA3oOxypf872TrxzQ=', 'AN/h80ktIJi5PN1tjXueYixOfo9DZvUDOqvtiooBKDw=', GETDATE(), GETDATE(), DATEADD(day, -330, GETDATE()));
SET @uid15 = SCOPE_IDENTITY();

INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Gmail', 'olivia@gmail.com', '+DTGP4WEuF2Go14c', '55vmXO+XoBvnuGl12Ma/Hw==', 'pD8+tOey3hg8jxww4bU=', 'Personal', '4FEEFC0103E332C884CBDA499F1878464883E7D3', 0, GETDATE());
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Instagram', 'olivia_fox', '0nOmR3zXOv1UZe4U', 'oKWbaeI4XTn4Gc5JvaErcA==', 'KT7VoUgdDQI5Kone+MQ=', NULL, '5587294EBB65D5725C8C30ABE75CD936AEA212BD', 0, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Facebook', 'olivia.fox', '90aFJIitZaHe5wBh', 'GxviML0cpkWCrwdO9VXNNA==', 'i/X13DcRkf0=', NULL, '5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8', 1, DATEADD(day, -15, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Twitter', 'olivia_fox', 'nk+NLYuV1SPn1nLr', 'gYlc9xOPFBaW7MPRMS8y6g==', 'PpkNibDs', NULL, '7C4A8D09CA3762AF61E59520943DC26494F8941B', 1, DATEADD(day, -100, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Amazon', 'olivia@secure.com', 'lNEP43TLK0vkdleN', 'NtQ/ygmYMnu/x53XQA7cwA==', 'OyM/+Kg5hE63eZFg8L0=', NULL, '2D878610F8DD5D49410508C4E41155F10DB5DB01', 0, DATEADD(day, -110, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'Netflix', 'olivia@secure.com', 'r9G+FzHy5tHfnwvR', '++R3cHox/hEh9kQD+3kC+w==', '9xq7v1L+540=', NULL, 'EE8D8728F435FD550F83852AABAB5234CE1DA528', 1, DATEADD(day, -105, GETDATE()));
INSERT INTO VaultItems (UserId, AccountName, AccountUsername, IV, Tag, CipherText, Notes, Sha1Hash, IsLeaked, LastUpdate)
VALUES (@uid15, 'YouTube', 'olivia@secure.com', '/zGrS6oHlEEgZW0p', 'yLDq5lku9v75ri8G9Ai4Hg==', 'im9nFY+hR3SHOyS2', NULL, 'E5C0A7AAF0270F68B59CAB6C5D7114DF49CD44B7', 0, GETDATE());

INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid15, 'kVeu887W8PXoS/YKt6bjjAu02MOvl7Z5HHb/0HmbASk=', 'seLKlqrW3gEv2uYWzgPmLA4GUGeufj64vezv4H/rRpc=', DATEADD(day, -270, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid15, 'OPil9JXWSa9SBi8cKNVuiWPnJbyd99lu58rK9iRQTHI=', 'l15YCYIGVRQ3whybLC7w+bdBfokDOmq46ykfGUAgQFk=', DATEADD(day, -180, GETDATE()));
INSERT INTO MasterPasswordHistory (UserId, PasswordKey, AuthSalt, CreatedAt)
VALUES (@uid15, '01Z8ybnDYYjqchaag1rrtyQsGD3xDQIrLqpQOT6zfaY=', 'tPy0loqhxEpheXkISI/hB6eKP5bPcAZqIqO7dOkIWgc=', DATEADD(day, -90, GETDATE()));


