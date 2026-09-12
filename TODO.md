# TODO

Points notés en cours de route, pas traités tout de suite.

## Moteur (Gnoj-Ham-Library)

- [ ] `PlayerSavePivot` : sauvegarde JSON non chiffrée (`// TODO decrypt` déjà dans le code).
- [ ] `PlayerSavePivot` : la classe elle-même est qualifiée d'"abomination" dans le code (mélange sérialisation/stats/état) — `PlayerSavePivot.cs:5`.
- [ ] `RulePivot.cs:5` : séparer les règles de la config (debug mode, tips...) — jamais fait.
- [ ] `RoundPivot.cs:1272` : l'ordre de vérification des Ron CPU peut marginalement influencer la décision.
- [ ] `RoundPivot.cs:310` : timing de la notification Kan (affichage dora) signalé comme potentiellement pas parfait, pas creusé.
- [ ] `ScoreTools.ComputeUma` : une seule règle d'uma gérée, pas de configuration alternative.
- [ ] `BasicCpuManagerPivot._itsuFamily` : verrouillé par `PonDecisionInternal`/`KanDecisionInternal`/`ChiiDecisionInternal` mais totalement ignoré par `DiscardDecisionInternal`, qui peut donc défausser les tuiles de la famille honitsu engagée pendant que les autres familles restent bloquées pour les calls — main potentiellement sabotée par ses deux propres logiques. Correction volontairement reportée (casserait les scores exacts des golden-seed tests d'`AutoPlay_Tests.cs`).
- [ ] `BasicCpuManagerPivot.ChiiDecisionInternal` (boucle sans `break`) : quand plusieurs séquences de chii sont valables, retient arbitrairement la dernière testée plutôt que la meilleure.

## Interface WPF (Gnoj-Ham-View)

- [ ] `BackgroundWorker` (MainWindow + AutoPlayWindow) daté, à migrer vers `async`/`await` + `Task` — chantier à part, risque de régression, pas un fix ponctuel.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
- [ ] Options de l'onglet "Règles" (IntroWindow) non persistées entre lancements, contrairement à l'onglet "Options".
- [ ] Le marqueur Akadora disparaît de la main lors d'une décision de riichi sur la tuile "non-akadora" (mais identique).
- [ ] Les overlays s'empilent de façon bizarre.
- [ ] Toutes les conséquences d'un Kan ne sont probablement pas gérées (ex : deux Kans à la suite).
- [ ] `MainWindow.CancelCallProcess` (ligne 354) : nettoyage du highlight qualifié de "lazy" par l'auteur.
- [ ] `MainWindow.xaml.cs:435` : TODO non documenté dans `RunWorkerCompleted`, intention pas claire.
- [ ] `MainWindow.xaml.cs:813` : contenu de tooltip à extraire en ressource plutôt qu'en dur.
- [ ] `MainWindow.SetWallsLength` (ligne 1035) : constante `WallTileSizeRate` dépendante de la taille du conteneur définie côté vue, couplage fragile.
- [ ] `MainWindow.SetWallsLength` (ligne 1049) : les murs sont entièrement reconstruits à chaque fois au lieu d'une mise à jour incrémentale.
- [ ] `IntroWindow` : `Height` fixe calée à la main sur l'onglet le plus grand (`Règles` actuellement) — jamais trouvé comment faire suivre dynamiquement la fenêtre à la taille de l'onglet sélectionné en WPF ; à resynchroniser manuellement à chaque fois qu'un onglet grossit.
