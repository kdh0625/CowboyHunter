using System;
using System.Collections.Generic;
using System.IO;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using UnityEditor;
using UnityEngine;

namespace CowboyHunter.Editor
{
    // 코드로 그린 임시 픽셀 아트.
    // 진짜 아트가 나오면 같은 이름의 PNG를 덮어쓰거나, 데이터 에셋의 sprite/icon을 바꾸면 된다.
    public static class PlaceholderArtGenerator
    {
        public const string Dir = "Assets/_Project/Art/Sprites/Placeholder";

        // ── 도안 (. 은 투명) ─────────────────────────

        static readonly string[] Cowboy =
        {
            "................",
            ".....KKKKKK.....",
            "....KHHHHHHK....",
            "..KKKHHHHHHKKK..",
            ".KHHHHHHHHHHHHK.",
            "..KKSSSSSSSSKK..",
            "....KSKSSKSK....",
            "....KSSSSSSK....",
            ".....KNNNNK.....",
            "....KBBNNBBK....",
            "...KBBBBBBBBKGG.",
            "...KSKBBBBKSKG..",
            "....KPPPPPPK....",
            "....KPPKKPPK....",
            "....KOOKKOOK....",
            "....KKK..KKK....",
        };

        static readonly string[] Ghost =
        {
            "................",
            ".....KKKKKK.....",
            "....KHHHHHHK....",
            "..KKKHHHHHHKKK..",
            ".KHHHHHHHHHHHHK.",
            "...KWWWWWWWWK...",
            "..KWWKWWWWKWWK..",
            "..KWWWWWWWWWWK..",
            "..KNNNNNNNNNNK..",
            "..KWNNNNNNNNWK..",
            "..KWWWWWWWWWWK..",
            "..KWWWWWWWWWWK..",
            ".KWWWWWWWWWWWWK.",
            ".KWWWKWWWWKWWWK.",
            "..KWK.KWWK.KWK..",
            "...K...KK...K...",
        };

        static readonly string[] Scorpion =
        {
            "................",
            "...........KK...",
            "..........KTTK..",
            "...........KTK..",
            "...........KTK..",
            "..........KTTK..",
            "..KK.....KTTK...",
            ".KCCK...KTTK....",
            ".KCK..KKTTK.....",
            "..KK.KTTTTTK....",
            "....KTTTTTTTK...",
            "...KTKTTKTTTKKK.",
            "...KTTTTTTTKCCK.",
            "...K.K.K.K.K.KK.",
            "..K.K.K.K.K.....",
            "................",
        };

        static readonly string[] Spider =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "..K..........K..",
            "...K..KKKK..K...",
            "....KKAAAAKK....",
            ".KK.KAAAAAAK.KK.",
            "...KKAAAAAAKK...",
            "..K.KAARRAAK.K..",
            ".K..KAAAAAAK..K.",
            "...KKAAAAAAKK...",
            "..K..KAAAAK..K..",
            ".K....KKKK....K.",
            "................",
        };

        static readonly string[] Vulture =
        {
            "................",
            ".....KKK........",
            "....KRRRK.......",
            "...KRKRRK.......",
            "..KYYKRRK.......",
            "...KK.KWWK......",
            "......KWWWKK....",
            ".....KDDDDDDKK..",
            "....KDDDDDDDDDK.",
            "....KDDDDDDDDDDK",
            ".....KDDDDDDDDK.",
            "......KDDDDDKK..",
            ".......KDDDK....",
            ".......KY.KY....",
            "......KYY.KYY...",
            "................",
        };

