# TODO

Points notés en cours de route, pas traités tout de suite.

## Règles

- [ ] Yakuman "doubles" non modélisés (Suuankou tanki, Kokushi 13 tuiles, Chuuren Poutou pur) : tout yakuman vaut 13 fans fixe dans `YakuPivot.cs`.
- [ ] `RoundPivot.IsTenpai` : edge case si les 4 exemplaires de la tuile d'attente sont déjà en main (règle incertaine, pas implémentée) — `RoundPivot.cs:896`.

## Moteur (Gnoj-Ham-Library)

- [ ] `RoundPivot` : classe trop grosse, plusieurs responsabilités mélangées (pas de `partial class`, à voir autrement). `HandPivot` traité (combinatoire extraite vers `TileCombinatoricsPivot`).
- [ ] Pas de tests sur les yaku dépendants du contexte (pas d'`Example` statique) : Riichi, Ippatsu, Tenhou, Chiihou, Renhou, Haitei, Rinshan Kaihou, Chankan, Menzen Tsumo, Daburu Riichi, Sanankou, Sankantsu, Suukantsu. Les 26 autres yaku sont couverts (`YakuPivot_Tests.cs`).
- [ ] `PlayerSavePivot` : sauvegarde JSON non chiffrée (`// TODO decrypt` déjà dans le code).
- [ ] `PlayerSavePivot` : la classe elle-même est qualifiée d'"abomination" dans le code (mélange sérialisation/stats/état) — `PlayerSavePivot.cs:5`.
- [ ] `RoundPivot.cs:262` : `Game.HumanPlayerIndex!.Value` repose sur une discipline d'appel (humain uniquement) non garantie par le typage.
- [ ] `RulePivot.cs:5` : séparer les règles de la config (debug mode, tips...) — jamais fait.
- [ ] `RoundPivot.cs:832` : que faire si 3 riichis sont déjà déclarés dans la manche ?
- [ ] `RoundPivot.cs:1272` : l'ordre de vérification des Ron CPU peut marginalement influencer la décision.
- [ ] `RoundPivot.HumanAutoPlay` (ligne 1427) : recalcule `RiichiDecision()` sans réutiliser `riichiTiles` déjà calculé juste au-dessus — même redondance que le fix perf de session, correctif trivial maintenant que `RiichiDecision(riichiTiles)` existe.
- [ ] `RoundPivot.cs:310` : timing de la notification Kan (affichage dora) signalé comme potentiellement pas parfait, pas creusé.
- [ ] `ScoreTools.ComputeUma` : une seule règle d'uma gérée, pas de configuration alternative.
- [ ] `GamePivot.FirstEastIndex` (ligne 71) : implémentation qualifiée de "gross" par l'auteur, à relire.

## Interface WPF (Gnoj-Ham-View)

- [ ] `BackgroundWorker` (MainWindow + AutoPlayWindow) daté, à migrer vers `async`/`await` + `Task` — chantier à part, risque de régression, pas un fix ponctuel.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
- [ ] `MainWindow.xaml.cs:649` : `CallKan(...)!` peut renvoyer `null`, pas revérifié juste avant le `!`.
- [ ] Options de l'onglet "Règles" (IntroWindow) non persistées entre lancements, contrairement à l'onglet "Options".
- [ ] Le marqueur Akadora disparaît de la main lors d'une décision de riichi sur la tuile "non-akadora" (mais identique).
- [ ] Les overlays s'empilent de façon bizarre.
- [ ] Toutes les conséquences d'un Kan ne sont probablement pas gérées (ex : deux Kans à la suite).
- [ ] `MainWindow.CancelCallProcess` (ligne 354) : nettoyage du highlight qualifié de "lazy" par l'auteur.
- [ ] `MainWindow.xaml.cs:435` : TODO non documenté dans `RunWorkerCompleted`, intention pas claire.
- [ ] `MainWindow.xaml.cs:813` : contenu de tooltip à extraire en ressource plutôt qu'en dur.
- [ ] `MainWindow.SetWallsLength` (ligne 1035) : constante `WallTileSizeRate` dépendante de la taille du conteneur définie côté vue, couplage fragile.
- [ ] `MainWindow.SetWallsLength` (ligne 1049) : les murs sont entièrement reconstruits à chaque fois au lieu d'une mise à jour incrémentale.
