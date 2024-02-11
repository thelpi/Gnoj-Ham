using System;
using System.Collections.Generic;
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
        public IReadOnlyCollection<YakuPivot> Upgrades => _upgrades;
        /// <summary>
        /// An example of valid hand; <c>null</c> if none.
        /// </summary>
        public IReadOnlyCollection<TilePivot> Example { get; private set; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the yaku is only valid when concealed.
        /// </summary>
        public bool IsConcealedOnly => FanCount == 0;
        /// <summary>
        /// Inferred; indicates the fan count when concealed.
        /// </summary>
        public int ConcealedFanCount => FanCount + ConcealedBonusFanCount;
        /// <summary>
        /// Inferred; indicates if the yaku is a yakuman (when concealed, at least).
        /// </summary>
        public bool IsYakuman => ConcealedFanCount == 13;

        #endregion Inferred properties

        #region Constructors

        private YakuPivot(string name, string nameEn, int fanCount, string description = null, int concealedBonusFanCount = 0, IReadOnlyCollection<TilePivot> example = null, params YakuPivot[] upgrades)
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
            Example = example;
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
                    var tiles = TilePivot.GetCompleteSet(false);

                    _yakus = new List<YakuPivot>
                    {
                        new YakuPivot(KOKUSHI_MUSOU, "Treize orphelins", 0, "Une tuile de chaque dragon, vent et terminal (1 et 9), dont une paire.", 13, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 9),
                            tiles.First(x => x.Wind == WindPivot.East),
                            tiles.First(x => x.Wind == WindPivot.South),
                            tiles.First(x => x.Wind == WindPivot.West),
                            tiles.First(x => x.Wind == WindPivot.North),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 9)
                        }),
                        new YakuPivot(DAISANGEN, "Trois grands dragons", 13, "Un brelan (ou carré) de chaque dragon.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4)
                        }),
                        new YakuPivot(SUUANKOU, "Quatre brelans cachés", 13, "Quatre brelans (ou carrés) cachés.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7)
                        }),
                        // Depending on rules, might count as double yakuman.
                        new YakuPivot(DAISUUSHII, "Quatre grands vents", 13, "Un brelan (ou carré) de chaque vent.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2)
                        }),
                        new YakuPivot(TSUUIISOU, "Tout honneur", 13, "Chaque combinaison contient des honneurs.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North)
                        }),
                        new YakuPivot(RYUUIISOU, "Tout vert", 13, "Que des tuiles \"vertes\" (2, 3, 4, 6, 8 de bambou et dragon vert) dans chaque combinaison.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3)
                        }),
                        new YakuPivot(CHINROUTOU, "Que des terminaux", 13, "Chaque combinaison contient exclusivement des terminaux (1 et 9).", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 9)
                        }),
                        new YakuPivot(CHUUREN_POUTOU, "Neuf portes", 0, "Séquence 1112345678999 dans une seule famille, plus un doublon pour la paire. Doit être caché.", 13, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9)
                        }),
                        new YakuPivot(SUUKANTSU, "Quatre carrés", 13, "Quatre carrés déclarés (cachés ou non)."),
                        new YakuPivot(TENHOU, "Main du ciel", 0, "Main de départ valide pour le joueur en est.", 13),
                        new YakuPivot(CHIIHOU, "Main de la terre", 0, "Main de départ valide pour le joueur en sud, ouest ou nord.", 13),
                        new YakuPivot(RENHOU, "Main de l'homme", 5, "Victoire sur 'Ron' au premier tour, sans autre appel dans le tour."),
                        // Depending on rules, might be ignored.
                        new YakuPivot(NAGASHI_MANGAN, "Nagashi mangan", 0, $"Fin de partie en 'Ryuukyoku': les tuiles jetées ne contiennent que des honneurs et terminaux.{Environment.NewLine}Le joueur n'a fait aucun appel, et aucun adversaire n'a fait d'appel sur les tuiles jetées.", 5),
                        new YakuPivot(CHINIISOU, "Main pure", 5, "Toutes les tuiles sont de la même famille.", 1, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 2)
                        }),
                        new YakuPivot(JUNCHAN, "Terminaux partout", 2, "Chaque combinaison contient un terminal (1 ou 9).", 1, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1)
                        }),
                        new YakuPivot(RYANPEIKOU, "Double iipeikou", 0, "Double iipeikou. Doit être caché.", 3, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red)
                        }),
                        // Note : concealed kans are allowed for dealer.
                        new YakuPivot(DABURU_RIICHI, "Premier tour riichi", 0, "Déclaration 'Riichi' au premier tour. Aucun appel fait dans le tour.", 2),
                        new YakuPivot(SANSHOKU_DOUJUN, "Suite dans trois familles", 1, "La même suite dans chaque famille.", 1, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1)
                        }),
                        new YakuPivot(ITTSU, "Suite complète", 1, "Trois suites 123, 456 et 789 dans une seule famille.", 1, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East)
                        }),
                        new YakuPivot(TOITOI, "Que des brelans", 2, "Quatre brelans (ou carrés).", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red)
                        }),
                        new YakuPivot(SANANKOU, "Trois brelans cachés", 2, "Trois brelans cachés (ou carrés cachés)."),
                        new YakuPivot(SANSHOKU_DOUKOU, "Brelan dans trois familles", 2, "Le même brelan (ou carré) dans les trois familles.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green)
                        }),
                        new YakuPivot(SANKANTSU, "Trois carrés", 2, "Trois carrés déclarés (cachés ou non)."),
                        new YakuPivot(CHIITOITSU, "Sept paires", 0, "Sept paires.", 2, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                            tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                        }),
                        new YakuPivot(HONROUTOU, "Terminaux et honneurs", 2, "Chaque combinaison contient un honneur ou un terminal (1 ou 9).", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green)
                        }),
                        new YakuPivot(SHOUSANGEN, "Trois petits dragons", 2, "Deux brelans (ou carrés) et une paire de dragons.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.Red),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Dragon == DragonPivot.White),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Dragon == DragonPivot.Green)
                        }),
                        new YakuPivot(HAITEI, "Victoire à la dernière tuile", 1, "La dernière tuile de la pioche, ou jetée, complète la main."),
                        new YakuPivot(RINSHAN_KAIHOU, "Victoire sur compensation", 1, "La tuile de compensation après un appel 'Kan' complète la main."),
                        new YakuPivot(CHANKAN, "Vol sur Kan", 1, "Sur une déclaration de 'Kan' ouvert, la tuile volée permet à un autre joueur de compléter sa main."),
                        // Depending on rules, need to be closed.
                        new YakuPivot(TANYAO, "Aucun honneur ou terminal", 1, "Chaque combinaison ne contient ni honneur, ni terminal (1 ou 9).", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2)
                        }),
                        // Note : cumulative.
                        new YakuPivot(YAKUHAI, "Brelan de valeur", 1, "Un brelan (ou carré) de dragon, vent du tour ou vent dominant.", 0, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Green),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3)
                        }),
                        new YakuPivot(IPPATSU, "Premier tour après Riichi", 0, "Victoire dans le premier tour après avoir déclaré 'Riichi' (aucun appel dans le tour).", 1),
                        new YakuPivot(MENZEN_TSUMO, "Tout pioché", 0, "Tout caché et victoire par 'Tsumo'.", 1),
                        new YakuPivot(PINFU, "Que des suites", 0, $"Quatre suites. La paire n'est pas un dragon, un vent dominant ou du tour.{Environment.NewLine}La dernière tuile doit être sur les deux bords d'une suite. Doit être cachée.", 1, new List<TilePivot>
                        {
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 7),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 8),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 6),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5),
                            tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 5)
                        })
                    };
                    _yakus.Add(new YakuPivot(HONIISOU, "Main semi-pure", 2, "La main contient une seule famille et des honneurs.", 1, new List<TilePivot>
                    {
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 4),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 6),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 8),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                        tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                        tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red)
                    }, _yakus.Find(y => y.Name == CHINIISOU)));
                    _yakus.Add(new YakuPivot(CHANTA, "Terminaux et honneurs", 1, "Chaque combinaison contient un terminal (1 ou 9) ou un honneur.", 1, new List<TilePivot>
                    {
                        tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 3),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 7),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 8),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 9),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 1),
                        tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9),
                        tiles.First(x => x.Family == FamilyPivot.Caracter && x.Number == 9)
                    }, _yakus.Find(y => y.Name == JUNCHAN), _yakus.Find(y => y.Name == HONROUTOU)));
                    _yakus.Add(new YakuPivot(RIICHI, "Main prête", 0, "Annoncer être à une tuile d'une main complète. Doit être cachée.", 1, null, _yakus.Find(y => y.Name == DABURU_RIICHI)));
                    _yakus.Add(new YakuPivot(IIPEIKOU, "Suites identiques", 0, "Deux fois la même suite dans une famille. Doit être cachée.", 1, new List<TilePivot>
                    {
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 3),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 4),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                        tiles.First(x => x.Family == FamilyPivot.Circle && x.Number == 5),
                        tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                        tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                        tiles.First(x => x.Family == FamilyPivot.Dragon && x.Dragon == DragonPivot.Red),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East)
                    }, _yakus.Find(y => y.Name == RYANPEIKOU)));
                    _yakus.Add(new YakuPivot(SHOUSUUSHII, "Quatre petits vents", 13, "Trois brelans (ou carrés) de vents, et une paire de vents.", 0, new List<TilePivot>
                    {
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.East),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.North),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.West),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Bamboo && x.Number == 2),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South),
                        tiles.First(x => x.Family == FamilyPivot.Wind && x.Wind == WindPivot.South)
                    }, _yakus.Find(y => y.Name == DAISUUSHII)));
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

        /// <summary>
        /// Dora display name.
        /// </summary>
        public const string Dora = "Dora";
        /// <summary>
        /// Ura dora display name.
        /// </summary>
        public const string UraDora = "Uradora";
        /// <summary>
        /// Red dora display name.
        /// </summary>
        public const string RedDora = "Akadora";

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
        private const string MENZEN_TSUMO = "Menzen tsumo";
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
        /// <exception cref="ArgumentNullException"><paramref name="yakus"/> is <c>Null</c>.</exception>
        public static List<YakuPivot> GetBestYakusFromList(IEnumerable<List<YakuPivot>> yakus, bool concealedHand)
        {
            return yakus == null
                ? throw new ArgumentNullException(nameof(yakus))
                : yakus.OrderByDescending(ys => ys.Sum(y => concealedHand ? y.ConcealedFanCount : y.FanCount)).FirstOrDefault() ?? new List<YakuPivot>();
        }

        /// <summary>
        /// Computes the list of yakus available with this sequence of combinations.
        /// If the list contains one or several yakumans, it can't contain any regular yakus.
        /// Two <see cref="YakuPivot"/> are not computed :
        /// <list type="bullet">
        /// <item><see cref="KokushiMusou"/></item>
        /// <item><see cref="NagashiMangan"/></item>
        /// </list>
        /// </summary>
        /// <param name="combinationsSequence">Sequence of combinations.</param>
        /// <param name="context">Context.</param>
        /// <returns>List of yakus with this sequence of combinations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="combinationsSequence"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>Null</c>.</exception>
        /// <exception cref="NotImplementedException">The <see cref="YakuPivot"/> to check is not implemented.</exception>
        public static List<YakuPivot> GetYakus(List<TileComboPivot> combinationsSequence, WinContextPivot context)
        {
            if (combinationsSequence == null)
            {
                throw new ArgumentNullException(nameof(combinationsSequence));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var yakus = new List<YakuPivot>();

            foreach (var yaku in Yakus.Where(y => y.IsYakuman))
            {
                var addYaku = false;
                if (yaku == Daisangen)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Dragon) == 3;
                }
                else if (yaku == Suuankou)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.IsConcealed && (!c.Tiles.Contains(context.LatestTile) || context.DrawType.IsSelfDraw())) == 4;
                }
                else if (yaku == Shousuushii)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Wind) == 3
                        && combinationsSequence.Any(c => c.IsPair && c.Family == FamilyPivot.Wind);
                }
                else if (yaku == Daisuushii)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Wind) == 4;
                }
                else if (yaku == Tsuuiisou)
                {
                    addYaku = combinationsSequence.All(c => c.IsHonor);
                }
                else if (yaku == Ryuuiisou)
                {
                    addYaku = combinationsSequence.All(c =>
                        (c.Family == FamilyPivot.Bamboo && c.Tiles.All(t => new[] { 2, 3, 4, 6, 8 }.Contains(t.Number)))
                        || (c.Family == FamilyPivot.Dragon && c.Tiles.First().Dragon == DragonPivot.Green)
                    );
                }
                else if (yaku == Chinroutou)
                {
                    addYaku = combinationsSequence.All(c => c.IsTerminal);
                }
                else if (yaku == ChuurenPoutou)
                {
                    if (combinationsSequence.All(c => c.IsConcealed)
                        && combinationsSequence.Select(c => c.Family).Distinct().Count() == 1)
                    {
                        var numberPattern = string.Join(string.Empty, combinationsSequence.SelectMany(c => c.Tiles).Select(t => t.Number).OrderBy(i => i));
                        addYaku = new[]
                        {
                            "11112345678999", "11122345678999", "11123345678999",
                            "11123445678999", "11123455678999", "11123456678999",
                            "11123456778999", "11123456788999", "11123456789999"
                        }.Contains(numberPattern);
                    }
                }
                else if (yaku == Suukantsu)
                {
                    addYaku = combinationsSequence.Count(c => c.IsSquare) == 4;
                }
                else if (yaku == Tenhou)
                {
                    addYaku = context.IsTenhou();
                }
                else if (yaku == Chiihou)
                {
                    addYaku = context.IsChiihou();
                }
                else if (yaku == KokushiMusou)
                {
                    // Do nothing here, but prevents the exception below.
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (addYaku)
                {
                    yakus.Add(yaku);
                }
            }

            // Remove yakumans with existant upgrade (it's an overkill as the only known case is "Shousuushii" vs. "Daisuushii").
            yakus.RemoveAll(y => y.Upgrades.Any(yu => yakus.Contains(yu)));

            // Only return yakumans if any.
            if (yakus.Count >= 1)
            {
                return yakus;
            }

            foreach (var yaku in Yakus.Where(y => !y.IsYakuman))
            {
                var addYaku = false;
                var occurences = 1;
                if (yaku == Chiniisou)
                {
                    addYaku = combinationsSequence.Select(c => c.Family).Distinct().Count() == 1
                        && !combinationsSequence.Any(c => c.IsHonor);
                }
                else if (yaku == Haitei)
                {
                    addYaku = context.IsRoundLastTile;
                }
                else if (yaku == RinshanKaihou)
                {
                    addYaku = context.DrawType == DrawTypePivot.Compensation;
                }
                else if (yaku == Chankan)
                {
                    addYaku = context.DrawType == DrawTypePivot.OpponentKanCallOpen;
                }
                else if (yaku == Tanyao)
                {
                    addYaku = combinationsSequence.All(c => !c.HasTerminalOrHonor);
                }
                else if (yaku == Yakuhai)
                {
                    occurences = combinationsSequence.Count(c =>
                        c.IsBrelanOrSquare && (
                            c.Family == FamilyPivot.Dragon || (
                                c.Family == FamilyPivot.Wind && (
                                    c.Tiles.First().Wind == context.DominantWind || c.Tiles.First().Wind == context.PlayerWind
                                )
                            )
                        )
                    );
                    addYaku = occurences > 0;
                }
                else if (yaku == Riichi)
                {
                    addYaku = context.IsRiichi;
                }
                else if (yaku == Ippatsu)
                {
                    addYaku = context.IsIppatsu;
                }
                else if (yaku == MenzenTsumo)
                {
                    addYaku = context.DrawType.IsSelfDraw() && combinationsSequence.All(c => c.IsConcealed);
                }
                else if (yaku == Honiisou)
                {
                    addYaku = combinationsSequence.Where(c => !c.IsHonor).Select(c => c.Family).Distinct().Count() == 1;
                }
                else if (yaku == Pinfu)
                {
                    addYaku = combinationsSequence.Count(c => c.IsSequence && c.IsConcealed) == 4
                        && !HandPivot.HandWithValuablePair(combinationsSequence, context.DominantWind, context.PlayerWind)
                        && combinationsSequence.Any(c => c.IsSequence && c.Tiles.Contains(context.LatestTile)
                            && !context.LatestTile.TileIsEdgeWait(c) && !context.LatestTile.TileIsMiddleWait(c));
                }
                else if (yaku == Iipeikou)
                {
                    var sequencesCount = combinationsSequence.Count(c => c.IsSequence);
                    addYaku = combinationsSequence.All(c => c.IsConcealed) && sequencesCount >= 2
                        && combinationsSequence.Where(c => c.IsSequence).Distinct().Count() < sequencesCount;
                }
                else if (yaku == Shousangen)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Dragon) == 2
                        && combinationsSequence.Any(c => c.IsPair && c.Family == FamilyPivot.Dragon);
                }
                else if (yaku == Honroutou)
                {
                    addYaku = combinationsSequence.All(c => c.IsTerminal || c.IsHonor);
                }
                else if (yaku == Chiitoitsu)
                {
                    addYaku = combinationsSequence.All(c => c.IsPair);
                }
                else if (yaku == Sankantsu)
                {
                    addYaku = combinationsSequence.Count(c => c.IsSquare) == 3;
                }
                else if (yaku == SanshokuDoukou)
                {
                    addYaku = combinationsSequence
                                .Where(c => c.IsBrelanOrSquare && !c.IsHonor)
                                .GroupBy(c => c.Tiles.First().Number)
                                .FirstOrDefault(b => b.Count() >= 3)?
                                .Select(b => b.Family)?
                                .Distinct()?
                                .Count() == 3;
                }
                else if (yaku == Sanankou)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.IsConcealed && (!c.Tiles.Contains(context.LatestTile) || context.DrawType.IsSelfDraw())) == 3;
                }
                else if (yaku == Toitoi)
                {
                    addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare) == 4;
                }
                else if (yaku == Ittsu)
                {
                    var ittsuFamilyCombos =
                        combinationsSequence
                            .Where(c => c.IsSequence)
                            .GroupBy(c => c.Family)
                            .FirstOrDefault(b => b.Count() >= 3)?
                            .ToList();

                    addYaku = ittsuFamilyCombos != null
                        && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 1)
                        && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 4)
                        && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 7);
                }
                else if (yaku == SanshokuDoujun)
                {
                    addYaku = combinationsSequence
                                .Where(c => c.IsSequence)
                                .GroupBy(c => c.SequenceFirstNumber)
                                .FirstOrDefault(b => b.Count() >= 3)?
                                .Select(b => b.Family)?
                                .Distinct()?
                                .Count() == 3;
                }
                else if (yaku == Chanta)
                {
                    addYaku = combinationsSequence.All(c => c.HasTerminalOrHonor);
                }
                else if (yaku == DaburuRiichi)
                {
                    addYaku = context.IsRiichi && context.IsFirstTurnRiichi;
                }
                else if (yaku == Ryanpeikou)
                {
                    addYaku = combinationsSequence.All(c => c.IsConcealed)
                        && combinationsSequence.Count(c => c.IsSequence) == 4
                        && combinationsSequence.Where(c => c.IsSequence).Distinct().Count() <= 2;
                }
                else if (yaku == Junchan)
                {
                    addYaku = combinationsSequence.All(c => c.HasTerminal);
                }
                else if (yaku == Renhou)
                {
                    addYaku = context.IsRenhou();
                }
                else if (yaku == NagashiMangan)
                {
                    // Do nothing here, but prevents the exception below.
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (addYaku)
                {
                    for (var i = 0; i < occurences; i++)
                    {
                        yakus.Add(yaku);
                    }
                }
            }

            // Remove yakus with existant upgrade.
            // It works because Upgrades is not recursive.
            yakus.RemoveAll(y => y.Upgrades.Any(yu => yakus.Contains(yu)));

            // On a concealed chanka, only Kokushi is allowed.
            if (context.DrawType == DrawTypePivot.OpponentKanCallConcealed && !yakus.Contains(KokushiMusou))
            {
                yakus.Clear();
            }

            return yakus;
        }

        #endregion Static methods
    }
}