        static readonly string[] Snake =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            ".........KKK....",
            "........KGGGK...",
            "........KGKGK...",
            ".........KGGK.R.",
            "....KKK...KGGKR.",
            "...KGGGK...KGK..",
            "..KGGKGGK..KGK..",
            "..KGK.KGGKKGGK..",
            "..KGGK.KGGGGK...",
            "...KKK..KKKK....",
        };

        static readonly string[] Bullet =
        {
            "................",
            ".KKKKKKKKKKKK...",
            ".KCCCCCCCCKTTK..",
            ".KCCCCCCCCKTTTK.",
            ".KCCCCCCCCKTTTK.",
            ".KCCCCCCCCKTTK..",
            ".KKKKKKKKKKKK...",
            "................",
        };

        static readonly Dictionary<RelicSlot, string[]> RelicShapes = new()
        {
            [RelicSlot.Muzzle] = new[]
            {
                "............", "............", "............",
                "..KKKKKKKKK.",
                ".KMMMMMMMMMK",
                "KMLLLLLLLLMK",
                "KMMMMMMMMMMK",
                ".KMMMMMMMMMK",
                "..KKKKKKKKK.",
                "............", "............", "............",
            },
            [RelicSlot.Scope] = new[]
            {
                "............",
                "....KKKK....",
                "..KKMMMMKK..",
                ".KMMLLLLMMK.",
                ".KMLLKKLLMK.",
                "KMMLKLLKLMMK",
                "KMMLKLLKLMMK",
                ".KMLLKKLLMK.",
                ".KMMLLLLMMK.",
                "..KKMMMMKK..",
                "....KKKK....",
                "............",
            },
            [RelicSlot.Cylinder] = new[]
            {
                "............",
                "....KKKK....",
                "..KKMMMMKK..",
                ".KMKKMMKKMK.",
                ".KMKKMMKKMK.",
                "KMMMMKKMMMMK",
                "KMMMMKKMMMMK",
                ".KMKKMMKKMK.",
                ".KMKKMMKKMK.",
                "..KKMMMMKK..",
                "....KKKK....",
                "............",
            },
            [RelicSlot.Grip] = new[]
            {
                "............",
                ".KKKKKKK....",
                ".KMMMMMMK...",
                ".KMLLLLMMK..",
                "..KMLLLLMMK.",
                "...KMLLLLMK.",
                "...KMLLLLMK.",
                "...KMLLLLMK.",
                "...KMLLLLMK.",
                "...KMMMMMMK.",
                "...KKKKKKKK.",
                "............",
            },
        };

        // ── 색 ───────────────────────────────────

        static Color32 C(int hex) => new((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);
        static Color32 Lighter(Color32 c) => new((byte)Math.Min(255, c.r + 50), (byte)Math.Min(255, c.g + 50), (byte)Math.Min(255, c.b + 50), 255);
        static readonly Color32 Outline = C(0x2B1D14);

        static Dictionary<char, Color32> CowboyPalette(int hat, int skin, int shirt, int scarf, int pants) => new()
        {
            ['H'] = C(hat), ['S'] = C(skin), ['B'] = C(shirt), ['N'] = C(scarf), ['P'] = C(pants), ['O'] = C(0x5A3A20), ['G'] = C(0x9A9A9A)
        };

        // 에셋 이름 → (파일 이름, 도안, 색)
        static readonly Dictionary<string, (string file, string[] art, Dictionary<char, Color32> palette)> Characters = new()
        {
            ["Player"] = ("char_player", Cowboy, CowboyPalette(0x8A5A30, 0xE0A878, 0xB89868, 0xC03030, 0x4A5A8A)),
            ["Enemy_PixelBandit"] = ("char_bandit", Cowboy, CowboyPalette(0x6A4A2A, 0xE0A878, 0x5A5A5A, 0xC03030, 0x3A3A50)),
            ["Enemy_SkeletalGunslinger"] = ("char_skeleton", Cowboy, CowboyPalette(0x3A3030, 0xE8E0D0, 0x6A5040, 0xA02020, 0x4A4040)),
            ["Enemy_CoyoteChief"] = ("char_coyote", Cowboy, CowboyPalette(0x8A6A40, 0xC07840, 0x4A7A4A, 0xC03030, 0x6A4A30)),
            ["Boss_DustyJack"] = ("boss_dusty_jack", Cowboy, CowboyPalette(0xB09060, 0xE0A878, 0x8A6040, 0xC03030, 0x5A4A3A)),
            ["Boss_TwoGunRosa"] = ("boss_two_gun_rosa", Cowboy, CowboyPalette(0xA03050, 0xE8B090, 0x6A2040, 0x202020, 0x3A2A3A)),
            ["Boss_SheriffHale"] = ("boss_sheriff_hale", Cowboy, CowboyPalette(0x303030, 0xD89868, 0x505A70, 0xD0B030, 0x3A3A48)),
            ["Boss_LastOutlaw"] = ("boss_last_outlaw", Cowboy, CowboyPalette(0x101010, 0xC8B8A8, 0x202020, 0x606060, 0x181818)),
            ["Enemy_GhostlyOutlaw"] = ("char_ghost", Ghost, new() { ['H'] = C(0x303030), ['W'] = C(0xBFE8E8), ['N'] = C(0x202020) }),
            ["Enemy_VenomousScorpion"] = ("char_scorpion", Scorpion, new() { ['T'] = C(0x8A9A40), ['C'] = C(0xA0B050) }),
            ["Enemy_GiantSpider"] = ("char_spider", Spider, new() { ['A'] = C(0x8A6A30), ['R'] = C(0xD03020) }),
            ["Enemy_DesertVulture"] = ("char_vulture", Vulture, new() { ['R'] = C(0xD08880), ['Y'] = C(0xE0B040), ['W'] = C(0xE8E0D0), ['D'] = C(0x4A3020) }),
            ["Enemy_Rattlesnake"] = ("char_snake", Snake, new() { ['G'] = C(0xC8A060), ['R'] = C(0xD03030) }),
        };

        // 탄환 에셋 이름 → (탄두 색, 탄피 색)
        static readonly Dictionary<string, (int tip, int casing)> BulletColors = new()
        {
            ["Bullet_Normal"] = (0x9A9A9A, 0xD0A040),
            ["Bullet_Guard"] = (0x5080D0, 0x6A7A9A),
            ["Bullet_Fire"] = (0xE06020, 0xD0A040),
            ["Bullet_Pierce"] = (0xE0E0F0, 0xD0A040),
            ["Bullet_Poison"] = (0x60C040, 0xD0A040),
            ["Bullet_Silver"] = (0xE8F0FF, 0xB8C0D0),
            ["Bullet_Firebird"] = (0xFF3020, 0xE08030),
            ["Bullet_Bounty"] = (0xFFD040, 0xD0A040),
            ["Bullet_Steel"] = (0x606878, 0x8A8A90),
            ["Bullet_Armored"] = (0x7090B0, 0x8A8A90),
            ["Bullet_Smoke"] = (0xC8C8C8, 0x9A9A9A),
            ["Bullet_Hex"] = (0x9050C0, 0x6A4A7A),
        };

        // 유물 에셋 이름 → 색
        static readonly Dictionary<string, int> RelicColors = new()
        {
            ["Relic_LongBarrel"] = 0x8A8A90,
            ["Relic_PowderInjector"] = 0xC06030,
            ["Relic_SilverBarrel"] = 0xC8D0E0,
            ["Relic_HawkEye"] = 0x4A7AB0,
            ["Relic_QuickScope"] = 0x5A9A6A,
            ["Relic_SpareBelt"] = 0x9A7A40,
            ["Relic_ExtendedDrum"] = 0x6A6A70,
            ["Relic_LeatherGrip"] = 0x8A5A30,
            ["Relic_IronGrip"] = 0x5A5A60,
            ["Relic_HunterGloves"] = 0xA07040,
        };

        // UI 스킨: 파일 이름 → 9-slice 테두리 (0이면 테두리 없음)
        public static readonly Dictionary<string, int> UiBorders = new()
        {
            ["ui_parchment"] = 3, ["ui_button"] = 4, ["ui_dark"] = 3, ["ui_slot"] = 3, ["ui_wood_tile"] = 0, ["ui_cylinder"] = 0, ["bg_desert"] = 0,
        };

        [MenuItem("CowboyHunter/임시 픽셀 아트 다시 만들기")]
        public static void Generate()
        {
            Directory.CreateDirectory(Dir);

            foreach (var (_, (file, art, palette)) in Characters) Save(file, Draw(art, palette));
            foreach (var (name, (tip, casing)) in BulletColors)
                Save("bullet_" + FileKey(name), Draw(Bullet, new() { ['T'] = C(tip), ['C'] = C(casing) }));
            foreach (var relic in Load<RelicData>("t:RelicData"))
            {
                var color = RelicColors.TryGetValue(relic.name, out var hex) ? C(hex) : C(0x8A8A8A);
                Save("relic_" + FileKey(relic.name), Draw(RelicShapes[relic.slot], new() { ['M'] = color, ['L'] = Lighter(color) }));
            }
            Save("ui_parchment", Panel(C(0xB89868), C(0xE8D8B0), C(0xDCCA9C)));
            Save("ui_dark", Panel(C(0x3A2A1E), C(0x2A1E16), C(0x32241A)));
            Save("ui_slot", Panel(C(0x7A7A80), C(0x4A4A50), C(0x55555C)));
            Save("ui_button", Button());
            Save("ui_wood_tile", Wood());
            Save("ui_cylinder", Cylinder());
            Save("bg_desert", Desert());

            AssetDatabase.Refresh();
            foreach (var (file, border) in UiBorders)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath($"{Dir}/{file}.png");
                importer.spriteBorder = new Vector4(border, border, border, border);
                // 9-slice + 타일 반복(Tiled)에는 FullRect 메시가 필요하다
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                if (file == "ui_wood_tile") importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            AssignToData();
            AssetDatabase.SaveAssets();
            Debug.Log("임시 픽셀 아트를 만들고 데이터에 연결했습니다: " + Dir);
        }

        public static Sprite Get(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{file}.png");

        static void AssignToData()
        {
            foreach (var enemy in Load<EnemyData>("t:EnemyData"))
                if (Characters.TryGetValue(enemy.name, out var c)) { enemy.sprite = Get(c.file); EditorUtility.SetDirty(enemy); }
            foreach (var bullet in Load<BulletData>("t:BulletData"))
                if (BulletColors.ContainsKey(bullet.name)) { bullet.icon = Get("bullet_" + FileKey(bullet.name)); EditorUtility.SetDirty(bullet); }
            foreach (var relic in Load<RelicData>("t:RelicData"))
            {
                relic.icon = Get("relic_" + FileKey(relic.name));
                EditorUtility.SetDirty(relic);
            }
        }

        // "Enemy_PixelBandit" → "pixel_bandit"
        static string FileKey(string assetName)
        {
            var s = assetName.Substring(assetName.IndexOf('_') + 1);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(s[i]));
            }
            return sb.ToString();
        }

        static List<T> Load<T>(string filter) where T : UnityEngine.Object
        {
            var list = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { "Assets/_Project/Data" }))
                list.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }

        // ── 그리기 ─────────────────────────────────

        static Texture2D Blank(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color32[w * h];
            tex.SetPixels32(clear);
            return tex;
        }

        // 도안의 0번째 줄이 맨 위
        static Texture2D Draw(string[] art, Dictionary<char, Color32> palette)
        {
            int h = art.Length, w = art[0].Length;
            var tex = Blank(w, h);
            for (int row = 0; row < h; row++)
            {
                if (art[row].Length != w) throw new InvalidOperationException($"도안 {row}번째 줄 길이가 {art[row].Length}입니다 (기대값 {w}).");
                for (int x = 0; x < w; x++)
                {
                    char ch = art[row][x];
                    if (ch == '.') continue;
                    Color32 color = ch == 'K' ? Outline : palette.TryGetValue(ch, out var c) ? c : throw new InvalidOperationException($"색이 정해지지 않은 글자 '{ch}'");
                    tex.SetPixel(x, h - 1 - row, color);
                }
            }
            return tex;
        }

        static bool Speck(int x, int y) => ((x * 73 + y * 151) ^ (x * y * 31)) % 11 == 0;

        static Texture2D Panel(Color32 edge, Color32 fill, Color32 speck)
        {
            var tex = Blank(16, 16);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool outer = x == 0 || y == 0 || x == 15 || y == 15;
                bool inner = x == 1 || y == 1 || x == 14 || y == 14;
                tex.SetPixel(x, y, outer ? Outline : inner ? edge : Speck(x, y) ? speck : fill);
            }
            return tex;
        }

        static Texture2D Button()
        {
            var tex = Blank(16, 16);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool outer = x == 0 || y == 0 || x == 15 || y == 15;
                var color = outer ? Outline : y >= 13 ? C(0xA87850) : y <= 2 ? C(0x4A2E1A) : C(0x7A5030);
                tex.SetPixel(x, y, color);
            }
            return tex;
        }

        static Texture2D Wood()
        {
            var tex = Blank(16, 16);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                var color = y % 8 == 0 ? C(0x3A2414) : (x * 7 + y * 13) % 29 == 0 ? C(0x553620) : C(0x4A301C);
                tex.SetPixel(x, y, color);
            }
            return tex;
        }

        static Texture2D Cylinder()
        {
            const int size = 48;
            var tex = Blank(size, size);
            var c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > 23.5f) continue;
                var color = d > 22 ? Outline : d > 20 ? C(0x6A6A72) : d < 5 ? C(0x5A5A62) : C(0x8A8A94);
                if (d > 5 && d < 6.2f) color = Outline;
                tex.SetPixel(x, y, color);
            }
            return tex;
        }

        static Texture2D Desert()
        {
            const int w = 160, h = 60;
            var tex = Blank(w, h);
            Color32[] sky = { C(0xF2D29A), C(0xEEC285), C(0xE9B474), C(0xE3A566) };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int fromTop = h - 1 - y;
                Color32 color = sky[Math.Min(3, fromTop / 9)];
                // 메사(탁자 모양 바위산)
                float mesa = Mesa(x, 20, 18, 16) + Mesa(x, 70, 26, 22) + Mesa(x, 128, 20, 14);
                if (fromTop > 44 - mesa && fromTop <= 44) color = C(0xA85A30);
                if (fromTop > 44 - mesa && fromTop <= 46 - mesa && mesa > 0) color = C(0xC06A38);
                if (fromTop > 44) color = Speck(x, y) ? C(0xC89050) : C(0xD8A860);
                tex.SetPixel(x, y, color);
            }
            return tex;
        }

        static float Mesa(int x, int center, int halfWidth, int height)
        {
            int d = Math.Abs(x - center);
            if (d > halfWidth + 4) return 0;
            if (d <= halfWidth) return height;
            return height * (halfWidth + 4 - d) / 4f;
        }

        static void Save(string file, Texture2D tex)
        {
            File.WriteAllBytes($"{Dir}/{file}.png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
