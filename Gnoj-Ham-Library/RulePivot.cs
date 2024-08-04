using System;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library
{
    /// <summary>
    /// Represents a set of rules.
    /// </summary>
    public class RulePivot : IEquatable<RulePivot>
    {
        #region Static properties

        // do not use directly
        private static RulePivot _default = null;

        /// <summary>
        /// Default rules.
        /// </summary>
        public static RulePivot Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new RulePivot(InitialPointsRules.K25, EndOfGameRules.EnchousenAndTobi, true, true, false, false);
                }

                return _default;
            }
        }

        #endregion Static properties

        #region Embedded properties

        /// <summary>
        /// Initial points rule.
        /// </summary>
        public InitialPointsRules InitialPointsRule { get; }

        /// <summary>
        /// End of game rule.
        /// </summary>
        public EndOfGameRules EndOfGameRule { get; }

        /// <summary>
        /// Use of red doras.
        /// </summary>
        public bool UseRedDoras { get; }

        /// <summary>
        /// Use of <see cref="YakuPivot.NagashiMangan"/>.
        /// </summary>
        public bool UseNagashiMangan { get; }

        /// <summary>
        /// Use debug mode.
        /// </summary>
        public bool DebugMode { get; }

        /// <summary>
        /// Use discard tip.
        /// </summary>
        public bool DiscardTip { get; }

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initialPointsRule"><see cref="InitialPointsRule"/>.</param>
        /// <param name="endOfGameRule"><see cref="EndOfGameRule"/>.</param>
        /// <param name="useRedDoras"><see cref="UseRedDoras"/>.</param>
        /// <param name="useNagashiMangan"><see cref="UseNagashiMangan"/>.</param>
        /// <param name="debugMode"><see cref="DebugMode"/>.</param>
        /// <param name="discardTip"><see cref="DiscardTip"/>.</param>
        public RulePivot(InitialPointsRules initialPointsRule, EndOfGameRules endOfGameRule, bool useRedDoras, bool useNagashiMangan, bool debugMode, bool discardTip)
        {
            InitialPointsRule = initialPointsRule;
            EndOfGameRule = endOfGameRule;
            UseRedDoras = useRedDoras;
            UseNagashiMangan = useNagashiMangan;
            DebugMode = debugMode;
            DiscardTip = discardTip;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Checks if the instance is default ruleset.
        /// </summary>
        /// <returns><c>True</c> if default ruleset.</returns>
        internal bool AreDefaultRules() => Equals(Default);

        #endregion Public methods

        #region IEquatable implementation

        /// <inheritdoc />
        public bool Equals(RulePivot other)
        {
            return other != null
                && other.UseNagashiMangan == UseNagashiMangan
                && other.InitialPointsRule == InitialPointsRule
                && other.EndOfGameRule == EndOfGameRule
                && other.UseRedDoras == UseRedDoras
                && other.DebugMode == DebugMode
                && other.DiscardTip == DiscardTip;
        }

        #endregion IEquatable implementation
    }
}
