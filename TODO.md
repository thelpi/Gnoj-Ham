# TODO

Points notés en cours de route, pas traités tout de suite.

## Moteur (Gnoj-Ham-Library)

- [ ] `BasicCpuManagerPivot` : aucune anticipation (décisions purement locales/heuristiques) alors que le moteur est un simulateur parfait déjà disponible — une recherche Monte Carlo/expectimax à quelques coups d'avance pourrait renforcer le CPU sans aller jusqu'au RL/deep learning complet. Tenté sur `feature/monte-carlo-cpu` (rollouts + déterminisation), abandonné : trop lent, gains peu concluants sur petit échantillon.
- [ ] `BasicCpuManagerPivot` : le riichi est systématique dès l'éligibilité, sans réévaluation (damaten si yaku déjà garanti + attente faible) — plus discutable qu'un simple ajustement de bord, à valider prudemment (pas juste par winrate agrégé).

## Interface WPF (Gnoj-Ham-View)

- [ ] MVVM (branche `feat/mvvm`) : reste à ranger en dossiers les fichiers des projets `Gnoj-Ham-ViewModel` et `Gnoj-Ham-View` (presque tout est à la racine).
- [ ] `AutoPlayWindow` : le panneau « Games in progress... » et sa barre de progression sont visibles dès l'ouverture, avant tout Start — reproduit tel quel par `AutoPlayViewModel.IsWaitingPanelVisible`, à masquer à l'état `Idle`.
- [ ] `MainWindow` : revoir le temps d'affichage des annonces (pon, chi, kan, riichi, ron, tsumo).
- [ ] `ScoreWindow.xaml` : le séparateur sous la main d'un gagnant est une `Line` avec `Fill` mais sans `Stroke`, donc invisible depuis toujours — lui donner un `Stroke`, ou le retirer.
