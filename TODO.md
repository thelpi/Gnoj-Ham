# TODO

Points notés en cours de route, pas traités tout de suite.

## Moteur (Gnoj-Ham-Library)

- [ ] `BasicCpuManagerPivot` : aucune anticipation (décisions purement locales/heuristiques) alors que le moteur est un simulateur parfait déjà disponible — une recherche Monte Carlo/expectimax à quelques coups d'avance pourrait renforcer le CPU sans aller jusqu'au RL/deep learning complet.

## Interface WPF (Gnoj-Ham-View)

- [ ] `BackgroundWorker` (MainWindow + AutoPlayWindow) daté, à migrer vers `async`/`await` + `Task` — chantier à part, risque de régression, pas un fix ponctuel.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
- [ ] Le marqueur Akadora disparaît de la main lors d'une décision de riichi sur la tuile "non-akadora" (mais identique).
- [ ] Les overlays s'empilent de façon bizarre.
- [ ] Toutes les conséquences d'un Kan ne sont probablement pas gérées (ex : deux Kans à la suite).
- [ ] `MainWindow.CancelCallProcess` (ligne 354) : nettoyage du highlight qualifié de "lazy" par l'auteur.
- [ ] `MainWindow.xaml.cs:435` : TODO non documenté dans `RunWorkerCompleted`, intention pas claire.
- [ ] `MainWindow.xaml.cs:813` : contenu de tooltip à extraire en ressource plutôt qu'en dur.
- [ ] `MainWindow.SetWallsLength` (ligne 1035) : constante `WallTileSizeRate` dépendante de la taille du conteneur définie côté vue, couplage fragile.
- [ ] `MainWindow.SetWallsLength` (ligne 1049) : les murs sont entièrement reconstruits à chaque fois au lieu d'une mise à jour incrémentale.
- [ ] `IntroWindow` : `Height` fixe calée à la main sur l'onglet le plus grand (`Règles` actuellement) — jamais trouvé comment faire suivre dynamiquement la fenêtre à la taille de l'onglet sélectionné en WPF ; à resynchroniser manuellement à chaque fois qu'un onglet grossit.
- [ ] Ajouter une mention sur l'interface indiquant que les statistiques du joueur ne sont sauvegardées que si le ruleset par défaut est utilisé (`GamePivot.AreDefaultRules`).
- [ ] Ajouter un bouton de RAZ des règles (onglet "Règles") à leur valeur par défaut (`RulePivot.Default`) — le bouton "Réinitialiser" actuel ne recharge que la dernière config sauvegardée, plus les vraies valeurs par défaut depuis leur persistance.
