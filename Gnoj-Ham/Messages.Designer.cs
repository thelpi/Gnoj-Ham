﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Gnoj_Ham {
    using System;
    
    
    /// <summary>
    ///   Une classe de ressource fortement typée destinée, entre autres, à la consultation des chaînes localisées.
    /// </summary>
    // Cette classe a été générée automatiquement par la classe StronglyTypedResourceBuilder
    // à l'aide d'un outil, tel que ResGen ou Visual Studio.
    // Pour ajouter ou supprimer un membre, modifiez votre fichier .ResX, puis réexécutez ResGen
    // avec l'option /str ou régénérez votre projet VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Retourne l'instance ResourceManager mise en cache utilisée par cette classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Gnoj_Ham.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Remplace la propriété CurrentUICulture du thread actuel pour toutes
        ///   les recherches de ressources à l'aide de cette classe de ressource fortement typée.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The tile can&apos;t be discarded in this context..
        /// </summary>
        internal static string ImpossibleDiscard {
            get {
                return ResourceManager.GetString("ImpossibleDiscard", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Stealing is impossible in this context..
        /// </summary>
        internal static string ImpossibleStealingArgument {
            get {
                return ResourceManager.GetString("ImpossibleStealingArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à This call is invalid..
        /// </summary>
        internal static string InvalidCall {
            get {
                return ResourceManager.GetString("InvalidCall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The combination of tiles is invalid..
        /// </summary>
        internal static string InvalidCombination {
            get {
                return ResourceManager.GetString("InvalidCombination", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à Ippatsu impossible if not riichi..
        /// </summary>
        internal static string InvalidContextIppatsuValue {
            get {
                return ResourceManager.GetString("InvalidContextIppatsuValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à A draw can&apos;t be made in this context..
        /// </summary>
        internal static string InvalidDraw {
            get {
                return ResourceManager.GetString("InvalidDraw", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The total number of tiles is invalid (should be 14)..
        /// </summary>
        internal static string InvalidHandTilesCount {
            get {
                return ResourceManager.GetString("InvalidHandTilesCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The kan call can&apos;t be done with the specified tile..
        /// </summary>
        internal static string InvalidKanTileChoice {
            get {
                return ResourceManager.GetString("InvalidKanTileChoice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The specified latest tile should be include in the current hand..
        /// </summary>
        internal static string InvalidLatestTileContext {
            get {
                return ResourceManager.GetString("InvalidLatestTileContext", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The name of the human player is invalid..
        /// </summary>
        internal static string InvalidPlayerName {
            get {
                return ResourceManager.GetString("InvalidPlayerName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The number of tiles is invalid (should be between 2 and 4, or 1 and 3 with an open tile)..
        /// </summary>
        internal static string InvalidTilesCount {
            get {
                return ResourceManager.GetString("InvalidTilesCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Recherche une chaîne localisée semblable à The wind which the tile has been stolen is not specified..
        /// </summary>
        internal static string StolenFromNotSpecified {
            get {
                return ResourceManager.GetString("StolenFromNotSpecified", resourceCulture);
            }
        }
    }
}
