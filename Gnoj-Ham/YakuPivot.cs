﻿using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a yaku.
    /// </summary>
    public class YakuPivot
    {
        #region Embedded properties

        private readonly List<YakuPivot> _upgrades;

        /// <summary>
        /// Name (japanese).
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Name (english).
        /// </summary>
        public string NameEn { get; private set; }
        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// Fan count;
        /// <c>13</c> for yakuman;
        /// <c>0</c> if the yaku must be concealed.
        /// </summary>
        public int FanCount { get; private set; }
        /// <summary>
        /// Bonus fan count when concealed.
        /// </summary>
        public int ConcealedBonusFanCount { get; private set; }
        /// <summary>
        /// List of <see cref="YakuPivot"/> which make this yaku obsolete.
        /// Yakumans are not in this list as they make every other yakus obsolete.
        /// </summary>
        /// <remarks>Empty for yakumans.</remarks>
        public IReadOnlyCollection<YakuPivot> Upgrades
        {
            get
            {
                return _upgrades;
            }
        }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the yaku is only valid when concealed.
        /// </summary>
        public bool IsConcealedOnly
        {
            get
            {
                return FanCount == 0;
            }
        }
        /// <summary>
        /// Inferred; indicates the count of fans when concealed.
        /// </summary>
        public int ConcealedFanCount
        {
            get
            {
                return FanCount + ConcealedBonusFanCount;
            }
        }
        /// <summary>
        /// Inferred; indicates if the yaku is a yakuman (when concealed, at least).
        /// </summary>
        public bool IsYakuman
        {
            get
            {
                return ConcealedFanCount == 13;
            }
        }

        #endregion Inferred properties

        #region Constructors

        private YakuPivot(string name, string nameEn, int fanCount, string description = null, int concealedBonusFanCount = 0, params YakuPivot[] upgrades)
        {
            Name = name;
            NameEn = nameEn;
            Description = description ?? string.Empty;
            FanCount = fanCount;
            ConcealedBonusFanCount = concealedBonusFanCount;
            _upgrades = new List<YakuPivot>();
            if (upgrades != null && upgrades.Length > 0)
            {
                _upgrades.AddRange(upgrades);
            }
        }

        #endregion Constructors

        #region Static properties

        private static List<YakuPivot> _yakus = null;

        /// <summary>
        /// List of every instances of <see cref="YakuPivot"/>.
        /// </summary>
        public static IReadOnlyCollection<YakuPivot> Yakus
        {
            get
            {
                if (_yakus == null)
                {
                    _yakus = new List<YakuPivot>
                    {
                        new YakuPivot(KOKUSHI_MUSOU, "Thirteen orphans", 0, "One tile of each dragon, wind and terminal, plus a duplicate.", 13),
                        new YakuPivot(DAISANGEN, "Big three dragons", 13, "Brelan of each dragon type."),
                        new YakuPivot(SUUANKOU, "Four concealed triplets", 13, "Four concealed brelans."),
                        // Depending on rules, might count as double yakuman.
                        new YakuPivot(DAISUUSHII, "Big four winds", 13, "Brelan of each wind type."),
                        new YakuPivot(TSUUIISOU, "All honors", 13, "Every combinations contain honors."),
                        new YakuPivot(RYUUIISOU, "All green", 13, "Only green tiles (2, 3, 4, 6, 8 of bamboo and green dragon) in every combinations."),
                        new YakuPivot(CHINROUTOU, "All terminals", 13, "Every combinations contain only terminals."),
                        new YakuPivot(CHUUREN_POUTOU, "Nine gates", 0, "Sequence 1112345678999 in a single family, plus a duplicate. Must be concealed.", 13),
                        new YakuPivot(SUUKANTSU, "Four kans", 13, "Four declared squares (concealed or not)."),
                        new YakuPivot(TENHOU, "Heavenly hand", 0, "14th first tiles form a valid hand for east player.", 13),
                        new YakuPivot(CHIIHOU, "Earthly hand", 0, "14th first tiles form a valid hand for south/west/north player.", 13),
                        // Depending on rules, might not be a yakuman.
                        new YakuPivot(RENHOU, "Hand of man", 0, "Ron on first turn, no other call made.", 13),
                        // Depending on rules, might be ignored.
                        new YakuPivot(NAGASHI_MANGAN, "Discard mangan", 0, "Round ending in Ryuukyoku: the discard of the player contains only terminals and honors. No call has been made by the player, nor by opponents on player's discards.", 5),
                        new YakuPivot(CHINIISOU, "Flush", 5, "One family only.", 6),
                        new YakuPivot(JUNCHAN, "Terminal in each meld", 2, "Terminal in every combinations.", 3),
                        new YakuPivot(RYANPEIKOU, "Two sets of identical sequences", 0, "Double iipeikou. Must be concealed.", 3),
                        // Note : concealed kans are allowed for dealer.
                        new YakuPivot(DABURU_RIICHI, "Double ready", 0, "Riichi at first turn. No call made. Must be concealed.", 2),
                        new YakuPivot(SANSHOKU_DOUJUN, "Three colored straight", 1, "The same sequence in each family.", 2),
                        new YakuPivot(ITTSU, "Straight", 1, "Three sequences 123, 456 and 789 in the same family.", 2),
                        new YakuPivot(TOITOI, "All triplets", 2, "Each combination except the pair is a brelan (or a square)."),
                        new YakuPivot(SANANKOU, "Three concealed triplets", 2, "Three concealed brelans (or squares)."),
                        new YakuPivot(SANSHOKU_DOUKOU, "Three colored triplets", 2, "The same brelan (or squares) in each family."),
                        new YakuPivot(SANKANTSU, "Three kans", 2, "Three declared squares (concealed or not)."),
                        new YakuPivot(CHIITOITSU, "Seven pairs", 0, "Seven pairs.", 2),
                        new YakuPivot(HONROUTOU, "Terminals and honors", 2, "Only terminals and honors in every combinations."),
                        new YakuPivot(SHOUSANGEN, "Small three dragons", 2, "Two brelans and one pair of dragons."),
                        new YakuPivot(HAITEI, "Win by last draw / discard", 1, "Win at the last draw or discard."),
                        new YakuPivot(RINSHAN_KAIHOU, "Dead wall draw", 1, "Win with the compensation tile after declaring kan."),
                        new YakuPivot(CHANKAN, "Robbing a kan", 1, "Win with the fourth tile of an opponent declaring a non-concealed kan."),
                        // Depending on rules, need to be closed.
                        new YakuPivot(TANYAO, "All simples", 1, "No terminal, nor honor, in every combinations."),
                        // Note : cumulative.
                        new YakuPivot(YAKUHAI, "Value tiles", 1, "One brelan of dragon, or dominant wind, or player's turn wind."),
                        new YakuPivot(IPPATSU, "One shot", 0, "Win on the first turn (no call made) after declaring riichi.", 1),
                        new YakuPivot(MENZEN_TSUMO, "Self draw", 0, "Win by tsumo. Must be concealed.", 1),
                        new YakuPivot(PINFU, "All sequences", 0, "Fourth sequences. Last tile is winning on both side of a sequence. Must be concealed.", 1)
                    };
                    _yakus.Add(new YakuPivot(HONIISOU, "Half flush", 2, "Only one family and honors.", 3, _yakus.Find(y => y.Name == CHINIISOU)));
                    _yakus.Add(new YakuPivot(CHANTA, "Terminal or honor in each group", 1, "At least one terminal or honor in every combinations.", 2, _yakus.Find(y => y.Name == JUNCHAN), _yakus.Find(y => y.Name == HONROUTOU)));
                    _yakus.Add(new YakuPivot(RIICHI, "Ready hand", 0, "Win after declaring a tenpai hand. Must be concealed.", 1, _yakus.Find(y => y.Name == DABURU_RIICHI)));
                    _yakus.Add(new YakuPivot(IIPEIKOU, "Identical sequences", 0, "Twice the same sequence in one family. Must be concealed.", 1, _yakus.Find(y => y.Name == RYANPEIKOU)));
                    _yakus.Add(new YakuPivot(SHOUSUUSHII, "Little four winds", 13, "Brelan of three wind type, and a pair of the fourth wind.", 0, _yakus.Find(y => y.Name == DAISUUSHII)));
                }
                return _yakus;
            }
        }

        /// <summary>
        /// Kokushi musou; yakuman.
        /// </summary>
        public static readonly YakuPivot KokushiMusou = Yakus.First(y => y.Name == KOKUSHI_MUSOU);
        /// <summary>
        /// Daisangen; yakuman.
        /// </summary>
        public static readonly YakuPivot Daisangen = Yakus.First(y => y.Name == DAISANGEN);
        /// <summary>
        /// Suuankou; yakuman.
        /// </summary>
        public static readonly YakuPivot Suuankou = Yakus.First(y => y.Name == SUUANKOU);
        /// <summary>
        /// Shousuushii; yakuman.
        /// </summary>
        public static readonly YakuPivot Shousuushii = Yakus.First(y => y.Name == SHOUSUUSHII);
        /// <summary>
        /// Daisuushii; yakuman.
        /// </summary>
        public static readonly YakuPivot Daisuushii = Yakus.First(y => y.Name == DAISUUSHII);
        /// <summary>
        /// Tsuuiisou; yakuman.
        /// </summary>
        public static readonly YakuPivot Tsuuiisou = Yakus.First(y => y.Name == TSUUIISOU);
        /// <summary>
        /// Ryuuiisou; yakuman.
        /// </summary>
        public static readonly YakuPivot Ryuuiisou = Yakus.First(y => y.Name == RYUUIISOU);
        /// <summary>
        /// Chinroutou; yakuman.
        /// </summary>
        public static readonly YakuPivot Chinroutou = Yakus.First(y => y.Name == CHINROUTOU);
        /// <summary>
        /// Chuuren poutou; yakuman.
        /// </summary>
        public static readonly YakuPivot ChuurenPoutou = Yakus.First(y => y.Name == CHUUREN_POUTOU);
        /// <summary>
        /// Suukantsu; yakuman.
        /// </summary>
        public static readonly YakuPivot Suukantsu = Yakus.First(y => y.Name == SUUKANTSU);
        /// <summary>
        /// Tenhou; yakuman.
        /// </summary>
        public static readonly YakuPivot Tenhou = Yakus.First(y => y.Name == TENHOU);
        /// <summary>
        /// Chiihou; yakuman.
        /// </summary>
        public static readonly YakuPivot Chiihou = Yakus.First(y => y.Name == CHIIHOU);
        /// <summary>
        /// Renhou; yakuman (optionnal).
        /// </summary>
        public static readonly YakuPivot Renhou = Yakus.First(y => y.Name == RENHOU);
        /// <summary>
        /// Nagashi mangan.
        /// </summary>
        public static readonly YakuPivot NagashiMangan = Yakus.First(y => y.Name == NAGASHI_MANGAN);
        /// <summary>
        /// Chiniisou.
        /// </summary>
        public static readonly YakuPivot Chiniisou = Yakus.First(y => y.Name == CHINIISOU);
        /// <summary>
        /// Honiisou.
        /// </summary>
        public static readonly YakuPivot Honiisou = Yakus.First(y => y.Name == HONIISOU);
        /// <summary>
        /// Junchan.
        /// </summary>
        public static readonly YakuPivot Junchan = Yakus.First(y => y.Name == JUNCHAN);
        /// <summary>
        /// Ryanpeikou
        /// </summary>
        public static readonly YakuPivot Ryanpeikou = Yakus.First(y => y.Name == RYANPEIKOU);
        /// <summary>
        /// Daburu riichi.
        /// </summary>
        public static readonly YakuPivot DaburuRiichi = Yakus.First(y => y.Name == DABURU_RIICHI);
        /// <summary>
        /// Chanta.
        /// </summary>
        public static readonly YakuPivot Chanta = Yakus.First(y => y.Name == CHANTA);
        /// <summary>
        /// Sanshoku doujun.
        /// </summary>
        public static readonly YakuPivot SanshokuDoujun = Yakus.First(y => y.Name == SANSHOKU_DOUJUN);
        /// <summary>
        /// Ittsu.
        /// </summary>
        public static readonly YakuPivot Ittsu = Yakus.First(y => y.Name == ITTSU);
        /// <summary>
        /// Toitoi.
        /// </summary>
        public static readonly YakuPivot Toitoi = Yakus.First(y => y.Name == TOITOI);
        /// <summary>
        /// Sanankou.
        /// </summary>
        public static readonly YakuPivot Sanankou = Yakus.First(y => y.Name == SANANKOU);
        /// <summary>
        /// Sanshoku doukou.
        /// </summary>
        public static readonly YakuPivot SanshokuDoukou = Yakus.First(y => y.Name == SANSHOKU_DOUKOU);
        /// <summary>
        /// Sankantsu.
        /// </summary>
        public static readonly YakuPivot Sankantsu = Yakus.First(y => y.Name == SANKANTSU);
        /// <summary>
        /// Chiitoitsu.
        /// </summary>
        public static readonly YakuPivot Chiitoitsu = Yakus.First(y => y.Name == CHIITOITSU);
        /// <summary>
        /// Honroutou.
        /// </summary>
        public static readonly YakuPivot Honroutou = Yakus.First(y => y.Name == HONROUTOU);
        /// <summary>
        /// Shousangen.
        /// </summary>
        public static readonly YakuPivot Shousangen = Yakus.First(y => y.Name == SHOUSANGEN);
        /// <summary>
        /// Haitei.
        /// </summary>
        public static readonly YakuPivot Haitei = Yakus.First(y => y.Name == HAITEI);
        /// <summary>
        /// Rinshan kaihou.
        /// </summary>
        public static readonly YakuPivot RinshanKaihou = Yakus.First(y => y.Name == RINSHAN_KAIHOU);
        /// <summary>
        /// Chankan.
        /// </summary>
        public static readonly YakuPivot Chankan = Yakus.First(y => y.Name == CHANKAN);
        /// <summary>
        /// Tanyao.
        /// </summary>
        public static readonly YakuPivot Tanyao = Yakus.First(y => y.Name == TANYAO);
        /// <summary>
        /// Yakuhai.
        /// </summary>
        public static readonly YakuPivot Yakuhai = Yakus.First(y => y.Name == YAKUHAI);
        /// <summary>
        /// Riichi.
        /// </summary>
        public static readonly YakuPivot Riichi = Yakus.First(y => y.Name == RIICHI);
        /// <summary>
        /// Ippatsu.
        /// </summary>
        public static readonly YakuPivot Ippatsu = Yakus.First(y => y.Name == IPPATSU);
        /// <summary>
        /// Menzen tsumo.
        /// </summary>
        public static readonly YakuPivot MenzenTsumo = Yakus.First(y => y.Name == MENZEN_TSUMO);
        /// <summary>
        /// Pinfu.
        /// </summary>
        public static readonly YakuPivot Pinfu = Yakus.First(y => y.Name == PINFU);
        /// <summary>
        /// Iipeikou.
        /// </summary>
        public static readonly YakuPivot Iipeikou = Yakus.First(y => y.Name == IIPEIKOU);

        #endregion Static properties

        #region Constants

        private const string KOKUSHI_MUSOU = "Kokushi musou";
        private const string DAISANGEN = "Daisangen";
        private const string SUUANKOU = "Suuankou";
        private const string SHOUSUUSHII = "Shousuushii";
        private const string DAISUUSHII = "Daisuushii";
        private const string TSUUIISOU = "Tsuuiisou";
        private const string RYUUIISOU = "Ryuuiisou";
        private const string CHINROUTOU = "Chinroutou";
        private const string CHUUREN_POUTOU = "Chuuren poutou";
        private const string SUUKANTSU = "Suukantsu";
        private const string TENHOU = "Tenhou";
        private const string CHIIHOU = "Chiihou";
        private const string RENHOU = "Renhou";
        private const string NAGASHI_MANGAN = "Nagashi mangan";
        private const string CHINIISOU = "Chiniisou";
        private const string HONIISOU = "Honiisou";
        private const string JUNCHAN = "Junchan";
        private const string RYANPEIKOU = "Ryanpeikou";
        private const string DABURU_RIICHI = "Daburu riichi";
        private const string CHANTA = "Chanta";
        private const string SANSHOKU_DOUJUN = "Sanshoku doujun";
        private const string ITTSU = "Ittsu";
        private const string TOITOI = "Toitoi";
        private const string SANANKOU = "Sanankou";
        private const string SANSHOKU_DOUKOU = "Sanshoku doukou";
        private const string SANKANTSU = "Sankantsu";
        private const string CHIITOITSU = "Chiitoitsu";
        private const string HONROUTOU = "Honroutou";
        private const string SHOUSANGEN = "Shousangen";
        private const string HAITEI = "Haitei";
        private const string RINSHAN_KAIHOU = "Rinshan kaihou";
        private const string CHANKAN = "Chankan";
        private const string TANYAO = "Tanyao";
        private const string YAKUHAI = "Yakuhai";
        private const string RIICHI = "Riichi";
        private const string IPPATSU = "Ippatsu";
        private const string MENZEN_TSUMO  = "Menzen tsumo";
        private const string PINFU = "Pinfu";
        private const string IIPEIKOU = "Iipeikou";

        #endregion

        #region Static methods

        /// <summary>
        /// Gets the best yakus combination from a list of yakus combinations.
        /// </summary>
        /// <param name="yakus">The list of yakus combinations.</param>
        /// <param name="concealedHand"><c>True</c> if the hand is concealed; <c>False</c> otherwise.</param>
        /// <returns>The best combination of yakus.</returns>
        public static List<YakuPivot> GetBestYakusFromList(List<List<YakuPivot>> yakus, bool concealedHand)
        {
            return yakus.OrderByDescending(ys => ys.Sum(y => concealedHand ? y.ConcealedFanCount : y.FanCount)).FirstOrDefault() ?? new List<YakuPivot>();
        }

        #endregion Static methods
    }
}
